using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaybackSpAffector : AffectorBase
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
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		PaybackSpAffector paybackSpAffector = (PaybackSpAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.PaybackSp);
		if (paybackSpAffector == null)
			return 0.0f;

		return Random.Range(paybackSpAffector._affectorValueLevelTableData.fValue2, paybackSpAffector._affectorValueLevelTableData.fValue3);
	}
}