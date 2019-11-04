using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class ChangeActorStatusAffector : AffectorBase
{
	float _endTime;
	eActorStatus _eType;
	float _value;
	int _onDamageRemainCount;

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

		_eType = (eActorStatus)affectorValueLevelTableData.iValue1;
		_value = affectorValueLevelTableData.fValue2;
		_onDamageRemainCount = affectorValueLevelTableData.iValue2;

		_actor.actorStatus.OnChangedStatus(_eType);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
		{
			_actor.actorStatus.OnChangedStatus(_eType);
			return;
		}
	}

	void OnDamage()
	{
		if (_onDamageRemainCount > 0)
		{
			_onDamageRemainCount -= 1;
			if (_onDamageRemainCount == 0)
			{
				finalized = true;
				_actor.actorStatus.OnChangedStatus(_eType);
			}
		}
	}

	public static void OnDamage(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listCallAffectorValueAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ChangeActorStatus);
		if (listCallAffectorValueAffector == null)
			return;

		for (int i = 0; i < listCallAffectorValueAffector.Count; ++i)
		{
			if (listCallAffectorValueAffector[i].finalized)
				continue;
			ChangeActorStatusAffector changeActorStatusAffector = listCallAffectorValueAffector[i] as ChangeActorStatusAffector;
			if (changeActorStatusAffector == null)
				continue;
			changeActorStatusAffector.OnDamage();
		}
	}

	float GetValue(eActorStatus eType)
	{
		if (_eType == eType)
			return _value;
		return 0.0f;
	}

	public static float GetValue(AffectorProcessor affectorProcessor, eActorStatus eType)
	{
		List<AffectorBase> listCallAffectorValueAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ChangeActorStatus);
		if (listCallAffectorValueAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listCallAffectorValueAffector.Count; ++i)
		{
			if (listCallAffectorValueAffector[i].finalized)
				continue;
			ChangeActorStatusAffector changeActorStatusAffector = listCallAffectorValueAffector[i] as ChangeActorStatusAffector;
			if (changeActorStatusAffector == null)
				continue;
			result += changeActorStatusAffector.GetValue(eType);
		}
		return result;
	}
}