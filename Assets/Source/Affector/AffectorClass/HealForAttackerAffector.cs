using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;
using MEC;

public class HealForAttackerAffector : AffectorBase
{
	static string s_generatedId = "_generatedId_HealForAttacker";

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;

		Actor attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
		if (attackerActor == null)
			return;

		// 먼저 쿨타임 중인지를 판단해야한다.
		if (DefaultContainerAffector.ContainsValue(attackerActor.affectorProcessor, eAffectorType.HealForAttacker.ToString()))
			return;

		float heal = 0.0f;
		if (affectorValueLevelTableData.iValue1 == 0)
		{
			if (affectorValueLevelTableData.fValue3 > 0.0f)
				heal += (attackerActor.actorStatus.GetValue(eActorStatus.MaxHp) * affectorValueLevelTableData.fValue3);

			attackerActor.actorStatus.AddHP(heal);
		}
		else if (affectorValueLevelTableData.iValue1 == 1)
		{
			if (affectorValueLevelTableData.fValue3 > 0.0f)
				heal += (attackerActor.actorStatus.GetValue(eActorStatus.MaxSp) * affectorValueLevelTableData.fValue3);

			attackerActor.actorStatus.AddSP(heal);
		}


		// 쿨타임 등록
		AffectorValueLevelTableData cooltimeAffectorValueLevelTableData = new AffectorValueLevelTableData();
		// OverrideAffector가 제대로 호출되기 위해서 임시 아이디를 지정해줘야한다.
		cooltimeAffectorValueLevelTableData.affectorValueId = s_generatedId;
		cooltimeAffectorValueLevelTableData.fValue1 = affectorValueLevelTableData.fValue2;
		cooltimeAffectorValueLevelTableData.sValue1 = eAffectorType.HealForAttacker.ToString();
		attackerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.DefaultContainer, cooltimeAffectorValueLevelTableData, attackerActor, false);

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue4) == false)
		{
			GameObject onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
			if (onStartEffectPrefab != null)
			{
				Transform onStartEffectObject = BattleInstanceManager.instance.GetCachedObject(onStartEffectPrefab, attackerActor.cachedTransform.position, Quaternion.identity).transform;
				FollowTransform.Follow(onStartEffectObject, attackerActor.cachedTransform, Vector3.zero);
				Timing.RunCoroutine(ScreenHealEffectProcess());
			}
		}
	}

	IEnumerator<float> ScreenHealEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.6f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		FadeCanvas.instance.FadeIn(1.0f);
	}
}