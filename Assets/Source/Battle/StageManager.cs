#define USE_MAIN_SCENE

using System.Collections.Generic;
using UnityEngine;
#if USE_MAIN_SCENE
using MEC;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#else
using SubjectNerd.Utilities;
#endif

public class StageManager : MonoBehaviour
{
	public static StageManager instance;

#if USE_MAIN_SCENE
#else
	// temp code
	public GameObject defaultPlaneSceneObject;
	public GameObject defaultGroundSceneObject;
#endif

	public GameObject gatePillarPrefab;
	public GameObject fadeCanvasPrefab;

#if USE_MAIN_SCENE
#else
	[Reorderable] public GameObject[] planePrefabList;
	[Reorderable] public GameObject[] groundPrefabList;
	[Reorderable] public GameObject[] wallPrefabList;
	[Reorderable] public GameObject[] spawnFlagPrefabList;
#endif

	public int playChapter { get; set; }
	public int playStage { get; set; }
	public int lastClearChapter { get; set; }
	public int lastClearStage { get; set; }

	void Awake()
	{
		instance = this;
	}

#if USE_MAIN_SCENE
	public void InitializeStage(int chapter, int stage, int lastClearChapter, int lastClearStage)
	{
		playChapter = chapter;
		playStage = stage;
		this.lastClearChapter = lastClearChapter;
		this.lastClearStage = lastClearStage;

		StageDataManager.instance.CalcNextStageInfo(chapter, stage, lastClearChapter, lastClearStage);

		if (StageDataManager.instance.existNextStageInfo)
		{
			MapTableData mapTableData = TableDataManager.instance.FindMapTableData(StageDataManager.instance.reservedNextMap);
			if (mapTableData != null)
				PrepareNextMap(mapTableData, StageDataManager.instance.nextStageTableData.environmentSetting);
		}
	}
#else
	void Start()
	{

		// temp code
		_currentPlaneObject = defaultPlaneSceneObject;
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
#endif

	public void GetNextStageInfo()
	{
		int nextStage = playStage + 1;
		StageDataManager.instance.CalcNextStageInfo(playChapter, nextStage, lastClearChapter, lastClearStage);

		if (StageDataManager.instance.existNextStageInfo)
		{
			MapTableData mapTableData = TableDataManager.instance.FindMapTableData(StageDataManager.instance.reservedNextMap);
			if (mapTableData != null)
			{
				currentGatePillarPreview = mapTableData.gatePillarPreview;
#if USE_MAIN_SCENE
				PrepareNextMap(mapTableData, StageDataManager.instance.nextStageTableData.environmentSetting);
#endif
			}
		}
	}

	public float currentMonstrStandardHp { get { return _currentStageTableData.standardHp; } }
	public float currentMonstrStandardAtk { get { return _currentStageTableData.standardAtk; } }
	public float currentBossHpPer1Line { get { return _currentStageTableData.standardHp * _currentStageTableData.bossHpRatioPer1Line; } }
	public bool bossStage { get { return currentBossHpPer1Line != 0.0f; } }
	StageTableData _currentStageTableData = null;
	public StageTableData currentStageTableData { get { return _currentStageTableData; } set { _currentStageTableData = value; } }
	public void MoveToNextStage(bool ignorePlus = false)
	{
		if (StageDataManager.instance.existNextStageInfo == false)
			return;

		if (ignorePlus == false)
			playStage += 1;

		_currentStageTableData = StageDataManager.instance.nextStageTableData;
		StageDataManager.instance.nextStageTableData = null;

		string currentMap = StageDataManager.instance.reservedNextMap;
		StageDataManager.instance.reservedNextMap = "";
		//Debug.LogFormat("CurrentMap = {0}", currentMap);

		//StageTestCanvas.instance.RefreshCurrentMapText(playChapter, playStage, currentMap);

		MapTableData mapTableData = TableDataManager.instance.FindMapTableData(currentMap);
		if (mapTableData != null)
		{
			if (BattleManager.instance != null)
				BattleManager.instance.OnPreInstantiateMap();
			
			InstantiateMap(mapTableData);
		}

		GetNextStageInfo();
	}

#if USE_MAIN_SCENE
	AsyncOperationResult _handleNextPlanePrefab;
	AsyncOperationResult _handleNextGroundPrefab;
	AsyncOperationResult _handleNextWallPrefab;
	AsyncOperationResult _handleNextSpawnFlagPrefab;
	AsyncOperationResult _handleNextPortalFlagPrefab;
	AsyncOperationResult _handleEnvironmentSettingPrefab;
	void PrepareNextMap(MapTableData mapTableData, string environmentSetting)
	{
		_handleNextPlanePrefab = AddressableAssetLoadManager.GetAddressableAsset(mapTableData.plane, "Map");
		_handleNextGroundPrefab = AddressableAssetLoadManager.GetAddressableAsset(mapTableData.ground, "Map");
		_handleNextWallPrefab = AddressableAssetLoadManager.GetAddressableAsset(mapTableData.wall, "Map");
		_handleNextSpawnFlagPrefab = AddressableAssetLoadManager.GetAddressableAsset(mapTableData.spawnFlag, "Map");	// Spawn
		_handleNextPortalFlagPrefab = AddressableAssetLoadManager.GetAddressableAsset(mapTableData.portalFlag, "Map");

		_handleEnvironmentSettingPrefab = AddressableAssetLoadManager.GetAddressableAsset(environmentSetting, "EnvironmentSetting");
	}

	string _lastPlayerActorId = "";
	int _lastPlayerPowerSource = -1;
	AsyncOperationResult _handlePowerSourcePrefab = null;
	public void PreparePowerSource()
	{
		if (_lastPlayerActorId == BattleInstanceManager.instance.playerActor.actorId)
			return;
		_lastPlayerActorId = BattleInstanceManager.instance.playerActor.actorId;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_lastPlayerActorId);
		if (_lastPlayerPowerSource == actorTableData.powerSource)
			return;

		_lastPlayerPowerSource = actorTableData.powerSource;
		_handlePowerSourcePrefab = AddressableAssetLoadManager.GetAddressableAsset(PowerSource.Index2Name(_lastPlayerPowerSource), "PowerSource");
	}

