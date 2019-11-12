using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class TeleportingHitObjectAffector : AffectorBase
{
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
	}

	public override void UpdateAffector()
	{
		if (TeleportedAffector.GetActiveCount() > 0)
		{
			int remainSpawnedMonsterCount = BattleManager.instance.GetSpawnedMonsterCount() - TeleportedAffector.GetActiveCount();
			if (remainSpawnedMonsterCount == 0)
				TeleportedAffector.RestoreFirstObject();
		}
	}

	void OnEvent(AffectorProcessor defenderAffectorProcessor)
	{
		if (_actor == null)
			return;

		// 해당 스테이지의 마지막 몹이면 호출하지 말아야한다.
		if (BattleManager.instance != null)
		{
			int remainSpawnedMonsterCount = BattleManager.instance.GetSpawnedMonsterCount() - TeleportedAffector.GetActiveCount();
			if (remainSpawnedMonsterCount <= 1)
				return;
		}

		if (TeleportedAffector.GetActiveCount() >= _affectorValueLevelTableData.iValue1)
			return;

		float limitHp = _affectorValueLevelTableData.fValue3;
		if (defenderAffectorProcessor.actor is MonsterActor)
		{
			MonsterActor monsterActor = defenderAffectorProcessor.actor as MonsterActor;
			if (monsterActor != null && monsterActor.bossMonster)
				limitHp = _affectorValueLevelTableData.fValue4;
		}
		if (defenderAffectorProcessor.actor.actorStatus.GetHPRatio() < limitHp)
			return;

		if (_affectorValueLevelTableData.iValue2 == 1 && BurrowAffector.CheckBurrow(defenderAffectorProcessor))
			return;
		if (_affectorValueLevelTableData.iValue2 == 1 && defenderAffectorProcessor.actor.actionController.mecanimState.IsState((int)eMecanimState.DontDie))
			return;


		// 모든게 통과되면 확률검사를 한다.
		if (_affectorValueLevelTableData.fValue2 > 0.0f && Random.value <= _affectorValueLevelTableData.fValue2)
		{
			HitParameter hitParameter = new HitParameter();
			hitParameter.statusBase = _actor.actorStatus.statusBase;
			hitParameter.statusStructForHitObject.skillLevel = _affectorValueLevelTableData.level;
			SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _actor);

			defenderAffectorProcessor.ApplyAffectorValue(_affectorValueLevelTableData.sValue2, hitParameter, false);
		}
	}

	public static void OnEvent(AffectorProcessor affectorProcessor, AffectorProcessor defenderAffectorProcessor)
	{
		TeleportingHitObjectAffector teleportingHitObjectAffector = (TeleportingHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.TeleportingHitObject);
		if (teleportingHitObjectAffector == null)
			return;
		teleportingHitObjectAffector.OnEvent(defenderAffectorProcessor);
	}
}