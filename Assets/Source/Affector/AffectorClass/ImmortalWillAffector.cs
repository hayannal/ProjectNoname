using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ImmortalWillAffector : AffectorBase
{
	float _endTime;
	float _value;
	float value { get { return _value; } }

	const float StandardHitCountForMonsterKillingPlayer = 3.0f;

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

	public static bool CheckImmortal(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listImmortalWillAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ImmortalWill);
		if (listImmortalWillAffector == null)
			return false;

		float result = 0.0f;
		for (int i = 0; i < listImmortalWillAffector.Count; ++i)
		{
			if (listImmortalWillAffector[i].finalized)
				continue;
			ImmortalWillAffector immortalWillAffector = listImmortalWillAffector[i] as ImmortalWillAffector;
			if (immortalWillAffector == null)
				continue;
			result += immortalWillAffector.value;
		}
		if (result == 0.0f)
			return false;

		float rate = result * StandardHitCountForMonsterKillingPlayer / (result * StandardHitCountForMonsterKillingPlayer + 1.0f);
		bool immortal = (Random.value <= rate);
		if (immortal)
			FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Immortal, affectorProcessor.actor);
		return immortal;
	}
}