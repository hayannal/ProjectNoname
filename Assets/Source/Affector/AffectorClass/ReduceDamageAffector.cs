using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReduceDamageAffector : AffectorBase
{
	public enum eReduceDamageType
	{
		Melee,
		Collider,
		Crash,
		Trap,
	}

	bool _useEndTime;
	float _endTime;
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

		_affectorValueLevelTableData = affectorValueLevelTableData;

		// fValue1이 보통 시간인데 자리가 없어서 iValue1 에다가 0.001f 곱한걸 시간으로 쓰기로 한다. ms단위. 0이면 무제한.
		if (affectorValueLevelTableData.iValue1 > 0)
		{
			_useEndTime = true;
			_endTime = CalcEndTime(affectorValueLevelTableData.iValue1 * 0.001f);
		}
	}

	public override void UpdateAffector()
	{
		if (_useEndTime == false)
			return;

		if (CheckEndTime(_endTime) == false)
			return;
	}

	float GetValue(eReduceDamageType eType)
	{
		switch (eType)
		{
			case eReduceDamageType.Melee:
				return _affectorValueLevelTableData.fValue1;
			case eReduceDamageType.Collider:
				return _affectorValueLevelTableData.fValue2;
			case eReduceDamageType.Crash:
				return _affectorValueLevelTableData.fValue3;
			case eReduceDamageType.Trap:
				return _affectorValueLevelTableData.fValue4;
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