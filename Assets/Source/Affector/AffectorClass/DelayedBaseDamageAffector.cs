using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MEC;

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
					Timing.RunCoroutine(CallProcess());
					_stackedDamageTimeProcessedIndex = i;
				}
				else
					break;
			}
		}
	}

	IEnumerator<float> CallProcess()
	{
		yield return Timing.WaitForOneFrame;
		CallBaseDamageAffector();
	}

	public override void FinalizeAffector()
	{
		Timing.RunCoroutine(CallProcess());
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
			_baseDamageAffectorValue.iValue3 = _affectorValueLevelTableData.iValue3;
		}
		_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.BaseDamage, _baseDamageAffectorValue, _affackerActor, false);
		HitRimBlink.ShowHitRimBlink(_affectorProcessor.cachedTransform, _affectorProcessor.cachedTransform.forward);

		if (_damageEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_damageEffectPrefab, _affectorProcessor.cachedTransform.position, Quaternion.identity);


		// 한가지 예외처리 할게 생겼다.
		// 다른 케이스에서는 괜찮은데 하필 TeleportedAffector가 붙어있는 상태에서 죽을 경우
		// 되돌려주는 처리를 수동으로 해야했는데
		// 이 검사를 MonsterActor의 OnDie함수에서 체크하는게 맞아보이나
		// 그럼 굳이 평소에도 계속해서 TeleportedAffector가 붙어있는지를 체크해야하는 부하가 생겨버린다.
		//
		// 그래서 차라리 여기에서만 체크하기로 한다.
		if (_actor.actorStatus.IsDie())
		{
			TeleportedAffector teleportedAffector = (TeleportedAffector)_affectorProcessor.GetFirstContinuousAffector(eAffectorType.Teleported);
			if (teleportedAffector != null)
				teleportedAffector.FinalizeAffector();
		}
	}
}