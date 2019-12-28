using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RicochetHitObjectAffector : AffectorBase
{
	int _ricochetAddCount;
	public int ricochetAddCount { get { return _ricochetAddCount; } }

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

		_ricochetAddCount = affectorValueLevelTableData.iValue1;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		RicochetHitObjectAffector ricochetHitObjectAffector = (RicochetHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.RicochetHitObject);
		if (ricochetHitObjectAffector == null)
			return 0;

		return ricochetHitObjectAffector.ricochetAddCount;
	}

	public static float GetDamageRate(int ricochetAddCount, int index, int actorInstanceId)
	{
		return MonsterThroughHitObjectAffector.GetDamageRate("Ricochet", ricochetAddCount, index, actorInstanceId);
	}
}