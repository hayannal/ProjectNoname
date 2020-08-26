#define USE_MONSTER_LIST

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingProcessor : MonoBehaviour {

	public Actor actor { get; private set; }

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	void OnEnable()
	{
		ClearTarget();
	}

	public void ClearTarget()
	{
		_targetList.Clear();
	}

	public int GetTargetCount()
	{
		return _targetList.Count;
	}

	public List<Collider> GetTargetList()
	{
		return _targetList;
	}

	public Collider GetTarget(int index = 0)
	{
		if (index < _targetList.Count)
			return _targetList[index];	
		return null;
	}

	public Vector3 GetTargetPosition(int index = 0)
	{
		Collider collider = GetTarget(index);
		if (collider == null)
			return Vector3.zero;
		return BattleInstanceManager.instance.GetTransformFromCollider(collider).position;
	}

	public Transform GetTargetTransform(int index = 0)
	{
		Collider collider = GetTarget(index);
		if (collider == null)
			return null;
		return BattleInstanceManager.instance.GetTransformFromCollider(collider);
	}

	List<Collider> _targetList = new List<Collider>();

	// 이 함수가 사실 공용으로 쓰는 함수긴 한데 플레이어가 하단의 FindNearestMonster 함수를 쓰는거로 바꾸면서 몬스터만 사용하는 함수였다.
	// 그런데 소환용 아군 몬스터가 추가됨에 따라 적군 몬스터가 플레이어 뿐만 아니라 소환된 아군 몬스터를 타겟으로 삼을 수 있게 되면서
	// 타겟이 갱신되지 않는한 플레이어를 따라오지 않는 문제가 있었다.
	// 이걸 막기위해 onlyPlayerActor 파라미터를 추가하기로 한다.
	//
	// 사실 소환용 아군 몬스터가 적군 몬스터보다 먼저 나오지만 않는다면
	// 항상 플레이어 캐릭터가 먼저 타겟으로 검출된 후 죽을때까지 변하지 않기때문에 위와 같은 상황이 발생할 일이 없긴 한데
	// 혹시나 적군 몬스터들이 나오기 전에 소환이 가능해질까봐 안전하게 이렇게 처리하기로 한다.
	Transform _transform = null;
	Team _teamComponent = null;
	public bool FindNearestTarget(Team.eTeamCheckFilter teamFilter, float range, bool onlyPlayerActor = false)
	{
		if (_transform == null)
			_transform = GetComponent<Transform>();
		if (_teamComponent == null)
			_teamComponent = GetComponent<Team>();

		Vector3 position = _transform.position;
		float nearestDistance = float.MaxValue;
		Collider nearestCollider = null;
		Collider[] result = Physics.OverlapSphere(position, range); // range * _transform.localScale.x
		for (int i = 0; i < result.Length; ++i)
		{
			// affector processor
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(result[i]);
			if (affectorProcessor == null)
				continue;

			// team check
			if (_teamComponent != null)
			{
				if (!Team.CheckTeamFilter(_teamComponent.teamId, result[i], teamFilter, false))
					continue;
			}

			// hp
			Actor actor = affectorProcessor.actor;
			if (actor != null)
			{
				if (actor.actorStatus.IsDie())
					continue;
			}

			if (onlyPlayerActor && actor.IsPlayerActor() == false)
				continue;

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(result[i]);
			if (colliderRadius == -1.0f) continue;

			// distance
			Vector3 diff = BattleInstanceManager.instance.GetTransformFromCollider(result[i]).position - position;
			diff.y = 0.0f;
			float distance = diff.magnitude - colliderRadius;
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestCollider = result[i];
			}
		}

		_targetList.Clear();
		if (nearestDistance != float.MaxValue && nearestCollider != null)
		{
			_targetList.Add(nearestCollider);
			return true;
		}
		return false;
	}

	#region Multi Target
	int _lastRefreshMultiTargetFrameCount;
	List<Collider> _listMultiTargetTemporary;
	List<MonsterActor> _listMultiTargetMonsterTemporary;
	public void FindPresetMultiTargetMonsterList(MeHitObject meHit)
	{
		// 거리 기반이 아니라 거리 내 랜덤이다보니 매 프레임 호출할때마다 타겟이 변경되게 된다. 그러니 여러번 호출되어도 같은 결과를 보장하기 위해 프레임당 1회로 갱신을 제한해둔다.
		if (_lastRefreshMultiTargetFrameCount == Time.frameCount)
			return;

		if (actor.IsPlayerActor() == false)
			return;

		// 멀티타겟을 하는 캐릭터만 이 작업을 수행한다.
		if (cachedActorTableData.multiTargetAngle == 0.0f)
			return;

		if (_listMultiTargetTemporary == null)
			_listMultiTargetTemporary = new List<Collider>();
		_listMultiTargetTemporary.Clear();
		if (_listMultiTargetMonsterTemporary == null)
			_listMultiTargetMonsterTemporary = new List<MonsterActor>();
		_listMultiTargetMonsterTemporary.Clear();

		// 메인 타겟은 이미 정해져있는 상태일거다. 이 메인 타겟을 제외한 나머지를 채워두면 된다.
		// 타겟이 없다면 멀티타겟 할 수 없는 상태일테니 아무것도 하지 않는다.
		if (_targetList.Count == 0)
			return;

		// 타겟이 여러개 있다면 이전 호출에 의해 추가된걸거다. 새로 할때는 메인 타겟 남겨두고 삭제해야한다.
		if (_targetList.Count > 1)
		{
			for (int i = _targetList.Count - 1; i >= 1; --i)
				_targetList.RemoveAt(i);
		}

		Vector3 position = _transform.position;
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = 0; i < listMonsterActor.Count; ++i)
		{
			Collider monsterCollider = listMonsterActor[i].GetCollider();
			if (_targetList[0] == monsterCollider)
				continue;

			// team check
			if (_teamComponent != null)
			{
				if (!Team.CheckTeamFilter(_teamComponent.teamId, monsterCollider, Team.eTeamCheckFilter.Enemy, false))
					continue;
			}

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(monsterCollider);
			if (colliderRadius == -1.0f) continue;

			if (IsOutOfRangePresetMultiTarget(meHit, listMonsterActor[i].affectorProcessor))
				continue;

			// distance
			Vector3 diff = listMonsterActor[i].cachedTransform.position - position;
			diff.y = 0.0f;
			if (cachedActorTableData.attackRange > 0.0f)
			{
				float distance = diff.magnitude - colliderRadius;
				if (distance > cachedActorTableData.attackRange)
					continue;
			}

			// angle
			float angle = Vector3.Angle(actor.cachedTransform.forward, diff.normalized);
			float hypotenuse = Mathf.Sqrt(diff.sqrMagnitude + colliderRadius * colliderRadius);
			float adjustAngle = Mathf.Rad2Deg * Mathf.Acos(diff.magnitude / hypotenuse);
			if (cachedActorTableData.multiTargetAngle * 0.5f < angle - adjustAngle)
				continue;

			_listMultiTargetTemporary.Add(monsterCollider);
			_listMultiTargetMonsterTemporary.Add(listMonsterActor[i]);
		}

		// 섞고나서 추가해둔다. 이래야 랜덤이 제대로 된다.
		//ObjectUtil.Shuffle<Collider>(_listMultiTargetTemporary);

		// 그냥 섞었더니 마구잡이로 때려서 효율이 떨어진다. 피 비율이 낮은몹 위주로 때려보게 한다.
		_listMultiTargetMonsterTemporary.Sort(delegate (MonsterActor x, MonsterActor y)
		{
			if (x.actorStatus.GetHPRatio() > y.actorStatus.GetHPRatio()) return 1;
			else if (x.actorStatus.GetHPRatio() < y.actorStatus.GetHPRatio()) return -1;
			return 0;
		});

		//for (int i = 0; i < _listMultiTargetTemporary.Count; ++i)
		//	_targetList.Add(_listMultiTargetTemporary[i]);
		for (int i = 0; i < _listMultiTargetMonsterTemporary.Count; ++i)
			_targetList.Add(_listMultiTargetMonsterTemporary[i].GetCollider());

		_lastRefreshMultiTargetFrameCount = Time.frameCount;
	}

	ActorTableData _cachedActorTableData;
	public ActorTableData cachedActorTableData
	{
		get
		{
			if (_cachedActorTableData == null)
				_cachedActorTableData = TableDataManager.instance.FindActorTableData(actor.actorId);
			return _cachedActorTableData;
		}
	}
	#endregion

