using UnityEngine;
using System.Collections;

public class ChangeHitColliderSizeAffector : AffectorBase
{
	float _endTime;

	float _defaultColliderRadius;
	bool _applied;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// 액터한테 쓰는거라서 분명 Capsule Collider일것이다.
		Collider actorHitCollider = _actor.GetCollider();
		if (actorHitCollider == null)
		{
			finalized = true;
			return;
		}
		CapsuleCollider capsuleCollider = actorHitCollider as CapsuleCollider;
		if (capsuleCollider == null)
		{
			finalized = true;
			return;
		}

		_defaultColliderRadius = capsuleCollider.radius;
		capsuleCollider.radius = _defaultColliderRadius * affectorValueLevelTableData.fValue2;
		_applied = true;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
	}

	public override void FinalizeAffector()
	{
		if (_applied == false)
			return;

		CapsuleCollider capsuleCollider = _actor.GetCollider() as CapsuleCollider;
		capsuleCollider.radius = _defaultColliderRadius;
		_applied = false;
	}
}