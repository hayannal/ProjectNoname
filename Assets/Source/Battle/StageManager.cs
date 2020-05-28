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
	public GameObject challengeGatePillarPrefab;
	public GameObject fadeCanvasPrefab;
	public GameObject characterInfoGroundPrefab;

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

		// 씬의 시작에서 1챕터를 켜려고 할때 번들을 안받은 상태라면 로드가 실패할거다.
		// 그러니 0챕터를 대신 부르고 다운로드 대기 모드로 동작하게 처리한다.
		// 이땐 CalcNextStageInfo도 할 필요 없고 그냥 로비만 로딩한 후 못넘어가게 처리하면 된다.
		if (DownloadManager.instance.IsDownloaded() == false && chapter != 0)
		{
			DownloadManager.instance.standbyDownload = true;
			StageDataManager.instance.nextStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(0, 0, false);
			StageDataManager.instance.reservedNextMap = StageDataManager.instance.nextStageTableData.overridingMap;
			return;
		}

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

	// for switch challenge mode
	public void ChangeChallengeMode()
	{
		// 1. StageDataManager를 삭제
		// 2. StageDataManager.instance.CalcNextStageInfo 로 재계산. 이미 로비에 진입해있으니 0부터 할필요 없이 1부터 계산해서 새로운 맵을 로딩해둔다.
		// 이렇게 해두고나서 치고 들어가면 도전모드가 진행된다.
		StageDataManager.DestroyInstance();
		_handleNextPlanePrefab = _handleNextGroundPrefab = _handleNextWallPrefab = _handleNextSpawnFlagPrefab = _handleNextPortalFlagPrefab = _handleEnvironmentSettingPrefab = null;
		GetStageInfo(playChapter, playStage + 1);
	}

	// for in progress game
	bool _reloadInProgressGame = false;
	public void ReloadStage(int targetStage)
	{
		// ClientSaveData의 IsLoadingInProgressGame함수는 EnterGame 응답받고나서 사용할 수 있는 함수라..
		// Reload임을 체크할 또 다른 변수하나가 필요해졌다.
		// 그래서 플래그 하나 추가해둔다.
		_reloadInProgressGame = true;
		_handleNextPlanePrefab = _handleNextGroundPrefab = _handleNextWallPrefab = _handleNextSpawnFlagPrefab = _handleNextPortalFlagPrefab = _handleEnvironmentSettingPrefab = null;
		//StageManager.instance.playChapter = playChapter;
		StageManager.instance.playStage = targetStage - 1;
		StageManager.instance.GetNextStageInfo();
		_reloadInProgressGame = false;
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
		if (playStage == GetMaxStage(playChapter))
		{
			// last stage
			return;
		}
		if (DownloadManager.instance.standbyDownload)
			return;

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
			int maxStage = GetMaxStage(playChapter);
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

	public int GetMaxStage(int chapter)
	{
		return TableDataManager.instance.FindChapterTableData(playChapter).maxStage;
	}

	public float currentMonstrStandardHp { get { return _currentStageTableData.standardHp; } }
	public float currentMonstrStandardAtk { get { return _currentStageTableData.standardAtk; } }
	public float currentBossHpPer1Line { get { return _currentStageTableData.standardHp * _currentStageTableData.bossHpRatioPer1Line; } }
	public bool bossStage { get { return currentBossHpPer1Line != 0.0f; } }
	public int addDropExp { get; private set; }
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
			addDropExp = mapTableData.dropExpAdd;

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
	string _environmentSettingAddress;
	void PrepareNextMap(MapTableData mapTableData, string[] environmentSettingList)
	{
		_handleNextPlanePrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.plane, "Map");
		_handleNextGroundPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.ground, "Map");
		_handleNextWallPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.wall, "Map");
		_handleNextSpawnFlagPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.spawnFlag, "Map");	// Spawn
		_handleNextPortalFlagPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.portalFlag, "Map");

		if (_reloadInProgressGame)
		{
			// 재진입 한다고 항상 여기 값이 적혀있는건 아니다.
			// lobby에서만 셋팅이 되어있고 이후 1층부터 쭉 environmentSetting값이 비어져있으면 저장하는 타이밍이 없어서 lobby 셋팅대로 쭉 가게된다. 
			// 그러니 읽을게 없으면 재진입때도 아무런 셋팅하지 않고 지나간다.
			string cachedEnvironmentSetting = ClientSaveData.instance.GetCachedEnvironmentSetting();
			if (string.IsNullOrEmpty(cachedEnvironmentSetting) == false)
			{
				_handleEnvironmentSettingPrefab = AddressableAssetLoadManager.GetAddressableGameObject(cachedEnvironmentSetting, "EnvironmentSetting");
				return;
			}
		}

		// 환경은 위의 맵 정보와 달리 들어오면 설정하고 아니면 패스하는 형태다. 그래서 없을땐 null로 한다.
		if (environmentSettingList.Length == 0)
		{
			_handleEnvironmentSettingPrefab = null;
			_environmentSettingAddress = "";
		}
		else
		{
			string environmentSetting = environmentSettingList[Random.Range(0, environmentSettingList.Length)];
			_handleEnvironmentSettingPrefab = AddressableAssetLoadManager.GetAddressableGameObject(environmentSetting, "EnvironmentSetting");
			_environmentSettingAddress = environmentSetting;
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

		// 위의 맵 정보와 달리 테이블에 값이 있을때만 변경하는거라 이렇게 처리한다. 변경시에만 저장되는거라 변경을 안하면 저장된게 없을거다.
		if (_handleEnvironmentSettingPrefab != null)
		{
			if (_currentEnvironmentSettingObject != null)
			{
				_currentEnvironmentSettingObject.SetActive(false);
				_currentEnvironmentSettingObject = null;
			}
			_currentEnvironmentSettingObject = BattleInstanceManager.instance.GetCachedObject(_handleEnvironmentSettingPrefab.Result, null);
			bool lobby = false;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby) lobby = true;
			if (lobby == false)
				ClientSaveData.instance.OnChangedEnvironmentSetting(_environmentSettingAddress);
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

	public string GetCurrentSpawnFlagName()
	{
		if (_currentSpawnFlagObject != null)
		{
			string name = _currentSpawnFlagObject.name;
			name = name.Replace("SpawnFlag", "");
			name = name.Replace("(Clone)", "");
			return name;
		}
		return "";
	}

	public Vector3 currentGatePillarSpawnPosition { get; set; }
	public bool spawnPowerSourcePrefab { get; set; }
	public Vector3 currentPowerSourceSpawnPosition { get; set; }

	#region For Canvas EnvironmentSetting
	public GameObject currentEnvironmentSettingObjectForCanvas { get; set; }
	public void OnEnableEnvironmentSetting(GameObject newObject)
	{
		// 필드로 쓰려는게 올땐 아무것도 할 필요 없다.
		if (_currentEnvironmentSettingObject == newObject)
			return;
		// 혹은 필드에서 교체시 _currentEnvironmentSettingObject 이 null로 셋팅하고 교체하기 때문에 이때도 그냥 리턴한다.
		if (_currentEnvironmentSettingObject == null)
			return;

		// 캔버스가 독자적으로 가지고 있는거라면 현재 지형이 사용하는 환경셋팅과 분명 다를거다.
		// 이때만 임시 변수에 등록하면 된다.
		// 각각의 연출 캔버스-환경셋팅을 가지고 있는 캔버스-들이 이전 오브젝트를 기억하는 구조라 여기서는 마지막꺼 하나만 기억해두면 된다.
		currentEnvironmentSettingObjectForCanvas = newObject;
	}

	public GameObject DisableCurrentEnvironmentSetting()
	{
		// 시공간에서 캐릭터 창을 열때처럼 로비로 돌아가는게 아니라 별도의 환경셋팅을 사용하는 창끼리 넘어다닐때를 위해 이렇게 체크한다.
		if (currentEnvironmentSettingObjectForCanvas != null && currentEnvironmentSettingObjectForCanvas.activeInHierarchy)
		{
			currentEnvironmentSettingObjectForCanvas.SetActive(false);
			return currentEnvironmentSettingObjectForCanvas;
		}

		// 이게 아니라면 로비에 있는 메인 환경셋팅을 끄면 될거다.
		// 한번에 하나의 환경셋팅만 켜있을거기 때문에 위에 있는 임시값 아니면 이 아래있는 진짜 월드값 둘중 하나다.
		if (_currentEnvironmentSettingObject != null)
			_currentEnvironmentSettingObject.SetActive(false);
		return _currentEnvironmentSettingObject;
	}
	#endregion




	#region PlayerLevel
	ObscuredInt _playerLevel = 1;
	public int playerLevel { get { return _playerLevel; } }
	ObscuredInt _playerExp = 0;
	public int needLevelUpCount { get; set; }
	public void AddExp(int exp)
	{
		if (_playerLevel == GetMaxStageLevel())
			return;

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
		healAffectorValue.fValue3 += BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.LevelUpHealAddRate) * needLevelUpCount;
		BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, BattleInstanceManager.instance.playerActor, false);

		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerLevelUpEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);
		LobbyCanvas.instance.RefreshLevelText(_playerLevel);

		// 먼저 전용전투팩 얻는걸 체크. 여러개 얻을 경우 대비해서 누적시켜서 호출한다.
		for (int i = _playerLevel - needLevelUpCount + 1; i <= _playerLevel; ++i)
		{
			string exclusiveLevelPackId = TableDataManager.instance.FindActorLevelPackByLevel(BattleInstanceManager.instance.playerActor.actorId, i);
			if (string.IsNullOrEmpty(exclusiveLevelPackId))
				continue;

			// 전용팩은 레벨팩 데이터 매니저에 넣으면 안된다.
			//LevelPackDataManager.instance.AddLevelPack(BattleInstanceManager.instance.playerActor.actorId, exclusiveLevelPackId);
			BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack(exclusiveLevelPackId, true, i);
			LevelUpIndicatorCanvas.ShowExclusive(true, BattleInstanceManager.instance.playerActor.cachedTransform, exclusiveLevelPackId, i);
		}
		// 이후 레벨업 카운트만큼 처리
		LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, needLevelUpCount, 0, 0);
		ClientSaveData.instance.OnChangedRemainLevelUpCount(needLevelUpCount);

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

	#region InProgressGame
	public int playerExp { get { return _playerExp; } }
	public void SetLevelExpForInProgressGame(int exp)
	{
		_playerExp = exp;

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
		int levelUpCount = level - _playerLevel;
		LobbyCanvas.instance.SetLevelExpForInProgressGame(level, percent);
		if (levelUpCount == 0)
			return;
		_playerLevel = level;

		// 전용전투팩 얻는걸 체크. 스왑때 쓰려고 만든 함수를 대신 호출해서 처리한다.
		BattleInstanceManager.instance.playerActor.skillProcessor.CheckAllExclusiveLevelPack();
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
	#endregion
}