#if USE_MONSTER_LIST
	// 0이면 벽검사를 하지 않는다. 0보다 크면 SphereCast로 벽을 검사해서 타겟을 찾는다.
	public float sphereCastRadiusForCheckWall { get; set; }
	// 간혹가다 몬스터의 Collider를 꺼야할때가 있어서 Physic으로 검사하면 타겟팅이 잠시 풀리게 되버렸다. (땅 투과시)
	// 그래서 차라리 몬스터 리스트를 히트오브젝트처럼 등록해놨다가 받아오는 형태로 가기로 한다.
	// Die시 빠지기 때문에 Die검사를 추가로 할 필요도 없다.
	public bool FindNearestMonster(float findRange, float attackRange, float changeThreshold = 0.0f)
	{
		if (_transform == null)
			_transform = GetComponent<Transform>();
		if (_teamComponent == null)
			_teamComponent = GetComponent<Team>();

		Vector3 position = _transform.position;
		float nearestDistance = float.MaxValue;
		Collider nearestCollider = null;
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = 0; i < listMonsterActor.Count; ++i)
		{
			Collider monsterCollider = listMonsterActor[i].GetCollider();

			// team check
			if (_teamComponent != null)
			{
				if (!Team.CheckTeamFilter(_teamComponent.teamId, monsterCollider, Team.eTeamCheckFilter.Enemy, false))
					continue;
			}

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(monsterCollider);
			if (colliderRadius == -1.0f) continue;

			// distance
			Vector3 diff = listMonsterActor[i].cachedTransform.position - position;
			diff.y = 0.0f;
			float distance = diff.magnitude - colliderRadius;
			AdjustRange(listMonsterActor[i].affectorProcessor, listMonsterActor[i].cachedTransform.position, position, sphereCastRadiusForCheckWall, findRange, attackRange, ref distance);
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestCollider = monsterCollider;
			}
		}

		if (changeThreshold == 0.0f || _targetList.Count == 0 || _targetList[0] == null || nearestCollider == null || (cachedActorTableData.attackRange > 0.0f && nearestDistance > cachedActorTableData.attackRange))
		{
			_targetList.Clear();
			if (nearestDistance != float.MaxValue && nearestCollider != null)
			{
				_targetList.Add(nearestCollider);
				return true;
			}
		}
		else
		{
			Vector3 prevTargetPosition = BattleInstanceManager.instance.GetTransformFromCollider(_targetList[0]).position;
			Vector3 prevTargetDiff = prevTargetPosition - position;
			prevTargetDiff.y = 0.0f;
			float prevDistance = prevTargetDiff.magnitude - ColliderUtil.GetRadius(_targetList[0]);
			AffectorProcessor prevAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(_targetList[0]);
			AdjustRange(prevAffectorProcessor, prevTargetPosition, position, sphereCastRadiusForCheckWall, findRange, attackRange, ref prevDistance);

			Vector3 currentTargetPosition = BattleInstanceManager.instance.GetTransformFromCollider(nearestCollider).position;
			Vector3 currentTargetDiff = currentTargetPosition - position;
			currentTargetDiff.y = 0.0f;
			float currentDistance = currentTargetDiff.magnitude - ColliderUtil.GetRadius(nearestCollider);
			AffectorProcessor currentAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(nearestCollider);
			AdjustRange(currentAffectorProcessor, currentTargetPosition, position, sphereCastRadiusForCheckWall, findRange, attackRange, ref currentDistance);

			if (currentDistance <= prevDistance - changeThreshold)
			{
				_targetList.Clear();
				_targetList.Add(nearestCollider);
			}
			return true;
		}
		return false;
	}

	public static bool IsOutOfRange(AffectorProcessor affectorProcessor)
	{
		// 특수한 상황에선 범위 밖 몬스터 인거처럼 처리해야한다.
		if (affectorProcessor.IsContinuousAffectorType(eAffectorType.Teleported))
			return true;
		return false;
	}

	static RaycastHit[] s_raycastHitList = null;
	public static bool CheckWall(Vector3 position, Vector3 targetPosition, float radius)
	{
		// temp - check wall
		if (s_raycastHitList == null)
			s_raycastHitList = new RaycastHit[100];

		// step 1. Physics.SphereCastNonAlloc
		Vector3 diff = targetPosition - position;
		float length = diff.magnitude;
		Vector3 rayPosition = position;
		rayPosition.y = 1.0f;
		int resultCount = Physics.SphereCastNonAlloc(rayPosition, radius, diff.normalized, s_raycastHitList, length - radius, 1);

		// step 2. Through Test
		float reservedNearestDistance = length - radius;
		Vector3 endPosition = Vector3.zero;
		for (int i = 0; i < resultCount; ++i)
		{
			if (i >= s_raycastHitList.Length)
				break;

			bool planeCollided = false;
			bool groundQuadCollided = false;
			Vector3 wallNormal = Vector3.forward;
			Collider col = s_raycastHitList[i].collider;
			if (col.isTrigger)
				continue;

			if (BattleInstanceManager.instance.planeCollider != null && BattleInstanceManager.instance.planeCollider == col)
			{
				planeCollided = true;
				wallNormal = s_raycastHitList[i].normal;
			}

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
			{
				groundQuadCollided = true;
				wallNormal = s_raycastHitList[i].normal;
			}

			AffectorProcessor targetAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (targetAffectorProcessor != null)
			{
				//if (Team.CheckTeamFilter(statusForHitObject.teamId, col, meHit.teamCheckType))
				//	monsterCollided = true;
			}
			else if (planeCollided == false && groundQuadCollided == false)
			{
				return true;
			}
		}
		return false;
	}

	static bool IsOutOfRangePresetMultiTarget(MeHitObject meHit, AffectorProcessor affectorProcessor)
	{
		if (IsOutOfRange(affectorProcessor))
			return true;
		if (affectorProcessor.IsContinuousAffectorType(eAffectorType.Burrow))
			return true;
		if (BurrowOnStartAffector.CheckBurrow(affectorProcessor))	// 패시브라서 타입으로 체크하면 항상 true가 되서 이 함수로 처리해야한다.
			return true;
		if (meHit.presetAnimatorRoot == false && JumpAffector.CheckJump(affectorProcessor))	// 몸에다가 직격을 날리는 프리셋만이 점프 중인 몹을 공격할 수 있다.
			return true;
		return false;
	}
	
	public static void AdjustRange(AffectorProcessor affectorProcessor, Vector3 targetPosition, Vector3 position, float sphereCastRadiusForCheckWall, float findRange, float attackRange, ref float distance)
	{
		bool applyOutOfRange = false;
		bool applyFarthest = false;
		if (IsOutOfRange(affectorProcessor))
			applyOutOfRange = true;

		if (sphereCastRadiusForCheckWall > 0.0f && CheckWall(position, targetPosition, sphereCastRadiusForCheckWall))
			applyFarthest = true;
		if (affectorProcessor.IsContinuousAffectorType(eAffectorType.Burrow))
			applyFarthest = true;
		if (BurrowOnStartAffector.CheckBurrow(affectorProcessor))
			applyFarthest = true;

		if (applyFarthest)
		{
			// 사거리가 있을때는 사거리 근처 쯤으로 보정하고 사거리가 없을때는 findRange 근처로 보정한다.
			float adjustBaseRange = findRange;
			if (attackRange > 0.0f) adjustBaseRange = attackRange;
			distance = adjustBaseRange + distance * 0.001f;
		}
		if (applyOutOfRange)
			distance += findRange * 2.0f;
	}
