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
	static public void InitializeHit(Transform parentTransform, MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction)
	{
		// step 1. 
		GameObject hitObject = null;
		if (meHit.hitObjectPrefab != null)
		{
			hitObject = BattleInstanceManager.instance.GetCachedObject(meHit.hitObjectPrefab, GetSpawnPosition(parentTransform, meHit, parentActor.cachedTransform), parentActor.cachedTransform.rotation);
			//hitObject = (GameObject)Instantiate(meHit.hitObjectPrefab, , );
		}
		else if (meHit.lifeTime > 0.0f)
		{
			hitObject = new GameObject();
			hitObject.transform.position = parentTransform.TransformPoint(meHit.offset);
			hitObject.transform.rotation = parentActor.cachedTransform.rotation;
		}
		if (hitObject != null)
		{
			HitObject hitObjectComponent = hitObject.GetComponent<HitObject>();
			if (hitObjectComponent == null) hitObjectComponent = hitObject.AddComponent<HitObject>();
			hitObjectComponent.InitializeHitObject(meHit, parentActor, hitSignalIndexInAction);
			if (meHit.lifeTime > 0.0f && meHit.movable)
			{
				HitObjectMovement hitObjectMovementComponent = hitObject.GetComponent<HitObjectMovement>();
				if (hitObjectMovementComponent == null) hitObjectMovementComponent = hitObject.AddComponent<HitObjectMovement>();
				hitObjectMovementComponent.InitializeSignal(meHit, parentActor, hitObjectComponent._rigidbody, hitSignalIndexInAction);
				hitObjectComponent.SetHitObjectMovement(hitObjectMovementComponent);
			}
		}

		// step 2. Find Target and Reaction
		/*
		
		*/
		switch(meHit.targetDetectType)
		{
		case HitObject.eTargetDetectType.Preset:
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
					hitParameter.hitNormal = parentActor.cachedTransform.forward;
					hitParameter.contactNormal = (targetCollider.transform.position - parentActor.cachedTransform.position).normalized;
					hitParameter.contactPoint = targetCollider.transform.position + (-hitParameter.contactNormal * colliderRadius * 0.7f);
					hitParameter.contactPoint.y += targetCollider.bounds.size.y * 0.5f;
					hitParameter.statusBase = parentActor.actorStatus.statusBase;
					CopyEtcStatusForHitObject(ref hitParameter.statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction);

					ApplyAffectorValue(affectorProcessor, meHit.affectorValueIdList, hitParameter);

					if (meHit.showHitEffect)
						HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, hitParameter.statusStructForHitObject.weaponIDAtCreation);
					if (meHit.showHitBlink)
						HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
					if (meHit.showHitRimBlink)
						HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);
				}
			}
			break;
		case HitObject.eTargetDetectType.Area:
			Vector3 areaPosition = parentTransform.TransformPoint(meHit.offset);	// meHit.offset * parentTransform.localScale
			StatusStructForHitObject statusStructForHitObject = new StatusStructForHitObject();
			CopyEtcStatusForHitObject(ref statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction);
			CheckHitArea(areaPosition, parentTransform.forward, meHit, parentActor.actorStatus.statusBase, statusStructForHitObject);
			break;
		case HitObject.eTargetDetectType.Collider:
			break;
		}
	}

	public static Vector3 GetSpawnPosition(Transform parentTransform, MeHitObject meHit, Transform parentActorTransform)
	{
		if (meHit.offset == Vector3.zero)
			return parentTransform.position;

		if (meHit.createPositionType != eCreatePositionType.Bone)
			return parentTransform.TransformPoint(meHit.offset);    // meHit.offset * parentTransform.localScale

		if (meHit.useBoneRotation)
			return parentTransform.TransformPoint(meHit.offset);    // meHit.offset * parentTransform.localScale

		Vector3 parentActorPosition = parentActorTransform.position;
		Vector3 offsetPosition = parentActorTransform.TransformPoint(meHit.offset);
		offsetPosition -= parentActorPosition;
		return parentTransform.position + offsetPosition;
	}

	static void CopyEtcStatusForHitObject(ref StatusStructForHitObject statusStructForHitObject, Actor actor, MeHitObject meHit, int hitSignalIndexInAction)
	{
		statusStructForHitObject.teamID = actor.team.teamID;
		statusStructForHitObject.weaponIDAtCreation = 0;
		//if (meHit.useWeaponHitEffect)
		//	statusStructForHitObject.weaponIDAtCreation = actor.GetWeaponID(meHit.weaponDummyName);
		statusStructForHitObject.skillLevel = actor.actionController.GetCurrentSkillLevelByCurrentAction();
		statusStructForHitObject.hitSignalIndexInAction = hitSignalIndexInAction;
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
	int _remainBounceWallCount;


	static int HITOBJECT_LAYER;
	public void InitializeHitObject(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction)
	{
		if (HITOBJECT_LAYER == 0) HITOBJECT_LAYER = LayerMask.NameToLayer("HitObject");
		if (gameObject.layer == 0)
			ObjectUtil.ChangeLayer(gameObject, HITOBJECT_LAYER);

		_signal = meHit;
		_createTime = Time.time;
		parentActor.actorStatus.CopyStatusBase(ref _statusBase);
		CopyEtcStatusForHitObject(ref _statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction);

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
		_remainBounceWallCount = _signal.bounceWallCount;

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

	HitObjectMovement _hitObjectMovement;
	public void SetHitObjectMovement(HitObjectMovement hitObjectMovement)
	{
		_hitObjectMovement = hitObjectMovement;
	}

	void Update()
	{
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
			FinalizeHitObject();
			return;
		}
	}

	void FinalizeHitObject()
	{
		if (_listOneHitPerTarget != null)
			_listOneHitPerTarget.Clear();
		if (_dicHitStayTime != null)
			_dicHitStayTime.Clear();
		BattleInstanceManager.instance.OnFinalizeHitObject(_collider);

		//Destroy(gameObject);
		gameObject.SetActive(false);
	}

	void OnFinalizeByCollision()
	{
		EnableRigidbodyAndCollider(false);

		for (int i = 0; i < _listDisableObjectAfterCollision.Count; ++i)
			_listDisableObjectAfterCollision[i].SetActive(false);

		if (_disableSelfObjectAfterCollision)
			FinalizeHitObject();
	}

	// 이런 함수들도 추가해야하지 않을까.
	void OnFinalizeByLifeTime()
	{
	}




	List<AffectorProcessor> _listOneHitPerTarget = null;
	void OnCollisionEnter(Collision collision)
	{
		//Debug.Log("hit object collision enter");
		bool collided = false;
		bool groundQuadCollided = false;
		bool wallCollided = false;
		bool monsterCollided = false;
		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			collided = true;
			if (_signal.showHitEffect)
				HitEffect.ShowHitEffect(_signal, contact.point, contact.normal, _statusStructForHitObject.weaponIDAtCreation);

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
				groundQuadCollided = true;

			bool ignoreAffectorProcessor = false;
			if (_triggerForHitStay != null && contact.thisCollider == _collider)
				ignoreAffectorProcessor = true;

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null)
			{
				if (Team.CheckTeamFilter(_statusStructForHitObject.teamID, col, _signal.teamCheckType))
				{
					monsterCollided = true;

					if (_signal.oneHitPerTarget)
					{
						if (_listOneHitPerTarget == null) _listOneHitPerTarget = new List<AffectorProcessor>();
						if (_listOneHitPerTarget.Contains(affectorProcessor))
							ignoreAffectorProcessor = true;
					}
					if (ignoreAffectorProcessor == false && _signal.useHitStay == false)
					{
						OnCollisionEnterAffectorProcessor(affectorProcessor, contact.point, contact.normal);
						if (_signal.oneHitPerTarget)
							_listOneHitPerTarget.Add(affectorProcessor);
					}
				}
			}
			else if (groundQuadCollided == false)
				wallCollided = true;

			if (_signal.contactAll == false)
				break;
		}

		OnPostCollided(collided, groundQuadCollided, wallCollided, monsterCollided);
	}

	void OnPostCollided(bool collided, bool groundQuadCollided, bool wallCollided, bool monsterCollided)
	{
		if (collided == false)
			return;

		bool useThrough = false;
		if (groundQuadCollided)
		{
			if (_signal.quadThrough)
				useThrough = true;
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		if (wallCollided)
		{
			if (_signal.wallThrough)
				useThrough = true;
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		// Check End of HitObject
		if (monsterCollided)
		{
			if (_remainMonsterThroughCount > 0 || _remainMonsterThroughCount == -1)
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

		if (useThrough)
		{
			_tempTriggerOnCollision = true;
			_collider.isTrigger = true;
			if (_hitObjectMovement != null)
				_hitObjectMovement.ReinitializeForThrough();
			return;
		}

		if (_remainBounceWallCount > 0)
		{

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
