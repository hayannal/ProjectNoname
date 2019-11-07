using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class HealOverTimeAffector : AffectorBase
{
	float _endTime;
	float _remainTickTime;
	float _hitParameterDamage;

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

		_remainTickTime = affectorValueLevelTableData.fValue2;

		if (affectorValueLevelTableData.fValue4 > 0.0f)
			_hitParameterDamage = hitParameter.statusStructForHitObject.damage;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_remainTickTime = affectorValueLevelTableData.fValue2;

		if (affectorValueLevelTableData.fValue4 > 0.0f)
			_hitParameterDamage = hitParameter.statusStructForHitObject.damage;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		_remainTickTime -= Time.deltaTime;
		if (_remainTickTime < 0.0f)
		{
			_remainTickTime += _affectorValueLevelTableData.fValue2;

			float heal = 0.0f;
			if (_affectorValueLevelTableData.fValue3 > 0.0f)
				heal += (_actor.actorStatus.GetValue(eActorStatus.MaxHp) * _affectorValueLevelTableData.fValue3);
			if (_affectorValueLevelTableData.fValue4 > 0.0f)
				heal += (_hitParameterDamage * _affectorValueLevelTableData.fValue4);

			_actor.actorStatus.AddHP(heal);
		}
	}
}