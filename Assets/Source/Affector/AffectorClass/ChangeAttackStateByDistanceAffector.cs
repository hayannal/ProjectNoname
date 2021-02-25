using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChangeAttackStateByDistanceAffector : AffectorBase
{
	bool _checkBurrow = false;
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

		_checkBurrow = (affectorValueLevelTableData.iValue1 == 1);
		_baseDistance = affectorValueLevelTableData.fValue1;
		_actionNameHash = Animator.StringToHash(affectorValueLevelTableData.sValue1);
	}

	bool CheckChange(ref int actionNameHash)
	{
		TargetingProcessor targetingProcessor = _actor.targetingProcessor;
		if (targetingProcessor.GetTarget() == null)
			return false;
		AffectorProcessor targetAffectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetingProcessor.GetTarget());
		if (targetAffectorProcessor == null)
			return false;

		// 타겟이 버로우면
		if (_checkBurrow)
		{
			bool applyChange = false;
			if (targetAffectorProcessor.IsContinuousAffectorType(eAffectorType.Burrow))
				applyChange = true;
			if (applyChange == false && BurrowOnStartAffector.CheckBurrow(targetAffectorProcessor))
				applyChange = true;
			if (applyChange)
			{
				actionNameHash = _actionNameHash;
				return true;
			}
		}

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