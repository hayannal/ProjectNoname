using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AddSpGainByHpAffector : AffectorBase
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
		_type = affectorValueLevelTableData.iValue1;
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

	int _type;
	float GetAddSpGain()
	{
		switch (_type)
		{
			case 0:
				return _value * (1.0f - _actor.actorStatus.GetHPRatio());
			case 1:
				if (_actor.actorStatus.GetHPRatio() >= 1.0f)
					return _value;
				break;
			//case 2:
				// AddAttackByHpAffector와 달리 2 타입을 사용하지 않는다.
				//break;
		}
		return 0.0f;
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddSpGainByHpAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddSpGainByHp);
		if (listAddSpGainByHpAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listAddSpGainByHpAffector.Count; ++i)
		{
			if (listAddSpGainByHpAffector[i].finalized)
				continue;
			AddSpGainByHpAffector addSpGainByHpAffector = listAddSpGainByHpAffector[i] as AddSpGainByHpAffector;
			if (addSpGainByHpAffector == null)
				continue;
			result += addSpGainByHpAffector.GetAddSpGain();
		}
		return result;
	}
}