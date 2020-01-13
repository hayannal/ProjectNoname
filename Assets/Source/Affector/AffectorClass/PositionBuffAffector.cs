using UnityEngine;
using System.Collections;
using MecanimStateDefine;

public class PositionBuffAffector : AffectorBase
{
	float _endTime;
	GameObject _startEffectPrefab;
	Transform _startEffectTransform;
	GameObject _endEffectPrefab;

	Vector3 _lastPosition;
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

		_lastPosition = _actor.cachedTransform.position;
		_startEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue4) == false)
			_endEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);

		if (_startEffectPrefab != null)
			_startEffectTransform = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _lastPosition, Quaternion.identity).transform;
		BattleInstanceManager.instance.AddPositionBuffAffector(this);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (_startEffectTransform != null)
			_startEffectTransform.gameObject.SetActive(false);

		if (_endEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_endEffectPrefab, _lastPosition, Quaternion.identity);

		_lastPosition = _actor.cachedTransform.position;
		if (_startEffectPrefab != null)
			_startEffectTransform = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _lastPosition, Quaternion.identity).transform;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public bool ignoreFinalizeEffect { get; set; }
	public override void FinalizeAffector()
	{
		if (_startEffectTransform != null)
			_startEffectTransform.gameObject.SetActive(false);

		if (ignoreFinalizeEffect)
			return;

		// ignoreFinalizeEffect를 할때는 매니저 쪽에서 전체 클리어 할때다. 직접 Remove 호출할 필요 없다.
		BattleInstanceManager.instance.RemovePositionBuffAffector(this);

		if (_endEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_endEffectPrefab, _lastPosition, Quaternion.identity);
	}

	bool IsInRange(AffectorProcessor affectorProcessor)
	{
		Vector3 diff = affectorProcessor.actor.cachedTransform.position - _lastPosition;
		float sqrMagnitude = diff.x * diff.x + diff.z * diff.z;
		if (sqrMagnitude > _affectorValueLevelTableData.fValue2 * _affectorValueLevelTableData.fValue2)
			return false;
		return true;
	}




	public static int GetCircularSectorCount(AffectorProcessor affectorProcessor)
	{
		PositionBuffAffector positionBuffAffector = (PositionBuffAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.PositionBuff);
		if (positionBuffAffector == null)
			return 0;
		if (positionBuffAffector.IsInRange(affectorProcessor))
			return positionBuffAffector._affectorValueLevelTableData.iValue1;
		return 0;
	}

	public static float GetAttackAddRate(AffectorProcessor affectorProcessor)
	{
		PositionBuffAffector positionBuffAffector = (PositionBuffAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.PositionBuff);
		if (positionBuffAffector == null)
			return 0;
		if (positionBuffAffector.IsInRange(affectorProcessor))
			return positionBuffAffector._affectorValueLevelTableData.fValue3;
		return 0;
	}
}