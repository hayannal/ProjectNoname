using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HitFlagAffector : AffectorBase
{
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
	}

	bool _hitted = false;
	void OnHit()
	{
		_hitted = true;
	}

	bool PopHitted()
	{
		bool result = _hitted;
		_hitted = false;
		return result;
	}

	public static void OnHit(AffectorProcessor affectorProcessor)
	{
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.actorStatus.IsDie())
			return;

		HitFlagAffector hitFlagAffector = (HitFlagAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.HitFlag);
		if (hitFlagAffector == null)
			return;
		hitFlagAffector.OnHit();
	}

	public static bool PopHitted(AffectorProcessor affectorProcessor)
	{
		HitFlagAffector hitFlagAffector = (HitFlagAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.HitFlag);
		if (hitFlagAffector == null)
			return false;
		return hitFlagAffector.PopHitted();
	}
}