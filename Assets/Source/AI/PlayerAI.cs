using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MecanimStateDefine;
using ECM.Controllers;

public class PlayerAI : MonoBehaviour
{
	public const float FindTargetRange = 50.0f;
	const float TargetFindDelay = 0.1f;
	const float TargetChangeThreshold = 2.0f;

	public Collider targetCollider { get; private set; }
	float _targetColliderRadius;

	Actor actor { get; set; }
	TargetingProcessor targetingProcessor { get; set; }
	BaseCharacterController baseCharacterController { get; set; }

	void OnDisable()
	{
		if (_cachedTargetingObjectTransform != null)
			_cachedTargetingObjectTransform.gameObject.SetActive(false);
	}

	// Start is called before the first frame update
	void Start()
    {
		actor = GetComponent<Actor>();
		targetingProcessor = GetComponent<TargetingProcessor>();
		baseCharacterController = GetComponent<BaseCharacterController>();

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actor.actorId);
		if (actorTableData != null)
		{
			_actorTableAttackRange = actorTableData.attackRange;
			#region Remove HitObject
			_actorTableAttackHitObjectRange = actorTableData.attackHitObjectRange;
			#endregion
		}
	}

    // Update is called once per frame
    void Update()
    {
		UpdateTargeting();
		UpdateTargetingObject();
		UpdateAttack();
		UpdateAttackRange();
	}

	float _currentFindDelay;
	Transform _cachedTargetingObjectTransform = null;
	//List<GameObject> _listCachedTargetingObject = null;
	void UpdateTargeting()
	{
		if (targetingProcessor == null)
			return;

		float deltaTime = (actor.actionController.animator.updateMode == AnimatorUpdateMode.UnscaledTime) ? Time.unscaledDeltaTime : Time.deltaTime;

		bool returnUpdateTargeting = false;
		// 공격중일땐 findDelay만 0에 가깝게 줄여놓고 대기타다가 Idle로 진입하면 바로 find한다.
		if (actor.actionController.mecanimState.IsState((int)eMecanimState.Attack))
			returnUpdateTargeting = true;
		if (returnUpdateTargeting == false && actor.actionController.mecanimState.IsState((int)eMecanimState.Ultimate))
		{
			if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.Roll) == false)
				returnUpdateTargeting = true;
		}
		if (returnUpdateTargeting)
			return;

		_currentFindDelay -= deltaTime;
		if (_currentFindDelay <= 0.0f)
		{
			_currentFindDelay += TargetFindDelay;
			if (targetingProcessor.FindNearestMonster(FindTargetRange, currentAttackRange, actor.actionController.mecanimState.IsState((int)eMecanimState.Move) ? 0.0f : TargetChangeThreshold))
			{
				targetCollider = targetingProcessor.GetTarget();
				_targetColliderRadius = ColliderUtil.GetRadius(targetCollider);
			}
			else
				targetCollider = null;
		}

		if (targetCollider != null)
		{
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
			if (affectorProcessor != null && affectorProcessor.actor != null)
			{
				if (affectorProcessor.actor.actorStatus.IsDie() || TargetingProcessor.IsOutOfRange(affectorProcessor) || (BattleManager.instance != null && BattleManager.instance.IsAutoPlay() == false))
				{
					_currentFindDelay = 0.0f;
					targetCollider = null;
					targetingProcessor.ClearTarget();
				}
			}
		}
	}

	bool _lastUseSleepObject = false;
	void UpdateTargetingObject()
	{
		if (targetCollider == null)
		{
			if (_cachedTargetingObjectTransform != null)
				_cachedTargetingObjectTransform.gameObject.SetActive(false);
			return;
		}

		bool useSleepObject = false;
		AffectorProcessor targetAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
		if (targetAffectorProcessor.IsContinuousAffectorType(eAffectorType.MonsterSleeping))
			useSleepObject = true;

		if (_cachedTargetingObjectTransform == null || useSleepObject != _lastUseSleepObject)
		{
			if (_cachedTargetingObjectTransform != null)
			{
				_cachedTargetingObjectTransform.gameObject.SetActive(false);
				_cachedTargetingObjectTransform = null;
			}
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(useSleepObject ? BattleManager.instance.targetCircleSleepObject : BattleManager.instance.targetCircleObject, null);
			_cachedTargetingObjectTransform = newObject.transform;
			_lastUseSleepObject = useSleepObject;
		}
		if (_cachedTargetingObjectTransform == null)
			return;

		_cachedTargetingObjectTransform.gameObject.SetActive(true);
		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		if (targetTransform.position.y < 0.0f)
		{
			Vector3 newPos = targetTransform.position;
			newPos.y = 0.0f;
			_cachedTargetingObjectTransform.position = newPos;
		}
		else
			_cachedTargetingObjectTransform.position = targetTransform.position;
	}

	public bool IsSleepingTarget()
	{
		if (_cachedTargetingObjectTransform != null && _cachedTargetingObjectTransform.gameObject.activeSelf && _lastUseSleepObject)
			return true;
		return false;
	}

	#region Remove HitObject
	float _actorTableAttackHitObjectRange;
	#endregion
	float _actorTableAttackRange;
	public float currentAttackRange { get { return _actorTableAttackRange + addAttackRange; } }
	public float addAttackRange { get; set; }
	public const string NormalAttackName = "Attack";
	Cooltime _normalAttackCooltime;
	void UpdateAttack()
	{
		if (actor.actorStatus.IsDie())
			return;

		// Attack Delay
		// 평타에 어택 딜레이가 쿨타임으로 적용되어있기 때문에 이걸 얻어와서 쓴다.
		// 참고로 스턴중에도 어택 딜레이는 줄어들게 되어있다.
		if (_normalAttackCooltime != null && _normalAttackCooltime.CheckCooltime())
			return;

		// ContinuousAffector 검사
		if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

		// 궁극기 중에는 어차피 돌릴 이유가 없지 않나
		if (actor.actionController.mecanimState.IsState((int)eMecanimState.Ultimate))
			return;

		// 시즈탱크 퉁퉁포처럼 플레이어가 이동하는 동안에도 포탑은 알아서 쏘는 거까지 커버하려면
		// 인풋이 없는거나 Move가 아닌거로 체크해선 안된다.
		// Idle 혹은 Attackable 같은 뭔가가 필요해보인다.
		// 공속 딜레이가 엄청 작아질걸 처리하다보니 공격 애니메이션이 끝나기도 전에 공격이 나가야해서 검사 코드가 늘어났다.
		// 그래도 조금이라도 연산 줄이기 위해서 if사용해서 순차적으로 묶어둔다.
		//
		// 먼저 이동이 아닌지 검사하고
		// Idle인지 검사 후 아니면 현재 공격중인 상태에서 공격이 끝났는지를 확인.
		bool autoAttackable = false;
		if (actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
		{
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Idle))
				autoAttackable = true;
			if (autoAttackable == false)
			{
				ActionController.ActionInfo actionInfo = actor.actionController.GetCurrentActionInfo();
				if (actionInfo != null && actionInfo.actionName == "Attack" && actor.actionController.mecanimState.IsState((int)eMecanimState.Attack) == false)
					autoAttackable = true;
			}
		}

		#region Remove HitObject
		Vector3 diff = Vector3.zero;
		if (_actorTableAttackHitObjectRange > 0.0f)
		{
			// 히트오브젝트를 향해서도 공격을 해야한다면 이쪽 루트를 탄다.
			if (!autoAttackable)
				return;

			// 타겟 몬스터가 없더라도 적이 마지막 순간에 날린 히트오브젝트는 남아있을 수 있으니 공격할 수 있게 처리해야한다.
			bool attackable = false;
			if (IsTargetColliderInAttackRange(ref diff))
				attackable = true;
			
			// 타겟 몬스터를 공격할 수 없는 상태라면 주변에 공격할 HitObject가 있는지 확인한다.
			if (attackable == false && CheckAttackableHitObject(ref diff))
				attackable = true;

			// 그래도 공격할게 없다면 리턴.
			if (attackable == false)
				return;
		}
		else
		{
			// 일반적인 경우라면 아래처럼 처리한다. 조건에 하나라도 맞지 않으면 바로 리턴한다.

			// no target
			if (targetCollider == null)
				autoAttackable = false;

			if (!autoAttackable)
				return;

			if (IsTargetColliderInAttackRange(ref diff) == false)
				return;
		}
		#endregion

		baseCharacterController.movement.rotation = Quaternion.LookRotation(diff);
		if (actor.actionController.PlayActionByActionName(NormalAttackName))
		{
			_normalAttackCooltime = actor.cooltimeProcessor.GetCooltime(NormalAttackName);
#if UNITY_EDITOR
			float deltaTime = Time.time - _prevAttackTime;
			//Debug.LogFormat("PlayerAI Attack by AI frameCount = {0} / Time = {1} / Delta = {2}", Time.frameCount, Time.time, deltaTime);
			_prevAttackTime = Time.time;
#endif
		}
	}
