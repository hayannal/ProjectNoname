using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CastAffector : AffectorBase
{
	float _endTime;
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

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_affectorValueLevelTableData = affectorValueLevelTableData;
		_onDamaged = false;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 원래라면 절대 들어오지 말아야하는데 Teleport중에 끝나지도 않았는데 Cast가 또 온거다.
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated CastAffector.");
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
		if (_actor.actorStatus.IsDie())
			return;
		if (_onDamaged)
			return;

		if (string.IsNullOrEmpty(_affectorValueLevelTableData.sValue1))
			return;
		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);
	}

	bool _onDamaged = false;
	void OnDamage()
	{
		if (_actor.actorStatus.IsDie())
			return;

		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue2), 0.05f);
		_onDamaged = true;
		finalized = true;
	}

	public static void OnDamage(AffectorProcessor affectorProcessor)
	{
		CastAffector castAffector = (CastAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Cast);
		if (castAffector == null)
			return;
		castAffector.OnDamage();
	}
}