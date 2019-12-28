using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BounceWallQuadHitObjectAffector : AffectorBase
{
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

		_bounceWallQuadAddCount = affectorValueLevelTableData.iValue1;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		BounceWallQuadHitObjectAffector bounceWallQuadHitObjectAffector = (BounceWallQuadHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.BounceWallQuadHitObject);
		if (bounceWallQuadHitObjectAffector == null)
			return 0;

		return bounceWallQuadHitObjectAffector.bounceWallQuadAddCount;
	}

	public static float GetDamageRate(int bounceWallQuadAddCount, int index, int actorInstanceId)
	{
		return MonsterThroughHitObjectAffector.GetDamageRate("BounceWallQuad", bounceWallQuadAddCount, index, actorInstanceId);
	}
}