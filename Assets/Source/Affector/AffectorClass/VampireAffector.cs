using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class VampireAffector : AffectorBase
{
	float _endTime;
	static float s_f3Constant = 35.0f;
	static float s_f4Constant = 30.0f;

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

	public static void OnKill(AffectorProcessor affectorProcessor)
	{
		if (affectorProcessor.actor == null)
			return;
		List<AffectorBase> listVampireAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.Vampire);
		if (listVampireAffector == null)
			return;

		float value = 0.0f;
		for (int i = 0; i < listVampireAffector.Count; ++i)
		{
			if (listVampireAffector[i].finalized)
				continue;
			VampireAffector vampireAffector = listVampireAffector[i] as VampireAffector;
			if (vampireAffector == null)
				continue;
			value += vampireAffector._affectorValueLevelTableData.fValue3;
		}
		if (value == 0.0f)
			return;

		float ratio = value / (1.0f + value) / s_f3Constant;
		float vampire = affectorProcessor.actor.actorStatus.GetValue(eActorStatus.MaxHp) * ratio;
		affectorProcessor.actor.actorStatus.AddHP(vampire);
	}

	public static void OnHit(AffectorProcessor affectorProcessor, float damage)
	{
		if (affectorProcessor.actor == null)
			return;
		List<AffectorBase> listVampireAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.Vampire);
		if (listVampireAffector == null)
			return;

		float value = 0.0f;
		for (int i = 0; i < listVampireAffector.Count; ++i)
		{
			if (listVampireAffector[i].finalized)
				continue;
			VampireAffector vampireAffector = listVampireAffector[i] as VampireAffector;
			if (vampireAffector == null)
				continue;
			value += vampireAffector._affectorValueLevelTableData.fValue4;
		}
		if (value == 0.0f)
			return;

		float ratio = value / (1.0f + value) / s_f4Constant;
		affectorProcessor.actor.actorStatus.AddHP(damage * ratio);
	}
}