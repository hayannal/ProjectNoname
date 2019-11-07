using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BackNwayGeneratorAffector : AffectorBase
{
	int _backNwayAddCount;
	public int backNwayAddCount { get { return _backNwayAddCount; } }

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

		_backNwayAddCount = affectorValueLevelTableData.iValue1;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		BackNwayGeneratorAffector backNwayGeneratorAffector = (BackNwayGeneratorAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.BackNwayGenerator);
		if (backNwayGeneratorAffector == null)
			return 0;

		return backNwayGeneratorAffector.backNwayAddCount;
	}
}