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
		DefenderPowerSource,
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
		bool useIntCompare = false;
		float baseValueFloat = 0.0f;
		int baseValueInt = 0;
		switch (conditionType)
		{
			case eConditionType.AttackerHpRatio:
				useFloatCompare = true;
				baseValueFloat = (hitParameter.statusBase._hp / hitParameter.statusBase.valueList[(int)eActorStatus.MaxHp]);
				break;
			case eConditionType.DefenderHpRatio:
				if (defenderActor == null) return false;
				useFloatCompare = true;
				baseValueFloat = defenderActor.actorStatus.GetHPRatio();
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
			case eConditionType.DefenderPowerSource:
				if (defenderActor == null) return false;
				ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(defenderActor.actorId);
				if (actorTableData == null) return false;
				useIntCompare = true;
				baseValueInt = actorTableData.powerSource;
				break;
			case eConditionType.AttackerUltimateSkillGaugeRatio:
				break;
			case eConditionType.AttackerUniqueGaugeRatio:
				break;
			case eConditionType.DistanceOnFire:
				useFloatCompare = true;
				break;
		}

		if (useFloatCompare)
		{
			float.TryParse(data.value, out float floatValue);
			switch (compareType)
			{
				case eCompareType.Equal:
					if (baseValueFloat == floatValue)
						return true;
					break;
				case eCompareType.NotEqual:
					if (baseValueFloat != floatValue)
						return true;
					break;
				case eCompareType.Little:
					if (baseValueFloat < floatValue)
						return true;
					break;
				case eCompareType.Greater:
					if (baseValueFloat > floatValue)
						return true;
					break;
				case eCompareType.LittleOrEqual:
					if (baseValueFloat <= floatValue)
						return true;
					break;
				case eCompareType.GreaterOrEqual:
					if (baseValueFloat >= floatValue)
						return true;
					break;
			}
		}

		if (useIntCompare)
		{
			int.TryParse(data.value, out int intValue);
			switch (compareType)
			{
				case eCompareType.Equal:
					if (baseValueInt == intValue)
						return true;
					break;
				case eCompareType.NotEqual:
					if (baseValueInt != intValue)
						return true;
					break;
				case eCompareType.Little:
					if (baseValueInt < intValue)
						return true;
					break;
				case eCompareType.Greater:
					if (baseValueInt > intValue)
						return true;
					break;
				case eCompareType.LittleOrEqual:
					if (baseValueInt <= intValue)
						return true;
					break;
				case eCompareType.GreaterOrEqual:
					if (baseValueInt >= intValue)
						return true;
					break;
			}
		}
		return false;
	}
}
