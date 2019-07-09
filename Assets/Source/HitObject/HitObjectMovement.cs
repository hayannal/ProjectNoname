using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitObjectMovement : MonoBehaviour {

	public enum eMovementType
	{
		UseVelocity,
		FollowTarget,
	}

	public enum eStartDirectionType
	{
		Forward,
		Direction,
		RangeDirection,
		ToTarget,
	}

	MeHitObject _signal;
	Rigidbody _rigidbody;

	Transform _followTargetTransform;
	float _currentCurve;


	public void InitializeSignal(MeHitObject meHit, Transform parentTransform, Rigidbody rigidbody)
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

		switch(_signal.startDirectionType)
		{
		case eStartDirectionType.Forward:
			_rigidbody.velocity = parentTransform.forward * _signal.speed;
			break;
		}
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
				Vector3 diffDir = _followTargetTransform.position - transform.position;
				if (_signal.curveLockY)
				{
					transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(diffDir), _currentCurve * Time.fixedDeltaTime);
				}
				else
				{
					Vector3 newDir = Vector3.RotateTowards(transform.forward, diffDir, _currentCurve * Time.fixedDeltaTime, 0.0f);
					transform.rotation = Quaternion.LookRotation(newDir);
				}
				_rigidbody.velocity = transform.forward * _signal.speed;
			}
			break;
		}
	}
}
