using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;
using ActorStatusDefine;

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
		if (BattleInstanceManager.instance.GetActiveTeleportedCount() > 0)
		{
			int remainSpawnedMonsterCount = BattleManager.instance.GetSpawnedMonsterCount() - BattleInstanceManager.instance.GetActiveTeleportedCount();
			if (remainSpawnedMonsterCount == 0)
				BattleInstanceManager.instance.RestoreFirstTeleportedObject();
		}
	}

	void OnEvent(AffectorProcessor defenderAffectorProcessor)
	{
		if (_actor == null)
			return;

		// 해당 스테이지의 마지막 몹이면 호출하지 말아야한다.
		if (BattleManager.instance != null)
		{
			int remainSpawnedMonsterCount = BattleManager.instance.GetSpawnedMonsterCount() - BattleInstanceManager.instance.GetActiveTeleportedCount();
			if (remainSpawnedMonsterCount <= 1)
				return;
		}

		MonsterActor defenderMonsterActor = null;
		bool bossMonster = false;
		if (defenderAffectorProcessor.actor.IsMonsterActor())
			defenderMonsterActor = defenderAffectorProcessor.actor as MonsterActor;
		if (defenderMonsterActor != null && defenderMonsterActor.bossMonster)
			bossMonster = true;

		// Start Delay가 긴 락워리어 같은 몹들이 잠들어있을땐 면역되서 텔레포트 되면 안된다. 혹시 툴에서 가만 스폰시킨 세워둔(AI끈) 락워리어일 경우는 제외하기로 한다. monsterAI enabled 체크.
		if (defenderMonsterActor != null && defenderMonsterActor.monsterAI.IsLongStartDelaying() && defenderMonsterActor.monsterAI.enabled)
			return;

		//if (BattleInstanceManager.instance.GetActiveTeleportedCount() >= _affectorValueLevelTableData.iValue1)
		//	return;
		if (bossMonster == false && BattleInstanceManager.instance.GetActiveTeleportedCountByType(false) >= _affectorValueLevelTableData.iValue1)
			return;
		if (bossMonster && BattleInstanceManager.instance.GetActiveTeleportedCountByType(true) >= _affectorValueLevelTableData.iValue3)
			return;

		float limitHp = bossMonster ? limitHp = _affectorValueLevelTableData.fValue4 : _affectorValueLevelTableData.fValue3;
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
			hitParameter.statusBase = new StatusBase();
			_actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
			SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _actor);
			hitParameter.statusStructForHitObject.skillLevel = _affectorValueLevelTableData.level;

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