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

	Transform _cachedParentActorTransform;
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
			if (_signal.overrideSpeedOnCollision > 0.0f)
				Debug.LogError("Invalid Setting. overrideSpeedOnCollision and useSpeedChange Both values ​​cannot be used at the same time.");

			float targetSpeed = _signal.targetSpeed;
			if (slowRate != 0.0f)
				targetSpeed *= (1.0f - slowRate);
			if (_tweenReferenceForSpeedChange != null)
				_tweenReferenceForSpeedChange.Kill();
			_tweenReferenceForSpeedChange = DOTween.To(() => _speed, x => _speed = x, targetSpeed, _signal.speedChangeTime).SetEase(_signal.speedChangeEase);
		}

		if (_signal.useCurveChange)
		{
			_remainCurveStartDelayTime = 0.0f;
			if (_signal.curveStartDelayTime == 0.0f)
				ApplyCurveChangeTween();
			else
				_remainCurveStartDelayTime = _signal.curveStartDelayTime;
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

				if (_signal.followMinRange > 0.0f)
					_cachedParentActorTransform = parentActor.cachedTransform;
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
						PlayerActor playerActor = parentActor as PlayerActor;
						if (playerActor != null)
							attackRange = playerActor.playerAI.actorTableAttackRange;
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

		_origSpeed = _overrideSpeedRemainTime = 0.0f;
		_needApplySpeed = false;
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

		if (_signal.movementType == eMovementType.Direct && IsAppliedRicochet() == false && _signal.bounceWallQuadCount == 0 && _signal.useHitStay == false)
		{
			if (_signal.monsterThroughCount == -1 || _signal.monsterThroughCount > 0 || _signal.wallThrough || _signal.quadThrough)
			{
				// 직선형 히트오브젝트의 경우 OnCollisionEnter 타이밍때 물리엔진의 결과에 의해 틀어질때가 있는데
				// 이걸 물리 설정만으로는 막을 방법이 없어서 - 물리 마테리얼도 바꿔봤는데 아무런 영향이 없다.
				// 직선인 발사체에서 대해서는 연산을 통해 위치를 보정하기로 한다.
				Vector3 startPosition = _hitObject.createPosition;
				Vector3 dirWithStart = cachedTransform.position - startPosition;
				if (Vector3.Angle(_velocity, dirWithStart) > 1.0f)
				{
					//Debug.Log("Adjust Through Direction!");
					_rigidbody.position = cachedTransform.position = startPosition + _velocity.normalized * dirWithStart.magnitude;
					_rigidbody.velocity = _velocity = cachedTransform.forward * _speed;
				}
			}
		}
	}

	int _lastBounceFrameCount;
	public void Bounce(Vector3 wallNormal)
	{
		//_velocity = Vector3.Reflect(_velocity, wallNormal);

		// 디버깅해보니 아래 코드가 있어도 여전히 rigidbody의 속도가 줄어드는 경우가 생겼다.
		//if (_velocity.magnitude < _speed * 0.9f)
		//	_velocity = _velocity.normalized * _speed;
		// 가장 의심되는 부분은 컬리더 두개가 닿아있는 곳에 충돌하는건데 그렇다고 이 ㄱ ㄴ 형태의 컬리더를 만들어낼 방법은 없으므로
		// 이 프레임에만 검사하지 않고 튕기고 나서 몇프레임동안 체크하는 식으로 바꾸는 식으로 해봤는데..
		//_bounceCheckRemainFrameCount = 150;
		//_bounceCheckRemainFixedFrameCount = 150;
		// 이렇게 해도 여전히 이상현상이 발생했다.
		// 결국 방향을 제대로 잡지 못하면 angularVelocity부터 다 틀어지는거라
		//
		//if (_lastBounceFrameCount == Time.frameCount)
		//	return;

		//_lastBounceFrameCount = Time.frameCount;
		//_velocity = Vector3.Reflect(_velocity, wallNormal);

		// 정석대로 가기로 한다.
		// 반사벡터를 구해서 
		Vector3 reflectVelocity = Vector3.Reflect(_velocity, wallNormal);

		// 반사된 벡터가 벽의 노말과 수직에 가깝다면 뭔가 잘못된거 아닐까. 그렇다고 안튕기게 할순 없으니
		if (Vector3.Dot(reflectVelocity.normalized, wallNormal.normalized) < 0.01f)
		{
			//Debug.LogError("Invalid reflect vector!!");
			reflectVelocity = Quaternion.Euler(0.0f, Random.Range(-45.0f, 45.0f), 0.0f) * wallNormal.normalized * reflectVelocity.magnitude;
		}

		// 간혹가다 벽을 수직으로 바라보는 반사벡터가 나오길래 예외처리 추가해본다.
		// 반사된 벡터가 하필 벽의 노말 안쪽으로 들어가는거라면 반대로 돌려준다.
		if (Vector3.Dot(reflectVelocity.normalized, wallNormal.normalized) < 0.0f)
		{
			reflectVelocity *= -1.0f;
		}
		
		// 반사방향으로 직선을 그어서 충돌하는게 있는지 확인한다. CheckWall
		// 이랬더니 ㄴ 사이에 들어가서 모서리에서 튕기지 않는 버그가 발생했다. 근접했다고 그냥 리턴하면 안되는거였다.
		// 그래서 체크로직을 조금만 수정해보기로 한다.
		if (_lastBounceFrameCount == Time.frameCount || (_lastBounceFrameCount + 1) == Time.frameCount || (_lastBounceFrameCount + 2) == Time.frameCount)
		{
			Vector3 endPosition = cachedTransform.position + reflectVelocity.normalized * 0.333f;
			if (CheckWallAndGroundQuad(cachedTransform.position, endPosition))
				return;
		}
		_lastBounceFrameCount = Time.frameCount;

		// 정리하자면,
		// ㄴ ㄱ 의 컬리더가 겹쳐있는 부분에서 반사가 이뤄질때 벽을 투과하는 방향으로 반사가 결정되었는데 하필 막혀서 벽을 따라 이동하는 현상이 발생한거고
		// 이걸 수정하기 위해 벽이나 쿼드 검사를 하기로 했는데
		// 정상적으로 1회만 바운스 되는 경우엔 이런 검사를 할 필요가 없으니 Bounce FrameCount를 기억해놨다가
		// 같은 프레임에 두번 일어나는 경우에만 체크해보고 정상적이지 않다면 리턴. 정상적이라면 덮어쓰는 형태로 가는거다.

		_velocity = reflectVelocity;
		_rigidbody.velocity = _velocity;
		_rigidbody.angularVelocity = Vector3.zero;
		_forward = cachedTransform.forward = _rigidbody.velocity.normalized;
	}

	// 반사방향을 적용하기 전에 유효하지 않은지 테스트하기 위해 추가한 함수.
	static RaycastHit[] s_raycastHitList = null;
	public static bool CheckWallAndGroundQuad(Vector3 position, Vector3 targetPosition)
	{
		// temp - check wall
		if (s_raycastHitList == null)
			s_raycastHitList = new RaycastHit[100];

		// step 1. Physics.RaycastNonAlloc
		Vector3 diff = targetPosition - position;
		float length = diff.magnitude;
		Vector3 rayPosition = position;
		rayPosition.y = 1.0f;
		int resultCount = Physics.RaycastNonAlloc(rayPosition, diff.normalized, s_raycastHitList, length, 1);

		// step 2. Ray Test
		float reservedNearestDistance = length;
		Vector3 endPosition = Vector3.zero;
		for (int i = 0; i < resultCount; ++i)
		{
			if (i >= s_raycastHitList.Length)
				break;

			bool planeCollided = false;
			bool groundQuadCollided = false;
			Vector3 wallNormal = Vector3.forward;
			Collider col = s_raycastHitList[i].collider;
			if (col.isTrigger)
				continue;

			if (BattleInstanceManager.instance.planeCollider != null && BattleInstanceManager.instance.planeCollider == col)
			{
				planeCollided = true;
				wallNormal = s_raycastHitList[i].normal;
			}

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
			{
				groundQuadCollided = true;
				wallNormal = s_raycastHitList[i].normal;
				return true;
			}

			AffectorProcessor targetAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (targetAffectorProcessor != null)
			{
				//if (Team.CheckTeamFilter(statusForHitObject.teamId, col, meHit.teamCheckType))
				//	monsterCollided = true;
			}
			else if (planeCollided == false && groundQuadCollided == false)
			{
				return true;
			}
		}
		return false;
	}

	float _remainCurveStartDelayTime = 0.0f;
	TweenerCore<float, float, FloatOptions> _tweenReferenceForCurveChange;
	void ApplyCurveChangeTween()
	{
		if (_tweenReferenceForCurveChange != null)
			_tweenReferenceForCurveChange.Kill();
		_tweenReferenceForCurveChange = DOTween.To(() => _currentCurve, x => _currentCurve = x, _signal.targetCurve, _signal.curveChangeTime).SetEase(_signal.curveChangeEase);
	}

	void Update()
	{
		if (_rigidbody.detectCollisions == false)
			return;

		//if (_bounceCheckRemainFrameCount > 0)
		//{
		//	_bounceCheckRemainFrameCount -= 1;
		//	_rigidbody.angularVelocity = Vector3.zero;
		//
		//	if (_rigidbody.velocity.magnitude < _speed * 0.9f)
		//		_rigidbody.velocity = _velocity.normalized * _speed;
		//}

		UpdateOverrideSpeed();

		switch (_signal.movementType)
		{
			case eMovementType.FollowTarget:
				if (_ignoreFollow)
					break;
				if (_signal.curveStartDelayTime > 0.0f)
				{
					if (_remainCurveStartDelayTime > 0.0f)
					{
						_remainCurveStartDelayTime -= Time.deltaTime;
						if (_remainCurveStartDelayTime <= 0.0f)
						{
							_remainCurveStartDelayTime = 0.0f;
							ApplyCurveChangeTween();
						}
					}
				}
				if (_followTargetActor != null && _followTargetActor.actorStatus.IsDie())
				{
					if (_signal.followLastPositionWhenDieTarget)
					{
						_followTargetPosition = _followTargetActor.cachedTransform.position;
						_followTargetActor = null;
					}
					else
						// 중간에 타겟을 바꿀일은 없나?
						_ignoreFollow = true;
					break;
				}
				// 아무래도 결함이 있는 코드기도 하고 이걸 써서 엄청나게 괜찮은 결과물을 만들어내지 못하는거 같기도 해서 제거하기로 한다.
				// 혹시 모르니 우선은 주석처리.
				//if (_signal.endFollowOverTargetDistance)
				//{
				//	Vector3 diffDir = Vector3.forward;
				//	if (_followTargetActor != null)
				//		diffDir = _followTargetActor.cachedTransform.position - cachedTransform.position;
				//	else
				//		diffDir = _followTargetPosition - cachedTransform.position;
					// 이렇게 계산하니 추적중 반이하로 남았을때 ignoreFollow가 켜지게 된다. 잘못 만든듯..
				//	Vector3 hitObjectDiff = cachedTransform.position - _createPosition;
				//	if (hitObjectDiff.sqrMagnitude > diffDir.sqrMagnitude)
				//	{
				//		_ignoreFollow = true;
				//		break;
				//	}
				//}
				break;
		}
	}

	void FixedUpdate()
	{
		if (_rigidbody.detectCollisions == false)
			return;

		switch(_signal.movementType)
		{
			case eMovementType.Direct:
				if (_signal.useSpeedChange || _needApplySpeed)
				{
					_velocity = _rigidbody.velocity = cachedTransform.forward * _speed;
					_needApplySpeed = false;
				}
				break;
			case eMovementType.FollowTarget:
				if (_needApplySpeed)
				{
					_velocity = _rigidbody.velocity = cachedTransform.forward * _speed;
					_forward = cachedTransform.forward;
					_needApplySpeed = false;
				}

				if (_ignoreFollow)
					break;
				if (_signal.curveStartDelayTime > 0.0f && _remainCurveStartDelayTime > 0.0f)
					break;

				Vector3 targetPosition = Vector3.zero;
				if (_followTargetActor != null)
					targetPosition = _followTargetActor.cachedTransform.position;
				else
					targetPosition = _followTargetPosition;
				if (_signal.followMinRange > 0.0f)
				{
					Vector3 createDiff = targetPosition - _createPosition;
					if (createDiff.sqrMagnitude < _signal.followMinRange * _signal.followMinRange && _cachedParentActorTransform != null && _cachedParentActorTransform.gameObject.activeSelf)
					{
						Vector3 newDiff = targetPosition - _cachedParentActorTransform.position;
						targetPosition = _cachedParentActorTransform.position + newDiff.normalized * _signal.followMinRange;
					}
				}
				Vector3 diffDir = targetPosition - cachedTransform.position;
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

	public bool IsTargetActor(Actor targetActor)
	{
		if (_followTargetActor == null)
			return false;
		if (_followTargetActor == targetActor)
			return true;
		return false;
	}

	public void ChangeFollowTargetActor(Actor targetActor)
	{
		_followTargetActor = targetActor;
	}

	float _origSpeed;
	float _overrideSpeedRemainTime;
	bool _needApplySpeed;
	public void ChangeOverrideSpeed(float speed, float remainTime)
	{
		if (_origSpeed == 0.0f)
			_origSpeed = _speed;

		_speed = speed;
		_overrideSpeedRemainTime = remainTime;
		_needApplySpeed = true;
	}

	void UpdateOverrideSpeed()
	{
		if (_overrideSpeedRemainTime > 0.0f)
		{
			_overrideSpeedRemainTime -= Time.deltaTime;
			if (_overrideSpeedRemainTime <= 0.0f)
			{
				_overrideSpeedRemainTime = 0.0f;
				_speed = _origSpeed;
				_needApplySpeed = true;
			}
		}
	}

	#region Ricochet
	const float DefaultRicochetRange = 4.0f;
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
	
	public bool IsEnableRicochet(int teamId, Team.eTeamCheckFilter filter)
	{
		Vector3 position = cachedTransform.position;
		Collider[] result = Physics.OverlapSphere(position, (_signal.overrideRicochetDistance > 0.0f) ? _signal.overrideRicochetDistance : DefaultRicochetRange); // range * _transform.localScale.x
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
			if (!Team.CheckTeamFilter(teamId, result[i], filter, false))
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
			if (_signal.wallThrough == false && _parentActorSphereCastRadiusForCheckWall > 0.0f && _listRicochet.Count > 0)
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

	public bool IsAppliedRicochet()
	{
		return (_listRicochet != null && _listRicochet.Count > 0);
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
