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


#if UNITY_EDITOR
	// OnGUI 함수가 실제 빌드로 들어가게 되면
	// 인풋이 발생할때마다 이쪽에 이벤트를 날리게 되고
	// 해당 OnGUI 스크립트를 돌리고 있는 개수만큼 GUIUtility.BeginGUI 함수가 콜되면서 꽤 많은 부하를 만들어낸다.
	// 저 함수 뿐만 아니라 UIEvents.IMGUIRenderOverlays 도 호출되면서 부하가 심해진다.
	// 몬스터 50마리에 거의 8ms 는 늘어나는 느낌이었다. 테스트폰 아임백.
	//
	// 이번 경우에는 하필 이게 몬스터마다 붙어있었고,
	// 몹이 많아진채로 터치를 하면 프레임 저하가 심해서 물리 문제라 생각했는데..
	// 결국 이 OnGUI의 문제도 원인제공자 중에 하나란걸 알게 되서
	// 폰에서 상태값이 필요할때만 UNITY_EDITOR 디파인 풀고 사용하도록 한다. 평소에는 에디터에서만 보면 될거다.

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
#endif
}
