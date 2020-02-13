using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MEC;
using MecanimStateDefine;
using DG.Tweening;
using UnityEngine.AI;

public class TeleportTargetPositionAffector : AffectorBase
{
	static float s_StandbyPositionY = 500.0f;

	Vector3 _origPosition;
	GameObject _onStartEffectPrefab;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		_affectorValueLevelTableData = affectorValueLevelTableData;

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
			_onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
		if (_onStartEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, null);

		_actor.baseCharacterController.movement.useGravity = false;
		_origPosition = _actor.cachedTransform.position;
		_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, s_StandbyPositionY, _actor.cachedTransform.position.z);
		Timing.RunCoroutine(TeleportProcess());
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 원래라면 절대 들어오지 말아야하는데 Teleport중에 끝나지도 않았는데 Teleport가 또 온거다.
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated TeleportTargetPosition Affector.");
	}

	IEnumerator<float> TeleportProcess()
	{
		yield return Timing.WaitForSeconds(_affectorValueLevelTableData.fValue1);

		if (this == null)
			yield break;

		if (_onStartEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, null);

		_actor.baseCharacterController.movement.useGravity = true;

		bool findTargetTransform = false;
		if (_actor.targetingProcessor.GetTargetCount() > 0)
		{
			Collider targetCollider = _actor.targetingProcessor.GetTarget();
			Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
			if (targetTransform != null)
			{
				findTargetTransform = true;
				Vector3 teleportPosition = GetTeleportPosition(targetTransform.position);
				_actor.cachedTransform.position = teleportPosition;
				_actor.cachedTransform.rotation = Quaternion.LookRotation(targetTransform.position - teleportPosition);
			}
		}
		
		if (findTargetTransform == false)
			_actor.cachedTransform.position = _origPosition;

		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);
		finalized = true;
	}

	Vector3 GetTeleportPosition(Vector3 targetPosition)
	{
		int tryBreakCount = 0;
		while (true)
		{
			Vector2 randomCircle = Random.insideUnitCircle.normalized * _affectorValueLevelTableData.fValue2;
			Vector3 desirePosition = targetPosition;
			desirePosition.x += randomCircle.x;
			desirePosition.z += randomCircle.y;

			NavMeshHit hit;
			if (NavMesh.SamplePosition(desirePosition, out hit, 0.1f, NavMesh.AllAreas))
				return desirePosition;

			// exception handling
			++tryBreakCount;
			if (tryBreakCount > 200)
			{
				Debug.LogErrorFormat("Teleport Position Error. {0}. Not found valid random position.", StageManager.instance.GetCurrentSpawnFlagName());
				return _origPosition;
			}
		}
	}
}