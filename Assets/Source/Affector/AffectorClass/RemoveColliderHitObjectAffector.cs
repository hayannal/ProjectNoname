using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RemoveColliderHitObjectAffector : AffectorBase
{
	float _endTime;
	GameObject _onStartEffectPrefab;
	Transform _onStartEffectTransform;
	float _radius;
	bool _disableCollider;
	bool _applyFollow;
	Vector3 _startPosition;
	Vector3 _startForward;
	float _areaAngle;

	public static int AnimatorParameterHash;

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
		_disableCollider = (affectorValueLevelTableData.iValue2 == 1);
		_applyFollow = (affectorValueLevelTableData.iValue3 == 1);
		_startPosition = _actor.cachedTransform.position;
		_startForward = _actor.cachedTransform.forward;
		_areaAngle = affectorValueLevelTableData.fValue3;

		if (AnimatorParameterHash == 0 && !string.IsNullOrEmpty(affectorValueLevelTableData.sValue1))
			AnimatorParameterHash = BattleInstanceManager.instance.GetActionNameHash(affectorValueLevelTableData.sValue1);

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
			_onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
		if (_onStartEffectPrefab != null)
			_onStartEffectTransform = BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, null).transform;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (_onStartEffectTransform != null)
			_onStartEffectTransform.gameObject.SetActive(false);

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
		bool removed = false;
		Remove(_applyFollow ? _actor.cachedTransform.position : _startPosition, _radius, _areaAngle, _applyFollow ? _actor.cachedTransform.forward : _startForward, _actor.team.teamId, ref removed);

		// 1회 공격을 해야만 반격액션을 사용할 수 있는 구조로 바꾼다.
		if (removed && AnimatorParameterHash != 0)
		{
			bool hitted = HitFlagAffector.GetHitted(_affectorProcessor);
			if (hitted)
				_actor.actionController.animator.SetBool(AnimatorParameterHash, true);
		}
	}

	public static void Remove(Vector3 centerPosition, float areaDistance, float areaAngle, Vector3 areaForward, int teamId, ref bool removed)
	{
		Vector3 areaPosition = centerPosition;

		// 대부분의 Bullet은 다 1.0 높이로 오기때문에 1로 바꿔서 처리한다.
		areaPosition.y = 1.0f;

		// step 1. Physics.OverlapSphere
		float maxDistance = areaDistance;
		Collider[] result = Physics.OverlapSphere(areaPosition, maxDistance); // meHit.areaDistanceMax * parentTransform.localScale.x

		// step 2. Check each object.
		float hitEffectShowRate = 1.0f;
		for (int i = 0; i < result.Length; ++i)
		{
			// affector processor
			HitObject hitObject = BattleInstanceManager.instance.GetHitObjectFromCollider(result[i]);
			if (hitObject == null)
				continue;

			if (hitObject.IsIgnoreRemoveColliderAffector())
				continue;

			// team check
			if (!Team.CheckTeamFilter(teamId, hitObject.statusStructForHitObject.teamId, Team.eTeamCheckFilter.Enemy))
				continue;

			// angle
			if (areaAngle > 0.0f)
			{
				Vector3 diff = BattleInstanceManager.instance.GetTransformFromCollider(result[i]).position - areaPosition;
				diff.y = 0.0f;
				float colliderRadius = ColliderUtil.GetRadius(result[i]);
				if (colliderRadius == -1.0f)
					continue;

				float angle = Vector3.Angle(areaForward, diff.normalized);
				float hypotenuse = Mathf.Sqrt(diff.sqrMagnitude + colliderRadius * colliderRadius);
				float adjustAngle = Mathf.Rad2Deg * Mathf.Acos(diff.magnitude / hypotenuse);
				if (areaAngle * 0.5f < angle - adjustAngle)
					continue;
			}

			// 동시에 너무 많은 히트이펙트가 나오다보니 같은 프레임에 여러개가 나올땐 제한을 걸어본다.
			hitObject.OnFinalizeByRemove(hitEffectShowRate);
			hitEffectShowRate *= 0.5f;
			removed = true;
		}
	}

	bool IsIgnoreColliderHitObject()
	{
		return _disableCollider;
	}

	public static bool IsIgnoreColliderHitObject(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listRemoveColliderHitObjectAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.RemoveColliderHitObject);
		if (listRemoveColliderHitObjectAffector == null)
			return false;

		for (int i = 0; i < listRemoveColliderHitObjectAffector.Count; ++i)
		{
			if (listRemoveColliderHitObjectAffector[i].finalized)
				continue;
			RemoveColliderHitObjectAffector removeColliderHitObjectAffector = listRemoveColliderHitObjectAffector[i] as RemoveColliderHitObjectAffector;
			if (removeColliderHitObjectAffector == null)
				continue;
			if (removeColliderHitObjectAffector.IsIgnoreColliderHitObject())
				return true;
		}
		return false;
	}
}