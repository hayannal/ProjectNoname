using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChangeAttackStateAffector : AffectorBase
{
	int _changeCount = 0;
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

		_changeCount = affectorValueLevelTableData.iValue1;
		_actionNameHash = Animator.StringToHash(affectorValueLevelTableData.sValue1);
	}

	int _count;
	void OnEventNormalAttack()
	{
		++_count;
		if (_count >= _changeCount)
			_count -= _changeCount;
	}

	bool CheckChange(ref int actionNameHash)
	{
		if ((_count + 1) == _changeCount)
		{
			actionNameHash = _actionNameHash;
			return true;
		}
		return false;
	}

	public static void OnEventNormalAttack(AffectorProcessor affectorProcessor)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.OnEventNormalAttack();
	}

	public static void CheckChange(AffectorProcessor affectorProcessor, ref int actionNameHash)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.CheckChange(ref actionNameHash);
	}
}