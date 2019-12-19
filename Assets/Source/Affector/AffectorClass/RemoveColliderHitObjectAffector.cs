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
		_applyFollow = (affectorValueLevelTableData.iValue1 == 3);
		_startPosition = _actor.cachedTransform.position;

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
			if (!Team.CheckTeamFilter(_actor.team.teamId, result[i], Team.eTeamCheckFilter.Enemy))
				continue;

			hitObject.FinalizeHitObject();
		}
	}
}