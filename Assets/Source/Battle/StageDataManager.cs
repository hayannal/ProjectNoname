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

	string CalcChaosNextMap(StageTableData stageTableData, int chapter, int nextStage)
	{
		return "";
	}
}
