using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VelocityAffector : AffectorBase
{
	float _endTime;

	Vector3 _reservedVelocity;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			// something else? for breakable object
			return;
		}

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		if (_actor.GetRigidbody() == null)
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_reservedVelocity.x = affectorValueLevelTableData.fValue2;
		_reservedVelocity.z = affectorValueLevelTableData.fValue3;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_reservedVelocity.x = affectorValueLevelTableData.fValue2;
		_reservedVelocity.z = affectorValueLevelTableData.fValue3;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public override void FixedUpdateAffector()
	{
		if (_actor.GetRigidbody().detectCollisions == false)
			return;

		// 프레임이 떨어져도 물리 결과가 비슷하게 유지되려면
		// update때마다 리셋하면 안되고
		// 설정된 값으로 fixedUpdate에서 계속해서 대입해줘야한다.
		// 그러니 셋팅되면 쭉 설정하는 형태로 구현한다.
		//if (_reservedVelocityState)
		//{
		//	_rigidbody.velocity = _reservedVelocity;
		//	_reservedVelocityState = false;
		//}

		_actor.GetRigidbody().velocity = _actor.cachedTransform.TransformDirection(_reservedVelocity);
	}

	public override void FinalizeAffector()
	{
		if (_actor.GetRigidbody() == null)
			return;
		_actor.GetRigidbody().velocity = Vector3.zero;
	}
}