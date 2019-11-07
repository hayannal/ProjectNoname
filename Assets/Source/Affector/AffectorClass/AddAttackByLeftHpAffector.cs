using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class AddAttackByLeftHpAffector : AffectorBase
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

	public static float GetValue(AffectorProcessor affectorProcessor, float hpRate)
	{
		List<AffectorBase> listAddAttackByHpAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByLeftHp);
		if (listAddAttackByHpAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listAddAttackByHpAffector.Count; ++i)
		{
			if (listAddAttackByHpAffector[i].finalized)
				continue;
			AddAttackByLeftHpAffector addAttackByLeftHpAffector = listAddAttackByHpAffector[i] as AddAttackByLeftHpAffector;
			if (addAttackByLeftHpAffector == null)
				continue;
			result += addAttackByLeftHpAffector.value;
		}
		if (result == 0.0f)
			return 0.0f;

		return result * (1.0f - hpRate);
	}
}