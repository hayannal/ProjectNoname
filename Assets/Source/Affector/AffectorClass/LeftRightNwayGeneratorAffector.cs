using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LeftRightNwayGeneratorAffector : AffectorBase
{
	int _leftRightNwayAddCount;
	public int leftRightNwayAddCount { get { return _leftRightNwayAddCount; } }

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

		_leftRightNwayAddCount = affectorValueLevelTableData.iValue1;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		LeftRightNwayGeneratorAffector leftRightNwayGeneratorAffector = (LeftRightNwayGeneratorAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.LeftRightNwayGenerator);
		if (leftRightNwayGeneratorAffector == null)
			return 0;

		return leftRightNwayGeneratorAffector.leftRightNwayAddCount;
	}
}