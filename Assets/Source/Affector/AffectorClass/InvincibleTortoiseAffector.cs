using UnityEngine;
using System.Collections;

public class InvincibleTortoiseAffector : AffectorBase
{
	float _endTime;
	AffectorBase _invincibleAffector;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
			return;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue1))
		{
			_actor.EnableAI(false);
			_actor.actionController.idleAnimator.enabled = false;
			_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(affectorValueLevelTableData.sValue1), 0.05f);
		}

		_returnStateName = affectorValueLevelTableData.sValue2;

		// invincible
		eAffectorType affectorType = eAffectorType.Invincible;
		AffectorValueLevelTableData invincibleAffectorValue = new AffectorValueLevelTableData();
		invincibleAffectorValue.fValue1 = -1.0f;
		_invincibleAffector = _affectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, invincibleAffectorValue, _actor, false);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	string _returnStateName;
	public override void FinalizeAffector()
	{
		if (_invincibleAffector != null)
			_invincibleAffector.finalized = true;

		if (!string.IsNullOrEmpty(_returnStateName))
		{
			_actor.EnableAI(true);
			_actor.actionController.idleAnimator.enabled = true;
			_actor.actionController.animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(_returnStateName), 0.05f);
		}
	}
}