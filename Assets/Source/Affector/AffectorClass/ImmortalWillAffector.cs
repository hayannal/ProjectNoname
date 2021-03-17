﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ImmortalWillAffector : AffectorBase
{
	float _endTime;
	float _value;
	float value { get { return _value; } }
	bool noConditionImmortal { get; set; }

	const float StandardHitCountForMonsterKillingPlayer = 4.5f;

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

		noConditionImmortal = (affectorValueLevelTableData.iValue1 == 1);
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

		bool noConditionImmortal = false;
		float result = 0.0f;
		for (int i = 0; i < listImmortalWillAffector.Count; ++i)
		{
			if (listImmortalWillAffector[i].finalized)
				continue;
			ImmortalWillAffector immortalWillAffector = listImmortalWillAffector[i] as ImmortalWillAffector;
			if (immortalWillAffector == null)
				continue;

			if (immortalWillAffector.noConditionImmortal)
			{
				noConditionImmortal = true;
				break;
			}

			result += immortalWillAffector.value;
		}
		if (noConditionImmortal)
		{
			FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Immortal, affectorProcessor.actor);
			return true;
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