using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CannotActionAffector : AffectorBase
{
	float _endTime;
	float _shakeStartTime;

	const float PingPongSpeed = 3.0f;
	const float PingPongMove = 0.15f;

	bool _applied = false;
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

		if (PositionFlag.IsInRange(_affectorProcessor, "ignoreCannotAction"))
		{
			finalized = true;
			return;
		}

		if (_affectorProcessor.IsContinuousAffectorType(eAffectorType.RemoveCannotAction))
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		SavePrevSpeed(_actor.actionController.animator.speed);
		_actor.actionController.animator.speed = 0.0f;
		_defaultAnimatorLocalPosition = _actor.actionController.cachedAnimatorTransform.localPosition;
		_actor.actorStatus.OnChangedStatus(ActorStatusDefine.eActorStatus.MoveSpeed);
		_shakeStartTime = Time.time;
		_applied = true;
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
			_actor.actorStatus.OnChangedStatus(ActorStatusDefine.eActorStatus.MoveSpeed);
			return;
		}

		if (_affectorProcessor.IsContinuousAffectorType(eAffectorType.RemoveCannotAction))
		{
			finalized = true;
			_actor.actorStatus.OnChangedStatus(ActorStatusDefine.eActorStatus.MoveSpeed);
			return;
		}

		UpdateShake();
	}

	public override void FinalizeAffector()
	{
		if (_applied == false)
			return;

		_actor.actionController.cachedAnimatorTransform.localPosition = _defaultAnimatorLocalPosition;
		RestorePrevSpeed(_actor.actionController.animator);
		_applied = false;
	}

	Vector3 _defaultAnimatorLocalPosition;
	void UpdateShake()
	{
		float delta = Time.time - _shakeStartTime;
		delta *= PingPongSpeed;
		float pingPong = Mathf.PingPong(delta, PingPongMove);
		pingPong -= PingPongMove * 0.5f;
		_actor.actionController.cachedAnimatorTransform.position = _actor.cachedTransform.position + new Vector3(pingPong, 0.0f, 0.0f);
		_actor.actionController.cachedAnimatorTransform.localPosition += _defaultAnimatorLocalPosition;
	}

	// 여러개의 CannotActionAffector가 적용될때를 대비해서 적용되기 전의 스피드를 static에다가 저장해두고 refCount로 관리하기로 한다.
	static int s_refCount = 0;
	static float s_prevSpeed = 0.0f;
	public static void SavePrevSpeed(float speed)
	{
		if (s_refCount == 0)
			s_prevSpeed = speed;

		++s_refCount;
	}

	public static void RestorePrevSpeed(Animator animator)
	{
		--s_refCount;
		if (s_refCount == 0)
		{
			animator.speed = s_prevSpeed;
			s_prevSpeed = 0.0f;
		}
	}
}