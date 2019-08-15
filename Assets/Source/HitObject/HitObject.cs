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

	MeHitObject _signal;
	float createTime;
	StatusBase _statusBase = new StatusBase();
	StatusStructForHitObject _statusStructForHitObject;
	Rigidbody _rigidbody { get; set; }
	Collider _collider { get; set; }
	List<TrailRenderer> _listTrailRendererAfterCollision;
	List<GameObject> _listDisableObjectAfterCollision;
	bool _disableSelfObjectAfterCollision;


	static int HITOBJECT_LAYER;
	public void InitializeHitObject(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction)
	{
		if (HITOBJECT_LAYER == 0) HITOBJECT_LAYER = LayerMask.NameToLayer("HitObject");
		if (gameObject.layer == 0)
			ObjectUtil.ChangeLayer(gameObject, HITOBJECT_LAYER);

		_signal = meHit;
		createTime = Time.time;
		parentActor.actorStatus.CopyStatusBase(ref _statusBase);
		CopyEtcStatusForHitObject(ref _statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction);

		if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
		if (_collider == null) _collider = GetComponent<Collider>();
		EnableRigidbodyAndCollider(true);
		InitializeDisableObject();

		if (_signal.targetDetectType != eTargetDetectType.Collider)
		{
			if (_collider != null) _collider.enabled = false;
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
		if (_signal.lifeTime > 0.0f && _signal.targetDetectType == eTargetDetectType.Area)
		{
			CheckHitArea(transform.position, transform.forward, _signal, _statusBase, _statusStructForHitObject);
		}
	}



	void FixedUpdate()
	{
		// for life time 0.0f
		if (createTime + _signal.lifeTime < Time.time)
		{
			FinalizeHitObject();
			return;
		}
	}

	void FinalizeHitObject()
	{
		BattleInstanceManager.instance.OnFinalizeHitObject(_collider);

		//Destroy(gameObject);
		gameObject.SetActive(false);
	}

	//List<Collider> _listEnteredCollider = new List<Collider>();
	//void OnTriggerEnter(Collider col)
	void OnCollisionEnter(Collision collision)
	{
		//Debug.Log("hit object collision enter");
		bool collided = false;
		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			collided = true;

			if (_signal.showHitEffect)
				HitEffect.ShowHitEffect(_signal, contact.point, contact.normal, _statusStructForHitObject.weaponIDAtCreation);
		}

		if (collided)
		{
			EnableRigidbodyAndCollider(false);

			for (int i = 0; i < _listDisableObjectAfterCollision.Count; ++i)
				_listDisableObjectAfterCollision[i].SetActive(false);

			if (_disableSelfObjectAfterCollision)
				FinalizeHitObject();
		}
	}

	void EnableRigidbodyAndCollider(bool enable)
	{
		EnableRigidbodyAndCollider(enable, _rigidbody, _collider);
	}

	public static void EnableRigidbodyAndCollider(bool enable, Rigidbody rigidbody, Collider collider, bool resetVelocityOnDisable = true)
	{
		if (rigidbody != null)
		{
			rigidbody.detectCollisions = enable;
			if (!enable && resetVelocityOnDisable) rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;
		}
		if (collider != null) collider.enabled = enable;
	}

	public void OnCollisionEnterAffectorProcessor(AffectorProcessor affectorProcessor, ContactPoint contact)
	{
		Collider col = contact.thisCollider;
		if (!Team.CheckTeamFilter(_statusStructForHitObject.teamID, col, _signal.teamCheckType))
			return;

		// object radius
		float colliderRadius = ColliderUtil.GetRadius(col);
		if (colliderRadius == -1.0f)
			return;

		// find target
		//target = col.target;
		//Debug.Log("dasfasfasfds");

		// Reaction
		HitParameter hitParameter = new HitParameter();
		hitParameter.hitNormal = transform.forward;
		hitParameter.contactNormal = -contact.normal;
		hitParameter.contactPoint = contact.point;
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
}
