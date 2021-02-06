using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AddAttackByContinuousKillAffector : AffectorBase
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

	float GetAddAttack()
	{
		return _value * _continuousKillCount;
	}

	int _continuousKillCount = 0;
	void OnDamage()
	{
		_continuousKillCount = 0;
	}

	void OnKill()
	{
		_continuousKillCount += 1;
	}

	public static void OnKill(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddAttackByContinuousKillAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByContinuousKill);
		if (listAddAttackByContinuousKillAffector == null)
			return;

		for (int i = 0; i < listAddAttackByContinuousKillAffector.Count; ++i)
		{
			if (listAddAttackByContinuousKillAffector[i].finalized)
				continue;
			AddAttackByContinuousKillAffector addAttackByContinuousKillAffector = listAddAttackByContinuousKillAffector[i] as AddAttackByContinuousKillAffector;
			if (addAttackByContinuousKillAffector == null)
				continue;
			addAttackByContinuousKillAffector.OnKill();
		}
	}

	public static void OnDamage(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddAttackByContinuousKillAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByContinuousKill);
		if (listAddAttackByContinuousKillAffector == null)
			return;

		for (int i = 0; i < listAddAttackByContinuousKillAffector.Count; ++i)
		{
			if (listAddAttackByContinuousKillAffector[i].finalized)
				continue;
			AddAttackByContinuousKillAffector addAttackByContinuousKillAffector = listAddAttackByContinuousKillAffector[i] as AddAttackByContinuousKillAffector;
			if (addAttackByContinuousKillAffector == null)
				continue;
			addAttackByContinuousKillAffector.OnDamage();
		}
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddAttackByContinuousKillAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByContinuousKill);
		if (listAddAttackByContinuousKillAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listAddAttackByContinuousKillAffector.Count; ++i)
		{
			if (listAddAttackByContinuousKillAffector[i].finalized)
				continue;
			AddAttackByContinuousKillAffector addAttackByContinuousKillAffector = listAddAttackByContinuousKillAffector[i] as AddAttackByContinuousKillAffector;
			if (addAttackByContinuousKillAffector == null)
				continue;
			result += addAttackByContinuousKillAffector.GetAddAttack();
		}
		return result;
	}
}