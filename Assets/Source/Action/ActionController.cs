using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActionController : MonoBehaviour {

	public Animator animator { get; private set; }
	public IdleAnimator idleAnimator { get; private set; }
	public MecanimState mecanimState { get; private set; }
	public SkillProcessor skillProcessor { get; private set; }

	public class ActionInfo
	{
		public string actionName;
		public List<int> listAllowingState;
		public List<int> listNotAllowingState;
		public int actionNameHash;
		public Control.eControllerType eControllerType;
		public Control.eInputType eInputType;
		public float fadeDuration;
		public string skillId;	// only Id. Find skillinfo when use skill.
		public string castingId;
	}

	List<ActionInfo> _listActionInfo;
	void Awake()
	{
		animator = GetComponentInChildren<Animator>();

		mecanimState = animator.GetComponent<MecanimState>();
		if (mecanimState == null) mecanimState = animator.gameObject.AddComponent<MecanimState>();

		idleAnimator = animator.GetComponent<IdleAnimator>();
		if (idleAnimator == null) idleAnimator = animator.gameObject.AddComponent<IdleAnimator>();
	}

	public void InitializeActionPlayInfo(string actorId)
	{
		skillProcessor = GetComponent<SkillProcessor>();
		//if (skillProcessor == null) skillProcessor = gameObject.AddComponent<SkillProcessor>();	// no addcomponent. for monster actor

		_listActionInfo = new List<ActionInfo>();

		for (int i = 0; i < TableDataManager.instance.actionTable.dataArray.Length; ++i)
		{
			ActionTableData actionTableData = TableDataManager.instance.actionTable.dataArray[i];
			if (actionTableData.actorId != actorId) continue;

			// not needed
			//if (!string.IsNullOrEmpty(actionTableData.skillId))
			//{
			//	if (actor.CheckSkillLearn(actionTableRow._SkillID) == false)
			//		continue;
			//}

			ActionInfo info = new ActionInfo();
			info.actionName = actionTableData.actionName;
			StringUtil.SplitIntList(actionTableData.listAllowingState, ref info.listAllowingState);
			StringUtil.SplitIntList(actionTableData.listNotAllowingState, ref info.listNotAllowingState);
			info.actionNameHash = Animator.StringToHash(actionTableData.mecanimName);
			info.fadeDuration = actionTableData.fadeDuration;
			info.skillId = actionTableData.skillId;

			if (!string.IsNullOrEmpty(actionTableData.controlId))
			{
				ControlTableData controlTableData = TableDataManager.instance.FindControlTableData(actionTableData.controlId);
				if (controlTableData != null)
				{
					info.eControllerType = (Control.eControllerType)controlTableData.controlType;
					info.eInputType = (Control.eInputType)controlTableData.inputType;
				}
			}

			/*
			if (!string.IsNullOrEmpty(actionTableRow._CastingID))
			{
				Google2u.CastingTableRow castingTableRow = Google2u.CastingTable.Instance.GetRow(actionTableRow._CastingID);
				if (castingTableRow != null)
					info.castingID = actionTableRow._CastingID;
			}
			*/

			_listActionInfo.Add(info);
		}

		/*
		ActionPlayInfo info = new ActionPlayInfo();
		info.actionID = "action01";
		StringUtil.SplitIntList("", ref info.listAllowingState);
		StringUtil.SplitIntList("3,4", ref info.listNotAllowingState);
		info.actionNameHash = Animator.StringToHash("Base Layer.Attack.attack1");
		info.eInputType = ControllerUI.eTouchInput.Tab;
		info.fadeDuration = 0.5f;
		m_listActionPlayInfo.Add(info);

		info = new ActionPlayInfo();
		info.actionID = "Attack2";
		StringUtil.SplitIntList("", ref info.listAllowingState);
		StringUtil.SplitIntList("", ref info.listNotAllowingState);
		info.actionNameHash = Animator.StringToHash("Base Layer.Attack.attack2");
		info.eInputType = ControllerUI.eTouchInput.None;
		info.fadeDuration = 0.25f;
		m_listActionPlayInfo.Add(info);

		info = new ActionPlayInfo();
		info.actionID = "action02";
		StringUtil.SplitIntList("", ref info.listAllowingState);
		StringUtil.SplitIntList("3,4", ref info.listNotAllowingState);
		info.actionNameHash = Animator.StringToHash("Base Layer.Run");
		info.eInputType = ControllerUI.eTouchInput.Drag;
		info.fadeDuration = 0.25f;
		m_listActionPlayInfo.Add(info);

		ActionInfo info = new ActionInfo();
		info.actionName = "Idle";
		StringUtil.SplitIntList("", ref info.listAllowingState);
		StringUtil.SplitIntList("", ref info.listNotAllowingState);
		info.actionNameHash = Animator.StringToHash("Base Layer.Idle");
		//info.eControllerType = 
		//info.eInputType = Control.eInputType.
		info.fadeDuration = 0.2f;
		_listActionInfo.Add(info);

		info = new ActionInfo();
		info.actionName = "Move";
		StringUtil.SplitIntList("", ref info.listAllowingState);
		StringUtil.SplitIntList("3, 4", ref info.listNotAllowingState);
		info.actionNameHash = Animator.StringToHash("Base Layer.Move");
		//info.eControllerType = Control.eControllerType.ScreenController;
		//info.eInputType = Control.eInputType.
		info.fadeDuration = 0.1f;
		_listActionInfo.Add(info);

		// after delay Move cancel
		string[] moveActionID = {"Left", "Right", "Front", "Back"};
		for (int i = 0; i < 4; ++i)
		{
			info = new ActionPlayInfo();
			info.actionID = moveActionID[i];
			info.listAllowingState = new List<int>();
			StringUtil.SplitIntList(MecanimStateUtil.GetNotAllowingMoveStateList(), ref info.listNotAllowingState);
			info.actionNameHash = Animator.StringToHash("Base Layer.Walk");
			info.fadeDuration = 0.15f;
			info.AIRangeMin = info.AIRangeMax = 0.0f;
			info.AIPriority = -1;
			m_listActionPlayInfo.Add(info);
		}
		*/
	}

	public bool PlayActionByActionName(string actionID)
	{
		if (mecanimState == null)
			return false;
		
		for (int i = 0; i < _listActionInfo.Count; ++i)
		{
			if (_listActionInfo[i].actionName != actionID)
				continue;

			return PlayAction(_listActionInfo[i]);
		}

		return false;
	}

	public bool PlayActionByControl(Control.eControllerType eControllerType, Control.eInputType eInputType)
	{
		if (mecanimState == null)
			return false;

		for (int i = 0; i < _listActionInfo.Count; ++i)
		{
			if (_listActionInfo[i].eControllerType != eControllerType || _listActionInfo[i].eInputType != eInputType)
				continue;

			if (PlayAction(_listActionInfo[i]))
				return true;
		}

		return false;
	}

	bool PlayAction(ActionInfo actionPlayInfo)
	{
		if (!CheckMecanimState(actionPlayInfo.listNotAllowingState, actionPlayInfo.listAllowingState))
			return false;

		int actionNameHash = actionPlayInfo.actionNameHash;
		Cooltime cooltimeInfo = null;
		if (!string.IsNullOrEmpty(actionPlayInfo.skillId) && skillProcessor != null)
		{
			SkillProcessor.SkillInfo skillInfo = skillProcessor.GetSkillInfo(actionPlayInfo.skillId);
			if (skillInfo != null)
			{
				if (skillInfo.cooltimeInfo != null)
				{
					cooltimeInfo = skillInfo.cooltimeInfo;
					if (cooltimeInfo.CheckCooltime())
						return false;
				}
				if (skillInfo.actionNameHash != 0)
					actionNameHash = skillInfo.actionNameHash;
			}
		}

		// Play Action
		if (actionPlayInfo.fadeDuration > 0.0f)
		{	
			if (animator.GetNextAnimatorStateInfo(0).fullPathHash == actionNameHash)
				return false;
		}
		animator.CrossFade(actionNameHash, actionPlayInfo.fadeDuration);

		if (cooltimeInfo != null)
			cooltimeInfo.ApplyCooltime();

		return true;
	}

	public ActionInfo GetActionInfoByControllerType(Control.eControllerType eControllerType)
	{
		for (int i = 0; i < _listActionInfo.Count; ++i)
		{
			if (_listActionInfo[i].eControllerType == eControllerType)
				return _listActionInfo[i];
		}
		return null;
	}

	bool CheckMecanimState(List<int> listNotAllowingState, List<int> listAllowingState)
	{
		bool bCheckState = true;
		for (int i = 0; i < (int)listNotAllowingState.Count; ++i)
		{
			if (mecanimState.IsState(listNotAllowingState[i]) == true)
			{
				bCheckState = false;
				break;
			}
		}
		if (bCheckState)
		{
			for (int i = 0; i < (int)listAllowingState.Count; ++i)
			{
				if (mecanimState.IsState(listAllowingState[i]) == false)
				{
					bCheckState = false;
					break;
				}
			}
		}
		return bCheckState;
	}

	/*
	void Update()
	{
		if (m_MecanimState == null)
			return;

		for (int i = 0; i < m_listActionPlayInfo.Count; ++i)
		{
			bool bCheckPlay = false;
			if (InputChecker.Check(m_listActionPlayInfo[i].key, m_listActionPlayInfo[i].checkType)) bCheckPlay = true;
			if (m_bFlagPlayAIAction && m_nSelectedAIAction == i) bCheckPlay = true;
			if (!bCheckPlay) continue;

			if (!CheckMecanimState(m_listActionPlayInfo[i].listNotAllowingState, m_listActionPlayInfo[i].listAllowingState))
				continue;

			// Play Action
			if (m_listActionPlayInfo[i].checkType == InputChecker.CheckType.HoldDown && m_listActionPlayInfo[i].fadeDuration > 0.0f)
			{	
				if (m_Animator.GetNextAnimatorStateInfo(0).fullPathHash == m_listActionPlayInfo[i].actionNameHash)
					return;
			}
			m_Animator.CrossFade(m_listActionPlayInfo[i].actionNameHash, m_listActionPlayInfo[i].fadeDuration);
			if (m_bFlagPlayAIAction) m_bFlagPlayAIAction = false;
			return;
		}
	}
	*/

	/*
	///////////////////////////////////////////////////////////////////////////
	/// For AI
	///////////////////////////////////////////////////////////////////////////
	public bool IsSelectedActionForAI() { return (m_nSelectedAIActionIndex != -1); }
	int m_nSelectedAIActionIndex = -1;
	TargetSystem _targetSystem = null;
	public bool SelectActionForAI(bool normalAttack)
	{
		for (int i = 0; i < m_listActionPlayInfo.Count; ++i)
		{
			if (normalAttack && m_listActionPlayInfo[i].AIPriority == 0)
			{
				if (m_listActionPlayInfo[i].coolTimeInfo != null && m_listActionPlayInfo[i].coolTimeInfo.CheckCooltime())
					continue;
				if (!CheckMecanimState(m_listActionPlayInfo[i].listNotAllowingState, m_listActionPlayInfo[i].listAllowingState))
					continue;
				m_nSelectedAIActionIndex = i;
				return true;
			}
			if (!normalAttack && m_listActionPlayInfo[i].AIPriority > 0)
			{
				if (m_listActionPlayInfo[i].coolTimeInfo != null && m_listActionPlayInfo[i].coolTimeInfo.CheckCooltime())
					continue;
				if (!CheckMecanimState(m_listActionPlayInfo[i].listNotAllowingState, m_listActionPlayInfo[i].listAllowingState))
					continue;
				if (m_listActionPlayInfo[i].AIRangeMin > 0.0f)
				{
					if (_targetSystem == null)
						_targetSystem = GetComponent<TargetSystem>();
					if (_targetSystem.GetTargetCount() == 0)
						continue;
					
					Vector3 diff = _targetSystem.GetTargetPosition() - transform.position;
					if (Vector3.SqrMagnitude(diff) < m_listActionPlayInfo[i].AIRangeMin * m_listActionPlayInfo[i].AIRangeMin)
						continue;
				}
				if (Random.value < 0.5f)
					continue;
				m_nSelectedAIActionIndex = i;
				return true;
			}
		}
		return false;
	}

	List<int> _listSelectedAIActionWithRange = new List<int>();
	public bool SelectActionWithRangeForAI()
	{
		if (_targetSystem == null)
			_targetSystem = GetComponent<TargetSystem>();
		if (_targetSystem.GetTargetCount() == 0)
			return false;

		_listSelectedAIActionWithRange.Clear();
		for (int i = 0; i < m_listActionPlayInfo.Count; ++i)
		{
			if (m_listActionPlayInfo[i].coolTimeInfo != null && m_listActionPlayInfo[i].coolTimeInfo.CheckCooltime())
				continue;
			if (!CheckMecanimState(m_listActionPlayInfo[i].listNotAllowingState, m_listActionPlayInfo[i].listAllowingState))
				continue;
			Vector3 diff = _targetSystem.GetTargetPosition() - transform.position;
			if (m_listActionPlayInfo[i].AIRangeMin > 0.0f)
			{
				if (Vector3.SqrMagnitude(diff) < m_listActionPlayInfo[i].AIRangeMin * m_listActionPlayInfo[i].AIRangeMin)
					continue;
			}
			if (Vector3.SqrMagnitude(diff) > m_listActionPlayInfo[i].AIRangeMax * m_listActionPlayInfo[i].AIRangeMax)
				continue;

			_listSelectedAIActionWithRange.Add(i);
		}
		if (_listSelectedAIActionWithRange.Count == 0)
			return false;
		m_nSelectedAIActionIndex = _listSelectedAIActionWithRange[Random.Range(0, _listSelectedAIActionWithRange.Count)];
		return true;
	}

	public void ResetSelectActionForAI() { m_nSelectedAIActionIndex = -1; }

	public void SelectMoveActionForAI()
	{
		m_nSelectedAIActionIndex = m_listActionPlayInfo.Count-1;
	}

	public float GetSelectedActionAIRangeMax()
	{
		if (m_nSelectedAIActionIndex == -1)
			return 0.0f;
		return m_listActionPlayInfo[m_nSelectedAIActionIndex].AIRangeMax;
	}

	public int GetSelectedActionAIPriority()
	{
		if (m_nSelectedAIActionIndex == -1)
			return 0;
		return m_listActionPlayInfo[m_nSelectedAIActionIndex].AIPriority;
	}

	public void PlayActionForAI()
	{
		if (m_nSelectedAIActionIndex == -1)
			return;

		if (!CheckMecanimState(m_listActionPlayInfo[m_nSelectedAIActionIndex].listNotAllowingState, m_listActionPlayInfo[m_nSelectedAIActionIndex].listAllowingState))
		{
			m_nSelectedAIActionIndex = -1;
			return;
		}

		if (m_listActionPlayInfo[m_nSelectedAIActionIndex].fadeDuration > 0.0f)
		{	
			if (m_Animator.GetNextAnimatorStateInfo(0).fullPathHash == m_listActionPlayInfo[m_nSelectedAIActionIndex].actionNameHash)
				return;
		}
		m_Animator.CrossFade(m_listActionPlayInfo[m_nSelectedAIActionIndex].actionNameHash, m_listActionPlayInfo[m_nSelectedAIActionIndex].fadeDuration);

		if (m_listActionPlayInfo[m_nSelectedAIActionIndex].coolTimeInfo != null)
			m_listActionPlayInfo[m_nSelectedAIActionIndex].coolTimeInfo.UseCoolTime();
		
		_nPrevSelectedAIActionIndex = m_nSelectedAIActionIndex;
		m_nSelectedAIActionIndex = -1;
	}

	// for Normal Continuous Atttack
	int _nPrevSelectedAIActionIndex = -1;
	public int GetPrevSelectedAIActionIndex() { return _nPrevSelectedAIActionIndex; }
	*/
}
