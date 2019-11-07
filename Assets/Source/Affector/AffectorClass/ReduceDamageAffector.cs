using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReduceDamageAffector : AffectorBase
{
	float _endTime;

	public enum eReduceDamageType
	{
		Collider,
		Crash,
	}

	AffectorValueLevelTableData _affectorValueLevelTableData;
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

		_affectorValueLevelTableData = affectorValueLevelTableData;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	float GetValue(eReduceDamageType eType)
	{
		switch (eType)
		{
			case eReduceDamageType.Collider:
				return _affectorValueLevelTableData.fValue2;
			case eReduceDamageType.Crash:
				return _affectorValueLevelTableData.fValue3;
		}
		return 0.0f;
	}

	public static float GetValue(AffectorProcessor affectorProcessor, eReduceDamageType eType)
	{
		List<AffectorBase> listReduceDamageAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ReduceDamage);
		if (listReduceDamageAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listReduceDamageAffector.Count; ++i)
		{
			if (listReduceDamageAffector[i].finalized)
				continue;
			ReduceDamageAffector reduceDamageAffector = listReduceDamageAffector[i] as ReduceDamageAffector;
			if (reduceDamageAffector == null)
				continue;
			result += reduceDamageAffector.GetValue(eType);
		}
		return result;
	}
}