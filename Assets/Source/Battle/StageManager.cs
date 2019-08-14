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
	public GameObject playerIndicatorPrefab;

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

		// get next stage info
		GetNextStageInfo();
	}

	void GetNextStageInfo()
	{
		int nextStage = playStage + 1;
		_reservedNextMap = CalcNextStageInfo(playChapter, nextStage);

		if (_nextStageTableData != null)
		{
			MapTableData mapTableData = TableDataManager.instance.FindMapTableData(_reservedNextMap);
			if (mapTableData != null)
				currentGatePillarPreview = mapTableData.gatePillarPreview;
		}
	}

	public float currentMonstrStandardHp { get { return _currentStageTableData.standardHp; } }
	public float currentMonstrStandardAtk { get { return _currentStageTableData.standardAtk; } }
	public float currentMonstrStandardDef { get { return _currentStageTableData.standardDef; } }
	public bool currentStageSwappable { get { return _currentStageTableData.swap; } }
	StageTableData _currentStageTableData = null;
	public StageTableData currentStageTableData { get { return _currentStageTableData; } set { _currentStageTableData = value; } }
	public void MoveToNextStage()
	{
		if (existNextStageInfo == false)
			return;

		playStage += 1;

		_currentStageTableData = _nextStageTableData;
		_nextStageTableData = null;

		string currentMap = _reservedNextMap;
		_reservedNextMap = "";
		Debug.LogFormat("CurrentMap = {0}", currentMap);

		StageTestCanvas.instance.RefreshCurrentMapText(playChapter, playStage, currentMap);

		MapTableData mapTableData = TableDataManager.instance.FindMapTableData(currentMap);
		if (mapTableData != null)
		{
			//defaultGroundSceneObject.SetActive(false);
			InstantiateMap(mapTableData);
		}

		GetNextStageInfo();
	}

	public bool existNextStageInfo { get { return (_nextStageTableData != null && string.IsNullOrEmpty(_reservedNextMap) == false); } }
	StageTableData _nextStageTableData = null;
	string _reservedNextMap = "";
	Dictionary<int, List<string>> _dicStageInfoByGrouping = new Dictionary<int, List<string>>();
	Dictionary<int, int> _dicCurrentIndexByGrouping = new Dictionary<int, int>();
	string CalcNextStageInfo(int chapter, int stage)
	{
		_nextStageTableData = null;
		StageTableData stageTableData = TableDataManager.instance.FindStageTableData(chapter, stage);
		if (stageTableData == null)
			return "";
		_nextStageTableData = stageTableData;

		if (string.IsNullOrEmpty(stageTableData.overridingMap) == false)
			return stageTableData.overridingMap;

		if (stageTableData.chapter > lastClearChapter || stageTableData.stage > lastClearStage)
			return stageTableData.firstFixedMap;

		List<string> listStageId = null;
		int currentIndex = 0;
		int currentGrouping = stageTableData.grouping;
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
				if (stageTableData.chapter != diffData.chapter)
					continue;
				if (stageTableData.grouping != diffData.grouping)
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



	// temp code
	public GameObject GetCurrentPowerSourcePrefab()
	{
		return null;
	}
}
