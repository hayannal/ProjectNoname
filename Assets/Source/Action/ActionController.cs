using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActionController : MonoBehaviour {

	public Animator animator { get; private set; }
	public IdleAnimator idleAnimator { get; private set; }
	public MecanimState mecanimState { get; private set; }
	public Actor actor { get; private set; }

	public SkillProcessor skillProcessor { get; private set; }
	public CooltimeProcessor cooltimeProcessor { get; private set; }

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
		//public string castingId;
	}

	List<ActionInfo> _listActionInfo;
	void Awake()
	{
		animator = GetComponentInChildren<Animator>();

		mecanimState = animator.GetComponent<MecanimState>();
		if (mecanimState == null) mecanimState = animator.gameObject.AddComponent<MecanimState>();

		idleAnimator = animator.GetComponent<IdleAnimator>();
		if (idleAnimator == null) idleAnimator = animator.gameObject.AddComponent<IdleAnimator>();

		actor = GetComponent<Actor>();
	}

	string[] _defaultActionNameList = { "Idle", "Move", "Die", "Attack" };
	public void InitializeActionPlayInfo(string actorId)
	{
		skillProcessor = GetComponent<SkillProcessor>();
		//if (skillProcessor == null) skillProcessor = gameObject.AddComponent<SkillProcessor>();	// no addcomponent. for monster actor

		cooltimeProcessor = GetComponent<CooltimeProcessor>();
		if (cooltimeProcessor == null) cooltimeProcessor = gameObject.AddComponent<CooltimeProcessor>();

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
			
			//if (!string.IsNullOrEmpty(actionTableRow._CastingID))
			//{
			//	Google2u.CastingTableRow castingTableRow = Google2u.CastingTable.Instance.GetRow(actionTableRow._CastingID);
			//	if (castingTableRow != null)
			//		info.castingID = actionTableRow._CastingID;
			//}

			_listActionInfo.Add(CreateActionInfo(actionTableData));
		}

		for (int i = 0; i < _defaultActionNameList.Length; ++i)
		{
			ActionInfo actionInfo = GetActionInfoByName(_defaultActionNameList[i]);
			if (actionInfo != null)
				continue;

			if (TableDataManager.instance.FindActorTableData(actorId) != null)
			{
				ActionTableData actionTableData = TableDataManager.instance.FindActionTableData("DefaultPlayer", _defaultActionNameList[i]);
				if (actionTableData != null)
					_listActionInfo.Add(CreateActionInfo(actionTableData));
			}
			else if (TableDataManager.instance.FindMonsterTableData(actorId) != null)
			{
				ActionTableData actionTableData = TableDataManager.instance.FindActionTableData("DefaultMonster", _defaultActionNameList[i]);
				if (actionTableData != null)
					_listActionInfo.Add(CreateActionInfo(actionTableData));
			}
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

	ActionInfo CreateActionInfo(ActionTableData actionTableData)
	{
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
		return info;
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

	public bool waitAttackSignal { get; set; }
	bool PlayAction(ActionInfo actionPlayInfo)
	{
		if (!CheckMecanimState(actionPlayInfo.listNotAllowingState, actionPlayInfo.listAllowingState))
			return false;

		bool normalAttack = false;
		if (actionPlayInfo.actionName == "Attack")
		{
			normalAttack = true;
			if (cooltimeProcessor.CheckCooltime(actionPlayInfo.actionName))
				return false;
		}

		#region Attack Cancel
		// 공격 액션 실행 후 어택 시그널 오지도 않은채 액션을 바꿨다면 캔슬로 여기고 쿨타임을 초기화해준다.
		bool cancelAttack = false;
		if (waitAttackSignal && (actionPlayInfo.actionName == "Move" || actionPlayInfo.actionName == "Idle"))
		{
			Cooltime cooltime = cooltimeProcessor.GetCooltime("Attack");
			if (cooltime != null && cooltime.CheckCooltime())
			{
				waitAttackSignal = false;
				cooltime.cooltime = 0.0f;
				cancelAttack = true;
			}
		}
		#endregion

		#region Fast Attack Delay
		// 공격 딜레이가 매우 많이 줄어들면 공격 애니메이션이 끝나기도 전에 다시 공격이 나가야한다.
		// 공격 시그널이 발동 된 후에 처리되는거라 쿨 초기화는 하지 않고 crossFade만 예외처리 해준다.
		bool ignoreCrossFade = false;
		if (cancelAttack == false && actionPlayInfo.actionName == "Attack")
		{
			ActionInfo currentActionInfo = GetCurrentActionInfo();
			if (currentActionInfo != null && currentActionInfo.actionName == "Attack")
				ignoreCrossFade = true;
		}
		#endregion

		SkillProcessor.SkillInfo selectedSkillInfo = null;
		int actionNameHash = actionPlayInfo.actionNameHash;
		if (!string.IsNullOrEmpty(actionPlayInfo.skillId) && skillProcessor != null)
		{
			selectedSkillInfo = skillProcessor.GetSkillInfo(actionPlayInfo.skillId);
			if (selectedSkillInfo != null)
			{
				if (cooltimeProcessor.CheckCooltime(selectedSkillInfo.skillId))
					return false;
				if (selectedSkillInfo.actionNameHash != 0)
					actionNameHash = selectedSkillInfo.actionNameHash;
			}
		}

		// Play Action
		if (actionPlayInfo.fadeDuration > 0.0f)
		{	
			if (animator.GetNextAnimatorStateInfo(0).fullPathHash == actionNameHash)
				return false;
		}
		if (ignoreCrossFade)
			animator.CrossFade(actionNameHash, 0.01f, 0, 0.0f);
		else
			// 어택 캔슬할땐 빠르게 블렌딩 되어야 시그널 호출이 문제없이 호출되게 된다.
			animator.CrossFade(actionNameHash, cancelAttack ? 0.02f : actionPlayInfo.fadeDuration);

		if (actionPlayInfo.actionName == "Ultimate")
		{
			actor.actorStatus.AddSP(-actor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxSp));

			#region Ultimate Force Set
			// 간혹가다 궁극기를 눌렀는데 일반어택이 씹어버리고 덮는 경우가 발생했다.
			// 생각해보니 현재 액션 시스템 특성상 다음 프레임에 애니메이션이 적용되면서 시그널이 들어가기 때문에
			// 궁극기 버튼 누르는 같은 프레임에 AI가 돌면서 일반 어택을 실행하면 궁극기 액션 걸어둔걸 덮고 실행될 수 있는 구조였다.
			// 유저 손으로 할땐 이런 문제가 없지만 AI는 그렇지 않아서 발생하는건데..
			//
			// 해결책으로 다음과 같은 방법을 쓰기로 한다.
			// 궁극기 실행 타임에 아래 코드로 얼티메이트 상태를 임시로 넣어둔다.
			// 다음 프레임에 궁극기 액션이 시작됨과 동시에 이 임시로 넣어둔건 빠지게될거고(로직상 액션 변경시 fullPathHash가 0인건 삭제되게 되어있다.)
			// 궁극기 상태는 레인지시그널 설정해둔 만큼만 잘 작동하게 될거다.
			// 이렇게 처리해두면 같은 프레임에 AI가 돌아도 일반공격이 덮어쓰지 못할테니 더이상 버그가 발생하지 않게될거다.
			actor.actionController.mecanimState.StartState((int)MecanimStateDefine.eMecanimState.Ultimate, 0);
			#endregion
		}

		if (normalAttack && actor != null)
		{
			cooltimeProcessor.ApplyCooltime(actionPlayInfo.actionName, actor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.AttackDelay));
			waitAttackSignal = true;
		}
		if (selectedSkillInfo != null)
			cooltimeProcessor.ApplyCooltime(selectedSkillInfo.skillId, selectedSkillInfo.cooltime);

		#region HitSignal Index
		if (_dicHitSignalIndexInfo.ContainsKey(actionNameHash))
			_dicHitSignalIndexInfo[actionNameHash] = 0;
		else
			_dicHitSignalIndexInfo.Add(actionNameHash, 0);
		#endregion

		#region DetectPlayAction
		if (_needDetectPlayAction)
			detectedPlayAction = true;
		#endregion
		return true;
	}

	#region DetectPlayAction
	// 지금까지 이런 처리가 한번도 필요하지 않았는데, 어태치하는 이펙트가 생기고 나서 필요하게 되었다.
	// 현재 구조상 움직이거나 해서 임의의 액션을 실행할때
	// 먼저 transform값이 변경(포지션 혹은 로테이션)되면서 애니를 CrossFade 시켜놓고
	// 다음프레임에 애니메이션이 적용되면서 시그널이 돌아가게 된다.
	// 이러다보니 어태치된 레이 이펙트가 한 프레임 회전되는 캐릭터를 따라가고 나서 그 다음 프레임부터 사라지게 된다.
	// 이게 별로라서 고치는 방법으로 임의의 액션을 실행할때(다음 프레임에 처리되는 CrossFade를 기다리지 않도록) 
	// 직접 PlayAction을 호출해서 액션을 변경하려는지를 감지하기로 했다.
	// 참고로 평소에는 할필요가 없으니 필요할때만 체크하게 한다.
	bool _needDetectPlayAction = false;
	public void EnableDetectPlayAction(bool enable)
	{
		_needDetectPlayAction = enable;
		detectedPlayAction = false;
	}
	public bool detectedPlayAction { get; private set; }
	#endregion

	public ActionInfo GetActionInfoByControllerType(Control.eControllerType eControllerType)
	{
		for (int i = 0; i < _listActionInfo.Count; ++i)
		{
			if (_listActionInfo[i].eControllerType == eControllerType)
				return _listActionInfo[i];
		}
		return null;
	}

	public ActionInfo GetActionInfoByName(string actionName)
	{
		for (int i = 0; i < _listActionInfo.Count; ++i)
		{
			if (_listActionInfo[i].actionName == actionName)
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

	#region Current Action
	// for caching
	int _lastCurrentStateFullPathHash;
	ActionInfo _lastCurrentActionInfo;
	public ActionInfo GetCurrentActionInfo()
	{
		AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (_lastCurrentStateFullPathHash == animatorStateInfo.fullPathHash && _lastCurrentActionInfo != null)
			return _lastCurrentActionInfo;

		for (int i = 0; i < _listActionInfo.Count; ++i)
		{
			if (_listActionInfo[i].actionNameHash == animatorStateInfo.fullPathHash)
			{
				_lastCurrentStateFullPathHash = animatorStateInfo.fullPathHash;
				_lastCurrentActionInfo = _listActionInfo[i];
				return _listActionInfo[i];
			}
		}
		return null;
	}

	public int GetCurrentSkillLevelByCurrentAction()
	{
		int skillLevel = 0;
		ActionInfo currentActionInfo = GetCurrentActionInfo();
		if (currentActionInfo != null)
		{
			if (!string.IsNullOrEmpty(currentActionInfo.skillId) && skillProcessor != null)
			{
				SkillProcessor.SkillInfo skillInfo = skillProcessor.GetSkillInfo(currentActionInfo.skillId);
				if (skillInfo != null)
					skillLevel = skillInfo.skillLevel;
			}
		}
		return skillLevel;
	}
	#endregion

	#region HitSignal Index
	Dictionary<int, int> _dicHitSignalIndexInfo = new Dictionary<int, int>();
	public int OnHitObjectSignal(int fullPathHash)
	{
		int hitSignalIndexInAction = 0;
		if (_dicHitSignalIndexInfo.ContainsKey(fullPathHash))
		{
			int lastValue = _dicHitSignalIndexInfo[fullPathHash];
			hitSignalIndexInAction = lastValue;
			lastValue += 1;
			_dicHitSignalIndexInfo[fullPathHash] = lastValue;
		}
		return hitSignalIndexInAction;
	}
	#endregion

	#region Attack Ani Speed Ratio
	int _attackAniSpeedRatioHash = 0;
	public void OnChangedAttackSpeedAddRatio(float attackSpeedAddRatio)
	{
		if (_attackAniSpeedRatioHash == 0)
			_attackAniSpeedRatioHash = BattleInstanceManager.instance.GetActionNameHash("AttackAniSpeedRatio");

		float attackAniSpeedRatio = 1.0f + attackSpeedAddRatio * 0.3333f;
		animator.SetFloat(_attackAniSpeedRatioHash, attackAniSpeedRatio);
	}
	#endregion

	#region DummyFinder
	DummyFinder _dummyFinder;
	public DummyFinder dummyFinder
	{
		get
		{
			if (_dummyFinder != null)
				return _dummyFinder;

			if (_dummyFinder == null)
				_dummyFinder = animator.GetComponent<DummyFinder>();
			if (_dummyFinder == null)
				_dummyFinder = animator.gameObject.AddComponent<DummyFinder>();
			return _dummyFinder;
		}
	}
	#endregion

	Transform _animatorTransform;
	public Transform cachedAnimatorTransform
	{
		get
		{
			if (_animatorTransform == null)
				_animatorTransform = animator.transform;
			return _animatorTransform;
		}
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
