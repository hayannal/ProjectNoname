using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InstantDeathAffector : AffectorBase
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

	public static bool CheckInstantDeath(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listInstantDeathAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.InstantDeath);
		if (listInstantDeathAffector == null)
			return false;

		float result = 0.0f;
		for (int i = 0; i < listInstantDeathAffector.Count; ++i)
		{
			if (listInstantDeathAffector[i].finalized)
				continue;
			InstantDeathAffector instantDeathAffector = listInstantDeathAffector[i] as InstantDeathAffector;
			if (instantDeathAffector == null)
				continue;
			result += instantDeathAffector.value;
		}
		if (result == 0.0f)
			return false;

		return (Random.value <= result);
	}
}