using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 캐릭터별로 전용팩의 개수가 달라지면서 Transfer할때 전용팩들은 아예 이전을 할 필요가 없어졌다.(레벨에 따라 자동으로 획득하면 된다.)
// 그래서 공용팩만 매니저에서 관리하기로 하고 전용팩은 캐릭터의 스킬 프로세서에서 관리하기로 한다.
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

	List<LevelPackTableData> _listAcquirableLevelPack = new List<LevelPackTableData>();
	List<LevelPackTableData> GetAcquirableActorLevelPackList()
	{
		if (_listAcquirableLevelPack.Count != 0)
			return _listAcquirableLevelPack;

		for (int i = 0; i < TableDataManager.instance.levelPackTable.dataArray.Length; ++i)
		{
			LevelPackTableData levelPackTableData = TableDataManager.instance.levelPackTable.dataArray[i];
			if (levelPackTableData.exclusive == false)
				_listAcquirableLevelPack.Add(levelPackTableData);
		}
		return _listAcquirableLevelPack;
	}

	public class RandomLevelPackInfo
	{
		public LevelPackTableData levelPackTableData;
		public float rate;
	}
	List<RandomLevelPackInfo> _listRandomLevelPackInfo = new List<RandomLevelPackInfo>();

	public List<RandomLevelPackInfo> GetRandomLevelPackTableDataList(PlayerActor playerActor, bool onlyNoHitLevelPack)
	{
		_listRandomLevelPackInfo.Clear();
		List<LevelPackTableData> listLevelPackTableData = GetAcquirableActorLevelPackList();
		float sumWeight = 0.0f;
		for (int i = 0; i < listLevelPackTableData.Count; ++i)
		{
			if (listLevelPackTableData[i].max != -1 && playerActor.skillProcessor.GetLevelPackStackCount(listLevelPackTableData[i].levelPackId) >= listLevelPackTableData[i].max)
				continue;
			if (listLevelPackTableData[i].dropWeight == 0.0f)
				continue;
			if (StageManager.instance.playChapter < listLevelPackTableData[i].openChapter)
				continue;
			if (onlyNoHitLevelPack && listLevelPackTableData[i].noHit == false)
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


	#region LevelPack List for Swap Character
	Dictionary<string, List<string>> _dicPlayerAcquiredLevelPack = new Dictionary<string, List<string>>();
	public void AddLevelPack(string playerActorId, string levelPackId)
	{
#if UNITY_EDITOR
		LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(levelPackId);
		if (levelPackTableData != null && levelPackTableData.exclusive)
		{
			Debug.LogError("Invalid Data : Exclusive LevelPack cannot be added to LevelPackDataManager");
			return;
		}
#endif

		if (_dicPlayerAcquiredLevelPack.ContainsKey(playerActorId))
		{
			_dicPlayerAcquiredLevelPack[playerActorId].Add(levelPackId);
			return;
		}

		List<string> listPlayerAcquiredLevelPack = new List<string>();
		listPlayerAcquiredLevelPack.Add(levelPackId);
		_dicPlayerAcquiredLevelPack.Add(playerActorId, listPlayerAcquiredLevelPack);
	}

	public void TransferLevelPackList(PlayerActor prevPlayerActor, PlayerActor nextPlayerActor)
	{
		// 이전하기 전에 먼저 레벨에 따라 자동으로 획득되는 팩부터 체크한다.
		nextPlayerActor.skillProcessor.CheckAllExclusiveLevelPack();

		if (_dicPlayerAcquiredLevelPack.ContainsKey(prevPlayerActor.actorId) == false)
			return;

		List<string> listPrevPlayerAcquiredLevelPack = _dicPlayerAcquiredLevelPack[prevPlayerActor.actorId];
		int currentPlayerAcquiredLevelPackCount = 0;
		if (_dicPlayerAcquiredLevelPack.ContainsKey(nextPlayerActor.actorId))
			currentPlayerAcquiredLevelPackCount = _dicPlayerAcquiredLevelPack[nextPlayerActor.actorId].Count;
		for (int i = 0; i < listPrevPlayerAcquiredLevelPack.Count; ++i)
		{
			if (i < currentPlayerAcquiredLevelPackCount)
				continue;
			string levelPackId = listPrevPlayerAcquiredLevelPack[i];
			LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(levelPackId);
			if (levelPackTableData == null)
				continue;

			if (levelPackTableData.exclusive == false)
			{
				// 이어받을 수 있는건 이어받으면 된다. 이어받지 못하는건 레벨에 따라 획득되는 전용레벨팩일거라 위에서 이미 얻어둔 상태일거다.
				AddLevelPack(nextPlayerActor.actorId, levelPackId);
				nextPlayerActor.skillProcessor.AddLevelPack(levelPackId, false, 0);
			}
		}
	}
	#endregion
}
