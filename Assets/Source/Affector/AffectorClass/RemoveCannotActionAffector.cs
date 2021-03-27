using UnityEngine;
using System.Collections;
using DG.Tweening;

public class RemoveCannotActionAffector : AffectorBase
{
	float _endTime;
	GameObject _startAttachEffectPrefab;
	Transform _startAttachEffectTransform;
	GameObject _startEffectPrefab;
	Transform _startEffectTransform;
	Transform _loopEffectTransform;

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

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// loop effect
		GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
		if (loopEffectPrefab != null)
			_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation).transform;

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
			_startEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
		if (_startEffectPrefab != null)
			_startEffectTransform = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity).transform;

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue2))
			_startAttachEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue2);
		if (_startAttachEffectPrefab != null)
			_startAttachEffectTransform = BattleInstanceManager.instance.GetCachedObject(_startAttachEffectPrefab, _actor.cachedTransform.position, Quaternion.identity).transform;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (_startEffectTransform != null)
			_startEffectTransform.gameObject.SetActive(false);
		if (_startEffectPrefab != null)
			_startEffectTransform = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity).transform;

		if (_startAttachEffectTransform != null)
			_startAttachEffectTransform.gameObject.SetActive(false);
		if (_startAttachEffectPrefab != null)
			_startAttachEffectTransform = BattleInstanceManager.instance.GetCachedObject(_startAttachEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, _actor.cachedTransform).transform;
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
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			_loopEffectTransform.gameObject.SetActive(false);
			_loopEffectTransform = null;
		}
	}
}