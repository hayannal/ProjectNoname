using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DelayedBaseDamageAffector : AffectorBase
{
	float _endTime;

	Actor _affackerActor;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			finalized = true;
			return;
		}
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;
		_affackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);

		// lifeTime - 평소와 달리 fValue2를 사용
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue2);

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
			_damageEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
	}

	List<float> _listStackedDamageTime;
	int _stackedDamageTimeProcessedIndex = -1;
	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 이미 있는 상태에서 온다면 Stack형태로 관리
		if (_listStackedDamageTime == null)
			_listStackedDamageTime = new List<float>();

		// 기존 endTime을 리스트에 넣어두고
		_listStackedDamageTime.Add(_endTime);

		// lifeTime 갱신
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue2);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		if (_listStackedDamageTime != null)
		{
			for (int i = _stackedDamageTimeProcessedIndex + 1; i < _listStackedDamageTime.Count; ++i)
			{
				if (Time.time > _listStackedDamageTime[i])
				{
					CallBaseDamageAffector();
					_stackedDamageTimeProcessedIndex = i;
				}
				else
					break;
			}
		}
	}

	public override void FinalizeAffector()
	{
		CallBaseDamageAffector();
	}

	GameObject _damageEffectPrefab;
	AffectorValueLevelTableData _baseDamageAffectorValue;
	void CallBaseDamageAffector()
	{
		// 죽는 순간에 걸린거라면 어차피 데미지 입힐게 없으므로 패스.
		if (_affectorValueLevelTableData == null || _actor.actorStatus.IsDie())
			return;

		if (_baseDamageAffectorValue == null)
		{
			_baseDamageAffectorValue = new AffectorValueLevelTableData();
			_baseDamageAffectorValue.fValue1 = _affectorValueLevelTableData.fValue1;			
		}
		_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.BaseDamage, _baseDamageAffectorValue, _affackerActor, false);
		HitRimBlink.ShowHitRimBlink(_affectorProcessor.cachedTransform, _affectorProcessor.cachedTransform.forward);

		if (_damageEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_damageEffectPrefab, _affectorProcessor.cachedTransform.position, Quaternion.identity);
	}
}