using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SubjectNerd.Utilities;

public class StageManager : MonoBehaviour
{
	public static StageManager instance;

	// temp code
	public GameObject defaultGroundSceneObject;
	public GameObject gatePillarPrefab;
	public GameObject fadeCanvasPrefab;

	[Reorderable] public GameObject[] groundPrefabList;
	[Reorderable] public GameObject[] wallPrefabList;
	[Reorderable] public GameObject[] spawnFlagPrefabList;

	public int playChapter = 1;
	public int playStage = 0;
	public int lastClearChapter = 0;
	public int lastClearStage = 0;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		// temp code
		_currentGroundObject = defaultGroundSceneObject;
		BattleInstanceManager.instance.GetCachedObject(gatePillarPrefab, new Vector3(3.0f, 0.0f, 1.0f), Quaternion.identity);
	}

	public void NextStage()
	{
		playStage += 1;

		string currentMap = CalcStageInfo();
		Debug.LogFormat("CurrentMap = {0}", currentMap);

		StageTestCanvas.instance.RefreshCurrentMapText(playChapter, playStage, currentMap);

		MapTableData mapTableData = TableDataManager.instance.FindMapTableData(currentMap);
		if (mapTableData != null)
		{
			//defaultGroundSceneObject.SetActive(false);
			currentGatePillarPreview = mapTableData.gatePillarPreview;
			InstantiateMap(mapTableData);
		}
	}

	public float currentMonstrStandardHp { get; set; }
	public float currentMonstrStandardAtk { get; set; }
	public float currentMonstrStandardDef { get; set; }
	public bool currentStageSwappable { get; set; }
	Dictionary<int, List<string>> _dicStageInfoByGrouping = new Dictionary<int, List<string>>();
	Dictionary<int, int> _dicCurrentIndexByGrouping = new Dictionary<int, int>();
	string CalcStageInfo()
	{
		StageTableData currentStageTableData = TableDataManager.instance.FindStageTableData(playChapter, playStage);
		if (currentStageTableData == null)
			return "";

		currentMonstrStandardHp = currentStageTableData.standardHp;
		currentMonstrStandardAtk = currentStageTableData.standardAtk;
		currentMonstrStandardDef = currentStageTableData.standardDef;
		currentStageSwappable = currentStageTableData.swap;

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

		return listStageId[currentIndex];
	}

	GameObject _currentGroundObject;
	GameObject _currentWallObject;
	GameObject _currentSpawnFlagObject;
	void InstantiateMap(MapTableData mapTableData)
	{
		for (int i = 0; i < groundPrefabList.Length; ++i)
		{
			if (groundPrefabList[i].name.ToLower() == mapTableData.ground.ToLower())
			{
				GameObject newObject = Instantiate<GameObject>(groundPrefabList[i]);
				if (_currentGroundObject != null)
					_currentGroundObject.SetActive(false);
				_currentGroundObject = newObject;
				break;
			}
		}

		for (int i = 0; i < wallPrefabList.Length; ++i)
		{
			if (wallPrefabList[i].name.ToLower() == mapTableData.wall.ToLower())
			{
				GameObject newObject = Instantiate<GameObject>(wallPrefabList[i]);
				if (_currentWallObject != null)
					_currentWallObject.SetActive(false);
				_currentWallObject = newObject;
				break;
			}
		}

		for (int i = 0; i < spawnFlagPrefabList.Length; ++i)
		{
			if (spawnFlagPrefabList[i].name.ToLower() == mapTableData.spawnFlag.ToLower())
			{
				GameObject newObject = Instantiate<GameObject>(spawnFlagPrefabList[i]);
				if (_currentSpawnFlagObject != null)
					_currentSpawnFlagObject.SetActive(false);
				_currentSpawnFlagObject = newObject;
				break;
			}
		}

		BattleManager.instance.OnLoadedMap();
	}

	public Vector3 currentGatePillarSpawnPosition { get; set; }
	public string currentGatePillarPreview { get; set; }
}
