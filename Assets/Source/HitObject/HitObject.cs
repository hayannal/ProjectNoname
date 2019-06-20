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
	static public void InitializeHit(Transform parentTransform, MeHitObject meHit, Actor parentActor)
	{
		// step 1. 
		GameObject hitObject = null;
		if (meHit.hitObjectPrefab != null)
		{
			hitObject = (GameObject)Instantiate(meHit.hitObjectPrefab, parentTransform.TransformPoint(meHit.offset), parentTransform.rotation);
		}
		else if (meHit.lifeTime > 0.0f)
		{
			hitObject = new GameObject();
			hitObject.transform.position = parentTransform.TransformPoint(meHit.offset);
			hitObject.transform.rotation = parentTransform.rotation;
		}
		if (hitObject != null)
		{
			HitObject hitObjectComponent = hitObject.AddComponent<HitObject>();
			hitObjectComponent.InitializeHitObject(meHit, parentActor);
			if (meHit.lifeTime > 0.0f && meHit.movable)
			{
				HitObjectMovement hitObjectMovementComponent = hitObject.AddComponent<HitObjectMovement>();
				hitObjectMovementComponent.InitializeSignal(meHit, parentActor.cachedTransform);
			}
		}

		// step 2. Find Target and Reaction
		int weaponIDAtCreation = 0;
		/*
		if (meHit.useWeaponHitEffect) weaponIDAtCreation = parentActor.GetWeaponID(meHit.weaponDummyName);
		*/
		switch(meHit.targetDetectType)
		{
		case HitObject.eTargetDetectType.Preset:
			TargetingProcessor targetSystem = parentActor.GetComponent<TargetingProcessor>();
			if (targetSystem != null)
			{
				for (int i = 0; i < targetSystem.GetTargetCount(); ++i)
				{
					Collider targetCollider = targetSystem.GetTargetList()[i];
					if (targetCollider == null)
						continue;
					AffectorProcessor affectorProcessor = targetCollider.GetComponent<AffectorProcessor>();
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
					CopyStatusForHitObject(ref hitParameter.statusStructForHitObject, parentActor);

					ApplyAffectorValue(affectorProcessor, meHit.affectorValueIDList, hitParameter);

					if (meHit.showHitEffect)
						HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, weaponIDAtCreation);
					if (meHit.showHitBlink)
						HitBlink.ShowHitBlink(targetCollider.transform);
					if (meHit.showHitRimBlink)
						HitRimBlink.ShowHitRimBlink(targetCollider.transform);
				}
			}
			break;
		case HitObject.eTargetDetectType.Area:
			Vector3 areaPosition = parentTransform.TransformPoint(meHit.offset);	// meHit.offset * parentTransform.localScale
			StatusStructForHitObject statusStructForHitObject = new StatusStructForHitObject();
			CopyStatusForHitObject(ref statusStructForHitObject, parentActor);
			CheckHitArea(areaPosition, parentTransform.forward, meHit, parentActor.actorStatus.statusBase, statusStructForHitObject, weaponIDAtCreation);
			break;
		case HitObject.eTargetDetectType.Collider:
			break;
		}
	}

	static void CopyStatusForHitObject(ref StatusStructForHitObject statusStructForHitObject, Actor actor)
	{
		statusStructForHitObject.teamID = actor.team.teamID;
		statusStructForHitObject.hp = actor.actorStatus.GetHP();
	}

	static void CheckHitArea(Vector3 areaPosition, Vector3 areaForward, MeHitObject meHit, StatusBase statusBase, StatusStructForHitObject statusForHitObject, int weaponIDAtCreation)
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
			AffectorProcessor affectorProcessor = result[i].GetComponent<AffectorProcessor>();
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

			ApplyAffectorValue(affectorProcessor, meHit.affectorValueIDList, hitParameter);

			if (meHit.showHitEffect)
				HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, weaponIDAtCreation);
			if (meHit.showHitBlink)
				HitBlink.ShowHitBlink(result[i].transform);
			if (meHit.showHitRimBlink)
				HitRimBlink.ShowHitRimBlink(result[i].transform);
		}
	}

	static void ApplyAffectorValue(AffectorProcessor affectorProcessor, string affectorValueIDList, HitParameter hitParameter)
	{
		if (string.IsNullOrEmpty(affectorValueIDList)) return;
		if (affectorProcessor == null) return;

		string[] affectorValueID = affectorValueIDList.Split(',');
		for (int j = 0; j < affectorValueID.Length; ++j)
			affectorProcessor.ExcuteAffectorValue(affectorValueID[j], hitParameter, true);
	}
	#endregion


	MeHitObject _signal;
	float createTime;
	StatusBase _statusBase = new StatusBase();
	StatusStructForHitObject _statusStructForHitObject;
	int _weaponIDAtCreation;


	public void InitializeHitObject(MeHitObject meHit, Actor parentActor)
	{
		_signal = meHit;
		createTime = Time.time;
		parentActor.actorStatus.CopyStatusBase(ref _statusBase);
		CopyStatusForHitObject(ref _statusStructForHitObject, parentActor);
		//if (_signal.useWeaponHitEffect)
		//	_weaponIDAtCreation = parentActor.GetWeaponID(_signal.weaponDummyName);

		if (_signal.targetDetectType != eTargetDetectType.Collider)
		{
			Collider col = GetComponent<Collider>();
			if (col != null) col.enabled = false;
		}
	}

	void Update()
	{
		if (_signal.lifeTime > 0.0f && _signal.targetDetectType == eTargetDetectType.Area)
		{
			CheckHitArea(transform.position, transform.forward, _signal, _statusBase, _statusStructForHitObject, _weaponIDAtCreation);
		}
	}



	void FixedUpdate()
	{
		// for life time 0.0f
		if (createTime + _signal.lifeTime < Time.time)
		{
			Destroy(gameObject);
			return;
		}
	}

	//List<Collider> _listEnteredCollider = new List<Collider>();
	//void OnTriggerEnter(Collider col)
	void OnCollisionEnter(Collision collision)
	{
		bool collided = false;
		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			collided = true;

			AffectorProcessor affectorProcessor = col.GetComponent<AffectorProcessor>();
			if (affectorProcessor == null)
			{
				if (_signal.showHitEffect)
					HitEffect.ShowHitEffect(_signal, contact.point, contact.normal, _weaponIDAtCreation);
				continue;
			}

			if (!Team.CheckTeamFilter(_statusStructForHitObject.teamID, col, _signal.teamCheckType))
				continue;

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(col);
			if (colliderRadius == -1.0f)
				continue;

			// find target
			//target = col.target;
			Debug.Log("dasfasfasfds");

			// Reaction
			HitParameter hitParameter = new HitParameter();
			hitParameter.hitNormal = transform.forward;
			hitParameter.contactNormal = contact.normal;
			hitParameter.contactPoint = contact.point;
			//hitParameter.contactNormal = (col.transform.position - transform.position).normalized;
			//hitParameter.contactPoint = col.ClosestPointOnBounds(col.transform.position) + (hitParameter.contactNormal * colliderRadius * 0.3f);
			hitParameter.statusBase = _statusBase;
			hitParameter.statusStructForHitObject = _statusStructForHitObject;
			ApplyAffectorValue(affectorProcessor, _signal.affectorValueIDList, hitParameter);

			if (_signal.showHitEffect)
				HitEffect.ShowHitEffect(_signal, hitParameter.contactPoint, hitParameter.contactNormal, _weaponIDAtCreation);
			if (_signal.showHitBlink)
				HitBlink.ShowHitBlink(col.transform);
			if (_signal.showHitRimBlink)
				HitRimBlink.ShowHitRimBlink(col.transform);
		}

		if (collided)
			DestroyRigidbody();
	}

	void DestroyRigidbody()
	{
		Rigidbody rigidbody = GetComponent<Rigidbody>();
		Collider collider = GetComponent<Collider>();
		if (rigidbody != null) Destroy(rigidbody);
		if (collider != null) Destroy(collider);
	}
}
