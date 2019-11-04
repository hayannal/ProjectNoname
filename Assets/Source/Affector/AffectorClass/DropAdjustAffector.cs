using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DropAdjustAffector : AffectorBase
{
	public enum eDropAdjustType
	{
		GoldDropAmount,
		ItemDropRate,
		HeartDropRate,
	}

	AffectorValueLevelTableData _affectorValueLevelTableData;
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
		_affectorValueLevelTableData = affectorValueLevelTableData;
	}

	float GetValue(eDropAdjustType eType)
	{
		switch (eType)
		{
			case eDropAdjustType.GoldDropAmount:
				return _affectorValueLevelTableData.fValue2;
			case eDropAdjustType.ItemDropRate:
				return _affectorValueLevelTableData.fValue3;
			case eDropAdjustType.HeartDropRate:
				return _affectorValueLevelTableData.fValue4;
		}
		return 0.0f;
	}

	public static float GetValue(AffectorProcessor affectorProcessor, eDropAdjustType eType)
	{
		List<AffectorBase> listDropAdjustAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.DropAdjust);
		if (listDropAdjustAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listDropAdjustAffector.Count; ++i)
		{
			if (listDropAdjustAffector[i].finalized)
				continue;
			DropAdjustAffector dropAdjustAffector = listDropAdjustAffector[i] as DropAdjustAffector;
			if (dropAdjustAffector == null)
				continue;
			result += dropAdjustAffector.GetValue(eType);
		}
		return result;
	}
}