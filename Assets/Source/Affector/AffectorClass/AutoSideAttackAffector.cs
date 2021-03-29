using UnityEngine;
using System.Collections;

public class AutoSideAttackAffector : AffectorBase
{
	float _remainTime;

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

		_remainTime = affectorValueLevelTableData.fValue2;
	}

	public override void UpdateAffector()
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		if (_actor.targetingProcessor.GetTarget() == null)
			return;

		_remainTime -= Time.deltaTime;
		if (_remainTime < 0.0f)
		{
			_remainTime += _affectorValueLevelTableData.fValue2;
			SideAttack();
		}
	}

	AffectorValueLevelTableData _createHitObjectAffectorValue;
	void SideAttack()
	{
		if (_createHitObjectAffectorValue == null)
		{
			_createHitObjectAffectorValue = new AffectorValueLevelTableData();
			_createHitObjectAffectorValue.sValue1 = _affectorValueLevelTableData.sValue1;
		}
		_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.CreateHitObject, _createHitObjectAffectorValue, _actor, false);
	}

	void OnEventStartStage()
	{
		_remainTime = _affectorValueLevelTableData.fValue2;
	}



	public static void OnEventStartStage(AffectorProcessor affectorProcessor)
	{
		AutoSideAttackAffector autoSideAttackAffector = (AutoSideAttackAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.AutoSideAttack);
		if (autoSideAttackAffector == null)
			return;

		autoSideAttackAffector.OnEventStartStage();
	}
}