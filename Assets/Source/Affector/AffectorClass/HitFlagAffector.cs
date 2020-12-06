using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HitFlagAffector : AffectorBase
{
	int _setType;
	int _resetType;
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

		_setType = affectorValueLevelTableData.iValue1;
		_resetType = affectorValueLevelTableData.iValue2;
	}

	bool _hitted = false;
	public bool hitted { get { return _hitted; } }
	void OnHit(HitObject.eTargetDetectType targetDetectType)
	{
		switch (_setType)
		{
			case 0:
				// 0일때는 그냥 켜면 된다.
				break;
			case 1:
				// 1일때는 Collider 타입에 한해서면 켜야한다.
				if (targetDetectType != HitObject.eTargetDetectType.Collider)
					return;
				break;
			case 2:
				// 2일때는 Area 타입에 한해서면 켜야한다.
				if (targetDetectType != HitObject.eTargetDetectType.Area)
					return;
				break;
		}
		_hitted = true;
	}

	void Reset(HitObject.eTargetDetectType targetDetectType)
	{
		switch (_resetType)
		{
			case 0:
				break;
			case 1:
				if (targetDetectType != HitObject.eTargetDetectType.Collider)
					return;
				break;
			case 2:
				if (targetDetectType != HitObject.eTargetDetectType.Area)
					return;
				break;
		}
		_hitted = false;
	}

	public static void OnHit(AffectorProcessor affectorProcessor, HitObject.eTargetDetectType targetDetectType)
	{
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.actorStatus.IsDie())
			return;

		HitFlagAffector hitFlagAffector = (HitFlagAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.HitFlag);
		if (hitFlagAffector == null)
			return;
		hitFlagAffector.OnHit(targetDetectType);
	}

	public static bool GetHitted(AffectorProcessor affectorProcessor)
	{
		HitFlagAffector hitFlagAffector = (HitFlagAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.HitFlag);
		if (hitFlagAffector == null)
			return false;
		return hitFlagAffector.hitted;
	}

	public static void OnEventAttack(AffectorProcessor affectorProcessor, HitObject.eTargetDetectType targetDetectType)
	{
		HitFlagAffector hitFlagAffector = (HitFlagAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.HitFlag);
		if (hitFlagAffector == null)
			return;
		hitFlagAffector.Reset(targetDetectType);
	}
}