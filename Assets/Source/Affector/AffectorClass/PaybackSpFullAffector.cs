using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PaybackSpFullAffector : AffectorBase
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

	bool _readyForPayback;
	void OnEventStartStage()
	{
		_readyForPayback = true;
	}

	bool OnUseUltimateSp()
	{
		if (_readyForPayback)
		{
			_readyForPayback = false;
			return true;
		}

		return false;
	}

	public static void OnEventStartStage(AffectorProcessor affectorProcessor)
	{
		PaybackSpFullAffector paybackSpFullAffector = (PaybackSpFullAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.PaybackSpFull);
		if (paybackSpFullAffector == null)
			return;

		paybackSpFullAffector.OnEventStartStage();
	}

	public static bool OnUseUltimateSp(AffectorProcessor affectorProcessor)
	{
		PaybackSpFullAffector paybackSpFullAffector = (PaybackSpFullAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.PaybackSpFull);
		if (paybackSpFullAffector == null)
			return false;

		return paybackSpFullAffector.OnUseUltimateSp();
	}
}