using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlowHitObjectSpeedAffector : AffectorBase
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

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listSlowHitObjectSpeedAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.SlowHitObjectSpeed);
		if (listSlowHitObjectSpeedAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listSlowHitObjectSpeedAffector.Count; ++i)
		{
			if (listSlowHitObjectSpeedAffector[i].finalized)
				continue;
			SlowHitObjectSpeedAffector slowHitObjectSpeedAffector = listSlowHitObjectSpeedAffector[i] as SlowHitObjectSpeedAffector;
			if (slowHitObjectSpeedAffector == null)
				continue;
			result += slowHitObjectSpeedAffector.value;
		}
		return result;
	}
}