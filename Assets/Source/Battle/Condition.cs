using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class Condition : MonoBehaviour
{
	enum eConditionType
	{
		AttackerHpRatio = 1,
		DefenderHpRatio,
		AttackerActorState,
		DefenderActorState,
		AttackerAffectorValueId,
		DefenderAffectorValueId,
		AttackerUltimateSkillGaugeRatio,
		AttackerUniqueGaugeRatio,
		DistanceOnFire,
	}

	enum eCompareType
	{
		Equal = 1,
		NotEqual,
		Little,
		Greater,
		LittleOrEqual,
		GreaterOrEqual,
	}

	public static bool CheckCondition(string conditionValueId, HitParameter hitParameter, AffectorProcessor defenderAffectorProcessor, Actor defenderActor)
	{
		ConditionValueTableData data = TableDataManager.instance.FindConditionValueTableData(conditionValueId);
		if (data == null)
		{
			Debug.LogErrorFormat("Not Found ConditionValueId = {0}", conditionValueId);
			return false;
		}

		eConditionType conditionType = (eConditionType)data.conditionId;
		eCompareType compareType = (eCompareType)data.compareType;

		bool useFloatCompare = false;
		float baseValue = 0.0f;
		switch (conditionType)
		{
			case eConditionType.AttackerHpRatio:
				useFloatCompare = true;
				baseValue = (hitParameter.statusBase._hp / hitParameter.statusBase.valueList[(int)eActorStatus.MaxHP]);
				break;
			case eConditionType.DefenderHpRatio:
				if (defenderActor == null) return false;
				useFloatCompare = true;
				baseValue = defenderActor.actorStatus.GetHPRatio();
				break;
			case eConditionType.AttackerActorState:

				break;
			case eConditionType.DefenderActorState:
				if (defenderAffectorProcessor.IsActorState(data.value))
					return true;
				break;
			case eConditionType.AttackerAffectorValueId:
				break;
			case eConditionType.DefenderAffectorValueId:
				if (defenderAffectorProcessor.IsContinuousAffectorValueId(data.value))
					return true;
				break;
			case eConditionType.AttackerUltimateSkillGaugeRatio:
				break;
			case eConditionType.AttackerUniqueGaugeRatio:
				break;
			case eConditionType.DistanceOnFire:
				useFloatCompare = true;
				break;
		}

		if (!useFloatCompare)
			return false;
		float.TryParse(data.value, out float floatValue);

		switch (compareType)
		{
			case eCompareType.Equal:
				if (baseValue == floatValue)
					return true;
				break;
			case eCompareType.NotEqual:
				if (baseValue != floatValue)
					return true;
				break;
			case eCompareType.Little:
				if (baseValue < floatValue)
					return true;
				break;
			case eCompareType.Greater:
				if (baseValue > floatValue)
					return true;
				break;
			case eCompareType.LittleOrEqual:
				if (baseValue <= floatValue)
					return true;
				break;
			case eCompareType.GreaterOrEqual:
				if (baseValue >= floatValue)
					return true;
				break;
		}

		return false;
	}
}