	public GameObject GetPreparedPowerSourcePrefab()
	{
		return _handlePowerSourcePrefab.Result;
	}

	public bool IsDoneLoadAsyncNextStage()
	{
		if (_handleEnvironmentSettingPrefab == null || _handleEnvironmentSettingPrefab.IsDone == false)
			return false;
		if (_handlePowerSourcePrefab == null || _handlePowerSourcePrefab.IsDone == false)
			return false;
		return (_handleNextPlanePrefab.IsDone && _handleNextGroundPrefab.IsDone && _handleNextWallPrefab.IsDone && _handleNextSpawnFlagPrefab.IsDone && _handleNextPortalFlagPrefab.IsDone);
	}
#endif

	GameObject _currentPlaneObject;
	GameObject _currentGroundObject;
	GameObject _currentWallObject;
	GameObject _currentSpawnFlagObject;
	GameObject _currentPortalFlagObject;
	GameObject _currentEnvironmentSettingObject;
	void InstantiateMap(MapTableData mapTableData)
	{
#if USE_MAIN_SCENE
		if (_currentPlaneObject != null)
			_currentPlaneObject.SetActive(false);
		_currentPlaneObject = BattleInstanceManager.instance.GetCachedObject(_handleNextPlanePrefab.Result, Vector3.zero, Quaternion.identity);
		BattleInstanceManager.instance.planeCollider = _currentPlaneObject.GetComponent<Collider>();

		if (_currentGroundObject != null)
			_currentGroundObject.SetActive(false);
		_currentGroundObject = BattleInstanceManager.instance.GetCachedObject(_handleNextGroundPrefab.Result, Vector3.zero, Quaternion.identity);

		if (_currentWallObject != null)
			_currentWallObject.SetActive(false);
		_currentWallObject = BattleInstanceManager.instance.GetCachedObject(_handleNextWallPrefab.Result, Vector3.zero, Quaternion.identity);

		if (_currentSpawnFlagObject != null)
			_currentSpawnFlagObject.SetActive(false);
		_currentSpawnFlagObject = Instantiate<GameObject>(_handleNextSpawnFlagPrefab.Result);

		if (_currentPortalFlagObject != null)
			_currentPortalFlagObject.SetActive(false);
		_currentPortalFlagObject = BattleInstanceManager.instance.GetCachedObject(_handleNextPortalFlagPrefab.Result, Vector3.zero, Quaternion.identity);

		if (_currentEnvironmentSettingObject != null)
			_currentEnvironmentSettingObject.SetActive(false);
		_currentEnvironmentSettingObject = BattleInstanceManager.instance.GetCachedObject(_handleEnvironmentSettingPrefab.Result, null);
#else
		for (int i = 0; i < planePrefabList.Length; ++i)
		{
			if (planePrefabList[i].name.ToLower() == mapTableData.plane.ToLower())
			{
				if (_currentPlaneObject != null)
					_currentPlaneObject.SetActive(false);
				_currentPlaneObject = BattleInstanceManager.instance.GetCachedObject(planePrefabList[i], Vector3.zero, Quaternion.identity);
				break;
			}
		}

		for (int i = 0; i < groundPrefabList.Length; ++i)
		{
			if (groundPrefabList[i].name.ToLower() == mapTableData.ground.ToLower())
			{
				if (_currentGroundObject != null)
					_currentGroundObject.SetActive(false);
				_currentGroundObject = BattleInstanceManager.instance.GetCachedObject(groundPrefabList[i], Vector3.zero, Quaternion.identity);
				break;
			}
		}

		for (int i = 0; i < wallPrefabList.Length; ++i)
		{
			if (wallPrefabList[i].name.ToLower() == mapTableData.wall.ToLower())
			{
				if (_currentWallObject != null)
					_currentWallObject.SetActive(false);
				_currentWallObject = BattleInstanceManager.instance.GetCachedObject(wallPrefabList[i], Vector3.zero, Quaternion.identity);
				break;
			}
		}

		for (int i = 0; i < spawnFlagPrefabList.Length; ++i)
		{
			if (spawnFlagPrefabList[i].name.ToLower() == mapTableData.spawnFlag.ToLower())
			{
				if (_currentSpawnFlagObject != null)
					_currentSpawnFlagObject.SetActive(false);
				_currentSpawnFlagObject = Instantiate<GameObject>(spawnFlagPrefabList[i]);
				break;
			}
		}
#endif

		// 배틀매니저는 존재하지 않을 수 있다. 로딩속도 때문에 처음 진입해서 천천히 생성시킨다. 시작맵이라 없어도 플레이가 가능하다.
		if (BattleManager.instance != null)
			BattleManager.instance.OnLoadedMap();
	}

