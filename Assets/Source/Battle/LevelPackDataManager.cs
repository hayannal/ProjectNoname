using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelPackDataManager : MonoBehaviour
{
	public static LevelPackDataManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("LevelPackDataManager")).AddComponent<LevelPackDataManager>();
			return _instance;
		}
	}
	static LevelPackDataManager _instance = null;

	Dictionary<string, List<LevelPackTableData>> _dicActorLevelPack = new Dictionary<string, List<LevelPackTableData>>();
	public List<LevelPackTableData> FindActorLevelPackList(string actorId)
	{
		if (_dicActorLevelPack.ContainsKey(actorId))
			return _dicActorLevelPack[actorId];

		List<LevelPackTableData> listLevelPackTableData = new List<LevelPackTableData>();
		for (int i = 0; i < TableDataManager.instance.levelPackTable.dataArray.Length; ++i)
		{
			LevelPackTableData levelPackTableData = TableDataManager.instance.levelPackTable.dataArray[i];
			if (IsAcquirableLevelPack(levelPackTableData, actorId))
				listLevelPackTableData.Add(levelPackTableData);
		}
		_dicActorLevelPack.Add(actorId, listLevelPackTableData);
		return listLevelPackTableData;
	}

	bool IsAcquirableLevelPack(LevelPackTableData levelPackTableData, string actorId)
	{
		if (levelPackTableData.exclusive == false)
			return true;

		// exclusive 예외처리 추가.
		if (levelPackTableData.useActor.Length == 1 && levelPackTableData.useActor[0] == "All")
			return true;

		for (int i = 0; i < levelPackTableData.useActor.Length; ++i)
		{
			if (levelPackTableData.useActor[i] == actorId)
				return true;
		}
		return false;
	}

	LevelPackTableData _cachedExclusiveLevelPackTableDataAfterMax;
	public LevelPackTableData GetFallbackExclusiveLevelPackTableData()
	{
		if (_cachedExclusiveLevelPackTableDataAfterMax == null)
			_cachedExclusiveLevelPackTableDataAfterMax = TableDataManager.instance.FindLevelPackTableData(BattleInstanceManager.instance.GetCachedGlobalConstantString("ExclusiveLevelPackIdAfterMax"));
		return _cachedExclusiveLevelPackTableDataAfterMax;
	}
}
