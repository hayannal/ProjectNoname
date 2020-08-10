using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MEC;

public class MonsterSleepingAffector : AffectorBase
{
	float _endTime;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		_affectorValueLevelTableData = affectorValueLevelTableData;

		_actor.EnableAI(false);
		_actor.actionController.idleAnimator.enabled = false;
		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 원래라면 절대 들어오지 말아야하는데 슬립중에 중복
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated Sleep Affector.");
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
		{
			WakeUp(_affectorValueLevelTableData.sValue3);
			return;
		}
	}

	void WakeUp(string wakeUpStateName)
	{
		if (string.IsNullOrEmpty(wakeUpStateName))
		{
			_actor.actionController.idleAnimator.enabled = true;
			_actor.actionController.PlayActionByActionName("Idle");
			_actor.EnableAI(true);
			finalized = true;
		}
		else
		{
			// Sleep 풀리는 애니가 있을 경우엔 Sleep을 풀어주는 애니가 메카님 상에서 Idle로 연결되어있을테니 Sleep 풀어주는 애니를 실행하기만 하면 된다.
			// 이후 Idle로 넘어오는걸 체크해서 활성화 해주면 된다.
			_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(wakeUpStateName), 0.05f);
			Timing.RunCoroutine(WaitIdleState());
			finalized = true;
		}
	}

	IEnumerator<float> WaitIdleState()
	{
		while (true)
		{
			if (_affectorValueLevelTableData == null || _actor == null || _actor.gameObject == null || _actor.actorStatus.IsDie())
				yield break;

			if (_actor.actionController.mecanimState.IsState((int)MecanimStateDefine.eMecanimState.Idle))
				break;

			yield return Timing.WaitForOneFrame;
		}

		_actor.actionController.idleAnimator.enabled = true;
		_actor.actionController.PlayActionByActionName("Idle");
		_actor.EnableAI(true);
		yield break;
	}

	void OnDamage()
	{
		if (_actor.actorStatus.IsDie())
			return;

		WakeUp(_affectorValueLevelTableData.sValue2);
	}

	public static void OnDamage(AffectorProcessor affectorProcessor)
	{
		MonsterSleepingAffector monsterSleepingAffector = (MonsterSleepingAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.MonsterSleeping);
		if (monsterSleepingAffector == null)
			return;
		monsterSleepingAffector.OnDamage();
	}

	public static float GetDamageValue(AffectorProcessor affectorProcessor)
	{
		MonsterSleepingAffector monsterSleepingAffector = (MonsterSleepingAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.MonsterSleeping);
		if (monsterSleepingAffector == null)
			return 0.0f;
		return monsterSleepingAffector._affectorValueLevelTableData.fValue2;
	}
}