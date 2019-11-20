using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class CertainHpHitObjectAffector : AffectorBase
{
	AffectorValueLevelTableData _affectorValueLevelTableData;
	float[] _hpRatioList;
	float[] _bossHpRatioList;
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
		_hpRatioList = BattleInstanceManager.instance.GetCachedMultiHitDamageRatioList(affectorValueLevelTableData.sValue3);
		_bossHpRatioList = BattleInstanceManager.instance.GetCachedMultiHitDamageRatioList(affectorValueLevelTableData.sValue4);
	}

	void OnEvent(AffectorProcessor defenderAffectorProcessor, float hpRatio)
	{
		if (_actor == null)
			return;

		if (_affectorValueLevelTableData.iValue2 == 1 && BurrowAffector.CheckBurrow(defenderAffectorProcessor))
			return;
		if (_affectorValueLevelTableData.iValue2 == 1 && defenderAffectorProcessor.actor.actionController.mecanimState.IsState((int)eMecanimState.DontDie))
			return;

		float[] hpRatioList = _hpRatioList;
		if (defenderAffectorProcessor.actor.IsMonsterActor())
		{
			MonsterActor monsterActor = defenderAffectorProcessor.actor as MonsterActor;
			if (monsterActor != null && monsterActor.bossMonster)
				hpRatioList = _bossHpRatioList;
		}

		// 이 어펙터의 가장 큰 특징은 일정 hp구간이하로 내려갈때마다 확률을 굴려서 어펙터밸류를 콜하는건데,
		// 이걸 수행하기 위해선 해당 액터에 마지막으로 호출했던 certainHp를 기억시켜놔야한다.
		// 그런데 이게 여러개의 어펙터밸류가 동시에 동작할 수 있기 때문에, 아이디별로 certainHp를 따로 기억시켜야한다.
		float lastHpRatio = 1.0f;
		if (DefaultContainerAffector.ContainsValue(_actor.affectorProcessor, _affectorValueLevelTableData.affectorValueId))
			lastHpRatio = DefaultContainerAffector.GetFloatValue2(_actor.affectorProcessor, _affectorValueLevelTableData.affectorValueId);

		bool result = false;
		for (int i = 0; i < hpRatioList.Length; ++i)
		{
			if (lastHpRatio <= hpRatioList[i])
				continue;

			if (hpRatio <= hpRatioList[i])
			{
				result = true;
				AffectorValueLevelTableData affectorValueLevelTableData = new AffectorValueLevelTableData();
				// OverrideAffector가 제대로 호출되기 위해서 임시 아이디를 지정해줘야한다.
				affectorValueLevelTableData.affectorValueId = string.Format("_generatedId_{0}", _affectorValueLevelTableData.affectorValueId);
				affectorValueLevelTableData.fValue1 = -1.0f;
				affectorValueLevelTableData.fValue2 = hpRatio;
				affectorValueLevelTableData.sValue1 = _affectorValueLevelTableData.affectorValueId;
				_actor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.DefaultContainer, affectorValueLevelTableData, _actor, false);
				break;
			}
		}
		if (result == false)
			return;

		// 이게 통과되면 확률검사를 한다.
		if (_affectorValueLevelTableData.fValue2 > 0.0f && Random.value <= _affectorValueLevelTableData.fValue2)
		{
			HitParameter hitParameter = new HitParameter();
			hitParameter.statusBase = _actor.actorStatus.statusBase;
			hitParameter.statusStructForHitObject.skillLevel = _affectorValueLevelTableData.level;
			SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _actor);

			defenderAffectorProcessor.ApplyAffectorValue(_affectorValueLevelTableData.sValue2, hitParameter, false);
		}
	}

	public static void OnEvent(AffectorProcessor affectorProcessor, AffectorProcessor defenderAffectorProcessor, float hpRatio)
	{
		List<AffectorBase> listCertainHpHitObjectAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.CertainHpHitObject);
		if (listCertainHpHitObjectAffector == null)
			return;

		for (int i = 0; i < listCertainHpHitObjectAffector.Count; ++i)
		{
			if (listCertainHpHitObjectAffector[i].finalized)
				continue;
			CertainHpHitObjectAffector certainHpHitObjectAffector = listCertainHpHitObjectAffector[i] as CertainHpHitObjectAffector;
			if (certainHpHitObjectAffector == null)
				continue;
			certainHpHitObjectAffector.OnEvent(defenderAffectorProcessor, hpRatio);
		}
	}
}