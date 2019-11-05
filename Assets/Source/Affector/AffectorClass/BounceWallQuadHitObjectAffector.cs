using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class BounceWallQuadHitObjectAffector : AffectorBase
{
	float _endTime;
	int _bounceWallQuadAddCount;
	public int bounceWallQuadAddCount { get { return _bounceWallQuadAddCount; } }

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

		_bounceWallQuadAddCount = affectorValueLevelTableData.iValue1;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		BounceWallQuadHitObjectAffector bounceWallQuadHitObjectAffector = (BounceWallQuadHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.BounceWallQuadHitObject);
		if (bounceWallQuadHitObjectAffector == null)
			return 0;

		return bounceWallQuadHitObjectAffector.bounceWallQuadAddCount;
	}

	public static float GetDamageRate(int bounceWallQuadAddCount, int index)
	{
		DamageRateTableData damageRateTableData = TableDataManager.instance.FindDamageTableData("BounceWallQuad", bounceWallQuadAddCount);
		if (damageRateTableData == null)
			return 1.0f;
		if (index < damageRateTableData.rate.Length)
			return damageRateTableData.rate[index];
		return 1.0f;
	}
}