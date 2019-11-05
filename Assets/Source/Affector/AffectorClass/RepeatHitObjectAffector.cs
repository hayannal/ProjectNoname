using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RepeatHitObjectAffector : AffectorBase
{
	int _repeatAddCount;
	public int repeatAddCount { get { return _repeatAddCount; } }
	float _repeatInterval;
	public float repeatInterval { get { return _repeatInterval; } }

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

		_repeatAddCount = affectorValueLevelTableData.iValue1;
		_repeatInterval = affectorValueLevelTableData.fValue2;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		RepeatHitObjectAffector repeatHitObjectAffector = (RepeatHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.RepeatHitObject);
		if (repeatHitObjectAffector == null)
			return 0;

		return repeatHitObjectAffector.repeatAddCount;
	}

	public static float GetInterval(AffectorProcessor affectorProcessor)
	{
		RepeatHitObjectAffector repeatHitObjectAffector = (RepeatHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.RepeatHitObject);
		if (repeatHitObjectAffector == null)
			return 0;

		return repeatHitObjectAffector.repeatInterval;
	}

	public static float GetDamageRate(int repeatAddCount, int index)
	{
		DamageRateTableData damageRateTableData = TableDataManager.instance.FindDamageTableData("Repeat", repeatAddCount);
		if (damageRateTableData == null)
			return 1.0f;
		if (index < damageRateTableData.rate.Length)
			return damageRateTableData.rate[index];
		return 1.0f;
	}
}