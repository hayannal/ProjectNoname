using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReduceContinuousDamageAffector : AffectorBase
{
	float _endTime;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	float _reduceDamageTime;
	void OnDamage()
	{
		_reduceDamageTime = Time.time + _affectorValueLevelTableData.fValue3;
	}

	public static void OnDamage(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listReduceContinuousDamageAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ReduceContinuousDamage);
		if (listReduceContinuousDamageAffector == null)
			return;

		for (int i = 0; i < listReduceContinuousDamageAffector.Count; ++i)
		{
			if (listReduceContinuousDamageAffector[i].finalized)
				continue;
			ReduceContinuousDamageAffector reduceContinuousDamageAffector = listReduceContinuousDamageAffector[i] as ReduceContinuousDamageAffector;
			if (reduceContinuousDamageAffector == null)
				continue;
			reduceContinuousDamageAffector.OnDamage();
		}
	}

	float GetValue()
	{
		if (Time.time < _reduceDamageTime)
			return _affectorValueLevelTableData.fValue2;
		return 0.0f;
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listReduceContinuousDamageAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ReduceContinuousDamage);
		if (listReduceContinuousDamageAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listReduceContinuousDamageAffector.Count; ++i)
		{
			if (listReduceContinuousDamageAffector[i].finalized)
				continue;
			ReduceContinuousDamageAffector reduceContinuousDamageAffector = listReduceContinuousDamageAffector[i] as ReduceContinuousDamageAffector;
			if (reduceContinuousDamageAffector == null)
				continue;
			result += reduceContinuousDamageAffector.GetValue();
		}
		return result;
	}
}