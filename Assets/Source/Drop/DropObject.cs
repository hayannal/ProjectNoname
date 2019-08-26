using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropObject : MonoBehaviour
{
	public enum eDropType
	{
		None,
		Exp,
		Gold,
		LevelPack,
		Heart,
		Gacha,
		Ultimate,
	}

	static bool FloatRange(eDropType dropType)
	{
		switch (dropType)
		{
			case eDropType.Ultimate:
				return true;
		}
		return false;
	}

	public static void Drop(Transform rootTransform, string dropId, string addDropId)
	{
		Debug.Log("dropId : " + dropId + " / addDropId : " + addDropId);

		if (!string.IsNullOrEmpty(dropId))
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(dropId);
			if (dropTableData != null)
				Drop(rootTransform, dropTableData);
		}

		if (!string.IsNullOrEmpty(addDropId))
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(addDropId);
			if (dropTableData != null)
				Drop(rootTransform, dropTableData);
		}
	}

	static void Drop(Transform rootTransform, DropTableData dropTableData)
	{
		for (int i = 0; i < dropTableData.dropEnum.Length; ++i)
		{
			if (Random.value > dropTableData.probability[i])
				continue;

			eDropType dropType = (eDropType)dropTableData.dropEnum[i];
			if (FloatRange(dropType))
			{
				Drop(rootTransform, dropType, Random.Range(dropTableData.minValue[i], dropTableData.maxValue[i]), 0);
			}
			else
			{
				int minValue = Mathf.RoundToInt(dropTableData.minValue[i]);
				int maxValue = Mathf.RoundToInt(dropTableData.maxValue[i]);
				Drop(rootTransform, dropType, 0.0f, Random.Range(minValue, maxValue + 1));
			}
		}
	}

	public static void DropSp(Transform rootTransform, float dropSpValue)
	{
		if (dropSpValue == 0.0f)
			return;

		Debug.Log("dropSp : " + dropSpValue);

		Drop(rootTransform, eDropType.Ultimate, dropSpValue, 0);
	}

	static void Drop(Transform rootTransform, eDropType dropType, float floatValue, int intValue)
	{
		switch (dropType)
		{
			case eDropType.Ultimate:
				BattleInstanceManager.instance.playerActor.actorStatus.AddSP(floatValue);
				break;
		}
	}
}
