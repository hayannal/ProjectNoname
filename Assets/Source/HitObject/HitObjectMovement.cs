using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitObjectMovement : MonoBehaviour {

	public enum eMovementType
	{
		Direct,
		FollowTarget,
		//Howitzer,
	}

	public enum eStartDirectionType
	{
		Forward,
		Direction,
		ToFirstTarget,
		ToMultiTarget,
	}

	MeHitObject _signal;
	Rigidbody _rigidbody;

	Transform _followTargetTransform;
	float _currentCurve;

	public void InitializeSignal(MeHitObject meHit, Actor parentActor, Rigidbody rigidbody, int hitSignalIndexInAction)
	{
		_signal = meHit;
		_rigidbody = rigidbody;
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

		switch(_signal.movementType)
		{
		case eMovementType.FollowTarget:
			_currentCurve = _signal.curve;

			//TargetSystem targetSystem = parentTransform.GetComponent<TargetSystem>();

			// temp code
			GameObject tempObject = GameObject.Find("GoblinB(Clone)");
			if (tempObject != null) _followTargetTransform = tempObject.transform;
			break;
		}

		Vector3 targetPosition = Vector3.zero;
		if (_signal.startDirectionType == eStartDirectionType.ToFirstTarget || _signal.startDirectionType == eStartDirectionType.ToMultiTarget)
		{
			int targetIndex = -1;
			if (_signal.startDirectionType == eStartDirectionType.ToFirstTarget)
				targetIndex = 0;
			else if (_signal.startDirectionType == eStartDirectionType.ToMultiTarget)
				targetIndex = hitSignalIndexInAction;

			TargetingProcessor targetingProcessor = parentActor.targetingProcessor;
			if (targetingProcessor.IsRegisteredCustomTargetPosition())
				targetPosition = targetingProcessor.GetCustomTargetPosition(targetIndex);
			else if (targetingProcessor.GetTarget() != null)
				targetPosition = targetingProcessor.GetTargetPosition(targetIndex);
			else
				targetPosition = GetFallbackTargetPosition(parentActor.cachedTransform);
		}

		_rigidbody.velocity = GetStartDirection(meHit, cachedTransform.position, parentActor.cachedTransform, hitSignalIndexInAction, targetPosition) * _signal.speed;
		cachedTransform.forward = _rigidbody.velocity.normalized;
	}

	public static Vector3 GetFallbackTargetPosition(Transform t)
	{
		Vector3 fallbackPosition = new Vector3(0.0f, 0.0f, 4.0f);
		return t.TransformPoint(fallbackPosition);
	}

	public static Vector3 GetStartDirection(MeHitObject meHit, Vector3 spawnPosition, Transform parentActorTransform, int hitSignalIndexInAction, Vector3 targetPosition, bool applyRange = true)
	{
		Vector3 result = Vector3.zero;
		switch (meHit.startDirectionType)
		{
			case eStartDirectionType.Forward:
				result = Vector3.forward;
				break;
			case eStartDirectionType.Direction:
				result = meHit.startDirection;
				break;
			case eStartDirectionType.ToFirstTarget:
			case eStartDirectionType.ToMultiTarget:
				Vector3 diffToTargetPosition = targetPosition - spawnPosition;
				// 땅에 쏘는 직사를 구현할땐 이 라인을 패스하면 된다.
				diffToTargetPosition.y = 0.0f;
				// world to local
				result = parentActorTransform.InverseTransformDirection(diffToTargetPosition.normalized);
				break;
		}
		if (applyRange)
		{
			bool needNormalize = false;
			if (meHit.startDirectionOffsetRange.x != 0.0f) { result.x += Random.Range(-meHit.startDirectionOffsetRange.x, meHit.startDirectionOffsetRange.x); needNormalize = true; }
			if (meHit.startDirectionOffsetRange.y != 0.0f) { result.y += Random.Range(-meHit.startDirectionOffsetRange.y, meHit.startDirectionOffsetRange.y); needNormalize = true; }
			if (meHit.startDirectionOffsetRange.z != 0.0f) { result.z += Random.Range(-meHit.startDirectionOffsetRange.z, meHit.startDirectionOffsetRange.z); needNormalize = true; }
			if (needNormalize) result = result.normalized;
		}
		return parentActorTransform.TransformDirection(result);
	}

	void Update()
	{
		switch(_signal.movementType)
		{
		case eMovementType.FollowTarget:
			_currentCurve += Time.deltaTime * _signal.curveAdd;
			break;
		}
	}

	void FixedUpdate()
	{
		switch(_signal.movementType)
		{
		case eMovementType.FollowTarget:
			if (_followTargetTransform != null)
			{
				Vector3 diffDir = _followTargetTransform.position - cachedTransform.position;
				if (_signal.curveLockY)
				{
					cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, Quaternion.LookRotation(diffDir), _currentCurve * Time.fixedDeltaTime);
				}
				else
				{
					Vector3 newDir = Vector3.RotateTowards(cachedTransform.forward, diffDir, _currentCurve * Time.fixedDeltaTime, 0.0f);
					cachedTransform.rotation = Quaternion.LookRotation(newDir);
				}
				_rigidbody.velocity = cachedTransform.forward * _signal.speed;
			}
			break;
		}
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
