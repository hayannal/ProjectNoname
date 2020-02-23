using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MecanimStateDefine;
using UnityEngine.AI;
using SubjectNerd.Utilities;

public class MonsterAI : MonoBehaviour
{
	const float TargetFindDelay = 0.1f;

	public Actor targetActor { get; private set; }
	float targetRadius;

	Actor actor;
	float actorRadius;
	TargetingProcessor targetingProcessor { get; set; }
	PathFinderController pathFinderController { get; set; }

	// startState로 이미 시리얼라이즈 되어있기 때문에 새로운 타입을 추가하려면 맨 아래 추가해야한다.
	public enum eStateType
	{
		RandomMove,
		CustomAction,
		Chase,
		AttackAction,
		AttackDelay,
		StraightMove,

		TypeAmount,
	}
	eStateType _currentState;

	void NextStep()
	{
		eStateType currentState = _currentState;
		int stateValue = (int)currentState;
		for (int i = 0; i < (int)eStateType.TypeAmount; ++i)
		{
			eStateType nextState = ToNextStep((eStateType)stateValue);
			stateValue = (int)nextState;
			if (useStateList[stateValue])
			{
				_currentState = nextState;
				break;
			}
		}
	}

	eStateType ToNextStep(eStateType stateType)
	{
		switch (stateType)
		{
			case eStateType.RandomMove: return eStateType.StraightMove;
			case eStateType.StraightMove: return eStateType.CustomAction;
			case eStateType.AttackDelay: return eStateType.RandomMove;
		}
		return (eStateType)(stateType + 1);
	}

	public Vector2 startDelayRange;
	public eStateType startState = eStateType.RandomMove;
	public bool[] useStateList = new bool[(int)eStateType.TypeAmount];

	private void OnValidate()
	{
		if (useStateList != null && useStateList.Length != (int)eStateType.TypeAmount)
			System.Array.Resize<bool>(ref useStateList, (int)eStateType.TypeAmount);
	}

	void Awake()
	{
		actor = GetComponent<Actor>();
		targetingProcessor = GetComponent<TargetingProcessor>();
		pathFinderController = GetComponent<PathFinderController>();
	}

	void Start()
	{
		actorRadius = ColliderUtil.GetRadius(GetComponent<Collider>());
	}

	#region ObjectPool
	void OnDisable()
	{
		targetActor = null;
		targetRadius = 0.0f;

		ResetRandomMoveStateInfo();
		ResetStraightMoveStateInfo();
		ResetCustomActionStateInfo();
		ResetChaseStateInfo();
		ResetAttackActionStateInfo();
		ResetAttackDelayStateInfo();
	}
	#endregion

	// 같은 프리팹에 MonsterActor와 MonsterAI가 붙어있는데
	// MosnterAI의 Start와 Update가 호출되고나서 MonsterActor의 Start가 호출되는 경우도 발생하길래
	// 아예 순서를 MonsterActor가 제어하도록 한다.
	bool _initialized = false;
	public void InitializeAI()
	{
		_startDelayRemainTime = Random.Range(startDelayRange.x, startDelayRange.y);
		_currentState = startState;

		// exception handling
		if (useStateList[(int)_currentState] == false)
			_currentState = eStateType.TypeAmount;

		ResetRandomMoveStateInfo();
		ResetStraightMoveStateInfo();
		ResetCustomActionStateInfo();
		ResetChaseStateInfo();
		ResetAttackActionStateInfo();
		ResetAttackDelayStateInfo();

		_initialized = true;
	}

	void Update()
    {
		UpdateTargeting();
	}

