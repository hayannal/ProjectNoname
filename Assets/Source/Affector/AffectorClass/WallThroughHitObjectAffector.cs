using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallThroughHitObjectAffector : AffectorBase
{
	bool _wallThrough;
	bool _quadThrough;
	bool _overrideSphereCastRadius;
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

		_wallThrough = (affectorValueLevelTableData.iValue1 == 1);
		_quadThrough = (affectorValueLevelTableData.iValue2 == 1);

		if (affectorValueLevelTableData.iValue3 == 1)
		{
			_overrideSphereCastRadius = true;
			_actor.targetingProcessor.sphereCastRadiusForCheckWall = affectorValueLevelTableData.fValue1;
		}
	}

	public override void FinalizeAffector()
	{
		if (_overrideSphereCastRadius)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actor.actorId);
			_actor.targetingProcessor.sphereCastRadiusForCheckWall = actorTableData.targetingSphereRadius;
		}
	}

	void CheckThrough(ref bool wallThrough, ref bool quadThrough)
	{
		wallThrough = _wallThrough;
		quadThrough = _quadThrough;
	}

	public static void CheckThrough(AffectorProcessor affectorProcessor, ref bool wallThrough, ref bool quadThrough)
	{
		WallThroughHitObjectAffector piercingHitObjectAffector = (WallThroughHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.WallThroughHitObject);
		if (piercingHitObjectAffector == null)
			return;

		piercingHitObjectAffector.CheckThrough(ref wallThrough, ref quadThrough);
	}
}