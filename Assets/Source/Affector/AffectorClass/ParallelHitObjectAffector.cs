using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParallelHitObjectAffector : AffectorBase
{
	int _parallelAddCount;
	public int parallelAddCount { get { return _parallelAddCount; } }
	float _parallelDistance;
	public float parallelDistance { get { return _parallelDistance; } }

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

		_parallelAddCount = affectorValueLevelTableData.iValue1;
		_parallelDistance = affectorValueLevelTableData.fValue2;
	}

	public static float GetDistance(AffectorProcessor affectorProcessor)
	{
		ParallelHitObjectAffector parallelHitObjectAffector = (ParallelHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ParallelHitObject);
		if (parallelHitObjectAffector == null)
			return 0;

		return parallelHitObjectAffector.parallelDistance;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		ParallelHitObjectAffector parallelHitObjectAffector = (ParallelHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.ParallelHitObject);
		if (parallelHitObjectAffector == null)
			return 0;

		return parallelHitObjectAffector.parallelAddCount;
	}

	public static float GetDamageRate(int parallelAddCount)
	{
		DamageRateTableData damageRateTableData = TableDataManager.instance.FindDamageTableData("Parallel", parallelAddCount);
		if (damageRateTableData == null)
			return 1.0f;
		if (damageRateTableData.rate.Length > 0)
			return damageRateTableData.rate[0];
		return 1.0f;
	}
}