using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class MecanimState : MonoBehaviour {

	Animator m_Animator;

	struct StateInfo
	{
		public int fullPathHash;
		public int stateID;
	}
	List<StateInfo> m_listStateInfo = new List<StateInfo>();

	void OnEnable()
	{
		m_listStateInfo.Clear();
	}

	void Start()
	{
		m_Animator = GetComponent<Animator>();
	}

	StateInfo _stateInfo = new StateInfo();
	public void StartState(int stateID, int fullPathHash)
	{
		for (int i = 0; i < m_listStateInfo.Count; ++i)
		{
			if (m_listStateInfo[i].stateID == stateID && m_listStateInfo[i].fullPathHash == fullPathHash)
				return;
		}
		_stateInfo.stateID = stateID;
		_stateInfo.fullPathHash = fullPathHash;
		m_listStateInfo.Add(_stateInfo);
	}

	public void EndState(int stateID, int fullPathHash)
	{
		for (int i = 0; i < m_listStateInfo.Count; ++i)
		{
			if (m_listStateInfo[i].stateID == stateID && m_listStateInfo[i].fullPathHash == fullPathHash)
			{
				m_listStateInfo.RemoveAt(i);
				return;
			}
		}
	}

	public bool IsState(int stateID)
	{
		for (int i = 0; i < m_listStateInfo.Count; ++i)
		{
			if (m_listStateInfo[i].stateID == stateID)
				return true;
		}
		return false;
	}

	int _lastFullPathHash = 0;
	void LateUpdate()
	{
		int fullPathHash = 0;
		if (m_Animator.IsInTransition(0))
		{
			AnimatorStateInfo nextInfo = m_Animator.GetNextAnimatorStateInfo(0);
			fullPathHash = nextInfo.fullPathHash;
		}
		else
		{
			AnimatorStateInfo info = m_Animator.GetCurrentAnimatorStateInfo(0);
			fullPathHash = info.fullPathHash;
		}
		if (fullPathHash != 0 && fullPathHash != _lastFullPathHash)
		{
			for (int i = m_listStateInfo.Count-1; i >= 0; --i)
			{
				if (m_listStateInfo[i].fullPathHash != fullPathHash)
					m_listStateInfo.RemoveAt(i);
			}
			_lastFullPathHash = fullPathHash;
		}
	}


	#region debugging
	public bool showState = false;

	public Rect startRect = new Rect( 10, 10, 175, 50 );
	public  float frequency = 0.5F; // The update frequency of the fps
	public int nbDecimal = 1; // How many decimal do you want to display
	private GUIStyle style; // The style the text will be displayed at, based en defaultSkin.label.


	void OnGUI()
	{
		if (!showState)
			return;

		if (style == null)
		{
			style = new GUIStyle(GUI.skin.label);
			style.normal.textColor = Color.white;
			style.alignment = TextAnchor.MiddleCenter;
		}
		startRect = GUI.Window(0, startRect, DoMyWindow, "");
	}

	void DoMyWindow(int windowID)
	{
		GUI.Label(new Rect(0, 0, startRect.width, startRect.height), GetStateString(), style);
		GUI.DragWindow(new Rect(0, 0, Screen.width, Screen.height));
	}

	StringBuilder _stringBuilder = new StringBuilder();
	string GetStateString()
	{
		_stringBuilder.Remove(0, _stringBuilder.Length);
		for (int i = 0; i < m_listStateInfo.Count; ++i)
		{
			_stringBuilder.AppendFormat("{0}", m_listStateInfo[i].stateID);
			if (i < m_listStateInfo.Count - 1)
				_stringBuilder.AppendFormat(", ");
		}
		return _stringBuilder.ToString();
	}
	#endregion
}
