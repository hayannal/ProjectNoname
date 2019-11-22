using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public StageTableData nextStageTableData { get; set; }
	public string reservedNextMap { get; set; }
	public bool existNextStageInfo { get { return (nextStageTableData != null && string.IsNullOrEmpty(reservedNextMap) == false); } }

	public bool CalcNextStageInfo(int chapter, int nextStage, int highestPlayChapter, int highestClearStage)
	{
		nextStageTableData = null;
		StageTableData stageTableData = BattleInstanceManager.instance.GetCachedStageTableData(chapter, nextStage, PlayerData.instance.chaosMode);
		if (stageTableData == null)
			return false;
		nextStageTableData = stageTableData;
		reservedNextMap = CalcNextMap(stageTableData, chapter, nextStage, highestPlayChapter, highestClearStage);
		return true;
	}

	// 테이블 파싱에 캐싱찾기 기능을 넣으면서 미리 전부다 계산해도 크게 무리가 없을거 같아서 차라리 한번에 다 캐싱해두기로 한다.
	// 차후 튕겼을때 복구해주는거 하게될때 이 딕셔너리만 복구시켜주면 될거다.
	// 챕터 변경시 씬을 리로드하지 않는다면 클리어가 필요하다.
	Dictionary<int, string> _dicCachedMap = new Dictionary<int, string>();
	string CalcNextMap(StageTableData stageTableData, int chapter, int nextStage, int highestPlayChapter, int highestClearStage)
	{
		if (_dicCachedMap.Count == 0)
		{
			int maxStage = TableDataManager.instance.FindChapterTableData(chapter).maxStage;
			for (int i = nextStage; i <= maxStage; ++i)
			{
				StageTableData diffData = BattleInstanceManager.instance.GetCachedStageTableData(chapter, i, stageTableData.chaos);
				string mapId = "";
				if (stageTableData.chaos)
					mapId = CalcChaosNextMap(diffData, chapter, i);
				else
					mapId = CalcNormalNextMap(diffData, chapter, i, highestPlayChapter, highestClearStage);
				_dicCachedMap.Add(i, mapId);
			}
		}

		if (_dicCachedMap.ContainsKey(nextStage))
			return _dicCachedMap[nextStage];
		return "";
	}

	public string GetCachedMap(int stage)
	{
		if (_dicCachedMap.ContainsKey(stage))
			return _dicCachedMap[stage];
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
		if (stageTableData.chapter <= highestPlayChapter && stageTableData.stage <= highestClearStage)
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
				int maxStage = TableDataManager.instance.FindChapterTableData(chapter).maxStage;
				for (int i = 0; i <= maxStage; ++i)
				{
					StageTableData diffData = BattleInstanceManager.instance.GetCachedStageTableData(chapter, i, PlayerData.instance.chaosMode);
					if (stageTableData.grouping != diffData.grouping)
						continue;

					if (diffData.chapter > highestPlayChapter || diffData.stage > highestClearStage)
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
		if (stageTableData.chapter > highestPlayChapter || stageTableData.stage > highestClearStage)
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
	Dictionary<string, List<string>> _dicNormalMonsterMapListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, List<string>> _dicAngelMapListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, List<string>> _dicBossMapListByMapSet = new Dictionary<string, List<string>>();
	Dictionary<string, int> _dicNormalMonsterMapIndexByMapSet = new Dictionary<string, int>();
	Dictionary<string, int> _dicAngelMapIndexByMapSet = new Dictionary<string, int>();
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
				if (_dicNormalMonsterMapListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> normalMonsterMapList = new List<string>();
					for (int i = 0; i < mapSetTableData.normalMonsterMap.Length; ++i)
						normalMonsterMapList.Add(mapSetTableData.normalMonsterMap[i]);

					for (int i = 0; i < normalMonsterMapList.Count; ++i)
					{
						string temp = normalMonsterMapList[i];
						int randomIndex = Random.Range(i, normalMonsterMapList.Count);
						normalMonsterMapList[i] = normalMonsterMapList[randomIndex];
						normalMonsterMapList[randomIndex] = temp;
					}
					_dicNormalMonsterMapListByMapSet.Add(mapSetId, normalMonsterMapList);
					_dicNormalMonsterMapIndexByMapSet.Add(mapSetId, 0);
				}

				if (_dicAngelMapListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> angelMapList = new List<string>();
					for (int i = 0; i < mapSetTableData.angelMap.Length; ++i)
						angelMapList.Add(mapSetTableData.angelMap[i]);

					for (int i = 0; i < angelMapList.Count; ++i)
					{
						string temp = angelMapList[i];
						int randomIndex = Random.Range(i, angelMapList.Count);
						angelMapList[i] = angelMapList[randomIndex];
						angelMapList[randomIndex] = temp;
					}
					_dicAngelMapListByMapSet.Add(mapSetId, angelMapList);
					_dicAngelMapIndexByMapSet.Add(mapSetId, 0);
				}

				if (_dicBossMapListByMapSet.ContainsKey(mapSetId) == false)
				{
					List<string> bossMapList = new List<string>();
					for (int i = 0; i < mapSetTableData.bossMap.Length; ++i)
						bossMapList.Add(mapSetTableData.bossMap[i]);

					for (int i = 0; i < bossMapList.Count; ++i)
					{
						string temp = bossMapList[i];
						int randomIndex = Random.Range(i, bossMapList.Count);
						bossMapList[i] = bossMapList[randomIndex];
						bossMapList[randomIndex] = temp;
					}
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
				if (_dicNormalMonsterMapListByMapSet.ContainsKey(selectedMapSetId))
				{
					List<string> listMap = _dicNormalMonsterMapListByMapSet[selectedMapSetId];
					int index = _dicNormalMonsterMapIndexByMapSet[selectedMapSetId];
					selectedMap = listMap[index];
					++index;
					if (index > listMap.Count)
						index = 0;
					_dicNormalMonsterMapIndexByMapSet[selectedMapSetId] = index;
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
}
