using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DefaultContainerAffector : AffectorBase
{
	float _endTime;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public static bool ContainsValue(AffectorProcessor affectorProcessor, string sValue1)
	{
		List<AffectorBase> listDefaultContainerAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.DefaultContainer);
		if (listDefaultContainerAffector == null)
			return false;

		for (int i = 0; i < listDefaultContainerAffector.Count; ++i)
		{
			if (listDefaultContainerAffector[i].finalized)
				continue;
			DefaultContainerAffector defaultContainerAffector = listDefaultContainerAffector[i] as DefaultContainerAffector;
			if (defaultContainerAffector == null)
				continue;
			if (defaultContainerAffector._affectorValueLevelTableData.sValue1 == sValue1)
				return true;
		}
		return false;
	}

	public static float GetFloatValue2(AffectorProcessor affectorProcessor, string sValue1)
	{
		List<AffectorBase> listDefaultContainerAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.DefaultContainer);
		if (listDefaultContainerAffector == null)
			return 0.0f;

		for (int i = 0; i < listDefaultContainerAffector.Count; ++i)
		{
			if (listDefaultContainerAffector[i].finalized)
				continue;
			DefaultContainerAffector defaultContainerAffector = listDefaultContainerAffector[i] as DefaultContainerAffector;
			if (defaultContainerAffector == null)
				continue;
			if (defaultContainerAffector._affectorValueLevelTableData.sValue1 == sValue1)
				return defaultContainerAffector._affectorValueLevelTableData.fValue2;
		}
		return 0.0f;
	}
}