using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class HitObjectMovement : MonoBehaviour {

	public enum eMovementType
	{
		Direct,
		FollowTarget,
		Turn,
		Howitzer,
	}

	public enum eStartDirectionType
	{
		Forward,
		Direction,
		ToFirstTarget,
		ToMultiTarget,
	}

	public enum eHowitzerType
	{
		FixedTime,
		FixedSpeed,
	}

	HitObject _hitObject;
	MeHitObject _signal;
	float _createTime;
	Vector3 _createPosition;
	Rigidbody _rigidbody;
	float _speed;
	float _turnPower;

	Actor _followTargetActor;
	Vector3 _followTargetPosition;
	float _currentCurve;
	bool _ignoreFollow;

	#region Custom Howitzer
	public Vector3 howitzerTargetPosition { get; set; }
	#endregion

	TweenerCore<float, float, FloatOptions> _tweenReferenceForSpeedChange;
	public void InitializeSignal(HitObject hitObject, MeHitObject meHit, Actor parentActor, Rigidbody rigidbody, int hitSignalIndexInAction)
	{
		_hitObject = hitObject;
		_signal = meHit;
		_createTime = Time.time;
		_createPosition = cachedTransform.position;
		_rigidbody = rigidbody;
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		_speed = _signal.speed;
		float slowRate = 0.0f;
		if (parentActor.team.teamId == (int)Team.eTeamID.DefaultMonster)
		{
			slowRate = SlowHitObjectSpeedAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor);
			if (slowRate != 0.0f)
				_speed *= (1.0f - slowRate);
		}

		if (_signal.useSpeedChange)
		{
			float targetSpeed = _signal.targetSpeed;
			if (slowRate != 0.0f)
				targetSpeed *= (1.0f - slowRate);
			if (_tweenReferenceForSpeedChange != null)
				_tweenReferenceForSpeedChange.Kill();
			_tweenReferenceForSpeedChange = DOTween.To(() => _speed, x => _speed = x, targetSpeed, _signal.speedChangeTime).SetEase(_signal.speedChangeEase);
		}

		switch(_signal.movementType)
		{
			case eMovementType.FollowTarget:
				_currentCurve = _signal.curve;
				Collider targetCollider = parentActor.targetingProcessor.GetTarget();
				if (targetCollider != null)
				{
					AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
					_followTargetActor = affectorProcessor.actor;
				}
				else
				{
					if (parentActor.targetingProcessor.IsRegisteredCustomTargetPosition())
					{
						_followTargetPosition = parentActor.targetingProcessor.GetCustomTargetPosition(0);
						_followTargetActor = null;
					}
				}
				
				// FollowTarget역시 최초에 한번 설정해두는게 좋다. 타겟 없을때 앞으로 나가게 하려면 이게 제일 나은듯.
				_rigidbody.velocity = cachedTransform.forward * _speed;
				_forward = cachedTransform.forward;
				_ignoreFollow = false;
				break;
			case eMovementType.Direct:
			case eMovementType.Turn:
				// 생성 방향이 곧 날아가는 방향이다. 냅둔다.
				//Vector3 targetPosition = HitObject.GetTargetPosition(_signal, parentActor, hitSignalIndexInAction);
				//_rigidbody.velocity = HitObject.GetStartDirection(cachedTransform.position, meHit, parentActor.cachedTransform, targetPosition) * _speed;
				_velocity = _rigidbody.velocity = cachedTransform.forward * _speed;
				_forward = cachedTransform.forward;

				if (_signal.movementType == eMovementType.Turn)
				{
					if (_signal.useRandomTurn)
					{
						_turnPower = Random.Range(-meHit.accelTurn, meHit.accelTurn);
						float turnRatio = Mathf.Abs(_turnPower) / meHit.accelTurn;
						float angleY = Mathf.Lerp(_signal.randomTurnRotateYawRange.x, _signal.randomTurnRotateYawRange.y, turnRatio);
						angleY *= Random.Range(0.9f, 1.1f);
						if (_turnPower > 0.0f) angleY *= -1.0f;

						// 랜덤으로 나온 Turn의 양을 바탕으로 angleY를 구한 후 초기화 시점에 반영시킨다.
						_rigidbody.MoveRotation(_rigidbody.rotation * Quaternion.Euler(0.0f, angleY, 0.0f));
						cachedTransform.rotation = _rigidbody.rotation;
						_velocity = _rigidbody.velocity = cachedTransform.forward * _speed;
						_forward = cachedTransform.forward;
					}
					else
						_turnPower = _signal.accelTurn;
				}
				break;
			case eMovementType.Howitzer:
				howitzerTargetPosition = HitObject.GetTargetPosition(_signal, parentActor, hitSignalIndexInAction);
				howitzerTargetPosition = new Vector3(howitzerTargetPosition.x, parentActor.cachedTransform.position.y, howitzerTargetPosition.z);
				if (_signal.howitzerTargetPositionOffset != Vector2.zero)
				{
					howitzerTargetPosition += parentActor.cachedTransform.TransformVector(new Vector3(_signal.howitzerTargetPositionOffset.x, 0.0f, _signal.howitzerTargetPositionOffset.y));
				}
				if (_signal.howitzerRandomPositionRadiusRange != Vector2.zero)
				{
					float attackRange = 0.0f;
					if (parentActor.IsPlayerActor())
					{
						ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(parentActor.actorId);
						if (actorTableData != null)
							attackRange = actorTableData.attackRange;
					}
					Vector3 startPosition = cachedTransform.position;
					startPosition.y = 0.0f;
					Vector3 endPosition = howitzerTargetPosition;
					endPosition.y = 0.0f;
					float howitzerRandomPositionRadius = 0.0f;
					if (attackRange == 0.0f)
					{
						// 무제한 사거리일때는 10미터를 기준으로 값을 적었다고 판단한다.
						float distanceRatio = Vector3.Distance(startPosition, endPosition) / 10.0f;
						howitzerRandomPositionRadius = _signal.howitzerRandomPositionRadiusRange.x + (_signal.howitzerRandomPositionRadiusRange.y - _signal.howitzerRandomPositionRadiusRange.x) * distanceRatio;
					}
					else
					{
						// 사거리가 있을땐 범위에서 뽑아내면 된다.
						float distanceRatio = Vector3.Distance(startPosition, endPosition) / attackRange;
						howitzerRandomPositionRadius = Mathf.Lerp(_signal.howitzerRandomPositionRadiusRange.x, _signal.howitzerRandomPositionRadiusRange.y, distanceRatio);
					}
					Vector2 randomRadius = Random.insideUnitCircle * howitzerRandomPositionRadius;
					howitzerTargetPosition += new Vector3(randomRadius.x, 0.0f, randomRadius.y);
				}
				ComputeHowitzer();
				break;
		}

		if (_listRicochet != null)
			_listRicochet.Clear();
		_parentActorSphereCastRadiusForCheckWall = parentActor.targetingProcessor.sphereCastRadiusForCheckWall;
	}

	public void ComputeHowitzer()
	{
		switch (_signal.howitzerType)
		{
			case eHowitzerType.FixedTime:
				_velocity = _rigidbody.velocity = ProjectileHelper.ComputeVelocityToHitTargetAtTime(cachedTransform.position, howitzerTargetPosition, _signal.gravity, _signal.lifeTime);
				_forward = cachedTransform.forward = _rigidbody.velocity.normalized;
				break;
			case eHowitzerType.FixedSpeed:
				// 이거로 하면 out으로 나온 direction1,2 둘다 이상한 값이 들어있어서 쓸수가 없다. 차라리 위 함수를 응용해서 하기로 한다.
				//Vector3 direction1;
				//Vector3 direction2;
				//bool result = ProjectileHelper.ComputeDirectionToHitTargetWithSpeed(cachedTransform.position, targetPosition, _signal.gravity, _speed, out direction1, out direction2);
				//if (result == false)
				//{
				//	Debug.LogError("Invalid Parameter! HitObject can't reach the target.");
				//	break;
				//}
				//_velocity = _rigidbody.velocity = direction1.y > 0.0f ? direction2 : direction1;
				Vector3 diff = howitzerTargetPosition - cachedTransform.position;
				float time = diff.magnitude / _speed;
				_velocity = _rigidbody.velocity = ProjectileHelper.ComputeVelocityToHitTargetAtTime(cachedTransform.position, howitzerTargetPosition, _signal.gravity, time);
				_forward = cachedTransform.forward = _rigidbody.velocity.normalized;
				break;
		}
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
		float prevMagnitude = _velocity.magnitude;
		_velocity = Vector3.Reflect(_velocity, wallNormal);
		if (_velocity.magnitude < prevMagnitude * 0.9f)
			_velocity = _velocity.normalized * prevMagnitude;

		_rigidbody.velocity = _velocity;
		_rigidbody.angularVelocity = Vector3.zero;
		_forward = cachedTransform.forward = _rigidbody.velocity.normalized;
	}

	void Update()
	{
		UpdateLifeTimeWhenDieTarget();

		if (_rigidbody.detectCollisions == false)
			return;

		switch (_signal.movementType)
		{
			case eMovementType.FollowTarget:
				if (_ignoreFollow)
					break;
				if (_signal.curveStartDelayTime > 0.0f && (Time.time - _createTime) < _signal.curveStartDelayTime)
					break;
				if (_signal.curveLifeTime > 0.0f && (Time.time - _createTime - _signal.curveStartDelayTime) > _signal.curveLifeTime)
					break;
				if (_followTargetActor != null && _followTargetActor.actorStatus.IsDie())
				{
					if (_signal.overrideLifeTimeWhenDieTarget)
					{
						Vector3 diff = _followTargetActor.cachedTransform.position - cachedTransform.position;
						_remainOverrideLifeTime = (diff.magnitude + Random.Range(1.0f, 2.0f)) / _speed;
					}

					// 중간에 타겟을 바꿀일은 없나?
					_ignoreFollow = true;
					break;
				}
				_currentCurve += Time.deltaTime * _signal.curveAdd;
				break;
		}
	}

	float _remainOverrideLifeTime;
	void UpdateLifeTimeWhenDieTarget()
	{
		if (_remainOverrideLifeTime > 0.0f)
		{
			_remainOverrideLifeTime -= Time.deltaTime;
			if (_remainOverrideLifeTime <= 0.0f)
			{
				_remainOverrideLifeTime = 0.0f;
				_hitObject.OnFinalizeByLifeTime();
			}
		}
	}

	void FixedUpdate()
	{
		if (_rigidbody.detectCollisions == false)
			return;

		switch(_signal.movementType)
		{
			case eMovementType.Direct:
				if (_signal.useSpeedChange)
					_velocity = _rigidbody.velocity = cachedTransform.forward * _speed;
				break;
			case eMovementType.FollowTarget:
				if (_ignoreFollow)
					break;
				if (_signal.curveStartDelayTime > 0.0f && (Time.time - _createTime) < _signal.curveStartDelayTime)
					break;
				if (_signal.curveLifeTime > 0.0f && (Time.time - _createTime - _signal.curveStartDelayTime) > _signal.curveLifeTime)
					break;

				Vector3 diffDir = Vector3.forward;
				if (_followTargetActor != null)
					diffDir = _followTargetActor.cachedTransform.position - cachedTransform.position;
				else
					diffDir = _followTargetPosition - cachedTransform.position;

				if (_signal.endFollowOverTargetDistance)
				{
					Vector3 hitObjectDiff = cachedTransform.position - _createPosition;
					if (hitObjectDiff.sqrMagnitude > diffDir.sqrMagnitude)
					{
						_ignoreFollow = true;
						break;
					}
				}

				if (_signal.curveLockY)
				{
					diffDir.y = 0.0f;
					cachedTransform.rotation = Quaternion.RotateTowards(cachedTransform.rotation, Quaternion.LookRotation(diffDir), _currentCurve * Time.fixedDeltaTime);
				}
				else
				{
					Vector3 newDir = Vector3.RotateTowards(cachedTransform.forward, diffDir, _currentCurve * Time.fixedDeltaTime, 0.0f);
					cachedTransform.rotation = Quaternion.LookRotation(newDir);
				}
				_velocity = _rigidbody.velocity = cachedTransform.forward * _speed;
				_forward = cachedTransform.forward;
				break;
			case eMovementType.Turn:
				_rigidbody.MoveRotation(_rigidbody.rotation * Quaternion.Euler(0.0f, _turnPower * Time.fixedDeltaTime, 0.0f));
				cachedTransform.rotation = _rigidbody.rotation;
				//cachedTransform.Rotate(0.0f, _signal.accelTurn * Time.deltaTime, 0.0f, Space.Self);
				_velocity = _rigidbody.velocity = cachedTransform.forward * _speed;
				_forward = cachedTransform.forward;
				break;
			case eMovementType.Howitzer:
				Vector3 currentPosition = _rigidbody.position;
				ProjectileHelper.UpdateProjectile(ref currentPosition, ref _velocity, _signal.gravity, Time.fixedDeltaTime);
				// currentPosition을 적용해버리면 rigidbody도 앞으로 나아가고 transform도 앞으로 나아가서 두배로 가게 된다. 둘중 하나만 한다면 rigidbody만 하는게 맞다.
				_rigidbody.velocity = _velocity;
				_forward = cachedTransform.forward = _rigidbody.velocity.normalized;
				break;
		}
	}

	#region Ricochet
	const float RicochetRange = 4.0f;
	List<Collider> _listRicochet = null;
	float _parentActorSphereCastRadiusForCheckWall;
	public void AddRicochet(Collider collider, bool initialize)
	{
		if (_listRicochet == null)
			_listRicochet = new List<Collider>();

		if (initialize)
		{
			_listRicochet.Clear();
			if (_signal.overrideRicochetSpeed > 0.0f)
				_speed = _signal.overrideRicochetSpeed;
		}

		_listRicochet.Add(collider);
	}

	public Collider GetLastRicochetCollider()
	{
		if (_listRicochet == null)
			return null;

		if (_listRicochet.Count == 0)
			return null;
		return _listRicochet[_listRicochet.Count - 1];
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

			// wall check
			if (_parentActorSphereCastRadiusForCheckWall > 0.0f && _listRicochet.Count > 0)
			{
				Transform lastRicochetTransform = BattleInstanceManager.instance.GetTransformFromCollider(_listRicochet[_listRicochet.Count - 1]);
				Vector3 newPosition = cachedTransform.position;
				newPosition.x = lastRicochetTransform.position.x;
				newPosition.z = lastRicochetTransform.position.z;
				Vector3 targetPosition = BattleInstanceManager.instance.GetTransformFromCollider(result[i]).position;
				targetPosition.y = newPosition.y;
				if (TargetingProcessor.CheckWall(newPosition, targetPosition, _parentActorSphereCastRadiusForCheckWall))
					continue;
			}

			// 등록되어있는 개체라면 다르게 처리해야한다.
			bool contains = _listRicochet.Contains(result[i]);

			// distance
			Vector3 diff = BattleInstanceManager.instance.GetTransformFromCollider(result[i]).position - position;
			diff.y = 0.0f;
			float distance = diff.magnitude - colliderRadius;

			if (contains)
			{
				// contains를 체크할땐 마지막으로 등록된 타겟은 제외해야한다.
				if (_signal.ricochetOneHitPerTarget == false && distance < containsNearestDistance && _listRicochet[_listRicochet.Count - 1] != result[i])
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
		if (_signal.ricochetOneHitPerTarget == false && nearestCollider == null && containsNearestCollider != null)
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
		_velocity = _rigidbody.velocity = normalizedDiff * _speed;
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
