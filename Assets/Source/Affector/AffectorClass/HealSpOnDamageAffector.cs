using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class HealSpOnDamageAffector : AffectorBase
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
	
	public static void OnDamage(AffectorProcessor affectorProcessor, bool byBossMonsterActor)
	{
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.actorStatus.IsDie())
			return;
		List<AffectorBase> listHealSpOnDamageAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.HealSpOnDamage);
		if (listHealSpOnDamageAffector == null)
			return;

		float value = 0.0f;
		for (int i = 0; i < listHealSpOnDamageAffector.Count; ++i)
		{
			if (listHealSpOnDamageAffector[i].finalized)
				continue;
			HealSpOnDamageAffector healSpOnDamageAffector = listHealSpOnDamageAffector[i] as HealSpOnDamageAffector;
			if (healSpOnDamageAffector == null)
				continue;
			if (healSpOnDamageAffector._affectorValueLevelTableData.iValue1 == 1 && byBossMonsterActor == false)
				continue;
			value += healSpOnDamageAffector._affectorValueLevelTableData.fValue2;
		}
		if (value == 0.0f)
			return;

		affectorProcessor.actor.actorStatus.AddSP(affectorProcessor.actor.actorStatus.GetValue(eActorStatus.MaxSp) * value);
	}
}