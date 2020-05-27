using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;

public class StageDataManager : MonoBehaviour
{
	public static StageDataManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("StageDataManager")).AddComponent<StageDataManager>();
			return _instance;
		}
	}
	static StageDataManager _instance = null;

	public static void DestroyInstance()
	{
		if (_instance != null)
		{
			Destroy(_instance.gameObject);
			_instance = null;
		}
	}

	public StageTableData nextStageTableData { get; set; }
	public string reservedNextMap { get; set; }
	public bool existNextStageInfo { get { return (nextStageTableData != null && string.IsNullOrEmpty(reservedNextMap) == false); } }

	public bool CalcNextStageInfo(int chapter, int nextStage, int highestPlayChapter, int highestClearStage)
	{
		nextStageTableData = null;
		bool selectedChaosMode = PlayerData.instance.currentChaosMode;
		StageTableData stageTableData = BattleInstanceManager.instance.GetCachedStageTableData(chapter, nextStage, nextStage == 0 ? false : selectedChaosMode);
		if (stageTableData == null)
		{
#if UNITY_EDITOR
			Debug.LogErrorFormat("Not found StageTableData. chapter = {0} / stage = {1} / chaos = {2}", chapter, nextStage, selectedChaosMode);
#endif
			return false;
		}
		nextStageTableData = stageTableData;
		reservedNextMap = CalcNextMap(stageTableData, chapter, nextStage, selectedChaosMode, highestPlayChapter, highestClearStage);
		return true;
	}

	// 테이블 파싱에 캐싱찾기 기능을 넣으면서 미리 전부다 계산해도 크게 무리가 없을거 같아서 차라리 한번에 다 캐싱해두기로 한다.
	// 차후 튕겼을때 복구해주는거 하게될때 이 딕셔너리만 복구시켜주면 될거다.
	// 챕터 변경시 씬을 리로드하지 않는다면 클리어가 필요하다.
	// 원래는 <int, string> 이었는데 클라이언트 세이브 파일 시리얼라이즈를 위해 <string, string> 으로 바꾸게 되었다.
	Dictionary<string, string> _dicCachedMap = new Dictionary<string, string>();
	string CalcNextMap(StageTableData stageTableData, int chapter, int nextStage, bool chaos, int highestPlayChapter, int highestClearStage)
	{
		if (_dicCachedMap.Count == 0)
		{
			int maxStage = StageManager.instance.GetMaxStage(chapter);
			for (int i = nextStage; i <= maxStage; ++i)
			{
				StageTableData diffData = BattleInstanceManager.instance.GetCachedStageTableData(chapter, i, i == 0 ? false : chaos);
				string mapId = "";
				if (chaos)
					mapId = CalcChaosNextMap(diffData, chapter, i);
				else
					mapId = CalcNormalNextMap(diffData, chapter, i, highestPlayChapter, highestClearStage);
				_dicCachedMap.Add(i.ToString(), mapId);
			}
		}

		if (_dicCachedMap.ContainsKey(nextStage.ToString()))
			return _dicCachedMap[nextStage.ToString()];
		return "";
	}

	public string GetCachedMap(int stage)
	{
		if (_dicCachedMap.ContainsKey(stage.ToString()))
			return _dicCachedMap[stage.ToString()];
		return "";
	}

	Dictionary<int, List<string>> _dicStageInfoByGrouping = new Dictionary<int, List<string>>();
	Dictionary<int, int> _dicCurrentIndexByGrouping = new Dictionary<int, int>();
	string CalcNormalNextMap(StageTableData stageTableData, int chapter, int nextStage, int highestPlayChapter, int highestClearStage)
	{
		List<string> listStageId = null;
		int currentIndex = 0;
		int currentGrouping = stageTableData.grouping;
		bool containsCachingData = false;
		if (stageTableData.chapter < highestPlayChapter || (stageTableData.chapter == highestPlayChapter && stageTableData.stage <= highestClearStage))
		{
			// 클리어한 스테이지가 들어오면 캐싱하거나 이미 캐싱되어있다면 캐싱된걸 가져와 쓴다.
			if (_dicStageInfoByGrouping.ContainsKey(currentGrouping))
			{
				listStageId = _dicStageInfoByGrouping[currentGrouping];
				currentIndex = _dicCurrentIndexByGrouping[currentGrouping];
				containsCachingData = true;
			}
			else
			{
				listStageId = new List<string>();
				int maxStage = StageManager.instance.GetMaxStage(chapter);
				for (int i = 0; i <= maxStage; ++i)
				{
					StageTableData diffData = BattleInstanceManager.instance.GetCachedStageTableData(chapter, i, PlayerData.instance.currentChaosMode);
					if (stageTableData.grouping != diffData.grouping)
						continue;

					if (diffData.chapter > highestPlayChapter || (diffData.chapter == highestPlayChapter && diffData.stage > highestClearStage))
						break;

					if (string.IsNullOrEmpty(stageTableData.firstFixedMap) == false && listStageId.Contains(diffData.firstFixedMap) == false)
						listStageId.Add(diffData.firstFixedMap);

					for (int j = 0; j < diffData.addRandomMap.Length; ++j)
					{
						string addRandomMap = diffData.addRandomMap[j];
						if (string.IsNullOrEmpty(addRandomMap) == false && listStageId.Contains(addRandomMap) == false)
							listStageId.Add(addRandomMap);
					}
				}

				for (int i = 0; i < listStageId.Count; ++i)
				{
					string temp = listStageId[i];
					int randomIndex = Random.Range(i, listStageId.Count);
					listStageId[i] = listStageId[randomIndex];
					listStageId[randomIndex] = temp;
				}

				_dicStageInfoByGrouping.Add(currentGrouping, listStageId);
				_dicCurrentIndexByGrouping.Add(currentGrouping, currentIndex);
			}
		}

		// 오버라이딩이 있다면 캐싱되어있더라도 무시하고 오버라이딩 맵을 사용한다.
		if (string.IsNullOrEmpty(stageTableData.overridingMap) == false)
			return stageTableData.overridingMap;

		// 처음 올라가는 곳이라면 firstFixedMap을 우선으로 사용하고 없다면 addRandomMap을 사용한다. 없으면 캐싱된걸 쓴다.
		if (stageTableData.chapter > highestPlayChapter || (stageTableData.chapter == highestPlayChapter && stageTableData.stage > highestClearStage))
		{
			if (string.IsNullOrEmpty(stageTableData.firstFixedMap) == false)
				return stageTableData.firstFixedMap;
			if (stageTableData.addRandomMap.Length > 0)
				return stageTableData.addRandomMap[Random.Range(0, stageTableData.addRandomMap.Length)];
		}

		// 위 조건들에서 걸리지 않았다면 캐싱된걸 사용한다. 만들어진때를 제외하곤 +1 해가면서 사용한다.
		if (containsCachingData)
		{
			++currentIndex;
			if (currentIndex > listStageId.Count)
				currentIndex = 0;
			_dicCurrentIndexByGrouping[currentGrouping] = currentIndex;
		}
		return listStageId[currentIndex];
	}

	Dictionary<int, string> _dicStageMapSet = new Dictionary<int, string>();
	Dictionary<string, List<string>> _dicNormalMonsterMapEarlyListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, List<string>> _dicAngelMapListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, List<string>> _dicNormalMonsterMapLateListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, List<string>> _dicRightBeforeBossMapListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, List<string>> _dicBossMapListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, int> _dicNormalMonsterMapEarlyIndexByMapSet = new Dictionary<string, int>();
	Dictionary<string, int> _dicAngelMapIndexByMapSet = new Dictionary<string, int>();
	Dictionary<string, int> _dicNormalMonsterMapLateIndexByMapSet = new Dictionary<string, int>();
	Dictionary<string, int> _dicRightBeforeBossMapIndexByMapSet = new Dictionary<string, int>();
	Dictionary<string, int> _dicBossMapIndexByMapSet = new Dictionary<string, int>();
	string CalcChaosNextMap(StageTableData stageTableData, int chapter, int nextStage)
	{
		// 카오스 모드더라도 로비-0번맵을 부를땐 노말맵처럼 처리해야한다. 캐싱도 하지 않는다.
		if (nextStage == 0)
			return CalcNormalNextMap(stageTableData, chapter, nextStage, -1, -1);

		if (stageTableData.mapSetId.Length > 0)
		{
			// 이전 스테이지에서 쓰던 맵셋과 같다면 패스해야해서 이전값을 구해둔다.
			string prevMapSetId = "";
			if (_dicStageMapSet.ContainsKey(nextStage - 1))
				prevMapSetId = _dicStageMapSet[nextStage - 1];

			// 랜덤이면 굴려야한다. 하나일땐 굴리지 않고 그냥 쓴다.
			string mapSetId = "";
			if (stageTableData.mapSetId.Length == 1)
				mapSetId = stageTableData.mapSetId[0];
			else if (stageTableData.mapSetId.Length > 1)
			{
				while (true)
				{
					string random = stageTableData.mapSetId[Random.Range(0, stageTableData.mapSetId.Length)];
					if (random != prevMapSetId)
					{
						mapSetId = random;
						break;
					}
				}
			}
			
			MapSetTableData mapSetTableData = TableDataManager.instance.FindMapSetTableData(mapSetId);
			if (mapSetTableData != null)
			{
				for (int i = 0; i < stageTableData.stageCount; ++i)
					_dicStageMapSet.Add(nextStage + i, mapSetId);

				// 리스트를 저장해두고 셔플시켜놓는다. 이미 들어있을땐 캐싱 필요없음.
				if (_dicNormalMonsterMapEarlyListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> normalMonsterMapEarlyList = new List<string>();
					for (int i = 0; i < mapSetTableData.normalMonsterMapEarly.Length; ++i)
						normalMonsterMapEarlyList.Add(mapSetTableData.normalMonsterMapEarly[i]);

					ObjectUtil.Shuffle<string>(normalMonsterMapEarlyList);
					_dicNormalMonsterMapEarlyListByMapSet.Add(mapSetId, normalMonsterMapEarlyList);
					_dicNormalMonsterMapEarlyIndexByMapSet.Add(mapSetId, 0);
				}

				if (_dicAngelMapListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> angelMapList = new List<string>();
					for (int i = 0; i < mapSetTableData.angelMap.Length; ++i)
						angelMapList.Add(mapSetTableData.angelMap[i]);

					ObjectUtil.Shuffle<string>(angelMapList);
					_dicAngelMapListByMapSet.Add(mapSetId, angelMapList);
					_dicAngelMapIndexByMapSet.Add(mapSetId, 0);
				}

				if (_dicNormalMonsterMapLateListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> normalMonsterMapLateList = new List<string>();
					for (int i = 0; i < mapSetTableData.normalMonsterMapLate.Length; ++i)
						normalMonsterMapLateList.Add(mapSetTableData.normalMonsterMapLate[i]);

					ObjectUtil.Shuffle<string>(normalMonsterMapLateList);
					_dicNormalMonsterMapLateListByMapSet.Add(mapSetId, normalMonsterMapLateList);
					_dicNormalMonsterMapLateIndexByMapSet.Add(mapSetId, 0);
				}

				if (_dicRightBeforeBossMapListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> rightBeforeBossMapList = new List<string>();
					for (int i = 0; i < mapSetTableData.rightBeforeBossMap.Length; ++i)
						rightBeforeBossMapList.Add(mapSetTableData.rightBeforeBossMap[i]);

					ObjectUtil.Shuffle<string>(rightBeforeBossMapList);
					_dicRightBeforeBossMapListByMapSet.Add(mapSetId, rightBeforeBossMapList);
					_dicRightBeforeBossMapIndexByMapSet.Add(mapSetId, 0);
				}

				if (_dicBossMapListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> bossMapList = new List<string>();
					for (int i = 0; i < mapSetTableData.bossMap.Length; ++i)
						bossMapList.Add(mapSetTableData.bossMap[i]);

					ObjectUtil.Shuffle<string>(bossMapList);
					_dicBossMapListByMapSet.Add(mapSetId, bossMapList);
					_dicBossMapIndexByMapSet.Add(mapSetId, 0);
				}
			}
		}

		// MapSet 테이블이 없는건 위쪽에서 등록해놨을테니 가져다 쓰는 라인이다.
		string selectedMapSetId = "";
		if (_dicStageMapSet.ContainsKey(nextStage))
			selectedMapSetId = _dicStageMapSet[nextStage];

		string selectedMap = "";
		switch (stageTableData.stageType)
		{
			case 0:
				if (_dicNormalMonsterMapEarlyListByMapSet.ContainsKey(selectedMapSetId))
				{
					List<string> listMap = _dicNormalMonsterMapEarlyListByMapSet[selectedMapSetId];
					int index = _dicNormalMonsterMapEarlyIndexByMapSet[selectedMapSetId];
					selectedMap = listMap[index];
					++index;
					if (index > listMap.Count)
						index = 0;
					_dicNormalMonsterMapEarlyIndexByMapSet[selectedMapSetId] = index;
				}
				break;
			case 1:
				if (_dicAngelMapListByMapSet.ContainsKey(selectedMapSetId))
				{
					List<string> listMap = _dicAngelMapListByMapSet[selectedMapSetId];
					int index = _dicAngelMapIndexByMapSet[selectedMapSetId];
					selectedMap = listMap[index];
					++index;
					if (index > listMap.Count)
						index = 0;
					_dicAngelMapIndexByMapSet[selectedMapSetId] = index;
				}
				break;
			case 2:
				if (_dicNormalMonsterMapLateListByMapSet.ContainsKey(selectedMapSetId))
				{
					List<string> listMap = _dicNormalMonsterMapLateListByMapSet[selectedMapSetId];
					int index = _dicNormalMonsterMapLateIndexByMapSet[selectedMapSetId];
					selectedMap = listMap[index];
					++index;
					if (index > listMap.Count)
						index = 0;
					_dicNormalMonsterMapLateIndexByMapSet[selectedMapSetId] = index;
				}
				break;
			case 3:
				if (_dicRightBeforeBossMapListByMapSet.ContainsKey(selectedMapSetId))
				{
					List<string> listMap = _dicRightBeforeBossMapListByMapSet[selectedMapSetId];
					int index = _dicRightBeforeBossMapIndexByMapSet[selectedMapSetId];
					selectedMap = listMap[index];
					++index;
					if (index > listMap.Count)
						index = 0;
					_dicRightBeforeBossMapIndexByMapSet[selectedMapSetId] = index;
				}
				break;
			case 4:
				if (_dicBossMapListByMapSet.ContainsKey(selectedMapSetId))
				{
					List<string> listMap = _dicBossMapListByMapSet[selectedMapSetId];
					int index = _dicBossMapIndexByMapSet[selectedMapSetId];
					selectedMap = listMap[index];
					++index;
					if (index > listMap.Count)
						index = 0;
					_dicBossMapIndexByMapSet[selectedMapSetId] = index;
				}
				break;
		}
		return selectedMap;
	}


	#region InProgressGame
	public string GetCachedMapData()
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.SerializeObject(_dicCachedMap);
	}

	public void SetCachedMapData(string jsonCachedMapData)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicCachedMap = serializer.DeserializeObject<Dictionary<string, string>>(jsonCachedMapData);
	}
	#endregion
}
