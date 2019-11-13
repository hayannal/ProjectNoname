using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CannotMoveAffector : AffectorBase
{
	float _endTime;
	Transform _loopEffectTransform;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
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
		{
			_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation).transform;
			_loopEffectTransform.localScale = Vector3.zero;
			_loopEffectTransform.DOScale(1.0f, 0.4f).SetEase(Ease.OutQuad);
		}

		_actor.actorStatus.OnChangedStatus(ActorStatusDefine.eActorStatus.MoveSpeed);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
		{
			_actor.actorStatus.OnChangedStatus(ActorStatusDefine.eActorStatus.MoveSpeed);
			return;
		}

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		if (_loopEffectTransform != null)
			_loopEffectTransform.position = _actor.cachedTransform.position;
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			_loopEffectTransform.DOScale(0.0f, 0.4f).SetEase(Ease.OutQuad).onComplete = () => {
				_loopEffectTransform.gameObject.SetActive(false);
				_loopEffectTransform = null;
			};
		}
	}
}