	// 다른 클래스들의 Update에서 PlayAction 한게 있어도 덮어야하므로 LateUpdate에서 처리한다.
	// 대표적으로 PathFinderController의 Animate 함수.
	float _startDelayRemainTime;
	void LateUpdate()
	{
		if (!_initialized)
			return;
		if (actor.actorStatus.IsDie())
			return;
		if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

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
			case eStateType.StraightMove:
				UpdateStraightMove();
				break;
		}
	}

	static float s_LongStartDelay = 5.0f;
	public bool IsLongStartDelaying()
	{
		if (startDelayRange.x >= s_LongStartDelay && startDelayRange.y >= s_LongStartDelay && _startDelayRemainTime > 0.0f)
			return true;
		return false;
	}

	float _currentFindDelay;
	void UpdateTargeting()
	{
		if (targetingProcessor == null)
			return;

		if (targetActor != null)
		{
			if (targetActor.actorStatus.IsDie() || targetActor.gameObject.activeSelf == false)
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

			if (BattleInstanceManager.instance.targetOfMonster != null && BattleInstanceManager.instance.targetOfMonster.gameObject.activeSelf == false)
				BattleInstanceManager.instance.targetOfMonster = null;
			if (BattleInstanceManager.instance.targetOfMonster == null)
			{
				if (targetingProcessor.FindNearestTarget(Team.eTeamCheckFilter.Enemy, PlayerAI.FindTargetRange))
				{
					Collider targetCollider = targetingProcessor.GetTarget();
					targetRadius = ColliderUtil.GetRadius(targetCollider);
					AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
					BattleInstanceManager.instance.targetOfMonster = affectorProcessor.actor;
					BattleInstanceManager.instance.targetColliderOfMonster = targetCollider;
					targetActor = affectorProcessor.actor;
				}
				else
					targetActor = null;
			}
			else
			{
				targetingProcessor.ForceSetTarget(BattleInstanceManager.instance.targetColliderOfMonster);
				targetActor = BattleInstanceManager.instance.targetOfMonster;
				targetRadius = ColliderUtil.GetRadius(BattleInstanceManager.instance.targetColliderOfMonster);
			}
		}
	}

	#region RandomMove
	public Vector2 moveTimeRange;
	public Vector2 refreshTickTimeRange;
	public float desireDistance = 5.0f;
	float _moveRemainTime = 0.0f;
	float _moveRefreshRemainTime = 0.0f;
	void UpdateRandomMove()
	{
		if (_moveRemainTime == 0.0f)
		{
			_moveRemainTime = Random.Range(moveTimeRange.x, moveTimeRange.y);
			_moveRefreshRemainTime = Random.Range(refreshTickTimeRange.x, refreshTickTimeRange.y);
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
				_moveRefreshRemainTime += Random.Range(refreshTickTimeRange.x, refreshTickTimeRange.y);
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
		int tryBreakCount = 0;
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

			++tryBreakCount;
			if (tryBreakCount > 400)
			{
				Debug.LogErrorFormat("MonsterAI RandomMove Error. {0} / {1}. Not found valid random position.", StageManager.instance.GetCurrentSpawnFlagName(), actor.actorId);

				if (pathFinderController.agent.hasPath)
					pathFinderController.agent.ResetPath();
				ResetRandomMoveStateInfo();
				NextStep();
				return;
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

	#region StraightMove
	public Vector2 straightMoveTimeRange;
	public Vector2 straightRefreshTickTimeRange;
	public enum eStraightMoveType
	{
		Random,
		WorldAxis,
		Diagonal,
	}
	public eStraightMoveType straightMoveType;
	void UpdateStraightMove()
	{
		if (_moveRemainTime == 0.0f)
		{
			_moveRemainTime = Random.Range(straightMoveTimeRange.x, straightMoveTimeRange.y);
			_moveRefreshRemainTime = Random.Range(straightRefreshTickTimeRange.x, straightRefreshTickTimeRange.y);
			CalcStraightMovePosition();
			pathFinderController.diableAnimate = true;
			actor.actionController.PlayActionByActionName("Move");
		}

		if (_moveRemainTime > 0.0f)
		{
			_moveRemainTime -= Time.deltaTime;
			_moveRefreshRemainTime -= Time.deltaTime;
			if (_moveRemainTime <= 0.0f)
			{
				ResetStraightMoveStateInfo();
				NextStep();
				return;
			}
			if (_moveRefreshRemainTime <= 0.0f)
			{
				_moveRefreshRemainTime += Random.Range(straightRefreshTickTimeRange.x, straightRefreshTickTimeRange.y);
				CalcStraightMovePosition();
			}
		}

		if (actor.GetRigidbody() != null)
		{
			if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
			{
				actor.GetRigidbody().velocity = Vector3.zero;
				return;
			}

			actor.GetRigidbody().rotation = Quaternion.Slerp(actor.GetRigidbody().rotation, Quaternion.LookRotation(_straightMoveDirection), actor.baseCharacterController.angularSpeed * Mathf.Deg2Rad * Time.deltaTime);
			// velocity는 FixedUpdate에서만 제대로 동작하는 변수라서 일반 update에서 호출할 경우 속도가 꽤 느려진다. 프레임이 떨어질수록 더 느려진다.
			// 그래서 몬스터의 기본 이속이 벽을 뚫을 정도로 빠르진 않을테니 MovePosition을 쓰도록 한다.
			// 사실 이건 kinematic false인 오프젝트에 대해선 transform이동과 같다고 적혀있다.
			//actor.GetRigidbody().velocity = _straightMoveDirection * actor.baseCharacterController.speed;
			actor.GetRigidbody().MovePosition(actor.GetRigidbody().position + _straightMoveDirection * actor.baseCharacterController.speed * Time.deltaTime);
		}
	}

	Vector3 _straightMoveDirection;
	void CalcStraightMovePosition()
	{
		Vector3 randomDirection = Vector3.zero;
		float distance = actor.baseCharacterController.speed * straightRefreshTickTimeRange.x;
		int tryBreakCount = 0;
		while (true)
		{
			switch (straightMoveType)
			{
				case eStraightMoveType.Random:
					Vector2 randomCircle = Random.insideUnitCircle.normalized;
					randomDirection = new Vector3(randomCircle.x, 0.0f, randomCircle.y);
					break;
				case eStraightMoveType.WorldAxis:
					int random = Random.Range(0, 4);
					switch (random)
					{
						case 0: randomDirection = Vector3.forward; break;
						case 1: randomDirection = Vector3.back; break;
						case 2: randomDirection = Vector3.right; break;
						case 3: randomDirection = Vector3.left; break;
					}
					break;
				case eStraightMoveType.Diagonal:
					int randomDiagonal = Random.Range(0, 4);
					switch (randomDiagonal)
					{
						case 0: randomDirection = Vector3.forward + Vector3.right; break;
						case 1: randomDirection = Vector3.back + Vector3.right; break;
						case 2: randomDirection = Vector3.forward + Vector3.left; break;
						case 3: randomDirection = Vector3.back + Vector3.left; break;
					}
					break;
			}

			Vector3 desirePosition = actor.cachedTransform.position + randomDirection;
			desirePosition.y = 0.0f;

			NavMeshHit hit;
			if (NavMesh.SamplePosition(desirePosition, out hit, 1.0f, NavMesh.AllAreas))
			{
				// 바로앞에 Wall있는데 가는건 좀 이상하다. 이때는 패스다.
				Vector3 startPosition = actor.cachedTransform.position + randomDirection.normalized * actorRadius * 0.9f;
				Vector3 endPosition = startPosition + randomDirection;
				if (TargetingProcessor.CheckWall(startPosition, endPosition, 0.1f) == false)
				{
					_straightMoveDirection = randomDirection.normalized;
					return;
				}
			}

			++tryBreakCount;
			if (tryBreakCount > 200)
			{
				Debug.LogErrorFormat("MonsterAI StraightMove Error. {0} / {1}. Not found valid random position.", StageManager.instance.GetCurrentSpawnFlagName(), actor.actorId);
				_straightMoveDirection = randomDirection.normalized;
				return;
			}
		}
		
	}

	void ResetStraightMoveStateInfo()
	{
		_moveRemainTime = 0.0f;
		_moveRefreshRemainTime = 0.0f;
		pathFinderController.diableAnimate = false;
	}
	#endregion

	#region CustomAction
	public eActionPlayType customActionPlayType = eActionPlayType.State;
	public string customActionName;
	public float customActionFadeDuration = 0.05f;
	bool _customActionPlayed = false;
	void UpdateCustomAction()
	{
		if (targetActor == null)
			return;

		if (_customActionPlayed)
		{
			// Idle 하나만 남아있는지를 검사해야 더 정확하지 않을까?
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Idle))
			{
				ResetCustomActionStateInfo();
				NextStep();
			}
			return;
		}

		if (_customActionPlayed == false)
		{
			switch (customActionPlayType)
			{
				case eActionPlayType.Table:
					if (actor.actionController.PlayActionByActionName(customActionName))
						_customActionPlayed = true;
					break;
				case eActionPlayType.State:
					actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(customActionName), customActionFadeDuration);
					_customActionPlayed = true;
					break;
				case eActionPlayType.Trigger:
					actor.actionController.animator.SetTrigger(BattleInstanceManager.instance.GetActionNameHash(customActionName));
					_customActionPlayed = true;
					break;
			}
		}
	}

	void ResetCustomActionStateInfo()
	{
		_customActionPlayed = false;
	}
	#endregion

	#region Chase
	public Vector2 chaseDistanceRange;
	public Vector2 chaseCancelTimeRange;
	float _chaseDistance = 0.0f;
	bool _initChaseCancelTime = false;
	float _chaseCancelTime = 0.0f;
	Vector3 _lastGoalPosition = Vector3.up;
	void UpdateChase(bool callByAttackAction = false)
	{
		if (targetActor == null)
		{
			if (pathFinderController.agent.hasPath)
				pathFinderController.agent.ResetPath();
			return;
		}

		if (_chaseDistance == 0.0f && (chaseDistanceRange.x > 0.0f || chaseDistanceRange.y > 0.0f))
			_chaseDistance = Random.Range(chaseDistanceRange.x, chaseDistanceRange.y);
		if (_initChaseCancelTime == false)
		{
			_chaseCancelTime = Random.Range(chaseCancelTimeRange.x, chaseCancelTimeRange.y);
			if (_chaseCancelTime > 0.0f) _chaseCancelTime += Time.time;
			_initChaseCancelTime = true;
		}

		if (_initChaseCancelTime && _chaseCancelTime > 0.0f && Time.time > _chaseCancelTime)
		{
			ResetChaseStateInfo();
			_currentState = eStateType.AttackDelay;
			NextStep();
			return;
		}

		Vector3 diff = actor.cachedTransform.position - targetActor.cachedTransform.position;
		float sqrDiff = diff.sqrMagnitude;
		float sqrRadius = (targetRadius + actorRadius) * (targetRadius + actorRadius) + (_chaseDistance > 0.0f ? 0.01f : 0.0f) + (_chaseDistance * _chaseDistance);
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
		_chaseDistance = 0.0f;
		_initChaseCancelTime = false;
		_lastGoalPosition = Vector3.up;
	}
	#endregion

	#region AttackAction
	public enum eActionPlayType
	{
		Table,
		State,
		Trigger,
	}
	public eActionPlayType attackActionPlayType = eActionPlayType.Table;
	public string attackActionName;
	public float attackActionFadeDuration = 0.05f;
	public bool lookAtTargetBeforeAttack = true;
	bool _attackPlayed = false;
	bool _waitAttackState = false;
	void UpdateAttack()
	{
		if (targetActor == null)
			return;

		if (_attackPlayed)
		{
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Idle) && actor.actionController.mecanimState.IsState((int)eMecanimState.Attack) == false)
			{
				ResetAttackActionStateInfo();
				NextStep();
			}
			return;
		}

		if (_waitAttackState)
		{
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Attack))
			{
				_waitAttackState = false;
				_attackPlayed = true;
				if (lookAtTargetBeforeAttack)
					pathFinderController.movement.rotation = Quaternion.LookRotation(targetActor.cachedTransform.position - actor.cachedTransform.position);
			}
			return;
		}

		if (_attackPlayed == false)
		{
			// 어택을 하려면 Idle 상태로 진입할때까지 기다린다.
			// 고대버그이긴 한데 간혹가다 어택이 실행 안되는 버그가 있었다.
			// 디버깅 해보니 아래 PlayActionByActionName 실행 후 같은 프레임의 PathFinderController 업데이트에서
			// actionController.PlayActionByActionName("Idle"); 함수가 호출되면서 어택 시켜둔걸 덮는 문제였다.
			// 그래서 차라리 Idle로 진입 후에 attack 처리를 하는 형태로 바꾸기로 결정함.
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Idle) == false)
				return;

			switch (attackActionPlayType)
			{
				case eActionPlayType.Table:
					if (actor.actionController.PlayActionByActionName(attackActionName))
						_attackPlayed = true;
					break;
				case eActionPlayType.State:
					actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(attackActionName), attackActionFadeDuration);
					// 처음 몬스터가 만들어질땐 CorssFade 호출한 프레임부터 Idle이 없어지고 Attack 이 들어있어서 탈출이 안되고 대기타게 되는데
					// 이상하게 몇번 재활용 하다보면
					// 다음 프레임에도 Idle 상태가 유지되면서 NextStep으로 넘어가지게 되었다.
					// 아무래도 monsterAI 및 여러 스크립트를 껐다켰다 하면서 애니메이터쪽과 호출 순서가 꼬이는 듯 하다.
					// 그래서 CustomAction은 특정 임의의 State를 체크하기 어려워 그냥 두지만
					// 여기서는 Attack State 켜졌다가 꺼지는걸 보고 처리할 수 있으니 Trigger처럼 waitAttackState 변수를 사용하도록 하겠다.
					//_attackPlayed = true;
					_waitAttackState = true;
					break;
				case eActionPlayType.Trigger:
					// 트리거로 할땐 바로 위의 Table이나 State와 달리 한프레임 더 늦게 호출이 가 불러지지 않는다는 점때문에 (RandomPlayState를 쓰든 안쓰든 동일하다.)
					// MeState - Attack상태가 돌입하기도 전에 _attackPlayed값이 true로 바뀌고
					// 다음 프레임에 종료 조건에 걸려서 Reset이 호출되버린다.
					// 그래서 Attack상태에 돌입했다가 풀리는걸 보고 종료하게 바꾼다. waitAttackState 추가.
					// 
					// 참고로 위 CustomAction쪽과는 약간 다른게 있다.
					// 저건 Idle이 있으면 종료하는 조건으로 코딩해놨는데, 일반적으로 Idle상태가 없는 상태고
					// Custom Action 안에다가도 Idle 상태를 안넣는 형태다보니
					// 모든 행동이 끝나고 Idle Animator에 의해 Idle로 돌아갈때 종료되게 된다.
					// 그래서 이런 플래그 처리를 안해도 된다.
					//
					// 그런데 또 다른 문제가 발생했다.
					// 위의 처리로 시그널이 호출되는덴 문제가 없었는데
					// 이상하게 애니는 Idle로 나가는데 액션은 State Machine 그룹내에 있는 액션이 실행되는거였다.
					// (실제로 애니메이터 윈도우에선 State Machine안에 있는거로 실행되고있는데 애니는 Idle이 나가고 있었다.)
					// 처음 보는 현상이라 찾아보니 같은 프레임에 Idle을 Play걸면서 trigger를 on하니까 이런 버그같은 현상이 발생하는 거였다.
					// PlayRandomState를 붙이면 명시적으로 Play를 다시 시키니 문제가 발생하지 않는걸로 보아
					// Trigger쪽 관련 이슈인거 같은데, 딱히 좋은 해결책을 찾을 수 없어서 고민이다.
					// 이게 사실이라면 Custom Action쪽에서도 같은 문제가 발생할 수 있단건데 거긴 동시에 Play시키는 State가 없어서 그런지 괜찮았다.
					// 결국 State Machine내에 특정 State를 명시해서 호출하는게 필요했는데 PlayRandomState는 랜덤 돌리는게 있어서 PlayState 스크립트를 만들게 되었다.
					// 요거 붙이고 State Name 적어두면 같은 프레임에 Idle 호출했더라도 애니 제대로 보이면서 실행되게 된다.
					actor.actionController.animator.SetTrigger(BattleInstanceManager.instance.GetActionNameHash(attackActionName));
					//_attackPlayed = true;
					_waitAttackState = true;
					break;
			}
			if (_attackPlayed)
			{
				if (lookAtTargetBeforeAttack)
					pathFinderController.movement.rotation = Quaternion.LookRotation(targetActor.cachedTransform.position - actor.cachedTransform.position);
			}
		}

		// Start State를 Attack Action으로 해두면 무조건 Attack 한번 시작하고 나가야 할거 같은데
		// 이 코드가 위쪽에 있으면 시작과 동시에 UpdateChase를 호출해서 destination을 설정해버린다. 그래서 아래쪽으로 옮긴다.
		if (useStateList[(int)eStateType.Chase] && actor.actionController.mecanimState.IsState((int)eMecanimState.Idle))
		{
			bool enable = false;
			// 후딜이 없을때 혹은 후딜이 있더라도 어택을 실행하기 전에만 처리해야
			// 어택하고 후딜 있는데 후딜 안하고 넘어가는 경우가 생기지 않는다.
			if (useStateList[(int)eStateType.AttackDelay] == false) enable = true;
			if (useStateList[(int)eStateType.AttackDelay] && _attackPlayed == false) enable = true;
			if (_waitAttackState) enable = false;
			if (enable)
			{
				UpdateChase(true);
				// 정확히는 다음 프레임에 hasPath가 true로 된다. 그냥 이렇게 처리해도 괜찮은건가
				if (pathFinderController.agent.hasPath)
				{
					_currentState = eStateType.Chase;
					return;
				}
			}
		}
	}

	void ResetAttackActionStateInfo()
	{
		_attackPlayed = false;
		_waitAttackState = false;
	}
	#endregion

	#region AttackDelay
	public Vector2 attackDelayTimeRange;
	float _attackDelayRemainTime;
	void UpdateAttackDelay()
	{
		if (_attackDelayRemainTime == 0.0f)
		{
			_attackDelayRemainTime = Random.Range(attackDelayTimeRange.x, attackDelayTimeRange.y);
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

	#region Animator Parameter
	public enum eAnimatorParameterForAI
	{
		fHpRatio,
		fDistance,
		iMonsterCount,
		bMySummonAlive
	}
	string[] _animatorParameterNameList = { "fHpRatio", "fDistance", "iMonsterCount", "bMySummonAlive" };
	public bool useAnimatorParameterForAI = false;
	[Reorderable]
	public List<eAnimatorParameterForAI> listAnimatorParameterForAI;

	bool CheckAnimatorParameter(eAnimatorParameterForAI parameterType)
	{
		if (listAnimatorParameterForAI == null)
			return false;
		if (listAnimatorParameterForAI.Contains(parameterType) == false)
			return false;
		return true;
	}

	public void OnEventAnimatorParameter(eAnimatorParameterForAI parameterType, float value)
	{
		if (CheckAnimatorParameter(parameterType) == false)
			return;

		switch (parameterType)
		{
			case eAnimatorParameterForAI.fHpRatio:
			case eAnimatorParameterForAI.fDistance:
				actor.actionController.animator.SetFloat(BattleInstanceManager.instance.GetActionNameHash(_animatorParameterNameList[(int)parameterType]), value);
				break;
		}
	}

	public void OnEventAnimatorParameter(eAnimatorParameterForAI parameterType, int value)
	{
		if (CheckAnimatorParameter(parameterType) == false)
			return;

		switch (parameterType)
		{
			case eAnimatorParameterForAI.iMonsterCount:
				actor.actionController.animator.SetInteger(BattleInstanceManager.instance.GetActionNameHash(_animatorParameterNameList[(int)parameterType]), value);
				break;
		}
	}

	public void OnEventAnimatorParameter(eAnimatorParameterForAI parameterType, bool value)
	{
		if (CheckAnimatorParameter(parameterType) == false)
			return;

		switch (parameterType)
		{
			case eAnimatorParameterForAI.bMySummonAlive:
				actor.actionController.animator.SetBool(BattleInstanceManager.instance.GetActionNameHash(_animatorParameterNameList[(int)parameterType]), value);
				break;
		}
	}
	#endregion

	public void OnFinalizeTeleportedAffector()
	{
		// 원래 이 처리는 없었는데 텔레포트 하고와서 바로 공격하는 문제때문에 텔레포트가 오히려 더 안좋아졌다.
		// 그래서 상태를 강제로 바꿔서 좀 더 유용하게 바꿔본다.

		// 1. 랜덤무브를 가지고 있으면 랜덤무브로 보낸다.
		// 2. 어택딜레이를 가지고 있으면..
		// 3. 스트레이트무브를 가지고 있으면..
		// 4. 커스텀 액션이 있으면
		// 5. 어택이 있으면 어택의 다음으로 보낸다. 불가능할수도 있음.
		// 6. 냅둔다.(현재 상태를 재시작한다.)

		// 이미 AI는 한번 껐다가 켜진거라 Reset류는 호출하지 않는다.
		if (useStateList[(int)eStateType.RandomMove])
			_currentState = eStateType.RandomMove;
		else if (useStateList[(int)eStateType.AttackDelay])
			_currentState = eStateType.AttackDelay;
		else if (useStateList[(int)eStateType.StraightMove])
			_currentState = eStateType.StraightMove;
		else if (useStateList[(int)eStateType.CustomAction])
			_currentState = eStateType.CustomAction;
		else if (useStateList[(int)eStateType.AttackAction])
		{
			_currentState = eStateType.AttackAction;
			NextStep();
		}
		else
		{
			// 아무것도 하지 않는게 6번이다.
		}
	}
}
