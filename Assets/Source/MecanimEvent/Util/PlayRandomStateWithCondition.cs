﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 원래 초기에는 StateMachine에다가 화살표 그어가면서 condition으로 분기타려고 했었는데
// 한눈에 액션 리스트가 보여야 관리가 편한점, State와 선이 많아질수록 관리가 불편하단 점때문에
// PlayRandomState를 확장한 PlayRandomWithCondition 형태로 가기로 한다.
// 게다가 이 형태로 가면 좋은 점이 또 하나 있는데
// PlayRandomState에는 액션을 결정한다는 이유로 Motion을 넣지 않는데 제대로 탈출하지 않으면 보스를 멍때리게하는 버그로 이어져버린다.
// (유니티 특성이긴 한데 Motion이 없으면 1초짜리 루프로 돌게 된다.)
// StateMachine 분기가 많아질수록 이런 빈 State들은 더 많아질테고 그럼 관리이슈도 늘어날테니
// 이렇게 한 스크립트 내에서 처리해보도록 한다.
//
// 게다가 예전에 만들었던 구조인
// 액션을 먼저 선택하고 그 이후 해당 액션의 체이스 거리만큼 체이스로 가는건 약간 문제가 있는데..
// 먼저 액션을 고르고 그에 맞는 거리를 맞추려고 하다보면 설정해둔 어택 딜레이가 지났는데도 액션을 발동시키지 못하고 거리를 맞추기 위해 이동하는 상황이 올 수 있다.
// 이게 보통의 자동액션 rpg였으면 괜찮을지 몰라도 액션게임에서는 난이도가 들쭉날쭉해져서 별로인거 같다.
// 이럴바엔 딜레이마다 무조건 액션 하나는 나가는데 현재 상황에 적합한 액션이 선택되는게 더 나아보였다.
//
// 이렇게 이 스크립트 하나에서 랜덤 및 조건 검사를 한번에 하려다보니
// MonsterAI에서 하려고 했던 Animator Parameter는 아무리봐도 쓰기 불편한거 같단 결론을 내렸다.
// 액션을 선택할때만 필요한 hpRatio나 distance인데
// 이걸 계속 계산하는거 자체가 비효율적이니 제거하기로 한다. - MonsterAI 에서 Animator Parameter 부분 삭제.
public class PlayRandomStateWithCondition : ControlStateBase
{
	[Serializable]
	public class RandomStateWithConditionInfo : PlayRandomState.RandomStateInfo
	{
		public bool useDistance1;
		[ConditionalHide("useDistance1", true)]
		public Condition.eCompareType distanceCompareType1;
		[ConditionalHide("useDistance1", true)]
		public float distanceParameter1;
		public bool useDistance2;
		[ConditionalHide("useDistance2", true)]
		public Condition.eCompareType distanceCompareType2;
		[ConditionalHide("useDistance2", true)]
		public float distanceParameter2;
		public bool useHpRatio1;
		[ConditionalHide("useHpRatio1", true)]
		public Condition.eCompareType hpRatioCompareType1;
		[ConditionalHide("useHpRatio1", true)]
		public float hpRatioParameter1;
		public bool useHpRatio2;
		[ConditionalHide("useHpRatio2", true)]
		public Condition.eCompareType hpRatioCompareType2;
		[ConditionalHide("useHpRatio2", true)]
		public float hpRatioParameter2;
		public bool useActorState;
		[ConditionalHide("useActorState", true)]
		public string actorStateId;
		[ConditionalHide("useActorState", true)]
		public bool existActorStateParameter;
		public bool useTargetActorState;
		[ConditionalHide("useTargetActorState", true)]
		public string targetActorStateId;
		[ConditionalHide("useTargetActorState", true)]
		public bool existTargetActorStateParameter;
		public bool useActorAffectorType;
		[ConditionalHide("useActorAffectorType", true)]
		public eAffectorType actorAffectorType;
		[ConditionalHide("useActorAffectorType", true)]
		public bool existActorAffectorTypeParameter;
		public bool useMonsterCount;
		[ConditionalHide("useMonsterCount", true)]
		public Condition.eCompareType monsterCountCompareType;
		[ConditionalHide("useMonsterCount", true)]
		public int monsterCountParameter;
		[ConditionalHide("useMonsterCount", true)]
		public bool onlySummonMonsterCount;
		public bool useCheckWall;
		[ConditionalHide("useCheckWall", true)]
		public bool existWallParameter;
		public bool useActorCollider;
		[ConditionalHide("useActorCollider", true)]
		public bool enabledActorColliderParameter;
		public int actionCountLimit;
		public float actionCooltime;
		public bool applyCooltimeOnStart;
	}
	public RandomStateWithConditionInfo[] randomStateWithConditionInfoList;

	void OnDisable()
	{
		//Debug.Log("PlayRandomStateWithCondition OnDisable");
		if (_dicActionCount != null)
			_dicActionCount.Clear();
		_enabled = false;
	}

