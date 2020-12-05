using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChangeAttackStateByDistanceAffector : AffectorBase
{
	float _baseDistance = 0.0f;
	int _actionNameHash = 0;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			// something else? for breakable object
			return;
		}

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		_baseDistance = affectorValueLevelTableData.fValue1;
		_actionNameHash = Animator.StringToHash(affectorValueLevelTableData.sValue1);
	}

	bool CheckChange(ref int actionNameHash)
	{
		TargetingProcessor targetingProcessor = _actor.targetingProcessor;
		if (targetingProcessor.GetTarget() == null)
			return false;

		Vector3 targetPosition = targetingProcessor.GetTargetPosition();
		Vector3 diff = targetPosition - _actor.cachedTransform.position;
		diff.y = 0.0f;
		if (diff.sqrMagnitude > _baseDistance * _baseDistance)
		{
			actionNameHash = _actionNameHash;
			return true;
		}
		return false;
	}

	public static void CheckChange(AffectorProcessor affectorProcessor, ref int actionNameHash)
	{
		ChangeAttackStateByDistanceAffector changeAttackStateByDistanceAffector = (ChangeAttackStateByDistanceAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackStateByDistance);
		if (changeAttackStateByDistanceAffector == null)
			return;

		changeAttackStateByDistanceAffector.CheckChange(ref actionNameHash);
	}
}