using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class ChangeAttackStateAffector : AffectorBase
{
	int _swapTypeValue = 0;
	int _changeCount = 0;
	string _onStartStageKey = "";
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

		_swapTypeValue = affectorValueLevelTableData.iValue3;
		if (_swapTypeValue == 1)
			_changeCount = affectorValueLevelTableData.iValue1;
		_onStartStageKey = affectorValueLevelTableData.sValue2;
		_actionNameHash = Animator.StringToHash(affectorValueLevelTableData.sValue1);
	}

	int _count;
	void OnEventNormalAttack()
	{
		if (_remainBoostCount > 0)
		{
			--_remainBoostCount;
			return;
		}

		if (_swapTypeValue == 1)
		{
			++_count;
			if (_count >= _changeCount)
				_count -= _changeCount;
		}
	}

	void OnEventStartStage()
	{
		if (_swapTypeValue == 1)
		{
			if (string.IsNullOrEmpty(_onStartStageKey) == false && DefaultContainerAffector.ContainsValue(_affectorProcessor, _onStartStageKey))
			{
				_count = _changeCount - 1;
				return;
			}

			_count = 0;
		}
	}

	bool CheckChange(ref int actionNameHash)
	{
		if (_remainBoostCount > 0)
		{
			actionNameHash = _actionNameHash;
			return true;
		}

		if (_swapTypeValue == 1)
		{
			if ((_count + 1) == _changeCount)
			{
				actionNameHash = _actionNameHash;
				return true;
			}
		}
		else
		{
			actionNameHash = _actionNameHash;
			return true;
		}
		return false;
	}

	ObscuredInt _remainBoostCount;
	void CheckBulletBoost()
	{
		// 해당 아이템을 습득하면 5회는 무조건 강한 공격으로 나가게 된다.
		_remainBoostCount = 5;
	}

	public static void OnEventNormalAttack(AffectorProcessor affectorProcessor)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.OnEventNormalAttack();
	}

	public static void OnEventStartStage(AffectorProcessor affectorProcessor)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.OnEventStartStage();
	}

	public static void CheckChange(AffectorProcessor affectorProcessor, ref int actionNameHash)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.CheckChange(ref actionNameHash);
	}

	public static void CheckBulletBoost(AffectorProcessor affectorProcessor)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.CheckBulletBoost();
	}
}