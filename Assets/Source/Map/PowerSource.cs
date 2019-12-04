using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using ActorStatusDefine;

public class PowerSource : MonoBehaviour
{
	public static string Index2Address(int powerSource)
	{
		switch (powerSource)
		{
			case 0: return "PowerSourceMagic";
			case 1: return "PowerSourceMachine";
			case 2: return "PowerSourceNature";
			case 3: return "PowerSourceQigong";
		}
		return "";
	}

	public static string Index2Name(int powerSource)
	{
		switch (powerSource)
		{
			case 0: return UIString.instance.GetString("GameUI_Magic");
			case 1: return UIString.instance.GetString("GameUI_Machine");
			case 2: return UIString.instance.GetString("GameUI_Nature");
			case 3: return UIString.instance.GetString("GameUI_Qigong");
		}
		return "";
	}

	void OnEnable()
	{
		_spawnedGatePillar = false;
	}

	bool _spawnedGatePillar;
	void OnCollisionEnter(Collision collision)
	{
		if (_spawnedGatePillar)
			return;

		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;
			OnTriggerEnter(col);
			break;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (_spawnedGatePillar)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
		healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("PowerSourceHeal");
		healAffectorValue.fValue3 += affectorProcessor.actor.actorStatus.GetValue(eActorStatus.PowerSourceHealRate);
		affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, affectorProcessor.actor, false);
		affectorProcessor.actor.actorStatus.AddSP(affectorProcessor.actor.actorStatus.GetValue(eActorStatus.MaxSp) * BattleInstanceManager.instance.GetCachedGlobalConstantFloat("PowerSourceSpHeal"));

		BattleManager.instance.OnClearStage();
		_spawnedGatePillar = true;

		Timing.RunCoroutine(ScreenHealEffectProcess());
	}

	IEnumerator<float> ScreenHealEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.3f, 0.95f);
		yield return Timing.WaitForSeconds(0.3f);

		if (this == null)
			yield break;

		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("PowerSourceUI_Heal"), 2.5f);
		FadeCanvas.instance.FadeIn(1.75f);
	}
}
