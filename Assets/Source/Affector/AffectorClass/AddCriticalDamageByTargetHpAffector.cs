using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AddCriticalDamageByTargetHpAffector : AffectorBase
{
	float _endTime;
	float _value;
	float value { get { return _value; } }

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

		_value = affectorValueLevelTableData.fValue2;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public static float GetValue(AffectorProcessor affectorProcessor, float targetHpRate)
	{
		List<AffectorBase> listAddCriticalDamageByTargetHpAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddCriticalDamageByTargetHp);
		if (listAddCriticalDamageByTargetHpAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listAddCriticalDamageByTargetHpAffector.Count; ++i)
		{
			if (listAddCriticalDamageByTargetHpAffector[i].finalized)
				continue;
			AddCriticalDamageByTargetHpAffector addCriticalDamageByTargetHpAffector = listAddCriticalDamageByTargetHpAffector[i] as AddCriticalDamageByTargetHpAffector;
			if (addCriticalDamageByTargetHpAffector == null)
				continue;
			result += addCriticalDamageByTargetHpAffector.value;
		}
		if (result == 0.0f)
			return 0.0f;

		return result * (1.0f - targetHpRate);
	}
}