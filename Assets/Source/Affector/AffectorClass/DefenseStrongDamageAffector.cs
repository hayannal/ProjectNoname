using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DefenseStrongDamageAffector : AffectorBase
{
	float _endTime;

	float _percent;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_percent = affectorValueLevelTableData.fValue2;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	float OnDamage(float damage)
	{
		float limit = _actor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxHp) * _percent;
		if (damage > limit)
		{
			damage = limit;
			FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.DefenseStrongDamage, _actor);
		}
		return damage;
	}
	
	public static float OnDamage(AffectorProcessor affectorProcessor, float damage)
	{
		DefenseStrongDamageAffector defenseStrongDamageAffector = (DefenseStrongDamageAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.DefenseStrongDamage);
		if (defenseStrongDamageAffector == null)
			return damage;
		return defenseStrongDamageAffector.OnDamage(damage);
	}
}