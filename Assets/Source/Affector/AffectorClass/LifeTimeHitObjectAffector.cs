using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LifeTimeHitObjectAffector : AffectorBase
{
	float _endTime;
	GameObject _startEffectPrefab;
	GameObject _startEffectObject;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue4) == false)
		{
			_startEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);

			if (_startEffectPrefab != null)
			{
				_startEffectObject = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
				FollowTransform.Follow(_startEffectObject.transform, _actor.cachedTransform, Vector3.zero);
			}
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (_startEffectObject != null)
			_startEffectObject.SetActive(false);

		if (_startEffectPrefab != null)
		{
			_startEffectObject = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
			FollowTransform.Follow(_startEffectObject.transform, _actor.cachedTransform, Vector3.zero);
		}
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}


	public static float GetAddLifeTime(AffectorProcessor affectorProcessor)
	{
		LifeTimeHitObjectAffector lifeTimeHitObjectAffector = (LifeTimeHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.LifeTimeHitObject);
		if (lifeTimeHitObjectAffector == null)
			return 0.0f;
		return lifeTimeHitObjectAffector._affectorValueLevelTableData.fValue2;
	}
}