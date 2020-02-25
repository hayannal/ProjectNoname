using UnityEngine;
using System.Collections;
using ActorStatusDefine;
using UnityEngine.AI;

public class RotateAffector : AffectorBase
{
	float _endTime;

	float _rotateSpeed;
	string _endStateName;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_rotateSpeed = affectorValueLevelTableData.fValue2 / affectorValueLevelTableData.fValue1;
		_endStateName = affectorValueLevelTableData.sValue1;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 원래라면 절대 들어오지 말아야하는데 러쉬중에 끝나지도 않았는데 RotateAffector가 또 온거다.
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated Rotate Affector.");
	}

	public override void UpdateAffector()
	{
		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		{
			// 행동불가일때 _endTime을 해당 시간만큼 늘려놔야 미리 멈추는걸 방지할 수 있다. 아무것도 처리하지 않으니 리턴.
			if (_endTime > 0.0f)
				_endTime += Time.deltaTime;
			return;
		}

		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_actor.cachedTransform.Rotate(0.0f, _rotateSpeed * Time.deltaTime, 0.0f, Space.Self);
	}

	public override void FinalizeAffector()
	{
		if (_actor.actorStatus.IsDie())
			return;
		if (string.IsNullOrEmpty(_endStateName))
			return;
		_actor.actionController.animator.CrossFade(_endStateName, 0.05f);
	}
}