	List<RandomStateWithConditionInfo> _listRandomState;
	Actor _actor = null;
	int _lastState = 0;
	bool _enabled = false;
	// OnStateEnter is called before OnStateEnter is called on any state inside this state machine
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (randomStateWithConditionInfoList == null || randomStateWithConditionInfoList.Length == 0)
			return;

		if (animator.GetNextAnimatorStateInfo(0).fullPathHash == _lastState)
			return;

		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		if (_actor == null)
			return;

		if (_actor.actorStatus.IsDie())
		{
			_actor.actionController.PlayActionByActionName("Die");
			return;
		}

		if (_listRandomState == null)
			_listRandomState = new List<RandomStateWithConditionInfo>();
		_listRandomState.Clear();

		// 진짜 OnEnable함수에서는 animator에 접근할 수 없다보니 actor를 찾을 수 없다.
		// 그래서 액터가 생성되는 타이밍에 적용할 수는 없지만 최초 공격을 하려고 Enter되는 순간에 applyCooltimeOnStart을 처리하기로 한다.
		// 어느정도 비슷한 효과는 나올거다.
		if (_enabled == false)
		{
			_enabled = true;
			for (int i = 0; i < randomStateWithConditionInfoList.Length; ++i)
			{
				if (randomStateWithConditionInfoList[i].actionCooltime > 0.0f && randomStateWithConditionInfoList[i].applyCooltimeOnStart)
					_actor.actionController.cooltimeProcessor.ApplyCooltime(randomStateWithConditionInfoList[i].stateName, randomStateWithConditionInfoList[i].actionCooltime);
			}
		}

		float sumWeight = 0.0f;
		for (int i = 0; i < randomStateWithConditionInfoList.Length; ++i)
		{
			if (randomStateWithConditionInfoList[i].useDistance1 || randomStateWithConditionInfoList[i].useDistance2)
			{
				if (_actor.targetingProcessor.GetTargetCount() > 0)
				{
					Vector3 diff = _actor.targetingProcessor.GetTargetPosition() - _actor.cachedTransform.position;
					diff.y = 0.0f;
					float parameterSqrMagnitude1 = randomStateWithConditionInfoList[i].distanceParameter1 * randomStateWithConditionInfoList[i].distanceParameter1;
					if (randomStateWithConditionInfoList[i].useDistance1 && Condition.CompareValue(randomStateWithConditionInfoList[i].distanceCompareType1, diff.sqrMagnitude, parameterSqrMagnitude1) == false)
						continue;
					float parameterSqrMagnitude2 = randomStateWithConditionInfoList[i].distanceParameter2 * randomStateWithConditionInfoList[i].distanceParameter2;
					if (randomStateWithConditionInfoList[i].useDistance2 && Condition.CompareValue(randomStateWithConditionInfoList[i].distanceCompareType2, diff.sqrMagnitude, parameterSqrMagnitude2) == false)
						continue;
				}
				else
				{
					continue;
				}
			}

			if (randomStateWithConditionInfoList[i].useHpRatio1 || randomStateWithConditionInfoList[i].useHpRatio2)
			{
				float hpRatio = _actor.actorStatus.GetHPRatio();
				if (randomStateWithConditionInfoList[i].useHpRatio1 && Condition.CompareValue(randomStateWithConditionInfoList[i].hpRatioCompareType1, hpRatio, randomStateWithConditionInfoList[i].hpRatioParameter1) == false)
					continue;
				if (randomStateWithConditionInfoList[i].useHpRatio2 && Condition.CompareValue(randomStateWithConditionInfoList[i].hpRatioCompareType2, hpRatio, randomStateWithConditionInfoList[i].hpRatioParameter2) == false)
					continue;
			}

			if (randomStateWithConditionInfoList[i].useActorState)
			{
				if (_actor.affectorProcessor.IsActorState(randomStateWithConditionInfoList[i].actorStateId) != randomStateWithConditionInfoList[i].existActorStateParameter)
					continue;
			}

			if (randomStateWithConditionInfoList[i].useTargetActorState)
			{
				if (_actor.targetingProcessor.GetTargetCount() > 0)
				{
					Collider targetCollider = _actor.targetingProcessor.GetTarget();
					AffectorProcessor targetAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
					if (targetAffectorProcessor == null)
						continue;
					if (targetAffectorProcessor.IsActorState(randomStateWithConditionInfoList[i].targetActorStateId) != randomStateWithConditionInfoList[i].existTargetActorStateParameter)
						continue;
				}
				else
				{
					continue;
				}
			}

			if (randomStateWithConditionInfoList[i].useActorAffectorType)
			{
				if (_actor.affectorProcessor.IsContinuousAffectorType(randomStateWithConditionInfoList[i].actorAffectorType) != randomStateWithConditionInfoList[i].existActorAffectorTypeParameter)
					continue;
			}

			if (randomStateWithConditionInfoList[i].useMonsterCount)
			{
				int monsterCount = 0;
				if (randomStateWithConditionInfoList[i].onlySummonMonsterCount)
				{
					List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
					for (int j = 0; j < listMonsterActor.Count; ++j)
					{
						if (listMonsterActor[j].actorStatus.IsDie())
							continue;
						if (listMonsterActor[j].team.teamId != (int)Team.eTeamID.DefaultMonster || listMonsterActor[j].excludeMonsterCount)
							continue;
						if (listMonsterActor[j].summonMonster == false)
							continue;
						++monsterCount;
					}
				}
				else
					monsterCount = BattleManager.instance.GetSpawnedMonsterCount();

				if (Condition.CompareValue(randomStateWithConditionInfoList[i].monsterCountCompareType, monsterCount, randomStateWithConditionInfoList[i].monsterCountParameter) == false)
					continue;
			}

			if (randomStateWithConditionInfoList[i].useCheckWall)
			{
				if (_actor.targetingProcessor.GetTarget() == null)
					continue;

				Collider targetCollider = _actor.targetingProcessor.GetTarget();
				bool wallResult = TargetingProcessor.CheckWall(_actor.cachedTransform.position, BattleInstanceManager.instance.GetTransformFromCollider(targetCollider).position, 0.1f);

				if (randomStateWithConditionInfoList[i].existWallParameter && wallResult == false)
					continue;
				if (randomStateWithConditionInfoList[i].existWallParameter == false && wallResult)
					continue;
			}

			if (randomStateWithConditionInfoList[i].useActorCollider)
			{
				if (_actor.GetCollider() == null)
					continue;

				if (randomStateWithConditionInfoList[i].enabledActorColliderParameter != _actor.GetCollider().enabled)
					continue;
			}

			if (randomStateWithConditionInfoList[i].actionCountLimit > 0 && GetActionCount(randomStateWithConditionInfoList[i].stateName) >= randomStateWithConditionInfoList[i].actionCountLimit)
				continue;

			if (randomStateWithConditionInfoList[i].actionCooltime > 0.0f && _actor.actionController.cooltimeProcessor.CheckCooltime(randomStateWithConditionInfoList[i].stateName))
				continue;

			sumWeight += randomStateWithConditionInfoList[i].weight;
			randomStateWithConditionInfoList[i].sumWeight = sumWeight;
			_listRandomState.Add(randomStateWithConditionInfoList[i]);
		}

