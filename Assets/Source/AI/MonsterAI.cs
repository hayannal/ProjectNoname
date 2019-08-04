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
		if (_startDelayRemainTime > 0.0f)
		{
			_startDelayRemainTime -= Time.deltaTime;
			if (_startDelayRemainTime <= 0.0f)
				_startDelayRemainTime = 0.0f;
			return;
		}

		UpdateTargeting();

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
		while (true)
		{
			Vector2 randomCircle = Random.insideUnitCircle.normalized;
			Vector3 randomOffset = new Vector3(randomCircle.x * desireDistance, 0.0f, randomCircle.y * desireDistance);
			randomPosition = actor.cachedTransform.position + randomOffset;

			NavMeshHit hit;
			if (NavMesh.SamplePosition(randomPosition, out hit, 1.0f, NavMesh.AllAreas))
			{
				result = hit.position;
				break;
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
