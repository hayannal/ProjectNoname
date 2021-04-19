using UnityEngine;
using System.Collections;

public class RicochetDistanceHitObjectAffector : AffectorBase
{
	float _ricochetDistance;
	public float ricochetDistance { get { return _ricochetDistance; } }

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

		_ricochetDistance = affectorValueLevelTableData.fValue2;
	}

	public static float GetRicochetDistance(AffectorProcessor affectorProcessor)
	{
		RicochetDistanceHitObjectAffector ricochetDistanceHitObjectAffector = (RicochetDistanceHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.RicochetDistanceHitObject);
		if (ricochetDistanceHitObjectAffector == null)
			return 0.0f;

		return ricochetDistanceHitObjectAffector.ricochetDistance;
	}
}