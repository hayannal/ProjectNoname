using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class HitObject : MonoBehaviour
{
	public enum eTargetDetectType
	{
		Preset,
		Area,
		Collider,
	}

	public enum eCreatePositionType
	{
		Offset,
		Bone,
		TargetPosition,
	}

	#region staticFunction
	public static HitObject InitializeHit(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, int hitSignalIndexInAction, int repeatIndex)
	{
		// step 1. Find Target and Reaction
		if (meHit.targetDetectType == eTargetDetectType.Preset)
		{
			// Preset은 hitObject객체를 만들지 않는다.
			TargetingProcessor targetSystem = parentActor.targetingProcessor;
			if (targetSystem != null)
			{
				for (int i = 0; i < targetSystem.GetTargetCount(); ++i)
				{
					Collider targetCollider = targetSystem.GetTargetList()[i];
					if (targetCollider == null)
						continue;
					AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
					if (affectorProcessor == null)
						continue;
					if (!Team.CheckTeamFilter(parentActor.team.teamID, targetCollider, meHit.teamCheckType))
						continue;
					float colliderRadius = ColliderUtil.GetRadius(targetCollider);
					if (colliderRadius == -1.0f)
						continue;

					HitParameter hitParameter = new HitParameter();
					hitParameter.hitNormal = parentTransform.forward;
					hitParameter.contactNormal = (targetCollider.transform.position - parentTransform.position).normalized;
					hitParameter.contactPoint = targetCollider.transform.position + (-hitParameter.contactNormal * colliderRadius * 0.7f);
					hitParameter.contactPoint.y += targetCollider.bounds.size.y * 0.5f;
					hitParameter.statusBase = parentActor.actorStatus.statusBase;
					CopyEtcStatusForHitObject(ref hitParameter.statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex);

					ApplyAffectorValue(affectorProcessor, meHit.affectorValueIdList, hitParameter);

					if (meHit.showHitEffect)
						HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, hitParameter.statusStructForHitObject.weaponIDAtCreation);
					if (meHit.showHitBlink)
						HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
					if (meHit.showHitRimBlink)
						HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);
				}
			}
			return null;
		}
		else if (meHit.targetDetectType == eTargetDetectType.Area)
		{
			Vector3 areaPosition = spawnTransform.TransformPoint(meHit.offset); // meHit.offset * parentTransform.localScale
			StatusStructForHitObject statusStructForHitObject = new StatusStructForHitObject();
			CopyEtcStatusForHitObject(ref statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex);
			CheckHitArea(areaPosition, spawnTransform.forward, meHit, parentActor.actorStatus.statusBase, statusStructForHitObject);

			// HitObject 프리팹이 있거나 lifeTime이 있다면 생성하고 아니면 패스.
			Vector3 position = GetSpawnPosition(spawnTransform, meHit, parentTransform);
			Quaternion rotation = Quaternion.LookRotation(GetSpawnDirection(position, meHit, parentTransform, GetTargetPosition(meHit, parentActor, hitSignalIndexInAction)));
			HitObject hitObject = GetCachedHitObject(meHit, position, rotation);
			if (hitObject != null)
				hitObject.InitializeHitObject(meHit, parentActor, hitSignalIndexInAction, repeatIndex);
			return hitObject;
		}

		// step2. Collider타입은 상황에 맞게 1개 혹은 여러개 만들어야한다.
		Vector3 targetPosition = GetTargetPosition(meHit, parentActor, hitSignalIndexInAction);
		Vector3 defaultPosition = GetSpawnPosition(spawnTransform, meHit, parentTransform);
		Quaternion defaultRotation = Quaternion.LookRotation(GetSpawnDirection(defaultPosition, meHit, parentTransform, targetPosition));
		if (meHit.parallelCount > 0)
		{
			for (int i = 0; i < meHit.parallelCount; ++i)
			{
				Vector3 position = GetParallelSpawnPosition(spawnTransform, meHit, parentTransform, i);
				Quaternion rotation = Quaternion.LookRotation(GetSpawnDirection(defaultPosition, meHit, parentTransform, targetPosition));
				HitObject parallelHitObject = GetCachedHitObject(meHit, position, rotation);
				if (parallelHitObject == null)
					continue;
				parallelHitObject.InitializeHitObject(meHit, parentActor, hitSignalIndexInAction, repeatIndex);
			}
		}

		for (int i = 0; i < meHit.circularSectorCount; ++i)
		{
			Vector3 position = GetSpawnPosition(spawnTransform, meHit, parentTransform);
			float centerAngleY = meHit.circularSectorUseWorldSpace ? meHit.circularSectorWorldSpaceCenterAngleY : defaultRotation.eulerAngles.y;
			float baseAngle = meHit.circularSectorCount % 2 == 0 ? centerAngleY - (meHit.circularSectorBetweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, meHit.circularSectorBetweenAngle);
			HitObject circularSectorHitObject = GetCachedHitObject(meHit, position, Quaternion.Euler(0.0f, angle, 0.0f));
			if (circularSectorHitObject == null)
				continue;
			circularSectorHitObject.InitializeHitObject(meHit, parentActor, hitSignalIndexInAction, repeatIndex);
		}

		bool ignoreMainHitObjectByGenerator = false;
		if (meHit.continuousHitObjectGeneratorBaseList != null)
		{
			for (int i = 0; i < meHit.continuousHitObjectGeneratorBaseList.Count; ++i)
			{
				ContinuousHitObjectGeneratorBase continuousHitObjectGenerator = BattleInstanceManager.instance.GetContinuousHitObjectGenerator(meHit.continuousHitObjectGeneratorBaseList[i].gameObject, defaultPosition, defaultRotation);
				ignoreMainHitObjectByGenerator |= continuousHitObjectGenerator.ignoreMainHitObject;
				continuousHitObjectGenerator.InitializeGenerator(meHit, parentActor, hitSignalIndexInAction, repeatIndex, spawnTransform);
			}
		}

		bool createMainHitObject = true;
		if (meHit.ignoreMainHitObjectByParallel || meHit.ignoreMainHitObjectByCircularSector || ignoreMainHitObjectByGenerator)
			createMainHitObject = false;
		if (createMainHitObject)
		{
			HitObject hitObject = GetCachedHitObject(meHit, defaultPosition, defaultRotation);
			if (hitObject != null)
				hitObject.InitializeHitObject(meHit, parentActor, hitSignalIndexInAction, repeatIndex);
			return hitObject;
		}
		return null;
	}

	public static HitObject GetCachedHitObject(MeHitObject meHit, Vector3 position, Quaternion rotation)
	{
		HitObject hitObject = null;
		if (meHit.hitObjectPrefab != null)
		{
			hitObject = BattleInstanceManager.instance.GetCachedHitObject(meHit.hitObjectPrefab, position, rotation);
		}
		else if (meHit.lifeTime > 0.0f)
		{
			hitObject = BattleInstanceManager.instance.GetEmptyHitObject(position, rotation);
		}
		return hitObject;
	}

	public static Vector3 GetSpawnPosition(Transform spawnTransform, MeHitObject meHit, Transform parentActorTransform)
	{
		if (meHit.hitObjectPrefab == null && meHit.lifeTime > 0.0f)
			return spawnTransform.TransformPoint(meHit.offset);

		if (meHit.offset == Vector3.zero)
			return spawnTransform.position;

		if (meHit.createPositionType != eCreatePositionType.Bone)
			return spawnTransform.TransformPoint(meHit.offset);    // meHit.offset * parentTransform.localScale

		if (meHit.useBoneRotation)
			return spawnTransform.TransformPoint(meHit.offset);    // meHit.offset * parentTransform.localScale

		Vector3 parentActorPosition = parentActorTransform.position;
		Vector3 offsetPosition = parentActorTransform.TransformPoint(meHit.offset);
		offsetPosition -= parentActorPosition;
		return spawnTransform.position + offsetPosition;
	}

	static Vector3 GetParallelSpawnPosition(Transform spawnTransform, MeHitObject meHit, Transform parentActorTransform, int parallelIndex)
	{
		Vector3 baseSpawnPosition = GetSpawnPosition(spawnTransform, meHit, parentActorTransform);

		Vector3 parentActorPosition = parentActorTransform.position;
		Vector3 parallelOffset = Vector3.zero;
		parallelOffset.x = ((meHit.parallelCount - 1) * 0.5f * meHit.parallelDistance) * -1.0f + meHit.parallelDistance * parallelIndex;
		Vector3 offsetPosition = parentActorTransform.TransformPoint(parallelOffset);
		offsetPosition -= parentActorPosition;
		return baseSpawnPosition + offsetPosition;
	}

	public static Vector3 GetTargetPosition(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction)
	{
		Vector3 targetPosition = Vector3.zero;
		if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToFirstTarget || meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToMultiTarget)
		{
			int targetIndex = -1;
			if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToFirstTarget)
				targetIndex = 0;
			else if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToMultiTarget)
				targetIndex = hitSignalIndexInAction;

			TargetingProcessor targetingProcessor = parentActor.targetingProcessor;
			if (targetingProcessor.IsRegisteredCustomTargetPosition())
				targetPosition = targetingProcessor.GetCustomTargetPosition(targetIndex);
			else if (targetingProcessor.GetTarget() != null)
				targetPosition = targetingProcessor.GetTargetPosition(targetIndex);
			else
				targetPosition = GetFallbackTargetPosition(parentActor.cachedTransform);
		}
		return targetPosition;
	}

	public static Vector3 GetFallbackTargetPosition(Transform t)
	{
		Vector3 fallbackPosition = new Vector3(0.0f, 0.0f, 4.0f);
		return t.TransformPoint(fallbackPosition);
	}

	public static Vector3 GetSpawnDirection(Vector3 spawnPosition, MeHitObject meHit, Transform parentActorTransform, Vector3 targetPosition, bool applyRange = true)
	{
		Vector3 result = Vector3.zero;
		switch (meHit.startDirectionType)
		{
			case HitObjectMovement.eStartDirectionType.Forward:
				result = Vector3.forward;
				break;
			case HitObjectMovement.eStartDirectionType.Direction:
				result = meHit.startDirection.normalized;
				break;
			case HitObjectMovement.eStartDirectionType.ToFirstTarget:
			case HitObjectMovement.eStartDirectionType.ToMultiTarget:
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
		if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.Direction && meHit.useWorldSpaceDirection)
			return result;
		return parentActorTransform.TransformDirection(result);
	}

	static void CopyEtcStatusForHitObject(ref StatusStructForHitObject statusStructForHitObject, Actor actor, MeHitObject meHit, int hitSignalIndexInAction, int repeatIndex)
	{
		statusStructForHitObject.teamID = actor.team.teamID;
		statusStructForHitObject.weaponIDAtCreation = 0;
		//if (meHit.useWeaponHitEffect)
		//	statusStructForHitObject.weaponIDAtCreation = actor.GetWeaponID(meHit.weaponDummyName);
		statusStructForHitObject.skillLevel = actor.actionController.GetCurrentSkillLevelByCurrentAction();
		statusStructForHitObject.hitSignalIndexInAction = hitSignalIndexInAction;
		statusStructForHitObject.repeatIndex = repeatIndex;
	}

	static void CheckHitArea(Vector3 areaPosition, Vector3 areaForward, MeHitObject meHit, StatusBase statusBase, StatusStructForHitObject statusForHitObject)
	{
		// step 1. Physics.OverlapSphere
		Collider[] result = Physics.OverlapSphere(areaPosition, meHit.areaDistanceMax); // meHit.areaDistanceMax * parentTransform.localScale.x

		// step 2. Check each object.
		float distanceMin = meHit.areaDistanceMin; // * parentTransform.localScale.x;
		float distanceMax = meHit.areaDistanceMax; // * parentTransform.localScale.x;
		Vector3 forward = Quaternion.Euler(0.0f, meHit.areaRotationY, 0.0f) * areaForward;
		for (int i = 0; i < result.Length; ++i)
		{
			// affector processor
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(result[i]);
			if (affectorProcessor == null)
				continue;

			// team check
			if (!Team.CheckTeamFilter(statusForHitObject.teamID, result[i], meHit.teamCheckType))
				continue;

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(result[i]);
			if (colliderRadius == -1.0f) continue;

			// distance
			Vector3 diff = result[i].transform.position - areaPosition;
			diff.y = 0.0f;
			if (diff.magnitude + colliderRadius < distanceMin) continue;
			if (diff.magnitude - colliderRadius > distanceMax) continue;

			// angle
			float angle = Vector3.Angle(forward, diff.normalized);
			float hypotenuse = Mathf.Sqrt(diff.sqrMagnitude + colliderRadius * colliderRadius);
			float adjustAngle = Mathf.Rad2Deg * Mathf.Acos(diff.magnitude / hypotenuse);
			if (meHit.areaAngle * 0.5f < angle - adjustAngle) continue;

			HitParameter hitParameter = new HitParameter();
			hitParameter.hitNormal = forward;
			hitParameter.contactNormal = diff.normalized;
			hitParameter.contactPoint = result[i].transform.position + (-hitParameter.contactNormal * colliderRadius * 0.7f);
			hitParameter.contactPoint.y += (meHit.areaHeightMin + meHit.areaHeightMax) * 0.5f;
			hitParameter.statusBase = statusBase;
			hitParameter.statusStructForHitObject = statusForHitObject;

			ApplyAffectorValue(affectorProcessor, meHit.affectorValueIdList, hitParameter);

			if (meHit.showHitEffect)
				HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, statusForHitObject.weaponIDAtCreation);
			if (meHit.showHitBlink)
				HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
			if (meHit.showHitRimBlink)
				HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);
		}
	}

	static void ApplyAffectorValue(AffectorProcessor affectorProcessor, List<string> listAffectorValueId, HitParameter hitParameter)
	{
		if (listAffectorValueId == null || listAffectorValueId.Count == 0) return;
		if (affectorProcessor == null) return;

		for (int i = 0; i < listAffectorValueId.Count; ++i)
			affectorProcessor.ApplyAffectorValue(listAffectorValueId[i], hitParameter, false, true);
	}
	#endregion


	public StatusStructForHitObject statusStructForHitObject { get { return _statusStructForHitObject; } }
	public float createTime { get { return _createTime; } }

	MeHitObject _signal;
	float _createTime;
	StatusBase _statusBase = new StatusBase();
	StatusStructForHitObject _statusStructForHitObject;
	Rigidbody _rigidbody { get; set; }
	Collider _collider { get; set; }
	List<TrailRenderer> _listTrailRendererAfterCollision;
	List<GameObject> _listDisableObjectAfterCollision;
	bool _disableSelfObjectAfterCollision;

	// 기본적으로 벽 튕기는 처리나 투과를 위해서 모든 히트오브젝트는 컬리더로 되어있다.
	// 컬리젼으로 시작했다가 충돌시 잠시 트리거로 바뀌는걸 기억하기 위해 bool변수 하나 만들어둔다.
	bool _tempTriggerOnCollision;
	// 그러나 hitStay처럼 파고들어야하는 히트 오브젝트들은 기본 컬리더 말고 타격용 트리거를 추가로 가지고 있어야한다.
	// trigger만 존재하는 히트오브젝트의 가장 큰 단점이 빨라지면 결국 투과하기 때문에 충돌감지가 제대로 안된다는건데
	// OnTrigger에서 Ray를 쏘든 Collision으로 한프레임 바꾸든 혹은 매프레임 이전 포지션에 SphereCast를 쏘든 완벽하게 처리하려면 추가코드가 많이 필요하게 된다.
	// 그래서 차라리 개념을 바꿔서
	// 히트오브젝트에 붙은 컬리더와 똑같은 형태의 트리거를 만들어서 본체는 원래대로 충돌감지하고 이 트리거는 hitStay용도로 쓰기로 한다.
	Collider _triggerForHitStay;

	int _remainMonsterThroughCount;
	int _remainBounceWallQuadCount;
	int _remainRicochetCount;

	HitObjectMovement _hitObjectMovement;
	HitObjectLineRenderer _hitObjectLineRenderer;
	HitObjectAnimator _hitObjectAnimator;
	Animator _animator;

	void Awake()
	{
		_animator = GetComponent<Animator>();
	}

	static int HITOBJECT_LAYER;
	public void InitializeHitObject(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction, int repeatIndex)
	{
		if (HITOBJECT_LAYER == 0) HITOBJECT_LAYER = LayerMask.NameToLayer("HitObject");
		if (gameObject.layer == 0)
			ObjectUtil.ChangeLayer(gameObject, HITOBJECT_LAYER);

		_signal = meHit;
		_createTime = Time.time;
		parentActor.actorStatus.CopyStatusBase(ref _statusBase);
		CopyEtcStatusForHitObject(ref _statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex);

		if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
		if (_collider == null) _collider = GetComponentInChildren<Collider>();
		EnableRigidbodyAndCollider(true);
		InitializeDisableObject();

		if (_signal.targetDetectType != eTargetDetectType.Collider)
		{
			if (_collider != null) _collider.enabled = false;
		}

		// hitStay를 쓰면 트리거 하나를 추가로 가지고 있어야한다. 여기에 몹Through를 켜면 긁고 지나가는 투과형 다단히트 오브젝트가 된다.
		_tempTriggerOnCollision = false;
		if (_collider != null) _collider.isTrigger = false;
		if (_signal.useHitStay)
		{
			if (_triggerForHitStay == null)
			{
				_triggerForHitStay = ObjectUtil.CopyComponent<Collider>(_collider, gameObject);
				_triggerForHitStay.isTrigger = true;
			}
		}

		_remainMonsterThroughCount = _signal.monsterThroughCount;	// + level pack through count
		_remainBounceWallQuadCount = _signal.bounceWallQuadCount;
		_remainRicochetCount = _signal.ricochetCount;

		// Sub Component
		if (meHit.lifeTime > 0.0f)
		{
			if (meHit.movable)
			{
				if (_hitObjectMovement == null)
				{
					_hitObjectMovement = GetComponent<HitObjectMovement>();
					if (_hitObjectMovement == null) _hitObjectMovement = gameObject.AddComponent<HitObjectMovement>();
				}
				_hitObjectMovement.InitializeSignal(meHit, parentActor, _rigidbody, hitSignalIndexInAction);
			}
			if (meHit.useLineRenderer)
			{
				if (_hitObjectLineRenderer == null)
				{
					_hitObjectLineRenderer = GetComponent<HitObjectLineRenderer>();
					if (_hitObjectLineRenderer == null) _hitObjectLineRenderer = gameObject.AddComponent<HitObjectLineRenderer>();
				}
				_hitObjectLineRenderer.InitializeSignal(meHit, parentActor);
			}
			if (_animator != null)
			{
				if (_hitObjectAnimator == null)
				{
					_hitObjectAnimator = GetComponent<HitObjectAnimator>();
					if (_hitObjectAnimator == null) _hitObjectAnimator = gameObject.AddComponent<HitObjectAnimator>();
				}
				_hitObjectAnimator.InitializeSignal(parentActor, _animator);
				_hitObjectAnimatorStarted = false;
				_waitHitObjectAnimatorUpdateCount = 0;
			}
		}

		BattleInstanceManager.instance.OnInitializeHitObject(this, _collider);
	}

	void InitializeDisableObject()
	{
		if (_listDisableObjectAfterCollision == null)
		{
			_listDisableObjectAfterCollision = new List<GameObject>();
			_listTrailRendererAfterCollision = new List<TrailRenderer>();

			RFX4_PhysicsMotion physicsMotion = GetComponentInChildren<RFX4_PhysicsMotion>();
			if (physicsMotion != null)
			{
				for (int i = 0; i < physicsMotion.DeactivateObjectsAfterCollision.Length; ++i)
					_listDisableObjectAfterCollision.Add(physicsMotion.DeactivateObjectsAfterCollision[i]);
			}

			ProjectileMoveScript projectileMoveScript = GetComponent<ProjectileMoveScript>();
			if (projectileMoveScript != null)
			{
				for (int i = 0; i < projectileMoveScript.trails.Count; ++i)
				{
					TrailRenderer trailRenderer = projectileMoveScript.trails[i].GetComponent<TrailRenderer>();
					if (trailRenderer != null)
						_listTrailRendererAfterCollision.Add(trailRenderer);
					else
						_listDisableObjectAfterCollision.Add(projectileMoveScript.trails[i]);
				}
				// self?
				_disableSelfObjectAfterCollision = true;
			}
		}
		else
		{
			// Reactive by pool
			for (int i = 0; i < _listDisableObjectAfterCollision.Count; ++i)
				_listDisableObjectAfterCollision[i].SetActive(true);
			for (int i = 0; i < _listTrailRendererAfterCollision.Count; ++i)
				_listTrailRendererAfterCollision[i].Clear();
		}
	}

	void Update()
	{
		if (_waitHitObjectAnimatorUpdateCount > 0)
		{
			_waitHitObjectAnimatorUpdateCount -= 1;
			if (_waitHitObjectAnimatorUpdateCount == 0)
			{
				BattleInstanceManager.instance.OnFinalizeHitObject(_collider);
				gameObject.SetActive(false);
			}
			return;
		}

		if (_signal.lifeTime > 0.0f && _signal.targetDetectType == eTargetDetectType.Area)
		{
			CheckHitArea(transform.position, transform.forward, _signal, _statusBase, _statusStructForHitObject);
		}
	}

	//Vector3 _prevPosition = Vector3.zero;
	//void LateUpdate()
	//{
	//	_prevPosition = cachedTransform.position;
	//}


	void FixedUpdate()
	{
		// for life time 0.0f
		if (_createTime + _signal.lifeTime < Time.time)
		{
			OnFinalizeByLifeTime();
			return;
		}
	}

	int HitObjectAnimatorUpdateWaitCount = 3;
	int _waitHitObjectAnimatorUpdateCount = 0;
	bool _hitObjectAnimatorStarted = false;
	void FinalizeHitObject()
	{
		if (_listOneHitPerTarget != null)
			_listOneHitPerTarget.Clear();
		if (_dicHitStayTime != null)
			_dicHitStayTime.Clear();

		// 히트 오브젝트 애니메이터를 발동시켜놨으면 첫번째 프레임이 호출될때까지는 기다려야한다.
		if (_hitObjectAnimatorStarted)
		{
			_hitObjectAnimatorStarted = false;
			_waitHitObjectAnimatorUpdateCount = HitObjectAnimatorUpdateWaitCount;
			return;
		}

		BattleInstanceManager.instance.OnFinalizeHitObject(_collider);
		//Destroy(gameObject);
		gameObject.SetActive(false);
	}

	void OnFinalizeByCollision()
	{
		EnableRigidbodyAndCollider(false);

		for (int i = 0; i < _listDisableObjectAfterCollision.Count; ++i)
			_listDisableObjectAfterCollision[i].SetActive(false);

		if (_hitObjectLineRenderer != null)
			_hitObjectLineRenderer.DisableLineRenderer(false);
		if (_hitObjectAnimator != null && _hitObjectAnimator.OnFinalizeByCollision())
			_hitObjectAnimatorStarted = true;

		if (_disableSelfObjectAfterCollision)
			FinalizeHitObject();
	}

	void OnFinalizeByLifeTime()
	{
		if (_waitHitObjectAnimatorUpdateCount > 0)
			return;

		EnableRigidbodyAndCollider(false);

		if (_hitObjectAnimator != null && _hitObjectAnimator.OnFinalizeByLifeTime())
			_hitObjectAnimatorStarted = true;

		FinalizeHitObject();
	}




	List<AffectorProcessor> _listOneHitPerTarget = null;
	void OnCollisionEnter(Collision collision)
	{
		//Debug.Log("hit object collision enter");
		bool collided = false;
		bool groundQuadCollided = false;
		bool wallCollided = false;
		bool monsterCollided = false;
		Vector3 wallNormal = Vector3.forward;
		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			collided = true;
			if (_signal.showHitEffect)
				HitEffect.ShowHitEffect(_signal, contact.point, contact.normal, _statusStructForHitObject.weaponIDAtCreation);

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
			{
				groundQuadCollided = true;
				wallNormal = contact.normal;
			}

			bool ignoreAffectorProcessor = false;
			if (_triggerForHitStay != null && contact.thisCollider == _collider)
				ignoreAffectorProcessor = true;

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null && Team.CheckTeamFilter(_statusStructForHitObject.teamID, col, _signal.teamCheckType))
			{
				if (_signal.oneHitPerTarget)
				{
					if (_listOneHitPerTarget == null) _listOneHitPerTarget = new List<AffectorProcessor>();
					if (_listOneHitPerTarget.Contains(affectorProcessor))
						ignoreAffectorProcessor = true;
				}

				if (ignoreAffectorProcessor == false)
				{
					if (_signal.useHitStay == false)
					{
						OnCollisionEnterAffectorProcessor(affectorProcessor, contact.point, contact.normal);
						if (_signal.oneHitPerTarget)
							_listOneHitPerTarget.Add(affectorProcessor);
						if (_remainRicochetCount > 0 && _hitObjectMovement != null)
							_hitObjectMovement.AddRicochet(col, _remainRicochetCount == _signal.ricochetCount);
					}
					monsterCollided = true;
				}
			}
			else if (groundQuadCollided == false)
			{
				wallCollided = true;
				wallNormal = contact.normal;
			}

			if (_signal.contactAll == false)
				break;
		}

		OnPostCollided(collided, groundQuadCollided, wallCollided, monsterCollided, wallNormal);
	}

	void OnPostCollided(bool collided, bool groundQuadCollided, bool wallCollided, bool monsterCollided, Vector3 wallNormal)
	{
		if (collided == false)
			return;

		// Check End of HitObject
		bool useThrough = false;
		bool useBounce = false;
		if (monsterCollided)
		{
			bool ricochetApplied = false;
			if (_remainRicochetCount > 0 && _hitObjectMovement != null && _hitObjectMovement.IsEnableRicochet(_statusStructForHitObject.teamID))
			{
				// 리코세를 하기 위해선 각도에 따라 몹을 관통하기도 관통 안하기도 한다.
				// 그렇다고 이걸 일일이 각도 체크하면서 하기엔 위험부담이 있어서
				// 차라리 리코세 적용시에 몹의 몸 중심으로 옮겨놓고 트리거로 임시로 바꿔둔채(Through 하듯) 발사하는 식으로 풀게 되었다.
				// 그렇데 이렇게 할 경우 몹이 죽을때는 컬리더랑 리지드바디까지 다 끄기때문에 trigger로 해둔게 풀리지 않게 된다.
				// 그래서 해당몹의 컬리더 상태를 확인해서 처리하도록 한다.
				bool colliderEnabled = false;
				if (_hitObjectMovement.ApplyRicochet(ref colliderEnabled))
				{
					ricochetApplied = true;
					_remainRicochetCount -= 1;
					if (colliderEnabled)
						useThrough = true;
					else
						return;
				}
			}

			if (ricochetApplied)
			{
				// nothing
			}
			else if ((_remainMonsterThroughCount > 0 || _remainMonsterThroughCount == -1))
			{
				if (_remainMonsterThroughCount > 0) _remainMonsterThroughCount -= 1;
				useThrough = true;
			}
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		if (wallCollided)
		{
			if (_remainBounceWallQuadCount > 0)
			{
				_remainBounceWallQuadCount -= 1;
				useBounce = true;
			}
			else if (_signal.wallThrough)
				useThrough = true;
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		if (groundQuadCollided)
		{
			if (_remainBounceWallQuadCount > 0)
			{
				_remainBounceWallQuadCount -= 1;
				useBounce = true;
			}
			else if (_signal.quadThrough)
				useThrough = true;
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		if (useBounce)
		{
			if (_hitObjectMovement != null)
				_hitObjectMovement.Bounce(wallNormal);
			return;
		}

		if (useThrough)
		{
			_tempTriggerOnCollision = true;
			_collider.isTrigger = true;
			if (_hitObjectMovement != null)
				_hitObjectMovement.ReinitializeForThrough();
			return;
		}

		OnFinalizeByCollision();
	}

	void OnTriggerExit(Collider collider)
	{
		// 중첩되어있는 오브젝트들을 위해서라도 refCount 형태로 관리해야하려나.
		if (_tempTriggerOnCollision && _collider.isTrigger)
		{
			_collider.isTrigger = false;
			_tempTriggerOnCollision = false;
		}
	}

	// 컬리젼도 Stay가 가능하다. 부착된 채로 떨어지기전까지 계속 호출되는 구조다.
	// 그러나 컬리젼으로 Stay를 판단하는 경우가 거의 없을거 같고 충분히 트리거로도 할 수 있는거라 코드 간결성을 위해 빼기로 한다.
	//void OnCollisionStay(Collision collision)
	//{
	//}

	// OnTriggerEnter 호출되는 프레임부터 같이 호출되기 때문에 Stay에서만 처리해도 괜찮다.
	// 사실 관통중이라면 충돌을 담당하는 컬리더가 임시로 trigger로 바뀌어져있을테고 이때는 Stay가 두번 같이 올 수 있는데
	// 어차피 Interval따라 데미지 처리할 수 있는지 체크할거기 때문에 여러번 와도 상관없긴 하다.
	// 그리고 모든 충돌체 관련 처리는 OnCollisionEnter에서 하기때문에 여기선 hitStay처리만 하면 된다.
	//RaycastHit[] _hitInfoList = null;
	void OnTriggerStay(Collider other)
	{
		if (_tempTriggerOnCollision && _collider.isTrigger)
		{
			// 여기서 리턴시켜버리면 hitStay오브젝트가 관통하는 순간엔 체크가 안되서 하면 안된다.
			//return;
		}
		if (_triggerForHitStay == null)
			return;
		if (other.isTrigger)
			return;

		Collider col = other;
		if (col == null)
			return;

		if (_signal.useHitStay)
		{
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null && CheckHitStayInterval(affectorProcessor) && Team.CheckTeamFilter(_statusStructForHitObject.teamID, col, _signal.teamCheckType))
			{
				Vector3 contactPoint = Vector3.zero;
				Vector3 contactNormal = Vector3.forward;

				//if (_hitInfoList == null)
				//	_hitInfoList = new RaycastHit[10];

				bool collided = false;
				// Stay에서는 cachedTransform.position에다 표시하는게 더 어울리는거 같다.
				//Vector3 diff = cachedTransform.position - _prevPosition;
				//int resultCount = Physics.SphereCastNonAlloc(_prevPosition, ColliderUtil.GetRadius(_collider), diff.normalized, _hitInfoList, diff.magnitude);
				//if (resultCount > 0)
				//{
				//	for (int i = 0; i < resultCount; ++i)
				//	{
				//		if (_hitInfoList[i].collider != col)
				//			continue;
				//
				//		contactPoint = _hitInfoList[i].point;
				//		contactNormal = _hitInfoList[i].normal;
				//		collided = true;
				//		break;
				//	}
				//}
				if (!collided)
				{
					contactPoint = cachedTransform.position;
					contactNormal = cachedTransform.forward;
				}

				OnCollisionEnterAffectorProcessor(affectorProcessor, contactPoint, contactNormal);

				if (_signal.showHitEffect)
					HitEffect.ShowHitEffect(_signal, contactPoint, contactNormal, _statusStructForHitObject.weaponIDAtCreation);
			}
		}
	}

	Dictionary<AffectorProcessor, float> _dicHitStayTime = null;
	bool CheckHitStayInterval(AffectorProcessor affectorProcessor)
	{
		if (_dicHitStayTime == null)
			_dicHitStayTime = new Dictionary<AffectorProcessor, float>();

		if (_dicHitStayTime.ContainsKey(affectorProcessor) == false)
		{
			_dicHitStayTime.Add(affectorProcessor, Time.time);
			return true;
		}
		float lastTime = _dicHitStayTime[affectorProcessor];
		if (Time.time > lastTime + _signal.hitStayInterval)
		{
			_dicHitStayTime[affectorProcessor] = Time.time;
			return true;
		}
		return false;
	}

	void EnableRigidbodyAndCollider(bool enable)
	{
		EnableRigidbodyAndCollider(enable, _rigidbody, _collider, _triggerForHitStay);
	}

	public static void EnableRigidbodyAndCollider(bool enable, Rigidbody rigidbody, Collider collider, Collider additionalCollider = null, bool resetVelocityOnDisable = true)
	{
		if (rigidbody != null)
		{
			rigidbody.detectCollisions = enable;
			if (!enable && resetVelocityOnDisable) rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;
		}
		if (collider != null) collider.enabled = enable;
		if (additionalCollider != null) additionalCollider.enabled = enable;
	}

	void OnCollisionEnterAffectorProcessor(AffectorProcessor affectorProcessor, Vector3 contactPoint, Vector3 contactNormal)
	{
		// Reaction
		HitParameter hitParameter = new HitParameter();
		hitParameter.hitNormal = transform.forward;
		hitParameter.contactNormal = -contactNormal;
		hitParameter.contactPoint = contactPoint;
		//hitParameter.contactNormal = (col.transform.position - transform.position).normalized;
		//hitParameter.contactPoint = col.ClosestPointOnBounds(col.transform.position) + (hitParameter.contactNormal * colliderRadius * 0.3f);
		hitParameter.statusBase = _statusBase;
		hitParameter.statusStructForHitObject = _statusStructForHitObject;
		ApplyAffectorValue(affectorProcessor, _signal.affectorValueIdList, hitParameter);
		
		if (_signal.showHitBlink)
			HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
		if (_signal.showHitRimBlink)
			HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);
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