	public Vector3 currentGatePillarSpawnPosition { get; set; }
	public string currentGatePillarPreview { get; set; }
	public bool spawnPowerSourcePrefab { get; set; }
	public Vector3 currentPowerSourceSpawnPosition { get; set; }




	#region PlayerLevel
	int _playerLevel = 1;
	public int playerLevel { get { return _playerLevel; } }
	int _playerExp = 0;
	public int needLevelUpCount { get; set; }
	public void AddExp(int exp)
	{
		_playerExp += exp;

		// level, bottom exp bar
		int maxStageLevel = GetMaxStageLevel();
		int level = 0;
		float percent = 0.0f;
		for (int i = _playerLevel; i < TableDataManager.instance.stageExpTable.dataArray.Length; ++i)
		{
			if (_playerExp < TableDataManager.instance.stageExpTable.dataArray[i].requiredAccumulatedExp)
			{
				int currentPeriodExp = _playerExp - TableDataManager.instance.stageExpTable.dataArray[i - 1].requiredAccumulatedExp;
				percent = (float)currentPeriodExp / (float)TableDataManager.instance.stageExpTable.dataArray[i].requiredExp;
				level = TableDataManager.instance.stageExpTable.dataArray[i].level - 1;
				break;
			}
			if (TableDataManager.instance.stageExpTable.dataArray[i].level >= maxStageLevel)
			{
				level = maxStageLevel;
				percent = 1.0f;
				break;
			}
		}
		if (level == 0)
		{
			// max
			level = maxStageLevel;
			percent = 1.0f;
		}
		LobbyCanvas.instance.RefreshExpPercent(percent);
		if (_playerLevel == level)
			return;

		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerLevelUpEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity);

		needLevelUpCount = level - _playerLevel;
		PlayerGaugeCanvas.instance.RefreshLevelText(level);
		//LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform);
	}

	int GetMaxStageLevel()
	{
		int maxStageLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStageLevel");

		// 잠재력 개방에 따른 차이. 잠재력은 ActorStatus인가? 아니면 액터의 또다른 개방 정보인가. 결국 강화랑 저장할 곳이 또 뭔가가 필요할듯. 파워레벨 역시 스탯은 아닐듯.

		return maxStageLevel;
	}
	#endregion
}
