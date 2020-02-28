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
	List<Vector3> _listCandidatePosition;
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

		switch (_affectorValueLevelTableData.iValue1)
		{
			case 2:
			case 3:
				float[] valueList = BattleInstanceManager.instance.GetCachedMultiHitDamageRatioList(_affectorValueLevelTableData.sValue2);
				if (valueList.Length < 2)
				{
					finalized = true;
					return;
				}
				if (_listCandidatePosition == null) _listCandidatePosition = new List<Vector3>();
				_listCandidatePosition.Clear();
				for (int i = 0; i < valueList.Length; i += 2)
					_listCandidatePosition.Add(new Vector3(valueList[i], 0.0f, valueList[i + 1]));
				break;
		}

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

		_actor.baseCharacterController.movement.useGravity = true;

		bool findTargetTransform = false;
		switch (_affectorValueLevelTableData.iValue1)
		{
			case 0:
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
				break;
			case 1:
				_actor.cachedTransform.position = new Vector3(_affectorValueLevelTableData.fValue3, 0.0f, _affectorValueLevelTableData.fValue4);
				break;
			case 2:
			case 3:
				if (_actor.targetingProcessor.GetTargetCount() > 0)
				{
					Collider targetCollider = _actor.targetingProcessor.GetTarget();
					Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
					if (targetTransform != null)
					{
						Vector3 teleportPosition = GetNearestTeleportPosition(targetTransform.position, _affectorValueLevelTableData.iValue1 == 2);
						_actor.cachedTransform.position = teleportPosition;
						_actor.cachedTransform.rotation = Quaternion.LookRotation(targetTransform.position - teleportPosition);
					}
				}
				break;
		}

		if (_onStartEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, null);

		_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_affectorValueLevelTableData.sValue1), 0.05f);
		finalized = true;
	}

	int _agentTypeID = -1;
	Vector3 GetTeleportPosition(Vector3 targetPosition)
	{
		int tryBreakCount = 0;
		if (_agentTypeID == -1) _agentTypeID = MeLookAt.GetAgentTypeID(_actor);
		while (true)
		{
			Vector2 randomCircle = Random.insideUnitCircle.normalized * _affectorValueLevelTableData.fValue2;
			Vector3 desirePosition = targetPosition;
			desirePosition.x += randomCircle.x;
			desirePosition.z += randomCircle.y;

			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = _agentTypeID;
			if (NavMesh.SamplePosition(desirePosition, out hit, 0.1f, navMeshQueryFilter))
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

	Vector3 GetNearestTeleportPosition(Vector3 targetPosition, bool nearest)
	{
		float current = nearest ? float.MaxValue : 0.0f;
		int index = -1;
		for (int i = 0; i < _listCandidatePosition.Count; ++i)
		{
			Vector3 diff = _listCandidatePosition[i] - targetPosition;
			if (nearest)
			{
				if (diff.sqrMagnitude < current)
				{
					current = diff.sqrMagnitude;
					index = i;
				}
			}
			else
			{
				if (diff.sqrMagnitude > current)
				{
					current = diff.sqrMagnitude;
					index = i;
				}
			}
		}
		if (index == -1)
			return _listCandidatePosition[0];

		Vector3 desirePosition = Vector3.zero;
		if (index == -1)
			desirePosition = _listCandidatePosition[0];
		else
			desirePosition = _listCandidatePosition[index];

		Vector2 randomCircle = Random.insideUnitCircle * _affectorValueLevelTableData.fValue2;
		desirePosition.x += randomCircle.x;
		desirePosition.z += randomCircle.y;
		return desirePosition;
	}
}