using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class ChangeAttackStateByTimeAffector : AffectorBase
{
	float _chargingTime = 0.0f;
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

		_chargingTime = affectorValueLevelTableData.fValue2;
		_actionNameHash = Animator.StringToHash(affectorValueLevelTableData.sValue1);
	}

	public override void UpdateAffector()
	{
		UpdateMovedTime();
	}

	float _movedTime = 0.0f;
	void UpdateMovedTime()
	{
		if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			return;

		_movedTime += Time.deltaTime;
	}

	void OnEventNormalAttack()
	{
		_movedTime = 0.0f;
	}

	bool CheckChange(ref int actionNameHash)
	{
		if (_movedTime >= _chargingTime)
		{
			actionNameHash = _actionNameHash;
			return true;
		}
		return false;
	}

	public static void OnEventNormalAttack(AffectorProcessor affectorProcessor)
	{
		ChangeAttackStateByTimeAffector changeAttackStateByTimeAffector = (ChangeAttackStateByTimeAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackStateByTime);
		if (changeAttackStateByTimeAffector == null)
			return;

		changeAttackStateByTimeAffector.OnEventNormalAttack();
	}

	public static void CheckChange(AffectorProcessor affectorProcessor, ref int actionNameHash)
	{
		ChangeAttackStateByTimeAffector changeAttackStateByTimeAffector = (ChangeAttackStateByTimeAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackStateByTime);
		if (changeAttackStateByTimeAffector == null)
			return;

		changeAttackStateByTimeAffector.CheckChange(ref actionNameHash);
	}
}