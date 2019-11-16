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

	Dictionary<string, List<LevelPackTableData>> _dicAcquirableActorLevelPack = new Dictionary<string, List<LevelPackTableData>>();
	List<LevelPackTableData> FindAcquirableActorLevelPackList(string actorId)
	{
		if (_dicAcquirableActorLevelPack.ContainsKey(actorId))
			return _dicAcquirableActorLevelPack[actorId];

		List<LevelPackTableData> listLevelPackTableData = new List<LevelPackTableData>();
		for (int i = 0; i < TableDataManager.instance.levelPackTable.dataArray.Length; ++i)
		{
			LevelPackTableData levelPackTableData = TableDataManager.instance.levelPackTable.dataArray[i];
			if (IsAcquirableLevelPack(levelPackTableData, actorId))
				listLevelPackTableData.Add(levelPackTableData);
		}
		_dicAcquirableActorLevelPack.Add(actorId, listLevelPackTableData);
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
	LevelPackTableData GetFallbackExclusiveLevelPackTableData()
	{
		if (_cachedExclusiveLevelPackTableDataAfterMax == null)
			_cachedExclusiveLevelPackTableDataAfterMax = TableDataManager.instance.FindLevelPackTableData(BattleInstanceManager.instance.GetCachedGlobalConstantString("ExclusiveLevelPackIdAfterMax"));
		return _cachedExclusiveLevelPackTableDataAfterMax;
	}

	public class RandomLevelPackInfo
	{
		public LevelPackTableData levelPackTableData;
		public float rate;
	}
	List<RandomLevelPackInfo> _listRandomLevelPackInfo = new List<RandomLevelPackInfo>();

	public List<RandomLevelPackInfo> GetRandomLevelPackTableDataList(PlayerActor playerActor)
	{
		_listRandomLevelPackInfo.Clear();
		List<LevelPackTableData> listLevelPackTableData = FindAcquirableActorLevelPackList(playerActor.actorId);
		float sumWeight = 0.0f;
		for (int i = 0; i < listLevelPackTableData.Count; ++i)
		{
			if (listLevelPackTableData[i].max != -1 && playerActor.skillProcessor.GetLevelPackStackCount(listLevelPackTableData[i].levelPackId) >= listLevelPackTableData[i].max)
				continue;
			if (listLevelPackTableData[i].dropWeight == 0.0f)
				continue;

			sumWeight += listLevelPackTableData[i].dropWeight;
			RandomLevelPackInfo newInfo = new RandomLevelPackInfo();
			newInfo.levelPackTableData = listLevelPackTableData[i];
			newInfo.rate = sumWeight;
			_listRandomLevelPackInfo.Add(newInfo);
		}
		if (_listRandomLevelPackInfo.Count == 0 && listLevelPackTableData.Count > 0)
		{
			sumWeight = listLevelPackTableData[0].dropWeight;
			RandomLevelPackInfo newInfo = new RandomLevelPackInfo();
			newInfo.levelPackTableData = listLevelPackTableData[0];
			newInfo.rate = sumWeight;
			_listRandomLevelPackInfo.Add(newInfo);
		}
		if (_listRandomLevelPackInfo.Count == 0)
		{
			Debug.LogError("Invalid Result : There are no level packs available.");
			return null;
		}

		for (int i = 0; i < _listRandomLevelPackInfo.Count; ++i)
			_listRandomLevelPackInfo[i].rate = _listRandomLevelPackInfo[i].rate / sumWeight;

		// 클리어는 가져가서 사용하는 곳에서 다 쓰고 알아서 Clear시켜준다.
		return _listRandomLevelPackInfo;
	}

	public LevelPackTableData GetRandomExclusiveLevelPackTableData(PlayerActor playerActor)
	{
		_listRandomLevelPackInfo.Clear();
		List<LevelPackTableData> listLevelPackTableData = FindAcquirableActorLevelPackList(playerActor.actorId);
		float sumWeight = 0.0f;
		for (int i = 0; i < listLevelPackTableData.Count; ++i)
		{
			if (listLevelPackTableData[i].exclusive == false)
				continue;
			if (listLevelPackTableData[i].max != -1 && playerActor.skillProcessor.GetLevelPackStackCount(listLevelPackTableData[i].levelPackId) >= listLevelPackTableData[i].max)
				continue;
			if (listLevelPackTableData[i].dropWeight == 0.0f)
				continue;

			sumWeight += listLevelPackTableData[i].dropWeight;
			RandomLevelPackInfo newInfo = new RandomLevelPackInfo();
			newInfo.levelPackTableData = listLevelPackTableData[i];
			newInfo.rate = sumWeight;
			_listRandomLevelPackInfo.Add(newInfo);
		}
		// exclusive일때 가져올게 없다면 미리 지정해둔 fallback용 exclusive 레벨팩을 보여준다.
		if (_listRandomLevelPackInfo.Count == 0)
			return GetFallbackExclusiveLevelPackTableData();		

		for (int i = 0; i < _listRandomLevelPackInfo.Count; ++i)
			_listRandomLevelPackInfo[i].rate = _listRandomLevelPackInfo[i].rate / sumWeight;

		LevelPackTableData result = null;
		float lastRandom = Random.value;
		for (int i = 0; i < _listRandomLevelPackInfo.Count; ++i)
		{
			if (lastRandom <= _listRandomLevelPackInfo[i].rate)
			{
				result = _listRandomLevelPackInfo[i].levelPackTableData;
				break;
			}
		}
		_listRandomLevelPackInfo.Clear();
		return result;
	}
}
