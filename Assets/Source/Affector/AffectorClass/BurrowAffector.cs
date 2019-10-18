using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MEC;
using MecanimStateDefine;
using DG.Tweening;

public class BurrowAffector : AffectorBase
{
	public static float s_BurrowPositionY = -5.0f;
	const float BurrowAnimationPositionY = -2.0f;
	const string BurrowStartStateName = "BurrowStart";
	const string BurrowEndStateName = "BurrowEnd";

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
			return;

		_affectorValueLevelTableData = affectorValueLevelTableData;

		_actor.EnableAI(false);
		_actor.actionController.idleAnimator.enabled = false;
		_actor.baseCharacterController.movement.useGravity = false;
		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(BurrowStartStateName), 0.05f);
		Timing.RunCoroutine(BurrowOnProcess());
	}

	IEnumerator<float> BurrowOnProcess()
	{
		if (this == null)
			yield break;

		// 시그널에서 제어하기로 한다.
		//HitObject.EnableRigidbodyAndCollider(false, _actor.GetRigidbody(), _actor.GetCollider());
		//_actor.cachedTransform.DOLocalMoveY(BurrowAnimationPositionY, TweenDuration).SetEase(Ease.Linear);

		// 버로우 대기. 시간으로 체크하기엔 시그널의 길이가 얼마나 될지 몰라서 이렇게 높이와 collider 상태로 구분하기로 한다.
		while (true)
		{
			if (this == null)
				yield break;

			if (Mathf.Abs(_actor.cachedTransform.position.y - BurrowAnimationPositionY) < 0.01f && _actor.GetCollider().enabled == true)
				break;
			yield return Timing.WaitForOneFrame;
		}
		_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, s_BurrowPositionY, _actor.cachedTransform.position.z);

		// loop effect
		if (!string.IsNullOrEmpty(_affectorValueLevelTableData.sValue3))
		{			
			
		}

		_remainAttackCount = _affectorValueLevelTableData.iValue2;
		_attackDelayRemainTime = _affectorValueLevelTableData.fValue3;

#if UNITY_EDITOR
		if (_remainAttackCount == 0) Debug.LogErrorFormat("BurrowAffector Invalid Data : iValue2 is zero");
		if (_affectorValueLevelTableData.fValue1 == 0.0f) Debug.LogErrorFormat("BurrowAffector Invalid Data : fValue1 is zero");
		if (_affectorValueLevelTableData.fValue3 == 0.0f) Debug.LogErrorFormat("BurrowAffector Invalid Data : fValue3 is zero");
		if (_affectorValueLevelTableData.fValue4 == 0.0f) Debug.LogErrorFormat("BurrowAffector Invalid Data : fValue4 is zero");
		if (string.IsNullOrEmpty(_affectorValueLevelTableData.sValue3)) Debug.LogErrorFormat("BurrowAffector Invalid Data : sValue3 is empty");
#endif
	}

	public override void UpdateAffector()
	{
		UpdateBurrowAttack();
	}

	float _burrowEndRemainTime;
	int _remainAttackCount;
	float _attackDelayRemainTime;
	void UpdateBurrowAttack()
	{
		if (_actor.actorStatus.IsDie())
			return;

		if (_attackDelayRemainTime > 0.0f)
		{
			_attackDelayRemainTime -= Time.deltaTime;
			if (_attackDelayRemainTime <= 0.0f)
			{
				_remainAttackCount -= 1;
				if (!string.IsNullOrEmpty(_affectorValueLevelTableData.sValue4))
					_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue4), 0.05f);
				if (_remainAttackCount <= 0)
				{
					// 마지막 공격하고 나서 바로 올라오면 어색하니 조금 대기탔다가 올라온다.
					_attackDelayRemainTime = 0.0f;
					_burrowEndRemainTime = _affectorValueLevelTableData.fValue4;
				}
				else
				{
					_attackDelayRemainTime += _affectorValueLevelTableData.fValue1;
				}
			}
		}

		if (_burrowEndRemainTime > 0.0f)
		{
			_burrowEndRemainTime -= Time.deltaTime;
			if (_burrowEndRemainTime <= 0.0f)
			{
				_burrowEndRemainTime = 0.0f;
				_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, BurrowAnimationPositionY, _actor.cachedTransform.position.z);
				_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(BurrowEndStateName), 0.05f);
				Timing.RunCoroutine(BurrowOffProcess());
			}
		}
	}

	IEnumerator<float> BurrowOffProcess()
	{
		// 착지 대기
		while (true)
		{
			if (this == null)
				yield break;

			if (Mathf.Abs(_actor.cachedTransform.position.y) < 0.01f && _actor.GetCollider().enabled == true)
				break;
			yield return Timing.WaitForOneFrame;
		}

		_actor.baseCharacterController.movement.useGravity = true;
		_actor.actionController.idleAnimator.enabled = true;
		_actor.EnableAI(true);
		finalized = true;
	}

	bool CheckDie()
	{
		return false;
	}

	public static bool CheckDie(AffectorProcessor affectorProcessor)
	{
		BurrowAffector burrowAffector = (BurrowAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Burrow);
		if (burrowAffector == null)
			return false;

		return burrowAffector.CheckDie();
	}
}