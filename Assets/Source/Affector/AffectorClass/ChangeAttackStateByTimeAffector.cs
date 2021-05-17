using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class ChangeAttackStateByTimeAffector : AffectorBase
{
	float _chargingTime = 0.0f;
	string _onStartStageKey = "";
	int _actionNameHash = 0;
	PlayerActor _playerActor;
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

		if (_actor.IsPlayerActor())
		{
			_playerActor = _actor as PlayerActor;
			if (_playerActor == null)
			{
				finalized = true;
				return;
			}
		}

		_chargingTime = affectorValueLevelTableData.fValue2;
		_onStartStageKey = affectorValueLevelTableData.sValue2;
		_actionNameHash = Animator.StringToHash(affectorValueLevelTableData.sValue1);
	}

	public override void UpdateAffector()
	{
		UpdateMovedTime();
	}

	public override void DisableAffector()
	{
		FinalizeAffector();
	}

	public override void FinalizeAffector()
	{
		if (_canvasShowState)
		{
			if (PlayerIgnoreEvadeCanvas.instance != null && PlayerIgnoreEvadeCanvas.instance.gameObject.activeSelf)
				PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(false, null);
			_canvasShowState = false;
			_movedTime = 0.0f;
		}
	}

	float _movedTime = 0.0f;
	bool _canvasShowState = false;
	void UpdateMovedTime()
	{
		if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			return;

		if (_movedTime < _chargingTime)
			_movedTime += Time.deltaTime;
		if (_movedTime > _chargingTime)
			_movedTime = _chargingTime;

		// UI쪽 처리할땐 로컬 플레이어인지 확인해야한다.
		if (_actor != BattleInstanceManager.instance.playerActor)
		{
			if (ExperienceCanvas.instance != null && ExperienceCanvas.instance.gameObject.activeSelf && _playerActor == CharacterListCanvas.instance.selectedPlayerActor)
			{
			}
			else
			{
				//PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(false, null);
				return;
			}
		}

		if (_movedTime > 0.0f && _canvasShowState == false)
		{
			PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(true, _playerActor);
			PlayerIgnoreEvadeCanvas.instance.SetImageType(PlayerIgnoreEvadeCanvas.eImageType.Charging);
			_canvasShowState = true;
		}

		if (_canvasShowState)
			PlayerIgnoreEvadeCanvas.instance.SetPercent(_movedTime / _chargingTime);
	}

	void OnEventNormalAttack()
	{
		_movedTime = 0.0f;

		if (_canvasShowState)
		{
			PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(false, null);
			_canvasShowState = false;
		}
	}

	void OnEventStartStage()
	{
		if (string.IsNullOrEmpty(_onStartStageKey))
			return;

		if (DefaultContainerAffector.ContainsValue(_affectorProcessor, _onStartStageKey) == false)
			return;

		if (_canvasShowState == false)
		{
			PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(true, _playerActor);
			PlayerIgnoreEvadeCanvas.instance.SetImageType(PlayerIgnoreEvadeCanvas.eImageType.Charging);
			_canvasShowState = true;
		}

		_movedTime = _chargingTime;
		PlayerIgnoreEvadeCanvas.instance.SetPercent(1.0f);
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

	public static void OnEventStartStage(AffectorProcessor affectorProcessor)
	{
		ChangeAttackStateByTimeAffector changeAttackStateByTimeAffector = (ChangeAttackStateByTimeAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackStateByTime);
		if (changeAttackStateByTimeAffector == null)
			return;

		changeAttackStateByTimeAffector.OnEventStartStage();
	}

	public static void CheckChange(AffectorProcessor affectorProcessor, ref int actionNameHash)
	{
		ChangeAttackStateByTimeAffector changeAttackStateByTimeAffector = (ChangeAttackStateByTimeAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ChangeAttackStateByTime);
		if (changeAttackStateByTimeAffector == null)
			return;

		changeAttackStateByTimeAffector.CheckChange(ref actionNameHash);
	}
}