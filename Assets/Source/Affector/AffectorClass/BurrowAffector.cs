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

	Transform _scrollTransform;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			// fValue1을 지속시간으로 쓰고있는 대부분의 어펙터들은 Die 체크 후 그냥 리턴해도 무방하지만(지속시간 셋팅이 안되서 바로 finalize된다.)
			// 이 버로우 어펙터는 지속시간이 없기 때문에 빈 어펙터로 죽지않고 남게된다.
			// 꼭 finalize 해줘야한다.
			finalized = true;
			return;
		}

		_affectorValueLevelTableData = affectorValueLevelTableData;

		_actor.EnableAI(false);
		_actor.actionController.idleAnimator.enabled = false;
		_actor.baseCharacterController.movement.useGravity = false;
		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);
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

			// 복합적인 문제가 터졌다.
			// MEC 를 안쓰고 Update문에서 처리했다면,
			// 혹은 버로우 시작지점부터 컬리더를 꺼서 연타 공격을 허용하지 않았다면,
			// 혹은 MEC 대신에 진짜 코루틴을 썼더라면(AffectorBase는 Mono가 아니라서 불가능 하지만, Mono였다면 .. 코루틴 썼을지도)
			// 혹은 몬스터를 재사용하지 않았다면,
			// 문제가 발생하지 않았을텐데 아무튼, 상황은 이렇다.
			//
			// 연타 공격 중 첫번째 공격을 맞고 BurrowOnProcess 코루틴을 시작시켜놨는데
			// 연타 공격 중 두번째 공격을 맞고 죽는 애니가 나오면서 이 while문에 갇혀버린 것이다.(정해진 높이에 도달이 안됨)
			// AffectorProcessor의 continuous List에서는 삭제된 상태지만 자기 혼자 돌고있었던 것이다. 진짜 코루틴이었다면 알아서 중지됐을듯.
			// 이 상황에서
			// 몹을 리스폰하면서 재사용했고, 첫 공격을 맞아서 BurrowOnProcess이 발생했을때
			// 아까 남아있던 코루틴이 실행되면서 액터를 -5 위치로 옮겨버리고 자기는 더이상 물고있는 곳이 없으니 삭제되버리는 것이었다.
			// 그리고 정작 AffectorProcessor에 새로 들어온 버로우 어펙터는
			// 아까 그 남아있던 코루틴이 캐릭터를 -5 위치로 옮겼기때문에 이 while문을 탈출하지 못한채 기다리고 있는 것이었다.
			//
			// 모든 조건들이 맞아떨어지면서 이런 문제가 생긴건데
			// 결국 가장 중요한건 MEC의 사용법대로
			// gameObject 꺼질때를 대비해서 gameObject가 null이거나 activeSelf체크를 수동으로 해줘야한다는거다!!!!!!!
			// gameObject 꺼질때 자동으로 멈추는 옵션도 있긴 하나 결국 진짜 코루틴과 마찬가지로 오버헤드가 생기기 때문에 별로다.
			// 그러니 꼭 gameObject가 꺼질걸 대비해서 아래 코드를 수행해야한다!!!!!!!
			//
			// Update문에서 처리해도 되지만 코드가 지저분해지고, 일정시간 대기하는덴 불편하다. 그러니 MEC 사용법대로 잘 쓰자..
			if (_affectorProcessor.gameObject == null || _affectorProcessor.gameObject.activeSelf == false)
				yield break;

			// 애니메이션을 보면서 작업해야하는거라 시그널로 뺀건데 실수하면 매우 큰 버그가 나는 현상을 찾았다.
			// 아래 break 조건문을 기다리는 동안 두개의 레인지 시그널이 돌고있는데
			// 하나는 DisableActorCollider고 하나는 MovePositionCurve다.
			// 그런데 MovePositionCurve가 조금이라도 더 늦게까지 호출되서 위치를 바꿔버리면
			// 버로우 상태를 위해 -5로 옮겨둔 위치가 다시 -2쪽으로 이동되게 된다. (땅 뒤집는 이펙트가 허공에 나오게 된다.)
			// MovePositionCurve 시그널에서 버로우 상태일땐 셋팅 안하는 방법과
			// 버로우 상태일때 매프레임 -5로 셋팅하는 방법 두가지가 있는데, 둘다 맘에 들지 않는다.
			// 우선은 레인지 시그널의 길이를 똑같이 설정해서 동시에 끝나도록 처리할텐데 더 좋은 방법이 있는지 고민해보자.
			if (Mathf.Abs(_actor.cachedTransform.position.y - BurrowAnimationPositionY) < 0.01f && _actor.GetCollider().enabled == true)
				break;
			yield return Timing.WaitForOneFrame;
		}
		_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, s_BurrowPositionY, _actor.cachedTransform.position.z);

		// loop effect
		GameObject scrollPrefab = FindPreloadObject(_affectorValueLevelTableData.sValue3);
		if (scrollPrefab != null)
		{
			Vector3 scrollPosition = _actor.cachedTransform.position;
			scrollPosition.y = 0.0f;
			_scrollTransform = BattleInstanceManager.instance.GetCachedObject(scrollPrefab, scrollPosition, Quaternion.identity).transform;
			_scrollTransform.DOLocalJump(new Vector3(_actor.cachedTransform.position.x, 0.0f, _actor.cachedTransform.position.z), 1.0f, 1, 0.5f).SetEase(Ease.Linear);
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
				if (_scrollTransform != null)
				{
					_scrollTransform.gameObject.SetActive(false);
					_scrollTransform = null;
				}
				_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, BurrowAnimationPositionY, _actor.cachedTransform.position.z);
				_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue2), 0.05f);
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
			if (_affectorProcessor.gameObject == null || _affectorProcessor.gameObject.activeSelf == false)
				yield break;

			if (Mathf.Abs(_actor.cachedTransform.position.y) < 0.01f && _actor.GetCollider().enabled == true)
				break;
			yield return Timing.WaitForOneFrame;
		}

		_actor.baseCharacterController.movement.useGravity = true;
		_actor.actionController.idleAnimator.enabled = true;
		_actor.actionController.PlayActionByActionName("Idle");
		_actor.EnableAI(true);
		finalized = true;
	}

	bool CheckBurrow()
	{
		// 내려가있는지를 체크. 올라오거나 내려갈땐 false를 리턴.
		if (_scrollTransform != null && _scrollTransform.gameObject.activeSelf)
			return true;
		return false;
	}

	bool CheckDie()
	{
		if (CheckBurrow() == false)
			return false;

		DieDissolve.ShowDieDissolve(_scrollTransform, false);
		DieAshParticle.ShowParticle(_scrollTransform, false, 0.5f);

		// Restore
		_actor.baseCharacterController.movement.useGravity = true;
		_actor.actionController.idleAnimator.enabled = true;
		_actor.EnableAI(true);

		_actor.gameObject.SetActive(false);
		return true;
	}

	public static bool CheckBurrow(AffectorProcessor affectorProcessor)
	{
		BurrowAffector burrowAffector = (BurrowAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Burrow);
		if (burrowAffector == null)
			return false;

		return burrowAffector.CheckBurrow();
	}

	public static bool CheckDie(AffectorProcessor affectorProcessor)
	{
		BurrowAffector burrowAffector = (BurrowAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Burrow);
		if (burrowAffector == null)
			return false;

		return burrowAffector.CheckDie();
	}
}