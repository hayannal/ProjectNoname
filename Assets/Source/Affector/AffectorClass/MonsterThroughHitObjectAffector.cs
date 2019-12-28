using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MonsterThroughHitObjectAffector : AffectorBase
{
	int _piercingAddCount;
	public int piercingAddCount { get { return _piercingAddCount; } }

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

		_piercingAddCount = affectorValueLevelTableData.iValue1;
	}

	public static int GetAddCount(AffectorProcessor affectorProcessor)
	{
		MonsterThroughHitObjectAffector piercingHitObjectAffector = (MonsterThroughHitObjectAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.MonsterThroughHitObject);
		if (piercingHitObjectAffector == null)
			return 0;

		return piercingHitObjectAffector.piercingAddCount;
	}

	public static float GetDamageRate(int piercingAddCount, int index, int actorInstanceId)
	{
		return GetDamageRate("MonsterThrough", piercingAddCount, index, actorInstanceId);
	}

	public static float GetDamageRate(string id, int addCount, int index, int actorInstanceId)
	{
		string actorId = "";
		Actor actor = BattleInstanceManager.instance.FindActorByInstanceId(actorInstanceId);
		if (actor != null)
			actorId = actor.actorId;
		DamageRateTableData damageRateTableData = TableDataManager.instance.FindDamageTableData(id, addCount, actorId);
		if (damageRateTableData == null)
			return 1.0f;
		if (index >= damageRateTableData.rate.Length)
			index = damageRateTableData.rate.Length - 1;
		return damageRateTableData.rate[index];
	}
}