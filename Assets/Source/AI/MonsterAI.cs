using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MecanimStateDefine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
	const float TargetFindDelay = 0.1f;

	Actor targetActor;
	float targetRadius;

	Actor actor { get; set; }
	float actorRadius;
	TargetingProcessor targetingProcessor { get; set; }
	PathFinderController pathFinderController { get; set; }

	public enum eStateType
	{
		RandomMove,
		CustomAction,
		Chase,
		AttackAction,
		AttackDelay,

		TypeAmount,
	}
	eStateType _currentState;

	public float startDelay;
	public eStateType startState = eStateType.RandomMove;
	public bool[] useStateList = new bool[(int)eStateType.TypeAmount];

	bool _started = false;
	void Start()
	{
		actor = GetComponent<Actor>();
		actorRadius = ColliderUtil.GetRadius(GetComponent<Collider>());
		targetingProcessor = GetComponent<TargetingProcessor>();
		pathFinderController = GetComponent<PathFinderController>();

		InitializeAI();
		_started = true;
	}

	#region ObjectPool
	void OnEnable()
	{
		if (_started)
			InitializeAI();
	}

	void OnDisable()
	{
		targetActor = null;
		targetRadius = 0.0f;
	}
	#endregion

	void InitializeAI()
	{
		_startDelayRemainTime = startDelay;
		_currentState = startState;

		// exception handling
		if (useStateList[(int)_currentState] == false)
			_currentState = eStateType.TypeAmount;

		ResetRandomMoveStateInfo();
		ResetCustomActionStateInfo();
		ResetChaseStateInfo();
		ResetAttackActionStateInfo();
		ResetAttackDelayStateInfo();
	}

	float _startDelayRemainTime;
	// Update is called once per frame
	void Update()
    {
		UpdateTargeting();
	}

	// 다른 클래스들의 Update에서 PlayAction 한게 있어도 덮어야하므로 LateUpdate에서 처리한다.
	// 대표적으로 PathFinderController의 Animate 함수.
	void LateUpdate()
	{
		if (_startDelayRemainTime > 0.0f)
		{
			_startDelayRemainTime -= Time.deltaTime;
			if (_startDelayRemainTime <= 0.0f)
				_startDelayRemainTime = 0.0f;
			return;
		}

		switch (_currentState)
		{
			case eStateType.RandomMove:
				UpdateRandomMove();
				break;
			case eStateType.CustomAction:
				UpdateCustomAction();
				break;
			case eStateType.Chase:
				UpdateChase();
				break;
			case eStateType.AttackAction:
				UpdateAttack();
				break;
			case eStateType.AttackDelay:
				UpdateAttackDelay();
				break;
		}
	}

	float _currentFindDelay;
	void UpdateTargeting()
	{
		if (targetingProcessor == null)
			return;

		if (targetActor != null)
		{
			if (targetActor.actorStatus.IsDie())
			{
				if (BattleInstanceManager.instance.targetOfMonster == targetActor)
					BattleInstanceManager.instance.targetOfMonster = null;
				_currentFindDelay = 0.0f;
				targetActor = null;
			}
		}
		if (targetActor != null)
			return;

		_currentFindDelay -= Time.deltaTime;
		if (_currentFindDelay <= 0.0f)
		{
			_currentFindDelay += TargetFindDelay;

			if (BattleInstanceManager.instance.targetOfMonster == null)
			{
				if (targetingProcessor.FindNearestTarget(Team.eTeamCheckFilter.Enemy, PlayerAI.FindTargetRange))
				{
					Collider targetCollider = targetingProcessor.GetTarget();
					targetRadius = ColliderUtil.GetRadius(targetCollider);
					AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
					BattleInstanceManager.instance.targetOfMonster = affectorProcessor.actor;
					targetActor = affectorProcessor.actor;
				}
				else
					targetActor = null;
			}
			else
			{
				targetActor = BattleInstanceManager.instance.targetOfMonster;
			}
		}
	}

	void NextStep()
	{
		eStateType currentState = _currentState;
		int nextValue = (int)currentState;
		for (int i = 0; i < (int)eStateType.TypeAmount; ++i)
		{
			nextValue += 1;
			eStateType nextState = (eStateType)nextValue;
			if (nextState == eStateType.TypeAmount)
				nextValue = 0;
			if (useStateList[nextValue])
			{
				_currentState = (eStateType)nextValue;
				break;
			}
		}
	}

	#region RandomMove
	public float moveTime;
	public float refreshTickTime;
	public float desireDistance = 5.0f;
	float _moveRemainTime = 0.0f;
	float _moveRefreshRemainTime = 0.0f;
	void UpdateRandomMove()
	{
		if (_moveRemainTime == 0.0f)
		{
			_moveRemainTime = moveTime;
			_moveRefreshRemainTime = refreshTickTime;
			MoveRandomPosition();
		}

		if (_moveRemainTime > 0.0f)
		{
			_moveRemainTime -= Time.deltaTime;
			_moveRefreshRemainTime -= Time.deltaTime;
			if (_moveRemainTime <= 0.0f)
			{
				if (pathFinderController.agent.hasPath)
					pathFinderController.agent.ResetPath();
				ResetRandomMoveStateInfo();
				NextStep();
				return;
			}
			if (_moveRefreshRemainTime <= 0.0f)
			{
				_moveRefreshRemainTime += refreshTickTime;
				MoveRandomPosition();
			}
		}
	}

	void MoveRandomPosition()
	{
		Vector3 randomPosition = Vector3.zero;
		Vector3 result = Vector3.zero;
		float maxDistance = 1.0f;
		int tryCount = 0;
		while (true)
		{
			Vector2 randomCircle = Random.insideUnitCircle.normalized;
			Vector3 randomOffset = new Vector3(randomCircle.x * desireDistance, 0.0f, randomCircle.y * desireDistance);
			randomPosition = actor.cachedTransform.position + randomOffset;

			// 겹쳐서 생성될 경우 y가 높게 올라가서 무한루프에 빠지게 된다. 강제로 0으로 만들어준다.
			// 사실 엄청 고생하다가 찾은건데
			// 첨엔 NavMeshSurface가 안구워진 상태에서 길찾기를 호출할 경우 유니티가 멈춰버리는줄 알았는데 (절대 유니티가 그럴일이 없었다..)
			// 사실은 맵과 몹의 호출 순서로 인해 안구워진 상태에서 이렇게 while 돌면서 SamplePosition 하니 while문을 못빠져나갔던 것이었다.
			// 설상가상으로 몹이 겹쳐진채로 스폰되면 자리가 겹쳐있기 때문에 어느 하나가 다른 몹 위로 올라가게 되는데
			// 이때 y값이 땅보다 2나 높은 상태에서 SamplePosition을 호출하게 된거고
			// 당연히 실패하면서 while문을 못빠져나갔던 것 두 이슈가 동시에 터지니 정말로 NavMeshSurface의 문제인줄 알았던 것이다.
			// 그러나 내 코드가 문제였다..
			//
			// 암튼 땅의 위치가 0이 아닐 경우도 문제가 되긴 하니 10번이상 실패할 경우 distance값을 증가시키는 코드가 더 안전할거 같다.
			randomPosition.y = 0.0f;

			NavMeshHit hit;
			if (NavMesh.SamplePosition(randomPosition, out hit, maxDistance, NavMesh.AllAreas))
			{
				result = hit.position;
				break;
			}

			// exception handling
			++tryCount;
			if (tryCount > 20)
			{
				tryCount = 0;
				maxDistance += 1.0f;
			}
		}

		pathFinderController.agent.destination = result;
	}

	void ResetRandomMoveStateInfo()
	{
		_moveRemainTime = 0.0f;
		_moveRefreshRemainTime = 0.0f;
	}
	#endregion

	#region CustomAction
	public bool useTableCustomActionName = false;
	public string customActionName;
	public float customActionFadeDuration = 0.05f;
	bool _customActionPlayed = false;
	void UpdateCustomAction()
	{
		if (_customActionPlayed)
		{
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Idle))
			{
				ResetCustomActionStateInfo();
				NextStep();
			}
			return;
		}

		if (useTableCustomActionName)
		{
			if (actor.actionController.PlayActionByActionName(customActionName))
			{
				_customActionPlayed = true;
			}
		}
		else
		{
			actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(customActionName), customActionFadeDuration);
			_customActionPlayed = true;
		}
	}

	void ResetCustomActionStateInfo()
	{
		_customActionPlayed = false;
	}
	#endregion

	#region Chase
	public float chaseDistance;
	Vector3 _lastGoalPosition = Vector3.up;
	void UpdateChase(bool callByAttackAction = false)
	{
		if (targetActor == null)
			return;

		Vector3 diff = actor.cachedTransform.position - targetActor.cachedTransform.position;
		float sqrDiff = diff.sqrMagnitude;
		float sqrRadius = (targetRadius + actorRadius) * (targetRadius + actorRadius) + 0.01f + (chaseDistance * chaseDistance);
		if (sqrDiff <= sqrRadius)
		{
			if (callByAttackAction == false)
			{
				if (pathFinderController.agent.hasPath)
					pathFinderController.agent.ResetPath();
				ResetChaseStateInfo();
				NextStep();
			}
			return;
		}

		if (_lastGoalPosition != targetActor.cachedTransform.position)
		{
			pathFinderController.agent.destination = targetActor.cachedTransform.position;
			_lastGoalPosition = targetActor.cachedTransform.position;
		}
	}

	void ResetChaseStateInfo()
	{
		_lastGoalPosition = Vector3.up;
	}
	#endregion

	#region AttackAction
	public bool useTableAttackActionName = true;
	public string attackActionName;
	public float attackActionFadeDuration = 0.05f;
	public bool lookAtTargetBeforeAttack = true;
	bool _attackPlayed = false;
	void UpdateAttack()
	{
		if (targetActor == null)
			return;

		if (useStateList[(int)eStateType.Chase] && actor.actionController.mecanimState.IsState((int)eMecanimState.Idle))
		{
			bool enable = false;
			// 후딜이 없을때 혹은 후딜이 있더라도 어택을 실행하기 전에만 처리해야
			// 어택하고 후딜 있는데 후딜 안하고 넘어가는 경우가 생기지 않는다.
			if (useStateList[(int)eStateType.AttackDelay] == false) enable = true;
			if (useStateList[(int)eStateType.AttackDelay] && _attackPlayed == false) enable = true;
			if (enable)
			{
				UpdateChase(true);
				if (pathFinderController.agent.hasPath)
				{
					_currentState = eStateType.Chase;
					return;
				}
			}
		}

		if (_attackPlayed)
		{
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Idle))
			{
				ResetAttackActionStateInfo();
				NextStep();
			}
			return;
		}

		if (useTableAttackActionName)
		{
			if (actor.actionController.PlayActionByActionName(attackActionName))
			{
				if (lookAtTargetBeforeAttack)
					pathFinderController.movement.rotation = Quaternion.LookRotation(targetActor.cachedTransform.position - actor.cachedTransform.position);
				_attackPlayed = true;
			}
		}
		else
		{
			actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(attackActionName), attackActionFadeDuration);
			if (lookAtTargetBeforeAttack)
				pathFinderController.movement.rotation = Quaternion.LookRotation(targetActor.cachedTransform.position - actor.cachedTransform.position);
			_attackPlayed = true;
		}
	}

	void ResetAttackActionStateInfo()
	{
		_attackPlayed = false;
	}
	#endregion

	#region AttackDelay
	public float attackDelayTime = 0.0f;
	float _attackDelayRemainTime;
	void UpdateAttackDelay()
	{
		if (_attackDelayRemainTime == 0.0f)
		{
			_attackDelayRemainTime = attackDelayTime;
		}

		if (_attackDelayRemainTime > 0.0f)
		{
			_attackDelayRemainTime -= Time.deltaTime;
			if (_attackDelayRemainTime <= 0.0f)
			{
				ResetAttackDelayStateInfo();
				NextStep();
				return;
			}
		}
	}

	void ResetAttackDelayStateInfo()
	{
		_attackDelayRemainTime = 0.0f;
	}
	#endregion
}
