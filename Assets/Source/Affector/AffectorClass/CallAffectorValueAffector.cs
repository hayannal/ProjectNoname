using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

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
		OnKill,
	}

	float _endTime;
	int _eventRemainCount;
	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_affectorValueLevelTableData = affectorValueLevelTableData;

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

	void OnEvent(eEventType eventType, float argument = 0.0f, HitObject.eTargetDetectType targetDetectType = HitObject.eTargetDetectType.Preset)
	{
		if ((eEventType)_affectorValueLevelTableData.iValue3 != eventType)
			return;
		if (_actor == null)
			return;
		if (eventType == eEventType.HpRate && argument > _affectorValueLevelTableData.fValue1)
			return;
		if (eventType == eEventType.OnDamage && _actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;
		if (eventType == eEventType.OnHit && _affectorValueLevelTableData.iValue1 == 1 && targetDetectType != HitObject.eTargetDetectType.Area)
			return;

		bool needFinalize = false;
		if (_eventRemainCount > 0)
		{
			_eventRemainCount -= 1;
			if (_eventRemainCount == 0)
				needFinalize = true;
		}

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = new StatusBase();
		_actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
		SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _actor);
		hitParameter.statusStructForHitObject.skillLevel = _affectorValueLevelTableData.level;

		switch (eventType)
		{
			case eEventType.OnDamage:
			case eEventType.OnHit:
				hitParameter.statusStructForHitObject.damage = argument;
				break;
		}

		if (_affectorValueLevelTableData.sValue2.Contains(","))
		{
			string[] affectorValueIdList = BattleInstanceManager.instance.GetCachedString2StringList(_affectorValueLevelTableData.sValue2);
			for (int i = 0; i < affectorValueIdList.Length; ++i)
				_affectorProcessor.ApplyAffectorValue(affectorValueIdList[i], hitParameter, false);
		}
		else
			_affectorProcessor.ApplyAffectorValue(_affectorValueLevelTableData.sValue2, hitParameter, false);

		if (needFinalize)
			finalized = true;
	}

	public static void OnEvent(AffectorProcessor affectorProcessor, eEventType eventType, float argument = 0.0f, HitObject.eTargetDetectType targetDetectType = HitObject.eTargetDetectType.Preset)
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
			callAffectorValueAffector.OnEvent(eventType, argument, targetDetectType);
		}
	}
}