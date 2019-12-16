#define USE_MONSTER_LIST

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingProcessor : MonoBehaviour {

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

	List<Collider> _targetList = new List<Collider>();

	Transform _transform = null;
	Team _teamComponent = null;
	public bool FindNearestTarget(Team.eTeamCheckFilter teamFilter, float range)
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

#if USE_MONSTER_LIST
	// 0이면 벽검사를 하지 않는다. 0보다 크면 SphereCast로 벽을 검사해서 타겟을 찾는다.
	public float sphereCastRadiusForCheckWall { get; set; }
	// 간혹가다 몬스터의 Collider를 꺼야할때가 있어서 Physic으로 검사하면 타겟팅이 잠시 풀리게 되버렸다. (땅 투과시)
	// 그래서 차라리 몬스터 리스트를 히트오브젝트처럼 등록해놨다가 받아오는 형태로 가기로 한다.
	// Die시 빠지기 때문에 Die검사를 추가로 할 필요도 없다.
	public bool FindNearestMonster(float range, float changeThreshold = 0.0f)
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
			AdjustRange(listMonsterActor[i].affectorProcessor, listMonsterActor[i].cachedTransform.position, position, sphereCastRadiusForCheckWall, range, ref distance);
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestCollider = monsterCollider;
			}
		}

		if (changeThreshold == 0.0f || _targetList.Count == 0 || _targetList[0] == null || nearestCollider == null)
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
			AdjustRange(prevAffectorProcessor, prevTargetPosition, position, sphereCastRadiusForCheckWall, range, ref prevDistance);

			Vector3 currentTargetPosition = BattleInstanceManager.instance.GetTransformFromCollider(nearestCollider).position;
			Vector3 currentTargetDiff = currentTargetPosition - position;
			currentTargetDiff.y = 0.0f;
			float currentDistance = currentTargetDiff.magnitude - ColliderUtil.GetRadius(nearestCollider);
			AffectorProcessor currentAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(nearestCollider);
			AdjustRange(currentAffectorProcessor, currentTargetPosition, position, sphereCastRadiusForCheckWall, range, ref currentDistance);

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
	static bool CheckWall(Vector3 position, Vector3 targetPosition, float radius)
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
	
	public static void AdjustRange(AffectorProcessor affectorProcessor, Vector3 targetPosition, Vector3 position, float sphereCastRadiusForCheckWall, float findRange, ref float distance)
	{
		bool applyOutOfRange = false;
		bool applyFarthest = false;
		if (IsOutOfRange(affectorProcessor))
			applyOutOfRange = true;

		if (sphereCastRadiusForCheckWall > 0.0f && CheckWall(position, targetPosition, sphereCastRadiusForCheckWall))
			applyFarthest = true;
		if (affectorProcessor.IsContinuousAffectorType(eAffectorType.Burrow))
			applyFarthest = true;

		if (applyFarthest)
			distance += findRange;
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
