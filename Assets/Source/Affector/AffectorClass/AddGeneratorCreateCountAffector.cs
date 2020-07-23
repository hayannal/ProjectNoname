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
		List<AffectorBase> listAddGeneratorCreateCountAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddGeneratorCreateCount);
		if (listAddGeneratorCreateCountAffector == null)
			return 0;

		int result = 0;
		for (int i = 0; i < listAddGeneratorCreateCountAffector.Count; ++i)
		{
			if (listAddGeneratorCreateCountAffector[i].finalized)
				continue;
			AddGeneratorCreateCountAffector addGeneratorCreateCountAffector = listAddGeneratorCreateCountAffector[i] as AddGeneratorCreateCountAffector;
			if (addGeneratorCreateCountAffector == null)
				continue;
			result += addGeneratorCreateCountAffector.addCreateCount;
		}
		return result;
	}
}