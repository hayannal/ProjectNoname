using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TeleportedAffector : AffectorBase
{
	float _endTime;
	Vector3 _prevPosition;

	const float TeleportedHeight = 500.0f;

	bool _applied = false;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_affectorValueLevelTableData = affectorValueLevelTableData;

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue3))
		{
			GameObject onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (onStartEffectPrefab != null)
				BattleInstanceManager.instance.GetCachedObject(onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation);
		}

		_actor.EnableAI(false);
		_actor.actionController.idleAnimator.enabled = false;
		_actor.baseCharacterController.movement.useGravity = false;
		_prevPosition = _actor.cachedTransform.position;
		_actor.cachedTransform.position = new Vector3(_actor.cachedTransform.position.x, TeleportedHeight, _actor.cachedTransform.position.z);
		Push(this);
		_applied = true;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public override void FinalizeAffector()
	{
		if (_applied == false)
			return;

		Pop(this);
		_actor.cachedTransform.position = _prevPosition;
		_actor.baseCharacterController.movement.useGravity = true;
		_actor.actionController.idleAnimator.enabled = true;
		_actor.actionController.PlayActionByActionName("Idle");
		_actor.EnableAI(true);

		if (!string.IsNullOrEmpty(_affectorValueLevelTableData.sValue4))
		{
			GameObject onEndEffectPrefab = FindPreloadObject(_affectorValueLevelTableData.sValue4);
			if (onEndEffectPrefab != null)
				BattleInstanceManager.instance.GetCachedObject(onEndEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation);
		}
		_applied = false;
	}

	#region static
	static List<TeleportedAffector> s_listTeleportedAffector;
	public static void Push(TeleportedAffector teleportedAffector)
	{
		if (s_listTeleportedAffector == null)
			s_listTeleportedAffector = new List<TeleportedAffector>();

		s_listTeleportedAffector.Add(teleportedAffector);
	}

	public static void Pop(TeleportedAffector teleportedAffector)
	{
		if (s_listTeleportedAffector == null)
			return;

		s_listTeleportedAffector.Remove(teleportedAffector);
	}

	public static int GetActiveCount()
	{
		if (s_listTeleportedAffector == null)
			return 0;
		return s_listTeleportedAffector.Count;
	}

	public static void RestoreFirstObject()
	{
		if (s_listTeleportedAffector == null)
			return;

		if (s_listTeleportedAffector.Count > 0)
			s_listTeleportedAffector[0].finalized = true;
	}
	#endregion
}