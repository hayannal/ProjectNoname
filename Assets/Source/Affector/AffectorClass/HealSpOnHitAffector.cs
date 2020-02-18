using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class HealSpOnHitAffector : AffectorBase
{
	float _endTime;
	static float s_cooltimeValueA = 5.0f;
	static float s_cooltimeValueB = 2.0f;
	static float s_probValueA = 0.0f;
	static float s_probValueB = 0.03f;
	static float s_amountValueA = 17.65f;

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

	static string s_generatedId = "_generatedId_HealSpOnHit";
	public static void OnHit(AffectorProcessor affectorProcessor)
	{
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.actorStatus.IsDie())
			return;
		List<AffectorBase> listHealSpOnHitAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.HealSpOnHit);
		if (listHealSpOnHitAffector == null)
			return;

		// 먼저 쿨타임 중인지를 판단해야한다.
		if (DefaultContainerAffector.ContainsValue(affectorProcessor, eAffectorType.HealSpOnHit.ToString()))
			return;

		float cooltimeValue = 0.0f;
		float rateValue = 0.0f;
		for (int i = 0; i < listHealSpOnHitAffector.Count; ++i)
		{
			if (listHealSpOnHitAffector[i].finalized)
				continue;
			HealSpOnHitAffector healSpOnHitAffector = listHealSpOnHitAffector[i] as HealSpOnHitAffector;
			if (healSpOnHitAffector == null)
				continue;
			cooltimeValue += healSpOnHitAffector._affectorValueLevelTableData.fValue2;
			rateValue += healSpOnHitAffector._affectorValueLevelTableData.fValue3;
		}
		if (cooltimeValue == 0.0f || rateValue == 0.0f)
			return;

		float cooltime = s_cooltimeValueA / (s_cooltimeValueB + cooltimeValue);
		float rate = s_probValueA + rateValue * s_probValueB;

		if (Random.value > rate)
			return;
		affectorProcessor.actor.actorStatus.AddSP(s_amountValueA);

		FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.HealSpOnAttack, affectorProcessor.actor);

		// 쿨타임 등록
		AffectorValueLevelTableData affectorValueLevelTableData = new AffectorValueLevelTableData();
		// OverrideAffector가 제대로 호출되기 위해서 임시 아이디를 지정해줘야한다.
		affectorValueLevelTableData.affectorValueId = s_generatedId;
		affectorValueLevelTableData.fValue1 = cooltime;
		affectorValueLevelTableData.sValue1 = eAffectorType.HealSpOnHit.ToString();
		affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.DefaultContainer, affectorValueLevelTableData, affectorProcessor.actor, false);
	}
}