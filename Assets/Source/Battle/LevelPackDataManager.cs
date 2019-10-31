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

			bool find = false;
			for (int j = 0; j < levelPackTableData.useActor.Length; ++j)
			{
				if (levelPackTableData.useActor[j] == actorId)
				{
					find = true;
					break;
				}
			}
			if (find)
				listLevelPackTableData.Add(levelPackTableData);
		}
		_dicActorLevelPack.Add(actorId, listLevelPackTableData);
		return listLevelPackTableData;
	}
}