#if UNITY_EDITOR
	float _prevAttackTime;
#endif

	bool IsInAttackRange(Vector3 diff)
	{
		float maxDistance = currentAttackRange;
		if (maxDistance == 0.0f && BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			maxDistance = NodeWarProcessor.SpawnDistance;
		else
		{
			if (maxDistance == 0.0f)
				return true;
		}

		// 최적화 하겠다고 sqr인 상태에서 빼니 거리계산이 틀어진다. 그냥 magnitude로 계산하기로 한다.
		//if (diff.sqrMagnitude - (_targetColliderRadius * _targetColliderRadius) > maxDistance * maxDistance)
		if (diff.magnitude - _targetColliderRadius > maxDistance)
			return false;

		return true;
	}

	Collider[] _colliderList = null;
	bool CheckAttackableHitObject(ref Vector3 diff)
	{
		if (_colliderList == null)
			_colliderList = new Collider[50];

		// step 1. Physics.OverlapSphere
		int resultCount = Physics.OverlapSphereNonAlloc(actor.cachedTransform.position, _actorTableAttackHitObjectRange, _colliderList); // meHit.areaDistanceMax * parentTransform.localScale.x

		// step 2. Check object count
		for (int i = 0; i < resultCount; ++i)
		{
			if (i >= _colliderList.Length)
				break;

			Collider col = _colliderList[i];

			// affector processor
			HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(col);
			if (hitObject == null)
				continue;

			if (hitObject.IsIgnoreRemoveColliderAffector())
				continue;

			// team check
			if (!Team.CheckTeamFilter(actor.team.teamId, hitObject.statusStructForHitObject.teamId, Team.eTeamCheckFilter.Enemy))
				continue;

			// 매프레임 정렬해서 가장 가까운걸 찾기엔 부하가 더 클거 같아서 아무거나 감지되면 쳐다보기로 해본다.
			// 히트오브젝트가 순간이동하지 않는 이상 결국 바깥 경계부터 하나씩 들어올테니 이렇게 해본다.
			diff = hitObject.cachedTransform.position - actor.cachedTransform.position;
			diff.y = 0.0f;
			return true;
		}
		return false;
	}

	public bool IsTargetColliderInAttackRange(ref Vector3 diff)
	{
		if (targetCollider == null)
			return false;

		AffectorProcessor targetAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
		if (targetAffectorProcessor == null)
			return false;

		if (targetAffectorProcessor.IsContinuousAffectorType(eAffectorType.MonsterSleeping))
			return false;

		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		diff = targetTransform.position - actor.cachedTransform.position;
		diff.y = 0.0f;
		if (IsInAttackRange(diff) == false)
			return false;

		if (CheckNavMeshReachable(actor.cachedTransform.position, targetTransform.position) == false)
			return false;

		return true;
	}

	void UpdateAttackRange()
	{
		if (targetCollider == null)
			return;
		if (_actorTableAttackRange == 0.0f)
			return;
		if (actor.actorStatus.IsDie())
			return;

		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		Vector3 diff = targetTransform.position - actor.cachedTransform.position;
		diff.y = 0.0f;
		RangeIndicator.instance.ShowIndicator(currentAttackRange, !IsInAttackRange(diff), actor.cachedTransform, false);
	}

	NavMeshPath _navMeshPath;
	Vector3 _lastSourcePosition = Vector3.down;
	Vector3 _lastTargetPosition = Vector3.down;
	bool _lastNavMeshResult = true;
	bool CheckNavMeshReachable(Vector3 sourcePosition, Vector3 targetPosition)
	{
		if (ExperienceCanvas.instance != null && ExperienceCanvas.instance.gameObject.activeSelf)
			return true;
		if (actor.targetingProcessor.checkNavMeshReachable == false)
			return true;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			return true;

		if (_navMeshPath == null)
			_navMeshPath = new NavMeshPath();

		Vector3 diff1 = _lastSourcePosition - sourcePosition;
		Vector3 diff2 = _lastTargetPosition - targetPosition;
		if (diff1.sqrMagnitude < 1.0f && diff2.sqrMagnitude < 1.0f)
			return _lastNavMeshResult;

		NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
		navMeshQueryFilter.areaMask = NavMesh.AllAreas;
		navMeshQueryFilter.agentTypeID = BattleInstanceManager.instance.bulletFlyingAgentTypeID;
		_lastNavMeshResult = NavMesh.CalculatePath(sourcePosition, new Vector3(targetPosition.x, 0.0f, targetPosition.z), navMeshQueryFilter, _navMeshPath);
		_lastSourcePosition = sourcePosition;
		_lastTargetPosition = targetPosition;

		if (_lastNavMeshResult == false || _navMeshPath.status != NavMeshPathStatus.PathComplete)
		{
			_lastNavMeshResult = false;
			return false;
		}
		return _lastNavMeshResult;
	}
}
