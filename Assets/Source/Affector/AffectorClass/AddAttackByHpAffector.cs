using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AddAttackByHpAffector : AffectorBase
{
	float _endTime;
	float _value;
	float value { get { return _value; } }
	float _fValue3;

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
		_fValue3 = affectorValueLevelTableData.fValue3;
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
	float GetAddAttack()
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
				// 여기서 처리하지 않는다.
				//break;
		}
		return 0.0f;
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddAttackByHpAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByHp);
		if (listAddAttackByHpAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listAddAttackByHpAffector.Count; ++i)
		{
			if (listAddAttackByHpAffector[i].finalized)
				continue;
			AddAttackByHpAffector addAttackByHpAffector = listAddAttackByHpAffector[i] as AddAttackByHpAffector;
			if (addAttackByHpAffector == null)
				continue;
			result += addAttackByHpAffector.GetAddAttack();
		}
		return result;
	}

	public static float GetValueType2(AffectorProcessor affectorProcessor, float defenderActorHpRatio)
	{
		List<AffectorBase> listAddAttackByHpAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByHp);
		if (listAddAttackByHpAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listAddAttackByHpAffector.Count; ++i)
		{
			if (listAddAttackByHpAffector[i].finalized)
				continue;
			AddAttackByHpAffector addAttackByHpAffector = listAddAttackByHpAffector[i] as AddAttackByHpAffector;
			if (addAttackByHpAffector == null)
				continue;
			if (addAttackByHpAffector._type != 2)
				continue;
			if (defenderActorHpRatio < addAttackByHpAffector._fValue3)
				continue;
			result += addAttackByHpAffector.value;
		}
		return result;
	}
}