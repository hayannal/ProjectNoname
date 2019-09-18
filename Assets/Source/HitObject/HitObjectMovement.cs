using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitObjectMovement : MonoBehaviour {

	public enum eMovementType
	{
		Direct,
		FollowTarget,
		//Howitzer,
		//Homing
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
		_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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

		_velocity = _rigidbody.velocity = GetStartDirection(meHit, cachedTransform.position, parentActor.cachedTransform, hitSignalIndexInAction, targetPosition) * _signal.speed;
		_forward = cachedTransform.forward = _rigidbody.velocity.normalized;
	}

	Vector3 _velocity;
	Vector3 _forward;
	public void ReinitializeForThrough()
	{
		_rigidbody.velocity = _velocity;
		_rigidbody.angularVelocity = Vector3.zero;
		cachedTransform.forward = _forward;
	}

	public void Bounce(Vector3 wallNormal)
	{
		_velocity = Vector3.Reflect(_velocity, wallNormal);
		_rigidbody.velocity = _velocity;
		_rigidbody.angularVelocity = Vector3.zero;
		_forward = cachedTransform.forward = _rigidbody.velocity.normalized;
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
				result = meHit.startDirection.normalized;
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
			if (meHit.leftRightRandomAngle != 0.0f || meHit.upDownRandomAngle != 0.0f || meHit.leftRandomAngle != 0.0f || meHit.rightRandomAngle != 0.0f)
			{
				Vector3 tempUp = Vector3.up;
				if (result == tempUp) tempUp = -Vector3.forward;
				Vector3 right = Vector3.Cross(-tempUp, result);
				Vector3 up = Vector3.Cross(right, result);

				if (meHit.bothRandomAngle)
				{
					if (meHit.leftRightRandomAngle != 0.0f)
					{
						Quaternion rotation = Quaternion.AngleAxis(Random.Range(-meHit.leftRightRandomAngle, meHit.leftRightRandomAngle), up);
						result = rotation * result;
					}
				}
				else
				{
					if (meHit.leftRandomAngle != 0.0f || meHit.rightRandomAngle != 0.0f)
					{
						Quaternion rotation = Quaternion.AngleAxis(Random.Range(-meHit.leftRandomAngle, meHit.rightRandomAngle), up);
						result = rotation * result;
					}
				}
				if (meHit.upDownRandomAngle != 0.0f)
				{
					Quaternion rotation = Quaternion.AngleAxis(Random.Range(-meHit.upDownRandomAngle, meHit.upDownRandomAngle), right);
					result = rotation * result;
				}
			}
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

	#region Ricochet
	const float RicochetRange = 4.0f;
	List<Collider> _listRicochet = null;
	public void AddRicochet(Collider collider, bool initialize)
	{
		if (_listRicochet == null)
			_listRicochet = new List<Collider>();

		if (initialize)
			_listRicochet.Clear();

		_listRicochet.Add(collider);
	}
	
	public bool IsEnableRicochet(int teamId)
	{
		Vector3 position = cachedTransform.position;
		Collider[] result = Physics.OverlapSphere(position, RicochetRange); // range * _transform.localScale.x
		float nearestDistance = float.MaxValue;
		Collider nearestCollider = null;
		float containsNearestDistance = float.MaxValue;
		Collider containsNearestCollider = null;
		for (int i = 0; i < result.Length; ++i)
		{
			// affector processor
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(result[i]);
			if (affectorProcessor == null)
				continue;

			// team check
			if (!Team.CheckTeamFilter(teamId, result[i], Team.eTeamCheckFilter.Enemy, false))
				continue;

			// hp
			Actor actor = affectorProcessor.actor;
			if (actor != null)
			{
				if (actor.actorStatus.IsDie())
					continue;
			}

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(result[i]);
			if (colliderRadius == -1.0f) continue;

			// 등록되어있는 개체라면 다르게 처리해야한다.
			bool contains = _listRicochet.Contains(result[i]);

			// distance
			Vector3 diff = BattleInstanceManager.instance.GetTransformFromCollider(result[i]).position - position;
			diff.y = 0.0f;
			float distance = diff.magnitude - colliderRadius;

			if (contains)
			{
				// contains를 체크할땐 마지막으로 등록된 타겟은 제외해야한다.
				if (distance < containsNearestDistance && _listRicochet[_listRicochet.Count - 1] != result[i])
				{
					containsNearestDistance = distance;
					containsNearestCollider = result[i];
				}
			}
			else
			{
				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					nearestCollider = result[i];
				}
			}
		}

		// 리스트에 등록되지 않은 몹들중에 가장 가까운 적으로 리코세한다.
		if (nearestDistance != float.MaxValue && nearestCollider != null)
		{
			_nextReservedRicochetTargetCollider = nearestCollider;
			return true;
		}

		// existCount가 1보다는 큰데 nearestCollider 가 없다는 얘기는 존재하는 2개체 이상의 몹에 한번씩 리코세가 돌았다는 얘기다.
		// 이땐 등록된 리스트를 초기화 하고 마지막 객체를 제외한 나머지 중 가장 가까운 타겟으로 날아가야한다.
		if (nearestCollider == null && containsNearestCollider != null)
		{
			Collider lastCollider = _listRicochet[_listRicochet.Count - 1];
			_listRicochet.Clear();
			_listRicochet.Add(lastCollider);
			_nextReservedRicochetTargetCollider = containsNearestCollider;
			return true;
		}

		return false;
	}

	Collider _nextReservedRicochetTargetCollider;
	public bool ApplyRicochet(ref bool colliderEnabled)
	{
		if (_nextReservedRicochetTargetCollider == null)
			return false;

		if (_listRicochet == null || _listRicochet.Count == 0)
			return false;

		Transform lastRicochetTransform = BattleInstanceManager.instance.GetTransformFromCollider(_listRicochet[_listRicochet.Count - 1]);
		Vector3 newPosition = cachedTransform.position;
		newPosition.x = lastRicochetTransform.position.x;
		newPosition.z = lastRicochetTransform.position.z;
		cachedTransform.position = newPosition;
		colliderEnabled = _listRicochet[_listRicochet.Count - 1].enabled;

		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(_nextReservedRicochetTargetCollider);
		Vector3 diff = targetTransform.position - cachedTransform.position;
		diff.y = 0.0f;
		Vector3 normalizedDiff = diff.normalized;
		_velocity = _rigidbody.velocity = normalizedDiff * _signal.speed;
		_forward = cachedTransform.forward = normalizedDiff;
		return true;
	}
	#endregion











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
