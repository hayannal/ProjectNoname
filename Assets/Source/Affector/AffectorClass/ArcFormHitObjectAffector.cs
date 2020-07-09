using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArcFormHitObjectAffector : AffectorBase
{
	int _arcFormAdditionalCreateCount;
	float _arcFormDelay;

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

		_arcFormAdditionalCreateCount = affectorValueLevelTableData.iValue1;
		_arcFormDelay = affectorValueLevelTableData.fValue1;
	}

	public static bool GetInfo(AffectorProcessor affectorProcessor, ref int count, ref float delay)
	{
		ArcFormHitObjectAffector arcFormHitObjectAffector = (ArcFormHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ArcFormHitObject);
		if (arcFormHitObjectAffector == null)
			return false;

		count = arcFormHitObjectAffector._arcFormAdditionalCreateCount;
		delay = arcFormHitObjectAffector._arcFormDelay;
		return true;
	}
}