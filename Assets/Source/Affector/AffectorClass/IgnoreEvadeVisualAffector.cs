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

	void UpdateTargetMonster()
	{
		// Visual Affector이기 때문에 UI쪽 처리할땐 로컬 플레이어인지 확인해야한다.
		//bool show = true;
		//if (_actor != BattleInstanceManager.instance.playerActor)
		//	show = false;
	}

	bool _attackStateStarted = false;
	void UpdateMecanimState()
	{
		// Attack 상태가 시작되는 시점부터 ui 표시하면서 테이블에 적혀있는 시간동안 % 올리면 된다.
		if (_applyNormalAttack)
		{
			if (_attackStateStarted == false)
			{
				if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Attack))
				{
					_attackStateStarted = true;

					// show ui
				}
			}
			else
			{
				if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Attack) == false)
				{
					_attackStateStarted = false;
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

					// show ui
				}
			}
			else
			{
				if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Ultimate) == false)
				{
					_attackStateStarted = false;
				}
			}
		}
	}

	public override void FinalizeAffector()
	{
	}
}