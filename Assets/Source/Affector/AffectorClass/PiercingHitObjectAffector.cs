using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class PiercingHitObjectAffector : AffectorBase
{
	float _endTime;
	int _piercingAddCount;
	public int piercingAddCount { get { return _piercingAddCount; } }

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

		_piercingAddCount = affectorValueLevelTableData.iValue1;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		PiercingHitObjectAffector piercingHitObjectAffector = (PiercingHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.PiercingHitObject);
		if (piercingHitObjectAffector == null)
			return 0;

		return piercingHitObjectAffector.piercingAddCount;
	}

	public static float GetDamageRate(int piercingAddCount, int index)
	{
		DamageRateTableData damageRateTableData = TableDataManager.instance.FindDamageTableData("MonsterThrough", piercingAddCount);
		if (damageRateTableData == null)
			return 1.0f;
		if (index < damageRateTableData.rate.Length)
			return damageRateTableData.rate[index];
		return 1.0f;
	}
}