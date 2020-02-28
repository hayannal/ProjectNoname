using UnityEngine;
using System.Collections;
using ActorStatusDefine;
using UnityEngine.AI;

public class RushAffector : AffectorBase
{
	enum eRushType
	{
		Target,
		TargetPosition,
		RandomPosition,
		TargetWithDistance,	// 0을 기본베이스로 일정거리 달리면 끝나는 형태다.
	}

	float _endTime;
	float _checkBetweenDistance;

	Collider _targetCollider;
	Vector3 _startPosition;
	Vector3 _targetPosition;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;
		_startPosition = _actor.cachedTransform.position;

		// find target
		if (_actor.targetingProcessor.GetTargetCount() > 0)
		{
			_targetCollider = _actor.targetingProcessor.GetTarget();
			_targetPosition = _actor.targetingProcessor.GetTargetPosition();
			_targetRadius = ColliderUtil.GetRadius(_targetCollider);
		}
		else
		{
			_targetPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);
			_targetRadius = 0.5f;
		}

		// check exit condition
		Vector3 diff = Vector3.zero;
		switch ((eRushType)affectorValueLevelTableData.iValue1)
		{
			case eRushType.Target:
				diff = _targetPosition - _actor.cachedTransform.position;
				_lastDiffSqrMagnitude = diff.sqrMagnitude;
				// lookAt 시그널의 기능을 확장하면서 Rush에서는 더이상 rotation은 건들지 않기로 한다.
				//_actor.baseCharacterController.movement.rotation = Quaternion.LookRotation(diff);
				break;
			case eRushType.TargetPosition:
				diff = _targetPosition - _actor.cachedTransform.position;
				_lastDiffSqrMagnitude = diff.sqrMagnitude;
				_targetPosition = _actor.cachedTransform.position + _actor.cachedTransform.forward * diff.magnitude;
				//Vector2 randomOffset = Random.insideUnitCircle * affectorValueLevelTableData.fValue4;
				//diff.x += randomOffset.x;
				//diff.z += randomOffset.y;
				//_actor.baseCharacterController.movement.rotation = Quaternion.LookRotation(diff);
				break;
			case eRushType.RandomPosition:
				_targetPosition = GetRandomPosition();
				diff = _targetPosition - _actor.cachedTransform.position;
				//_actor.baseCharacterController.movement.rotation = Quaternion.LookRotation(diff);
				break;
			case eRushType.TargetWithDistance:
				diff = _targetPosition - _actor.cachedTransform.position;
				break;
		}

		_actorRadius = ColliderUtil.GetRadius(_actor.GetCollider());

		float rushTime = -1.0f;
		_minimunRushTime = affectorValueLevelTableData.fValue2 / affectorValueLevelTableData.fValue1;
		switch ((eRushType)affectorValueLevelTableData.iValue1)
		{
			case eRushType.TargetPosition:
			case eRushType.RandomPosition:
			case eRushType.TargetWithDistance:
				float rushDistance = diff.magnitude;
				rushDistance += _affectorValueLevelTableData.fValue3;
				if (rushDistance < 0.0f)
					rushDistance = 0.0f;
				rushTime = rushDistance / affectorValueLevelTableData.fValue1;
				if (rushTime < _minimunRushTime)
					rushTime = _minimunRushTime;
				break;
		}

		if (_affectorValueLevelTableData.iValue2 == -1)
			_checkBetweenDistance = -1.0f;
		else
			_checkBetweenDistance = _affectorValueLevelTableData.iValue2 * 0.01f;

		// lifeTime
		_endTime = CalcEndTime(rushTime);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 원래라면 절대 들어오지 말아야하는데 러쉬중에 끝나지도 않았는데 러쉬가 또 온거다.
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated Rush Affector.");
	}

	int _agentTypeID = -1;
	Vector3 GetRandomPosition()
	{
		Vector3 randomPosition = Vector3.zero;
		Vector3 result = Vector3.zero;
		float maxDistance = 1.0f;
		int tryCount = 0;
		int tryBreakCount = 0;
		if (_agentTypeID == -1) _agentTypeID = MeLookAt.GetAgentTypeID(_actor);
		while (true)
		{
			randomPosition = _actor.cachedTransform.position + _actor.cachedTransform.forward * Random.Range(0.0f, 10.0f);
			
			// AI쪽 코드에서 가져와본다.
			randomPosition.y = 0.0f;

			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = _agentTypeID;
			if (NavMesh.SamplePosition(randomPosition, out hit, maxDistance, navMeshQueryFilter))
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
				Debug.LogError("RushAffector RandomPosition Error. Not found valid random position.");
				return randomPosition;
			}
		}
		return result;
	}

	float _minimunRushTime;
	float _targetRadius;
	float _actorRadius;
	float _lastDiffSqrMagnitude = 0.0f;
	public override void UpdateAffector()
	{
		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		{
			// 행동불가일때 _endTime을 해당 시간만큼 늘려놔야 미리 멈추는걸 방지할 수 있다. 아무것도 처리하지 않으니 리턴.
			if (_endTime > 0.0f)
				_endTime += Time.deltaTime;
			return;
		}

		bool cannotMove = false;
		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
		{
			cannotMove = true;

			// 이동불가일때 _endTime을 해당 시간만큼 늘려놔야 미리 멈추는걸 방지할 수 있다.
			if (_endTime > 0.0f)
				_endTime += Time.deltaTime;
		}

		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// 최소 거리를 지날때까진 거리 검사를 하지 않는다.
		if (cannotMove == false && _minimunRushTime > 0.0f)
		{
			_minimunRushTime -= Time.deltaTime;
			if (_minimunRushTime <= 0.0f)
				_minimunRushTime = 0.0f;
			return;
		}

		Vector3 targetPosition = _targetPosition;
		if (_targetCollider != null && _targetCollider.gameObject.activeSelf)
			targetPosition = BattleInstanceManager.instance.GetTransformFromCollider(_targetCollider).position;

		float sqrRadius = (_targetRadius + _actorRadius) * (_targetRadius + _actorRadius) + 0.01f;
		switch ((eRushType)_affectorValueLevelTableData.iValue1)
		{
			case eRushType.Target:
				// iValue1 이 0일때는 타겟을 지나쳐야만 종료된다. rushTime이 무제한이니 이거로 꼭 풀어줘야 어펙터가 풀린다.
				Vector3 targetDistance = targetPosition - _startPosition;
				Vector3 currentDistance = _actor.cachedTransform.position - _startPosition;
				float sqrPassDistance = _affectorValueLevelTableData.fValue3 * _affectorValueLevelTableData.fValue3;
				if (_affectorValueLevelTableData.fValue3 < 0.0f) sqrPassDistance *= -1.0f;
				if (currentDistance.sqrMagnitude >= targetDistance.sqrMagnitude + sqrPassDistance)
				{
					finalized = true;
					return;
				}
				// 비껴서 지나가는게 아니라 정면으로 밀고갈걸 대비해서 액터사이의 거리가 멀어지는지도 체크해야한다.(혹은 동일한지)
				// 최초 근접시점을 기억했다가 passDistance만큼 멀어지면 중단한다.
				// 이 루틴으로 하려고 했는데 맵 끝에서 밀고나갈 경우엔 더이상 밀공간이 없어서 포지션 체크로는 안된다.
				// 그래서 안전하게 시간으로 체크하기로 한다.
				Vector3 currentDiff = _actor.cachedTransform.position - targetPosition;
				if (currentDiff.sqrMagnitude < _lastDiffSqrMagnitude)
				{
					_lastDiffSqrMagnitude = currentDiff.sqrMagnitude;
					//Debug.Log(_lastDiffSqrMagnitude);
				}
				else
				{
					if (_endTime == 0.0f)
					{
						// finish time
						float remainTime = _affectorValueLevelTableData.fValue3 / _affectorValueLevelTableData.fValue1;
						_endTime = CalcEndTime(remainTime);
					}
				}
				break;
		}

		if (_checkBetweenDistance == -1.0f)
			return;

		// 근접하면 돌진을 취소하고 공격한다. iValue1과 상관없이 설정되어있다면 체크한다.
		Vector3 diff = _actor.cachedTransform.position - targetPosition;
		float sqrDiff = diff.sqrMagnitude;
		if (sqrDiff <= sqrRadius + (_checkBetweenDistance * _checkBetweenDistance))
		{
			finalized = true;
			return;
		}
	}

	public override void FixedUpdateAffector()
	{
		if (_actor.GetRigidbody() == null)
			return;

		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction) || _actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
		{
			_actor.GetRigidbody().velocity = Vector3.zero;
			return;
		}

		// chase
		//bool checkDot = false;
		switch ((eRushType)_affectorValueLevelTableData.iValue1)
		{
			case eRushType.Target:
			case eRushType.TargetWithDistance:
				if (_targetCollider == null)
					break;
				if (_targetCollider.gameObject.activeSelf == false)
					break;
				if (_affectorValueLevelTableData.fValue4 <= 0.0f)
					break;
				if ((eRushType)_affectorValueLevelTableData.iValue1 == eRushType.Target && _endTime != 0.0f)
					break;

				// 발사체 추적기능의 Curve 코드를 사용한 예시다. 추적거리로 체크할 순 없으나 적당히 값만 입력하면 자연스럽게 보인다.
				// 0이면 추적 없음 50이면 완전 추적. 20 ~ 40 사이쯤이 적당하다.
				Vector3 currentTargetPosition = BattleInstanceManager.instance.GetTransformFromCollider(_targetCollider).position;
				Vector3 diffDir = currentTargetPosition - _actor.cachedTransform.position;
				_actor.cachedTransform.rotation = Quaternion.RotateTowards(_actor.cachedTransform.rotation, Quaternion.LookRotation(diffDir), _affectorValueLevelTableData.fValue4 * Time.fixedDeltaTime);
				break;

				/*
				// 이건 fValue4를 추적거리로 체크할 수 있는데 마지막쯤에 프레임이 확 튀면서 방향이 바뀔때가 있다.
				// dot검사로 안전장치를 해놔도 순간적으로 90 가까이 꺾이면 이상하게 보이긴 하다.
				Vector3 currentTargetPosition = BattleInstanceManager.instance.GetTransformFromCollider(_targetCollider).position;
				Vector3 offset = currentTargetPosition - _targetPosition;
				if (offset.magnitude > (_affectorValueLevelTableData.fValue4 * Time.fixedDeltaTime))
				{
					_targetPosition = _targetPosition + offset.normalized * _affectorValueLevelTableData.fValue4 * Time.fixedDeltaTime;
					checkDot = true;
				}
				else
					_targetPosition = currentTargetPosition;

				Vector3 diff = _targetPosition - _actor.cachedTransform.position;
				// 느리게 추적하다보면 추적포지션을 따라잡을때가 있다. 이땐 dot 검사를 해서 90 이상 방향이 바뀌는지 체크하는게 안전하다.
				if (checkDot && Vector3.Dot(diff.normalized, _actor.cachedTransform.forward) < 0.0f)
					break;
				_actor.baseCharacterController.movement.rotation = Quaternion.LookRotation(diff);
				//Debug.LogFormat("{0} / {1}", diff, _actor.cachedTransform.forward);
				break;
				*/
		}

		_actor.GetRigidbody().velocity = _actor.cachedTransform.forward * _affectorValueLevelTableData.fValue1;
	}

	public override void FinalizeAffector()
	{
		if (_actor.GetRigidbody() != null)
			_actor.GetRigidbody().velocity = Vector3.zero;
		if (_actor.actorStatus.IsDie())
			return;
		if (string.IsNullOrEmpty(_affectorValueLevelTableData.sValue1))
			return;
		_actor.actionController.animator.CrossFade(_affectorValueLevelTableData.sValue1, 0.05f);
	}

	public float GetCollisionDamageRate()
	{
		if (_affectorValueLevelTableData == null)
			return 1.0f;

		if (string.IsNullOrEmpty(_affectorValueLevelTableData.sValue2))
			return 1.0f;

		float fValue = 1.0f;
		if (float.TryParse(_affectorValueLevelTableData.sValue2, out fValue))
			return fValue;
		return 1.0f;
	}
}