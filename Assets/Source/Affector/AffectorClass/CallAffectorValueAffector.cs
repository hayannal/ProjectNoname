using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CallAffectorValueAffector : AffectorBase
{
	public enum eEventType
	{
		None,
		OnStartStage,
		OnDie,
		HpRate,
		OnDamage,
		OnHit,
	}

	float _endTime;
	int _eventRemainCount;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_tableEventType = (eEventType)affectorValueLevelTableData.iValue1;
		_tableAffectorValueId = affectorValueLevelTableData.sValue2;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// event count
		_eventRemainCount = affectorValueLevelTableData.iValue2;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	eEventType _tableEventType;
	string _tableAffectorValueId;
	void OnEvent(eEventType eventType, float argument = 0.0f)
	{
		if (_tableEventType != eventType)
			return;
		if (_actor == null)
			return;

		bool needFinalize = false;
		if (_eventRemainCount > 0)
		{
			_eventRemainCount -= 1;
			if (_eventRemainCount == 0)
				needFinalize = true;
		}

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = _actor.actorStatus.statusBase;
		SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _actor);
		_affectorProcessor.ApplyAffectorValue(_tableAffectorValueId, hitParameter, false);

		if (needFinalize)
			finalized = true;
	}

	public static void OnEvent(AffectorProcessor affectorProcessor, eEventType eventType, float argument = 0.0f)
	{
		List<AffectorBase> listCallAffectorValueAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.CallAffectorValue);
		if (listCallAffectorValueAffector == null)
			return;

		for (int i = 0; i < listCallAffectorValueAffector.Count; ++i)
		{
			if (listCallAffectorValueAffector[i].finalized)
				continue;
			CallAffectorValueAffector callAffectorValueAffector = listCallAffectorValueAffector[i] as CallAffectorValueAffector;
			if (callAffectorValueAffector == null)
				continue;
			callAffectorValueAffector.OnEvent(eventType, argument);
		}
	}
}