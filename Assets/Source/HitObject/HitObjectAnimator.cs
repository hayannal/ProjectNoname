using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitObjectAnimator : MonoBehaviour
{
	public Actor parentActor { get; private set; }
	public float parentHitObjectCreateTime { get; private set; }
	Animator _animator;

	const string ByLifeTimeStateName = "ByLifeTime";
	const string ByDistanceStateName = "ByDistance";
	const string OnCollisionStateName = "OnCollision";
	const string OnCollisionPlaneStateName = "OnCollisionPlane";
	bool _hasByLifeTimeState = false;
	bool _hasByDistanceState = false;
	bool _hasOnCollisionState = false;
	bool _hasOnCollisionPlaneState = false;
	void Start()
	{
		if (_animator == null)
			return;

		int stateHash = BattleInstanceManager.instance.GetActionNameHash(ByLifeTimeStateName);
		_hasByLifeTimeState = _animator.HasState(0, stateHash);

		stateHash = BattleInstanceManager.instance.GetActionNameHash(ByDistanceStateName);
		_hasByDistanceState = _animator.HasState(0, stateHash);

		stateHash = BattleInstanceManager.instance.GetActionNameHash(OnCollisionStateName);
		_hasOnCollisionState = _animator.HasState(0, stateHash);

		stateHash = BattleInstanceManager.instance.GetActionNameHash(OnCollisionPlaneStateName);
		_hasOnCollisionPlaneState = _animator.HasState(0, stateHash);
	}

	bool _played = false;
	public void InitializeSignal(Actor actor, Animator animator, float createTime)
	{
		parentActor = actor;
		parentHitObjectCreateTime = createTime;
		_animator = animator;
		_animator.Play(BattleInstanceManager.instance.GetActionNameHash("Standby"), 0, 0.0f);
		_played = false;
	}

	public bool OnFinalizeByLifeTime()
	{
		if (!_hasByLifeTimeState)
			return false;
		if (_played)
			return false;

		_animator.Play(BattleInstanceManager.instance.GetActionNameHash(ByLifeTimeStateName), 0, 0.0f);
		_played = true;
		return true;
	}

	public bool OnFinalizeByDistance()
	{
		if (!_hasByDistanceState)
			return false;
		if (_played)
			return false;

		_animator.Play(BattleInstanceManager.instance.GetActionNameHash(ByDistanceStateName), 0, 0.0f);
		_played = true;
		return true;
	}

	public bool OnFinalizeByCollision()
	{
		if (!_hasOnCollisionState)
			return false;
		if (_played)
			return false;

		_animator.Play(BattleInstanceManager.instance.GetActionNameHash(OnCollisionStateName), 0, 0.0f);
		_played = true;
		return true;
	}

	public bool OnFinalizeByCollisionPlane()
	{
		if (!_hasOnCollisionPlaneState)
			return false;
		if (_played)
			return false;

		_animator.Play(BattleInstanceManager.instance.GetActionNameHash(OnCollisionPlaneStateName), 0, 0.0f);
		_played = true;
		return true;
	}



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
