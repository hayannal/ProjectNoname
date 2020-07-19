using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RemoveColliderHitObjectAffector : AffectorBase
{
	float _endTime;
	GameObject _onStartEffectPrefab;
	Transform _onStartEffectTransform;
	float _radius;
	bool _applyFollow;
	Vector3 _startPosition;
	Vector3 _startForward;
	float _areaAngle;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_radius = affectorValueLevelTableData.fValue2;
		_applyFollow = (affectorValueLevelTableData.iValue3 == 1);
		_startPosition = _actor.cachedTransform.position;
		_startForward = _actor.cachedTransform.forward;
		_areaAngle = affectorValueLevelTableData.fValue3;

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
			_onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
		if (_onStartEffectPrefab != null)
			_onStartEffectTransform = BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, null).transform;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (_onStartEffectPrefab != null)
			_onStartEffectTransform = BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, null).transform;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		UpdateEffectTransform();
		RemoveColliderHitObject();
	}

	void UpdateEffectTransform()
	{
		if (_applyFollow == false)
			return;
		if (_onStartEffectTransform == null)
			return;

		_onStartEffectTransform.position = _actor.cachedTransform.position;
	}

	void RemoveColliderHitObject()
	{
		Vector3 areaPosition = _applyFollow ? _actor.cachedTransform.position : _startPosition;

		// step 1. Physics.OverlapSphere
		float maxDistance = _radius;
		Collider[] result = Physics.OverlapSphere(areaPosition, maxDistance); // meHit.areaDistanceMax * parentTransform.localScale.x

		// step 2. Check each object.
		for (int i = 0; i < result.Length; ++i)
		{
			// affector processor
			HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(result[i]);
			if (hitObject == null)
				continue;

			// team check
			if (!Team.CheckTeamFilter(_actor.team.teamId, hitObject.statusStructForHitObject.teamId, Team.eTeamCheckFilter.Enemy))
				continue;

			// angle
			if (_areaAngle > 0.0f)
			{
				Vector3 diff = BattleInstanceManager.instance.GetTransformFromCollider(result[i]).position - areaPosition;
				diff.y = 0.0f;
				float colliderRadius = ColliderUtil.GetRadius(result[i]);
				if (colliderRadius == -1.0f)
					continue;

				Vector3 areaForward = _applyFollow ? _actor.cachedTransform.forward : _startForward;
				float angle = Vector3.Angle(areaForward, diff.normalized);
				float hypotenuse = Mathf.Sqrt(diff.sqrMagnitude + colliderRadius * colliderRadius);
				float adjustAngle = Mathf.Rad2Deg * Mathf.Acos(diff.magnitude / hypotenuse);
				if (_areaAngle * 0.5f < angle - adjustAngle)
					continue;
			}

			hitObject.OnFinalizeByRemove();
		}
	}
}