using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WallThroughHitObjectAffector : AffectorBase
{
	bool _wallThrough;
	bool _quadThrough;
	bool _overrideSphereCastRadius;
	bool _overrideCheckNav;
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

		if (affectorValueLevelTableData.fValue1 > 0.0f)
		{
			_overrideSphereCastRadius = true;
			_actor.targetingProcessor.sphereCastRadiusForCheckWall = affectorValueLevelTableData.fValue2;
		}
		if (affectorValueLevelTableData.fValue3 > 0.0f)
		{
			_overrideCheckNav = true;
			_actor.targetingProcessor.checkNavMeshReachable = (affectorValueLevelTableData.fValue4 > 0.0f);
		}
	}

	public override void FinalizeAffector()
	{
		if (_overrideSphereCastRadius || _overrideCheckNav)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actor.actorId);
			if (_overrideSphereCastRadius)
				_actor.targetingProcessor.sphereCastRadiusForCheckWall = actorTableData.targetingSphereRadius;
			if (_overrideCheckNav)
				_actor.targetingProcessor.checkNavMeshReachable = actorTableData.checkNavMeshReachable;
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