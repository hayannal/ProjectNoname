using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class DiagonalNwayGeneratorAffector : AffectorBase
{
	int _diagonalNwayAddCount;
	public int diagonalNwayAddCount { get { return _diagonalNwayAddCount; } }

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

		_diagonalNwayAddCount = affectorValueLevelTableData.iValue1;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		DiagonalNwayGeneratorAffector diagonalNwayGeneratorAffector = (DiagonalNwayGeneratorAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.DiagonalNwayGenerator);
		if (diagonalNwayGeneratorAffector == null)
			return 0;

		return diagonalNwayGeneratorAffector.diagonalNwayAddCount;
	}
}