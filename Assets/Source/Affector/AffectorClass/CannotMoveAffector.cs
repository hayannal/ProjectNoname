using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CannotMoveAffector : AffectorBase
{
	float _endTime;
	Transform _loopEffectTransform;

	const float ScaleStartDuration = 0.4f;
	const float ScaleEndDuration = 0.2f;

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
		{
			_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation).transform;
			_loopEffectTransform.localScale = Vector3.zero;
			_loopEffectTransform.DOScale(1.0f, ScaleStartDuration).SetEase(Ease.OutQuad);
		}

		if (_actor.IsMonsterActor())
		{
			MonsterActor monsterActor = _actor as MonsterActor;
			if (monsterActor != null)
				monsterActor.AdjustMass(10.0f);
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
			_loopEffectTransform.DOScale(new Vector3(1.0f, 0.0f, 1.0f), ScaleEndDuration).SetEase(Ease.InQuad).onComplete = () => {
				_loopEffectTransform.gameObject.SetActive(false);
				_loopEffectTransform = null;
			};
		}

		if (_actor.IsMonsterActor())
		{
			MonsterActor monsterActor = _actor as MonsterActor;
			if (monsterActor != null)
				monsterActor.ResetAdjustMass();
		}
	}

	public override void DisableAffector()
	{
		// 몹을 죽지도 않았는데 Disable시킬 일은 없을거 같지만
		// 플레이어한테 걸릴걸 대비해서 미리 해둔다.
		FinalizeAffector();

		// 근데 이러면 다시 켜질때 이펙트 도로 생성해야하지 않나.
		// Disable말고 EnableAffector도 만들어야하나?
		// 우선 아직은 필요없는 상황이니 고려만 해둔다.
	}
}