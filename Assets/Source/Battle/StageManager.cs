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
	public int lastClearChapter = 1;
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

		// 차후에는 챕터의 0스테이지에서 시작하게 될텐데 0스테이지에서 쓸 맵을 알아내려면
		// 진입전에 아래 함수를 수행해서 캐싱할 수 있어야한다.
		// 방법은 세가지인데,
		// 1. static으로 빼서 데이터 처리만 먼저 할 수 있게 하는 방법
		// 2. DataManager 를 분리해서 데이터만 처리할 수 있게 하는 방법
		// 3. 스테이지 매니저가 언제나 살아있는 싱글톤 클래스가 되는 방법
		// 3은 다른 리소스도 들고있는데 살려둘 순 없으니 패스고 1은 너무 어거지다.
		// 결국 재부팅시 데이터 캐싱등의 처리까지 하려면 2번이 제일 낫다.
		bool result = StageDataManager.instance.CalcNextStageInfo(playChapter, playStage, lastClearChapter, lastClearStage);
		if (result)
		{
			string startMap = StageDataManager.instance.reservedNextMap;
			Debug.Log(startMap);
		}

		// get next stage info		
		GetNextStageInfo();
	}

	public void GetNextStageInfo()
	{
		int nextStage = playStage + 1;
		StageDataManager.instance.CalcNextStageInfo(playChapter, nextStage, lastClearChapter, lastClearStage);

		if (StageDataManager.instance.existNextStageInfo)
		{
			MapTableData mapTableData = TableDataManager.instance.FindMapTableData(StageDataManager.instance.reservedNextMap);
			if (mapTableData != null)
				currentGatePillarPreview = mapTableData.gatePillarPreview;
		}
	}

	public float currentMonstrStandardHp { get { return _currentStageTableData.standardHp; } }
	public float currentMonstrStandardAtk { get { return _currentStageTableData.standardAtk; } }
	public float currentMonstrStandardDef { get { return _currentStageTableData.standardDef; } }
	public float currentBossHpPer1Line { get { return _currentStageTableData.standardHp * _currentStageTableData.bossHpRatioPer1Line; } }
	public bool bossStage { get { return currentBossHpPer1Line != 0.0f; } }
	StageTableData _currentStageTableData = null;
	public StageTableData currentStageTableData { get { return _currentStageTableData; } set { _currentStageTableData = value; } }
	public void MoveToNextStage()
	{
		if (StageDataManager.instance.existNextStageInfo == false)
			return;

		playStage += 1;

		_currentStageTableData = StageDataManager.instance.nextStageTableData;
		StageDataManager.instance.nextStageTableData = null;

		string currentMap = StageDataManager.instance.reservedNextMap;
		StageDataManager.instance.reservedNextMap = "";
		//Debug.LogFormat("CurrentMap = {0}", currentMap);

		StageTestCanvas.instance.RefreshCurrentMapText(playChapter, playStage, currentMap);

		MapTableData mapTableData = TableDataManager.instance.FindMapTableData(currentMap);
		if (mapTableData != null)
		{
			//defaultGroundSceneObject.SetActive(false);
			BattleManager.instance.OnPreInstantiateMap();
			
			InstantiateMap(mapTableData);
		}

		GetNextStageInfo();
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
	public bool spawnPowerSourcePrefab { get; set; }
	public Vector3 currentPowerSourceSpawnPosition { get; set; }



	// temp code
	public GameObject[] powerSourcePrefabList;
	public GameObject GetCurrentPowerSourcePrefab()
	{
		int playerPowerSourceIndex = 0;
		if (BattleInstanceManager.instance.playerActor != null)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(BattleInstanceManager.instance.playerActor.actorId);
			if (actorTableData != null)
				playerPowerSourceIndex = actorTableData.powerSource;
		}
		if (playerPowerSourceIndex <= powerSourcePrefabList.Length)
			return powerSourcePrefabList[playerPowerSourceIndex];
		return null;
	}
}