#endif

	public void ForceSetTarget(Collider collider)
	{
		ClearTarget();
		_targetList.Add(collider);
	}


	#region Custom Position
	public bool IsRegisteredCustomTargetPosition()
	{
		if (_listCustomTargetPosition == null)
			return false;

		return _listCustomTargetPosition.Count > 0;
	}

	List<Vector3> _listCustomTargetPosition;
	public Vector3 GetCustomTargetPosition(int index)
	{
		if (_listCustomTargetPosition == null)
			return Vector3.zero;

		if (!IsRegisteredCustomTargetPosition())
			return Vector3.zero;

		if (index < _listCustomTargetPosition.Count)
			return _listCustomTargetPosition[index];
		return Vector3.zero;
	}

	public void SetCustomTargetPosition(Vector3 position)
	{
		if (_listCustomTargetPosition == null)
			_listCustomTargetPosition = new List<Vector3>();
		_listCustomTargetPosition.Clear();
		_listCustomTargetPosition.Add(position);
	}

	public void SetCustomTargetPosition(List<Collider> listTarget)
	{
		if (_listCustomTargetPosition == null)
			_listCustomTargetPosition = new List<Vector3>();
		_listCustomTargetPosition.Clear();
		for (int i = 0; i < listTarget.Count; ++i)
			_listCustomTargetPosition.Add(BattleInstanceManager.instance.GetTransformFromCollider(listTarget[i]).position);
	}

	public void ClearCustomTargetPosition()
	{
		if (_listCustomTargetPosition == null)
			return;
		_listCustomTargetPosition.Clear();
	}
	#endregion
}
