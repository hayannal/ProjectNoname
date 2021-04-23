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
	bool _applyUltimate;
	float _overrideUltimateCooltime;
	bool _bulletRemovable;
	float _endTime;
	Transform _loopEffectTransform;
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

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_swapTypeValue = affectorValueLevelTableData.iValue3;
		if (_swapTypeValue == 1)
			_changeCount = affectorValueLevelTableData.iValue1;
		_onStartStageKey = affectorValueLevelTableData.sValue2;
		_actionNameHash = Animator.StringToHash(affectorValueLevelTableData.sValue1);
		_applyUltimate = (affectorValueLevelTableData.iValue2 == 1);
		_overrideUltimateCooltime = affectorValueLevelTableData.fValue2;
		_bulletRemovable = (affectorValueLevelTableData.sValue4 == "1");

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue3) == false)
		{
			GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (loopEffectPrefab != null)
			{
				_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform.position, Quaternion.identity).transform;
				FollowTransform.Follow(_loopEffectTransform, _actor.cachedTransform, Vector3.zero);
			}
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_loopEffectTransform);
			_loopEffectTransform = null;
		}
	}

	public override void DisableAffector()
	{
		// 오래가는 버프라서 스왑을 대비해서 Disable처리를 해줘야한다.
		FinalizeAffector();
	}

	int _count;
	void OnEventNormalAttack()
	{
		if (_applyUltimate)
			return;

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
		if (_applyUltimate)
			return;

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
		if (_applyUltimate)
			return false;

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

	bool CheckChangeUltimate(ref int actionNameHash)
	{
		if (_applyUltimate == false)
			return false;

		actionNameHash = _actionNameHash;
		return true;
	}

	void CheckUltimateCooltime(ref float ultimateCooltime)
	{
		if (_applyUltimate == false)
			return;

		ultimateCooltime = _overrideUltimateCooltime;
	}

	ObscuredInt _remainBoostCount;
	void CheckBulletBoost()
	{
		// 해당 아이템을 습득하면 5회는 무조건 강한 공격으로 나가게 된다.
		_remainBoostCount = 5;
	}

	bool CheckBulletRemovable()
	{
		return _bulletRemovable;
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

	public static void CheckChangeUltimate(AffectorProcessor affectorProcessor, ref int actionNameHash)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.CheckChangeUltimate(ref actionNameHash);
	}

	public static void CheckUltimateCooltime(AffectorProcessor affectorProcessor, ref float ultimateCooltime)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return;

		changeAttackStateAffector.CheckUltimateCooltime(ref ultimateCooltime);
	}

	public static bool CheckBulletRemovable(AffectorProcessor affectorProcessor)
	{
		ChangeAttackStateAffector changeAttackStateAffector = (ChangeAttackStateAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackState);
		if (changeAttackStateAffector == null)
			return false;

		return changeAttackStateAffector.CheckBulletRemovable();
	}
}