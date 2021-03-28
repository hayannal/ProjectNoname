using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DelayedCreateHitObjectAffector : AffectorBase
{
	float _endTime;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			finalized = true;
			return;
		}
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
	}

	AffectorValueLevelTableData _createHitObjectAffectorValue;
	public override void FinalizeAffector()
	{
		if (_createHitObjectAffectorValue == null)
		{
			_createHitObjectAffectorValue = new AffectorValueLevelTableData();
			_createHitObjectAffectorValue.iValue3 = _affectorValueLevelTableData.iValue3;
			_createHitObjectAffectorValue.sValue1 = _affectorValueLevelTableData.sValue1;
		}
		_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.CreateHitObject, _createHitObjectAffectorValue, _actor, false);
	}
}