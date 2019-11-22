using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnlargeDamageAffector : AffectorBase
{
	float _endTime;

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

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_affectorValueLevelTableData = affectorValueLevelTableData;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	float GetValue()
	{
		return _affectorValueLevelTableData.fValue2;
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listEnlargeDamageAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.EnlargeDamage);
		if (listEnlargeDamageAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listEnlargeDamageAffector.Count; ++i)
		{
			if (listEnlargeDamageAffector[i].finalized)
				continue;
			EnlargeDamageAffector enlargeDamageAffector = listEnlargeDamageAffector[i] as EnlargeDamageAffector;
			if (enlargeDamageAffector == null)
				continue;
			result += enlargeDamageAffector.GetValue();
		}
		return result;
	}
}