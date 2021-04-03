using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class AddAttackRangeAffector : AffectorBase
{
	float _endTime;
	GameObject _startEffectPrefab;
	GameObject _startEffectObject;
	Transform _loopEffectTransform;

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

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue4) == false)
		{
			_startEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);

			if (_startEffectPrefab != null)
				_startEffectObject = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
		}

		// loop effect
		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue3) == false)
		{
			GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (loopEffectPrefab != null)
			{
				_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, _actor.cachedTransform).transform;
				FollowTransform.Follow(_loopEffectTransform, _actor.cachedTransform, Vector3.zero);
			}
		}

		// 매프레임 Get할때마다 어펙터 검색하는게 느릴거 같아서 차라리 캐싱하는 쪽에다가 덧셈할 값을 알려주기로 한다.
		if (_actor.IsPlayerActor())
		{
			PlayerActor playerActor = _actor as PlayerActor;
			if (playerActor != null)
				playerActor.playerAI.addAttackRange = affectorValueLevelTableData.fValue2;
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (_startEffectObject != null)
			_startEffectObject.SetActive(false);

		if (_startEffectPrefab != null)
			_startEffectObject = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public override void FinalizeAffector()
	{
		if (_actor.IsPlayerActor())
		{
			PlayerActor playerActor = _actor as PlayerActor;
			if (playerActor != null)
				playerActor.playerAI.addAttackRange = 0.0f;
		}

		if (_loopEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_loopEffectTransform);
			_loopEffectTransform = null;
		}
	}
}