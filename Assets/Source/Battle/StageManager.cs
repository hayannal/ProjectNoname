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
using CodeStage.AntiCheat.ObscuredTypes;
using ActorStatusDefine;

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
	public GameObject bossGatePillarPrefab;
	public GameObject fadeCanvasPrefab;

#if USE_MAIN_SCENE
#else
	[Reorderable] public GameObject[] planePrefabList;
	[Reorderable] public GameObject[] groundPrefabList;
	[Reorderable] public GameObject[] wallPrefabList;
	[Reorderable] public GameObject[] spawnFlagPrefabList;
#endif

	public ObscuredInt playChapter { get; set; }
	public ObscuredInt playStage { get; set; }

	void Awake()
	{
		instance = this;
	}

#if USE_MAIN_SCENE
	public void InitializeStage(int chapter, int stage)
	{
		playChapter = chapter;
		playStage = stage;
		GetStageInfo(playChapter, playStage);
	}

	void GetStageInfo(int chapter, int stage)
	{
		StageDataManager.instance.CalcNextStageInfo(chapter, stage, PlayerData.instance.highestPlayChapter, PlayerData.instance.highestClearStage);

		if (StageDataManager.instance.existNextStageInfo)
		{
			nextMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(StageDataManager.instance.reservedNextMap);
			if (nextMapTableData != null)
				PrepareNextMap(nextMapTableData, StageDataManager.instance.nextStageTableData.environmentSetting);
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
		GetStageInfo(playChapter, nextStage);
	}

	// 이건 9층 클리어 후 10층 보스가 나옴을 알리기 위해 빨간색 게이트 필라를 띄우는데 필요.
	public MapTableData nextMapTableData { get; private set; }

	// 이건 10층 클리어 후 20층 보스의 정보를 알기 위해 필요.
	public MapTableData nextBossMapTableData
	{
		get
		{
			int maxStage = TableDataManager.instance.FindChapterTableData(playChapter).maxStage;
			for (int i = playStage + 1; i <= maxStage; ++i)
			{
				string reservedMap = StageDataManager.instance.GetCachedMap(i);
				if (reservedMap == "")
					continue;
				MapTableData mapTableData = BattleInstanceManager.instance.GetCachedMapTableData(reservedMap);
				if (mapTableData == null)
					continue;
				if (string.IsNullOrEmpty(mapTableData.bossName))
					continue;
				return mapTableData;
			}
			return null;
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

		MapTableData mapTableData = BattleInstanceManager.instance.GetCachedMapTableData(currentMap);
		if (mapTableData != null)
		{
			if (BattleManager.instance != null)
				BattleManager.instance.OnPreInstantiateMap();
			
			InstantiateMap(mapTableData);
		}

		GetNextStageInfo();
	}

#if USE_MAIN_SCENE
	AsyncOperationGameObjectResult _handleNextPlanePrefab;
	AsyncOperationGameObjectResult _handleNextGroundPrefab;
	AsyncOperationGameObjectResult _handleNextWallPrefab;
	AsyncOperationGameObjectResult _handleNextSpawnFlagPrefab;
	AsyncOperationGameObjectResult _handleNextPortalFlagPrefab;
	AsyncOperationGameObjectResult _handleEnvironmentSettingPrefab;
	void PrepareNextMap(MapTableData mapTableData, string[] environmentSettingList)
	{
		_handleNextPlanePrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.plane, "Map");
		_handleNextGroundPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.ground, "Map");
		_handleNextWallPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.wall, "Map");
		_handleNextSpawnFlagPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.spawnFlag, "Map");	// Spawn
		_handleNextPortalFlagPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.portalFlag, "Map");

		// 환경은 위의 맵 정보와 달리 들어오면 설정하고 아니면 패스하는 형태다. 그래서 없을땐 null로 한다.
		if (environmentSettingList.Length == 0)
			_handleEnvironmentSettingPrefab = null;
		else
		{
			string environmentSetting = environmentSettingList[Random.Range(0, environmentSettingList.Length)];
			_handleEnvironmentSettingPrefab = AddressableAssetLoadManager.GetAddressableGameObject(environmentSetting, "EnvironmentSetting");
		}
	}

	string _lastPlayerActorId = "";
	int _lastPlayerPowerSource = -1;
	AsyncOperationGameObjectResult _handlePowerSourcePrefab = null;
	public void PreparePowerSource()
	{
		if (_lastPlayerActorId == BattleInstanceManager.instance.playerActor.actorId)
			return;
		_lastPlayerActorId = BattleInstanceManager.instance.playerActor.actorId;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_lastPlayerActorId);
		if (_lastPlayerPowerSource == actorTableData.powerSource)
			return;

		_lastPlayerPowerSource = actorTableData.powerSource;
		_handlePowerSourcePrefab = AddressableAssetLoadManager.GetAddressableGameObject(PowerSource.Index2Address(_lastPlayerPowerSource), "PowerSource");
	}

	public GameObject GetPreparedPowerSourcePrefab()
	{
		return _handlePowerSourcePrefab.Result;
	}

	public bool IsDoneLoadAsyncNextStage()
	{
		if (_handleEnvironmentSettingPrefab != null && _handleEnvironmentSettingPrefab.IsDone == false)
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

		// 위의 맵 정보와 달리 테이블에 값이 있을때만 변경하는거라 이렇게 처리한다.
		if (_handleEnvironmentSettingPrefab != null)
		{
			if (_currentEnvironmentSettingObject != null)
				_currentEnvironmentSettingObject.SetActive(false);
			_currentEnvironmentSettingObject = BattleInstanceManager.instance.GetCachedObject(_handleEnvironmentSettingPrefab.Result, null);
		}
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
	public bool spawnPowerSourcePrefab { get; set; }
	public Vector3 currentPowerSourceSpawnPosition { get; set; }

	#region LobbyUI
	public void EnableEnvironmentSettingForUI(bool show)
	{
		if (_currentEnvironmentSettingObject != null)
			_currentEnvironmentSettingObject.SetActive(show);
	}
	#endregion




	#region PlayerLevel
	ObscuredInt _playerLevel = 1;
	public int playerLevel { get { return _playerLevel; } }
	ObscuredInt _playerExp = 0;
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
		needLevelUpCount = level - _playerLevel;
		LobbyCanvas.instance.RefreshExpPercent(percent, needLevelUpCount);
		if (needLevelUpCount == 0)
			return;
		_playerLevel = level;

		AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
		healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("LevelUpHeal") * needLevelUpCount;
		healAffectorValue.fValue3 += BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.LevelUpHealRate) * needLevelUpCount;
		BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, BattleInstanceManager.instance.playerActor, false);

		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerLevelUpEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);
		LobbyCanvas.instance.RefreshLevelText(_playerLevel);
		LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, needLevelUpCount, 0, 0);
		Timing.RunCoroutine(LevelUpScreenEffectProcess());
	}

	IEnumerator<float> LevelUpScreenEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.333f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		FadeCanvas.instance.FadeIn(1.0f);
	}

	public int GetMaxStageLevel()
	{
		int maxStageLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStageLevel");

		// 잠재력 개방에 따른 차이. 잠재력은 ActorStatus인가? 아니면 액터의 또다른 개방 정보인가. 결국 강화랑 저장할 곳이 또 뭔가가 필요할듯. 파워레벨 역시 스탯은 아닐듯.

		return maxStageLevel;
	}
	#endregion

	#region Player List
	List<string> _listBattlePlayerActorIdList = new List<string>();
	public void AddBattlePlayer(string actorId)
	{
		if (_listBattlePlayerActorIdList.Contains(actorId))
			return;
		_listBattlePlayerActorIdList.Add(actorId);
	}

	public bool IsInBattlePlayerList(string actorId)
	{
		return _listBattlePlayerActorIdList.Contains(actorId);
	}
	#endregion


	#region Battle Result
	public void OnSuccess()
	{
		// 챕터 +1 해두고 디비에 기록. highest 갱신.
		int nextChapter = playChapter + 1;

		// 최종 챕터 확인
		int chaosChapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
		if (nextChapter == chaosChapterLimit)
		{
			// 최종 챕터를 깬거라 더이상 진행하면 안되서
			// 곧바로 카오스 모드로 진입시켜야한다.
		}
	}

	public void OnFail()
	{
		int purifyStartChapter = BattleInstanceManager.instance.GetCachedGlobalConstantInt("PurifyStartChapter");
		if (playChapter >= purifyStartChapter)
		{
			// 카오스 모드를 발동해야하는 시기다. 튜토리얼도 있을 예정.
		}
	}

	public void OnResult()
	{
		// 이기거나 지는거에 상관없이, 혹은 카오스 모드 여부에 상관없이
		// 최종 챕터를 플레이 했다면 보스를 클리어한 스테이지 수만큼 상자를 채워야한다.
		if (playChapter == PlayerData.instance.highestPlayChapter)
		{

		}
	}
	#endregion
}
