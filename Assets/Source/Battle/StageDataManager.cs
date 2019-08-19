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
	
	Dictionary<int, List<string>> _dicStageInfoByGrouping = new Dictionary<int, List<string>>();
	Dictionary<int, int> _dicCurrentIndexByGrouping = new Dictionary<int, int>();
	public bool CalcNextStageInfo(int chapter, int nextStage, int lastClearChapter, int lastClearStage)
	{
		nextStageTableData = null;
		StageTableData stageTableData = TableDataManager.instance.FindStageTableData(chapter, nextStage);
		if (stageTableData == null)
			return false;
		nextStageTableData = stageTableData;
		reservedNextMap = CalcNextMap(stageTableData, chapter, nextStage, lastClearChapter, lastClearStage);
		return true;
	}

	string CalcNextMap(StageTableData stageTableData, int chapter, int nextStage, int lastClearChapter, int lastClearStage)
	{
		List<string> listStageId = null;
		int currentIndex = 0;
		int currentGrouping = stageTableData.grouping;
		bool containsCachingData = false;
		if (stageTableData.chapter <= lastClearChapter && stageTableData.stage <= lastClearStage)
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
				for (int i = 0; i < TableDataManager.instance.stageTable.dataArray.Length; ++i)
				{
					StageTableData diffData = TableDataManager.instance.stageTable.dataArray[i];
					if (stageTableData.chapter != diffData.chapter)
						continue;
					if (stageTableData.grouping != diffData.grouping)
						continue;

					if (diffData.chapter > lastClearChapter || diffData.stage > lastClearStage)
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
		if (stageTableData.chapter > lastClearChapter || stageTableData.stage > lastClearStage)
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
}
