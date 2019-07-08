using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
	public static StageManager instance;

	public GameObject defaultGroundSceneObject;
	public GameObject[] groundPrefabList;
	public GameObject[] wallPrefabList;
	public GameObject[] spawnFlagPrefabList;

	public int playChapter = 1;
	public int playStage = 0;
	public int lastClearChapter = 0;
	public int lastClearStage = 0;

	void Awake()
	{
		instance = this;
	}

	public void NextStage()
	{
		playStage += 1;

		string currentMap = CalcStageInfo();
		Debug.LogFormat("CurrentMap = {0}", currentMap);

		StageTestCanvas.instance.RefreshCurrentMapText(playChapter, playStage, currentMap);
	}

	Dictionary<int, List<string>> _dicStageInfoByGrouping = new Dictionary<int, List<string>>();
	Dictionary<int, int> _dicCurrentIndexByGrouping = new Dictionary<int, int>();
	string CalcStageInfo()
	{
		StageTableData currentStageTableData = TableDataManager.instance.FindStageTableData(playChapter, playStage);
		if (currentStageTableData == null)
			return "";

		if (string.IsNullOrEmpty(currentStageTableData.overridingMap) == false)
			return currentStageTableData.overridingMap;

		if (currentStageTableData.chapter > lastClearChapter || currentStageTableData.stage > lastClearStage)
			return currentStageTableData.firstFixedMap;

		List<string> listStageId = null;
		int currentIndex = 0;
		int currentGrouping = currentStageTableData.grouping;
		if (_dicStageInfoByGrouping.ContainsKey(currentGrouping))
		{
			listStageId = _dicStageInfoByGrouping[currentGrouping];
			currentIndex = _dicCurrentIndexByGrouping[currentGrouping];
			++currentIndex;
			if (currentIndex > listStageId.Count)
				currentIndex = 0;
			_dicCurrentIndexByGrouping[currentGrouping] = currentIndex;
		}
		else
		{
			listStageId = new List<string>();
			for (int i = 0; i < TableDataManager.instance.stageTable.dataArray.Length; ++i)
			{
				StageTableData diffData = TableDataManager.instance.stageTable.dataArray[i];
				if (currentStageTableData.chapter != diffData.chapter)
					continue;
				if (currentStageTableData.grouping != diffData.grouping)
					continue;

				if (diffData.chapter > lastClearChapter || diffData.stage > lastClearStage)
					break;

				if (listStageId.Contains(diffData.firstFixedMap) == false)
					listStageId.Add(diffData.firstFixedMap);

				if (string.IsNullOrEmpty(diffData.addRandomMap) == false && listStageId.Contains(diffData.addRandomMap) == false)
					listStageId.Add(diffData.addRandomMap);
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

		return listStageId[currentIndex];
	}
}
