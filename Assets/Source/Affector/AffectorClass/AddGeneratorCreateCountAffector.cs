using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AddGeneratorCreateCountAffector : AffectorBase
{
	int _addCreateCount;
	public int addCreateCount { get { return _addCreateCount; } }

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

		_addCreateCount = affectorValueLevelTableData.iValue1;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		AddGeneratorCreateCountAffector addGeneratorCreateCountAffector = (AddGeneratorCreateCountAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.AddGeneratorCreateCount);
		if (addGeneratorCreateCountAffector == null)
			return 0;

		return addGeneratorCreateCountAffector.addCreateCount;
	}
}