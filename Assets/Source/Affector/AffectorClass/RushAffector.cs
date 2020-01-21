using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class RushAffector : AffectorBase
{
	float _endTime;

	Collider _targetCollider;
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

		Vector3 diff = _targetPosition - _actor.cachedTransform.position;
		Vector2 randomOffset = Random.insideUnitCircle * affectorValueLevelTableData.fValue2;
		diff.x += randomOffset.x;
		diff.z += randomOffset.y;
		float rushTime = diff.magnitude / affectorValueLevelTableData.fValue1;
		_minimunRushTime = affectorValueLevelTableData.fValue3 / affectorValueLevelTableData.fValue1;
		if (rushTime < _minimunRushTime)
			rushTime = _minimunRushTime;

		_actorRadius = ColliderUtil.GetRadius(_actor.GetCollider());

		// lifeTime
		_endTime = CalcEndTime(rushTime);
	}

	float _minimunRushTime;
	float _targetRadius;
	float _actorRadius;
	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}



		// 최소 거리를 지날때까진 거리 검사를 하지 않는다.
		if (_minimunRushTime > 0.0f)
		{
			_minimunRushTime -= Time.deltaTime;
			if (_minimunRushTime <= 0.0f)
				_minimunRushTime = 0.0f;
			return;
		}

		// 근접하면 돌진을 취소하고 공격한다.
		Vector3 targetPosition = _targetPosition;
		if (_targetCollider != null && _targetCollider.gameObject.activeSelf)
			targetPosition = BattleInstanceManager.instance.GetTransformFromCollider(_targetCollider).position;
		Vector3 diff = _actor.cachedTransform.position - targetPosition;
		float sqrDiff = diff.sqrMagnitude;
		float sqrRadius = (_targetRadius + _actorRadius) * (_targetRadius + _actorRadius) + 0.01f + (_affectorValueLevelTableData.fValue4 * _affectorValueLevelTableData.fValue4);
		if (sqrDiff <= sqrRadius)
		{
			finalized = true;
			return;
		}
	}

	public override void FixedUpdateAffector()
	{
		if (_actor.GetRigidbody() == null)
			return;
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
}