		if (sumWeight == 0.0f)
		{
			Debug.LogError("PlayRandomState Error : sumWeight is zero.");

			// sumWeight가 0이면 아무것도 실행이 안되서 AI가 멈추게 된다. 강제로 넣어두자.
			sumWeight = randomStateWithConditionInfoList[0].weight;
			_listRandomState.Add(randomStateWithConditionInfoList[0]);
		}

		float random = UnityEngine.Random.Range(0.0f, sumWeight);
		string selectedStateName = "";
		for (int i = 0; i < _listRandomState.Count; ++i)
		{
			if (random <= _listRandomState[i].sumWeight)
			{
				selectedStateName = _listRandomState[i].stateName;
				if (_listRandomState[i].actionCountLimit > 0)
					AddActionCount(_listRandomState[i].stateName);
				if (_listRandomState[i].actionCooltime > 0.0f)
					_actor.actionController.cooltimeProcessor.ApplyCooltime(_listRandomState[i].stateName, _listRandomState[i].actionCooltime);
				break;
			}
		}
		if (string.IsNullOrEmpty(selectedStateName) == false)
		{
			_lastState = BattleInstanceManager.instance.GetActionNameHash(selectedStateName);
			animator.CrossFade(_lastState, 0.05f);
		}
		_listRandomState.Clear();
	}

	Dictionary<string, int> _dicActionCount;
	void AddActionCount(string stateName)
	{
		if (_dicActionCount == null)
			_dicActionCount = new Dictionary<string, int>();
		if (_dicActionCount.ContainsKey(stateName))
			_dicActionCount[stateName] += 1;
		else
			_dicActionCount.Add(stateName, 1);
	}

	int GetActionCount(string stateName)
	{
		if (_dicActionCount == null)
			return 0;
		if (_dicActionCount.ContainsKey(stateName))
			return _dicActionCount[stateName];
		return 0;
	}

	// OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateExit is called before OnStateExit is called on any state inside this state machine
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		//if (_lastState == stateInfo.fullPathHash && stateInfo.normalizedTime != 0.0f)
		//	_lastState = 0;

		// State Machine에 붙여놨더니 안에 들어있는 State들에 전부 적용되서 안에 들어있는 State들 호출될때마다
		// PlayRandomState Enter가 호출되는거였다.
		// 그래서 State Machine에 붙이는 구조를 버리고
		// 외부에 따로 State를 둔 후(Motion은 아예 할당하지 않음) 여기에 PlayRandomState를 붙이기로 한다.
		// 이 State는 호출되자마자 랜덤하게 바꾸는 역할만 한다.(애니가 실행되진 않는다.)
		_lastState = 0;
	}

	// OnStateMove is called before OnStateMove is called on any state inside this state machine
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateIK is called before OnStateIK is called on any state inside this state machine
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateMachineEnter is called when entering a state machine via its Entry Node
	//override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
	//{
	//    
	//}

	// OnStateMachineExit is called when exiting a state machine via its Exit Node
	//override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
	//{
	//    
	//}
}