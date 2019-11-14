using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using ActorStatusDefine;

public class PowerSource : MonoBehaviour
{
	public static string Index2Name(int powerSource)
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

		BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
		_spawnedGatePillar = true;

		Timing.RunCoroutine(ScreenHealEffectProcess());
	}

	IEnumerator<float> ScreenHealEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.3f, 0.95f);
		yield return Timing.WaitForSeconds(0.3f);

		if (this == null)
			yield break;

		FadeCanvas.instance.FadeIn(1.75f);
	}
}
