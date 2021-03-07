using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChargingActionAffector : AffectorBase
{
	float _targetDamage;

	float _endTime;
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

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_affectorValueLevelTableData = affectorValueLevelTableData;

		_targetDamage = _actor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxHp) * affectorValueLevelTableData.fValue2;

		UIInstanceManager.instance.ShowCanvasAsync("BossMonsterChargingCanvas", () =>
		{
			BossMonsterChargingCanvas.instance.RefreshGauge(1.0f, false);
		});
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 들어올 일이 없어야한다.
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated ChargingActionAffector.");
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

		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		{
			// 행동불가일때 _endTime을 해당 시간만큼 늘려놔야 게이지 줄어드는걸 막을 수 있다.
			if (_endTime > 0.0f)
				_endTime += Time.deltaTime;
			return;
		}

		// 보스가 쓸테니 텔레포트는 따로 처리 안해도 되려나?
	}

	public override void FinalizeAffector()
	{
		if (BossMonsterChargingCanvas.instance != null && BossMonsterChargingCanvas.instance.gameObject.activeSelf)
			BossMonsterChargingCanvas.instance.gameObject.SetActive(false);

		if (_actor.actorStatus.IsDie())
			return;
		if (_breaked)
			return;

		if (string.IsNullOrEmpty(_affectorValueLevelTableData.sValue1))
			return;

		// 일정량의 데미지를 못채우면 차징 모션으로 넘어간다. 어차피 액션단에서 루프액션 써서 하고있을테니 idleAnimator는 건드리지 않아도 될거같다.
		//_actor.actionController.idleAnimator.enabled = false;
		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);
	}

	float _sumDamage;
	bool _breaked = false;
	void OnDamage(float damage)
	{
		_sumDamage += damage;

		if (BossMonsterChargingCanvas.instance != null && BossMonsterChargingCanvas.instance.gameObject.activeSelf)
		{
			float ratio = 1.0f - _sumDamage / _targetDamage;
			if (ratio > 1.0f) ratio = 1.0f;
			BossMonsterChargingCanvas.instance.RefreshGauge(ratio, true);
		}

		if (_sumDamage > _targetDamage)
		{
			_breaked = true;
			_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue2), 0.05f);
			finalized = true;
		}
	}

	public static void OnDamage(AffectorProcessor affectorProcessor, float damage)
	{
		ChargingActionAffector chargingActionAffector = (ChargingActionAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChargingAction);
		if (chargingActionAffector == null)
			return;

		chargingActionAffector.OnDamage(damage);
	}
}