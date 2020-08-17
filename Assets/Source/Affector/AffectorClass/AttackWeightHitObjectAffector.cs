﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;
using ActorStatusDefine;

public class AttackWeightHitObjectAffector : AffectorBase
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

	void OnEvent(AffectorProcessor defenderAffectorProcessor, float attackWeight)
	{
		if (_actor == null)
			return;

		if (_affectorValueLevelTableData.iValue2 == 1 && BurrowAffector.CheckBurrow(defenderAffectorProcessor))
			return;
		if (_affectorValueLevelTableData.iValue2 == 1 && BurrowOnStartAffector.CheckBurrow(defenderAffectorProcessor))
			return;
		if (_affectorValueLevelTableData.iValue2 == 1 && defenderAffectorProcessor.actor.actionController.mecanimState.IsState((int)eMecanimState.DontDie))
			return;

		// 공격 횟수에 상관없이 동일한 확률을 가진다면 이렇게 안하고 affectorValueIdList 에다가 직접 임의의 어펙터를 넣었어도 된다. 아니면 AddAffectorHitObject 사용하든가.
		// 그러나 이렇게 하면 다단히트 캐릭터일수록 확률이 높아지기 때문에 밸런싱을 맞출 수가 없게 된다.
		// 그래서 attackWeight값을 얻어와서 곱한 뒤 Random체크해서 사용하는거로 바꾸게 되었다.
		float baseRate = _affectorValueLevelTableData.fValue2;
		if (defenderAffectorProcessor.actor.IsMonsterActor())
		{
			MonsterActor monsterActor = defenderAffectorProcessor.actor as MonsterActor;
			if (monsterActor != null && monsterActor.bossMonster)
				baseRate = _affectorValueLevelTableData.fValue3;
		}
		float callRate = baseRate * attackWeight;
		if (callRate > 1.0f) callRate = 1.0f;
		if (callRate > 0.0f && Random.value <= callRate)
		{
			HitParameter hitParameter = new HitParameter();
			hitParameter.statusBase = new StatusBase();
			_actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
			SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _actor);
			hitParameter.statusStructForHitObject.skillLevel = _affectorValueLevelTableData.level;

			defenderAffectorProcessor.ApplyAffectorValue(_affectorValueLevelTableData.sValue2, hitParameter, false);
		}
	}

	public static void OnEvent(AffectorProcessor affectorProcessor, AffectorProcessor defenderAffectorProcessor, float attackWeight)
	{
		List<AffectorBase> listAttackWeightHitObjectAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AttackWeightHitObject);
		if (listAttackWeightHitObjectAffector == null)
			return;

		for (int i = 0; i < listAttackWeightHitObjectAffector.Count; ++i)
		{
			if (listAttackWeightHitObjectAffector[i].finalized)
				continue;
			AttackWeightHitObjectAffector attackWeightHitObjectAffector = listAttackWeightHitObjectAffector[i] as AttackWeightHitObjectAffector;
			if (attackWeightHitObjectAffector == null)
				continue;
			attackWeightHitObjectAffector.OnEvent(defenderAffectorProcessor, attackWeight);
		}
	}
}