using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class IgnoreEvadeVisualAffector : AffectorBase
{
	// 이름에 Visual이 붙은건 실제로 회피무시 처리를 하는 곳은 BaseDamageAffector의 iValue3이기 때문에 그렇다.
	// 여기서는 타겟을 잡았을때의 명중률 표시등을 나타내는 기능만 수행한다.

	float _endTime;
	bool _applyNormalAttack = false;
	bool _applyUltimateAttack = false;
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

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_applyNormalAttack = (affectorValueLevelTableData.fValue3 > 0.0f);
		_applyUltimateAttack = (affectorValueLevelTableData.fValue4 > 0.0f);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		UpdateTargetMonster();
		UpdateMecanimState();
	}

	Collider _lastTargetCollider = null;
	bool _canvasShowState = false;
	void UpdateTargetMonster()
	{
		// Visual Affector이기 때문에 UI쪽 처리할땐 로컬 플레이어인지 확인해야한다.
		if (_actor != BattleInstanceManager.instance.playerActor)
		{
			PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(false, null);
			return;
		}

		if (_lastTargetCollider == BattleInstanceManager.instance.playerActor.playerAI.targetCollider)
			return;

		bool needShow = false;
		float evadeRate = 0.0f;
		if (BattleInstanceManager.instance.playerActor.playerAI.targetCollider != null)
		{
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(BattleInstanceManager.instance.playerActor.playerAI.targetCollider);
			if (affectorProcessor != null && affectorProcessor.actor != null)
			{
				evadeRate = affectorProcessor.actor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.EvadeRate);
				if (evadeRate > 0.0f)
					needShow = true;
			}
		}
		_lastTargetCollider = BattleInstanceManager.instance.playerActor.playerAI.targetCollider;

		if (needShow == false && _canvasShowState)
		{
			_currentIgnoreEvade = 0.0f;
			PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(false, null);
			_canvasShowState = false;
			return;
		}

		if (needShow && _canvasShowState == false)
		{
			_initEvade = evadeRate;
			_currentIgnoreEvade = 1.0f - _initEvade;
			PlayerIgnoreEvadeCanvas.instance.ShowIgnoreEvade(true, BattleInstanceManager.instance.playerActor);
			PlayerIgnoreEvadeCanvas.instance.SetPercent(_currentIgnoreEvade);
			_canvasShowState = true;
		}
	}

	float _initEvade = 0.0f;
	float _currentIgnoreEvade = 0.0f;
	bool _attackStateStarted = false;
	float _addSpeed = 0.0f;
	void UpdateMecanimState()
	{
		if (_currentIgnoreEvade == 0.0f)
			return;

		// Attack 상태가 시작되는 시점부터 ui 표시하면서 테이블에 적혀있는 시간동안 % 올리면 된다.
		if (_applyNormalAttack)
		{
			if (_attackStateStarted == false)
			{
				if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Attack))
				{
					_attackStateStarted = true;
					_addSpeed = _initEvade / _affectorValueLevelTableData.fValue3;
				}
			}
			else
			{
				if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Attack) == false)
				{
					_attackStateStarted = false;
					_currentIgnoreEvade = 1.0f - _initEvade;
					PlayerIgnoreEvadeCanvas.instance.SetPercent(_currentIgnoreEvade);
				}
			}
		}

		if (_applyUltimateAttack)
		{
			if (_attackStateStarted == false)
			{
				if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Ultimate))
				{
					_attackStateStarted = true;
					_addSpeed = _initEvade / _affectorValueLevelTableData.fValue4;
				}
			}
			else
			{
				if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Ultimate) == false)
				{
					_attackStateStarted = false;
					_currentIgnoreEvade = 1.0f - _initEvade;
					PlayerIgnoreEvadeCanvas.instance.SetPercent(_currentIgnoreEvade);
				}
			}
		}

		if (_attackStateStarted)
		{
			_currentIgnoreEvade += (_addSpeed * Time.deltaTime);
			if (_currentIgnoreEvade > 1.0f)
				_currentIgnoreEvade = 1.0f;
			PlayerIgnoreEvadeCanvas.instance.SetPercent(_currentIgnoreEvade);
		}
	}



	public override void FinalizeAffector()
	{
	}
}