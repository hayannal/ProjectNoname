﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;

public class ClientSaveData : MonoBehaviour
{
	public static ClientSaveData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("ClientSaveData")).AddComponent<ClientSaveData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static ClientSaveData _instance = null;

	void Awake()
	{
		ObscuredPrefs.OnAlterationDetected += SavesAlterationDetected;
	}

	public bool savesAlterationDetected { get; private set; }
	void SavesAlterationDetected()
	{
		savesAlterationDetected = true;

		// 변조 감지되면 데이터를 버린다. 앱을 끄진 않는다.
		OnEndGame();
	}

	public bool IsCachedInProgressGame()
	{
		string inProgress = ObscuredPrefs.GetString("inProgress");
		string enterFlag = ObscuredPrefs.GetString("enterFlag");
		if (string.IsNullOrEmpty(enterFlag) || inProgress != "play")
			return false;

		// 저장을 해놔야 Cached읽을때 enterFlag를 비교할 수 있다.
		_cachedEnterFlag = enterFlag;

		if (string.IsNullOrEmpty(GetCachedString("mapData")))
		{
			// 실패시 캐싱값 초기화
			_cachedEnterFlag = "";
			return false;
		}

		// HasKey로 검사하면 enterFlag 검사를 못하기때문에 이렇게 하면 안된다.
		//if (ObscuredPrefs.HasKey("cachedStage") == false)
		//	return false;
		int stage = GetCachedStage();
		if (stage == 0)
		{
			_cachedEnterFlag = "";
			return false;
		}
		
		return true;
	}

	public void MoveToInProgressGame()
	{
		// 인풋부터 막고
		DelayedLoadingCanvas.Show(true);

		// 입장 패킷 보내기전에 필수로 해야하는 것들 위주로 셋팅한다.
		// 나머진 패킷 받고 재진입 다 완료한 후에 셋팅하는거로 한다.

		// 저장된 맵 구성 데이터를 로드하고
		StageDataManager.instance.SetCachedMapData(GetCachedString("mapData"));

		// 이동해야할 스테이지로 셋팅하는데 챕터는 어차피 현재 챕터일테니 스테이지만 바꾼다.
		StageManager.instance.ReloadStage(GetCachedStage());

		// 사용한 BattleActor 리스트도 반영.
		string jsonBattleActorData = GetCachedBattleActorData();
		if (string.IsNullOrEmpty(jsonBattleActorData) == false)
			BattleInstanceManager.instance.SetInProgressBattlePlayerData(jsonBattleActorData);

		// 혹시 메인 캐릭터도 변경해야한다면 로드를 걸어둔다. 여기서 해야 패킷 보내서 받는 시간까지 로딩에 쓸 수 있다.
		string battleActorId = GetCachedBattleActor();
		if (string.IsNullOrEmpty(battleActorId) == false && BattleInstanceManager.instance.playerActor.actorId != battleActorId)
		{
			// 불러와야할 캐릭터가 용병 캐릭터인데 오늘의 무료 용병이 바뀌어서 불러올 수 없다면 교체하지 않아야한다.
			bool ignoreChange = false;
			if (MercenaryData.IsMercenaryActor(battleActorId))
			{
				MercenaryData.instance.RefreshCharacterDataList();
				if (MercenaryData.instance.GetCharacterData(battleActorId) == null)
					ignoreChange = true;
			}

			if (ignoreChange == false)
			{
				standbySwapBattleActor = true;
				AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(battleActorId), "Character", OnLoadedPlayerActor);
			}
		}

		// GatePillar를 통해 이동해야한다.
		GatePillar.instance.EnterInProgressGame();
	}

	public bool IsLoadedPlayerActor { get { return _playerActorPrefab != null; } }
	GameObject _playerActorPrefab;
	void OnLoadedPlayerActor(GameObject prefab)
	{
		_playerActorPrefab = prefab;
	}

	public bool standbySwapBattleActor { get; private set; }
	public void ChangeBattleActor()
	{
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(_playerActorPrefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(_playerActorPrefab);
#endif

		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		string battleActorId = GetCachedBattleActor();
		if (MercenaryData.IsMercenaryActor(battleActorId))
			playerActor.mercenary = true;

		BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
		playerActor.OnChangedMainCharacter();
		standbySwapBattleActor = false;
	}


	//public enum eInfoType
	//{
	//	EnterFlag,
	//	Stage,
	//	Exp,
	//
	//}
	//
	// 각각 정보는 종류별로 따로 가지고 있는다.
	// 첨엔 하나의 스트링으로 만들어서 암호화해서 저장하려고 했는데
	// 어차피 타입별로 저장시점이 다르기도 하고
	// 이미 AntiCheat의 기능으로 각각 암호화 해서 저장하는게 가능해서 나눠서 저장하기로 한다.
	//public void SaveInfo(eInfoType type, int value)
	//{
	//}
	// 이렇게 하려다가 Enter할때 레벨/스테이지/엔터플래그 3개 동시저장하는데 어울리지가 않는다.
	// 그래서 차라리 각 이벤트당 하나의 함수로 만들고 안에서 여러개의 세이브 할수있게 바꾸는게 나을거 같아서 바꾸기로 한다.

	ObscuredString _cachedEnterFlag;
	public void OnEnterGame(bool inProgressEnterGame, string enterFlag)
	{
		// 세이브 된 데이터를 불러오는 게임이라면 아래 초기화 정보들로 덮어쓰면 안된다.
		// 로딩할거 다 하고나서 인게임 다 불러오고 로딩할 정보가 없을때 새로 부여받은 enterFlag를 저장해야한다.
		//if (inProgressEnterGame)
		//{
		//	_newEnterFlag = enterFlag;
		//	return;
		//}
		// 위 같은 방식으로 하려고 했는데 재진입 입장 패킷 받은 즉시 튕겨버리면
		// 캐싱된 값들에 옛날 enterFlag가 적혀있게 되서 다시 접속했을때 이 정보들을 쓸 수 없게 되버린다.
		// 그래서 무조건 새 enterFlag를 받으면 새거로 갱신하고
		// 로딩중이란 플래그를 따로 걸어야한다.
		if (inProgressEnterGame)
		{
			ResaveEnterFlagValues(enterFlag);
			_loadingInProgressGame = true;
			return;
		}

		// InGame 진입시 설정값들 저장.
		ObscuredPrefs.SetString("inProgress", "play");
		ObscuredPrefs.SetString("enterFlag", enterFlag);
		_cachedEnterFlag = enterFlag;

		SetCachedString("mapData", StageDataManager.instance.GetCachedMapData());

		// Enter 패킷 받을때는 Stage가 0이지만 1로 저장해둔다. 0으로 재진입 할순 없기 때문.
		OnChangedStage(1);
		OnChangedMonsterAllKill(false);
		OnChangedExp(0);
		OnChangedHpRatio(1.0f);
		OnChangedSpRatio(0.0f);
		ClearDropItemList();
		OnChangedDropGold(0.0f);
		OnChangedDropSeal(0);
		OnChangedDropChaosFragment(0);
		ClearEliteMonsterIndexList();
		OnChangedClearPoint(0);
		OnChangedLastPowerSourceSaved(false);
		OnChangedReturnScroll(false);
		OnChangedQuestTemporaryAddCount(0);
		OnChangedGuideQuestTemporaryAddCount(0);
		OnChangedAllyContinuousKillCount(0);
	}

	void ResaveEnterFlagValues(string newEnterFlag)
	{
		// enterFlag 바꾸기전에 값 얻어온 후
		int stage = GetCachedStage();
		string environmentSetting = GetCachedEnvironmentSetting();
		bool monsterKill = GetCachedMonsterAllKill();
		bool gatePillar = GetCachedGatePillar();
		bool powerSource = GetCachedPowerSource();
		bool closeSwap = GetCachedCloseSwap();
		int exp = GetCachedExp();
		float hpRatio = GetCachedHpRatio();
		float spRatio = GetCachedSpRatio();
		int levelUpCount = GetCachedRemainLevelUpCount();
		int levelPackCount = GetCachedRemainLevelPackCount();
		int noHitlevelPackCount = GetCachedRemainNoHitLevelPackCount();
		string jsonLevelPackData = GetCachedLevelPackData();
		string jsonRandomLevelPackData = GetCachedRandomLevelPackData();
		string jsonBattleActorData = GetCachedBattleActorData();
		string battleActorId = GetCachedBattleActor();
		string jsonDropItemData = GetCachedDropItemData();
		float dropGold = GetCachedDropGold();
		int dropSeal = GetCachedDropSeal();
		int dropChaosFragment = GetCachedDropChaosFragment();
		string stagePenaltyId = GetCachedStagePenalty();
		string jsonEliteMonsterData = GetCachedEliteMonsterData();
		int clearPoint = GetCachedClearPoint();
		int refreshStackCount = GetCachedRefreshStackCount();
		bool lastPowerSourceSaved = GetCachedLastPowerSourceSaved();
		int lastPowerSourceStage = GetCachedLastPowerSourceStage();
		string lastPowerSourceActorId = GetCachedLastPowerSourceActorId();
		bool returnScrollUsed = GetCachedReturnScroll();
		int questTemporaryAddCount = GetCachedQuestTemporaryAddCount();
		int guideQuestTemporaryAddCount = GetCachedGuideQuestTemporaryAddCount();
		int allyContinuousKillCount = GetCachedAllyContinuousKillCount();

		// 새 값으로 교체하고
		ObscuredPrefs.SetString("enterFlag", newEnterFlag);
		_cachedEnterFlag = newEnterFlag;

		// 정보들 재저장
		SetCachedString("mapData", StageDataManager.instance.GetCachedMapData());

		OnChangedStage(stage);
		if (string.IsNullOrEmpty(environmentSetting) == false) OnChangedEnvironmentSetting(environmentSetting);
		OnChangedMonsterAllKill(monsterKill);
		OnChangedGatePillar(gatePillar);
		OnChangedPowerSource(powerSource);
		OnChangedCloseSwap(closeSwap);
		OnChangedExp(exp);
		OnChangedHpRatio(hpRatio);
		OnChangedSpRatio(spRatio);
		if (levelUpCount > 0) OnChangedRemainLevelUpCount(levelUpCount);
		if (levelPackCount > 0) OnChangedRemainLevelPackCount(levelPackCount);
		if (noHitlevelPackCount > 0) OnChangedRemainNoHitLevelPackCount(noHitlevelPackCount);
		if (string.IsNullOrEmpty(jsonLevelPackData) == false) OnChangedLevelPackData(jsonLevelPackData);
		if (string.IsNullOrEmpty(jsonRandomLevelPackData) == false) OnChangedRandomLevelPackData(jsonRandomLevelPackData);
		if (string.IsNullOrEmpty(jsonBattleActorData) == false) OnChangedBattleActorData(jsonBattleActorData);
		OnChangedBattleActor(battleActorId);
		OnChangedDropItemData(jsonDropItemData);
		OnChangedDropGold(dropGold);
		OnChangedDropSeal(dropSeal);
		OnChangedDropChaosFragment(dropChaosFragment);
		if (string.IsNullOrEmpty(stagePenaltyId) == false) OnChangedStagePenalty(stagePenaltyId);
		if (string.IsNullOrEmpty(jsonEliteMonsterData) == false) OnChangedEliteMonsterData(jsonEliteMonsterData);
		OnChangedClearPoint(clearPoint);
		OnChangedRefreshStackCount(refreshStackCount);
		OnChangedLastPowerSourceSaved(lastPowerSourceSaved);
		OnChangedLastPowerSourceStage(lastPowerSourceStage);
		OnChangedLastPowerSourceActorId(lastPowerSourceActorId);
		OnChangedReturnScroll(returnScrollUsed);
		OnChangedQuestTemporaryAddCount(questTemporaryAddCount);
		OnChangedGuideQuestTemporaryAddCount(guideQuestTemporaryAddCount);
		OnChangedAllyContinuousKillCount(allyContinuousKillCount);
	}

	public bool IsLoadingInProgressGame()
	{
		if (string.IsNullOrEmpty(_cachedEnterFlag))
			return false;
		return _loadingInProgressGame;
	}

	bool _loadingInProgressGame;
	bool _inProgressGame;
	public void OnFinishLoadGame()
	{
		// 복구 완료 타이밍에 따로 저장할건 없지 않나..
		_loadingInProgressGame = false;

		// InProgressGame으로 들어왔을때는 총 플레이 시간을 체크하기가 까다로와서 빠른 클리어 체크를 하지 않기로 했는데
		// 복구하고 나면 복구된 게임과 그냥 진입했을때의 상태를 구분할 방법이 없어서
		// 플래그 하나를 추가해두기로 한다.
		// 이건 나가거나 클리어 했을때 초기화 하면 된다.
		_inProgressGame = true;
	}

	public bool inProgressGame { get { return _inProgressGame; } }

	public void OnEndGame()
	{
		_cachedEnterFlag = "";
		_inProgressGame = false;

		ObscuredPrefs.DeleteKey("inProgress");
		ObscuredPrefs.DeleteKey("enterFlag");
		ObscuredPrefs.DeleteKey("mapData");

		// 나머지 정보도 지워야할까.
		ObscuredPrefs.DeleteKey("cachedStage");
		ObscuredPrefs.DeleteKey("cachedEnvironment");
		ClearDropItemList();
		ClearEliteMonsterIndexList();
	}

	public string GetCachedEnterFlag() { return ObscuredPrefs.GetString("enterFlag"); }
	public string GetCachedMapData() { return GetCachedString("mapData"); }

	public void OnChangedStage(int stage) { SetCachedInt("cachedStage", stage); }
	public int GetCachedStage() { return GetCachedInt("cachedStage"); }
	public void OnChangedEnvironmentSetting(string environmentSetting) { SetCachedString("cachedEnvironment", environmentSetting); }
	public string GetCachedEnvironmentSetting() { return GetCachedString("cachedEnvironment"); }

	// 개별 층의 정보를 저장할때 가장 중요한 두가지는 나오는 몬스터를 다 잡았느냐와
	// 다 잡고나서 꼭 해야할 일들을 완료해 게이트필라가 나와있느냐
	// 이렇게 두가지다.
	public void OnChangedMonsterAllKill(bool enable) { SetCachedInt("cachedMonsterKill", enable ? 1 : 0); }
	public bool GetCachedMonsterAllKill() { return GetCachedInt("cachedMonsterKill") == 1; }
	public void OnChangedGatePillar(bool enable) { SetCachedInt("cachedGatePillar", enable ? 1 : 0); }
	public bool GetCachedGatePillar() { return GetCachedInt("cachedGatePillar") == 1; }
	public void OnChangedPowerSource(bool enable) { SetCachedInt("cachedPowerSource", enable ? 1 : 0); }
	public bool GetCachedPowerSource() { return GetCachedInt("cachedPowerSource") == 1; }
	public void OnChangedCloseSwap(bool close) { SetCachedInt("cachedCloseSwap", close ? 1 : 0); }
	public bool GetCachedCloseSwap() { return GetCachedInt("cachedCloseSwap") == 1; }

	public void OnChangedExp(int exp) { SetCachedInt("cachedExp", exp); }
	public int GetCachedExp() { return GetCachedInt("cachedExp"); }

	public void OnChangedRemainLevelUpCount(int count) { SetCachedInt("cachedLevelUp", count); }
	public void OnChangedRemainLevelPackCount(int count) { SetCachedInt("cachedLevelPack", count); }
	public void OnChangedRemainNoHitLevelPackCount(int count) { SetCachedInt("cachedNoLevelPack", count); }
	public int GetCachedRemainLevelUpCount() { return GetCachedInt("cachedLevelUp"); }
	public int GetCachedRemainLevelPackCount() { return GetCachedInt("cachedLevelPack"); }
	public int GetCachedRemainNoHitLevelPackCount() { return GetCachedInt("cachedNoLevelPack"); }
	public void OnAddedRemainLevelUpCount(int count) { SetCachedInt("cachedLevelUp", GetCachedRemainLevelUpCount() + count); }
	public void OnAddedRemainLevelPackCount(int count) { SetCachedInt("cachedLevelPack", GetCachedRemainLevelPackCount() + count); }
	public void OnAddedRemainNoHitLevelPackCount(int count) { SetCachedInt("cachedNoLevelPack", GetCachedRemainNoHitLevelPackCount() + count); }

	// 결정된 팩 리스트
	public void OnChangedLevelPackData(string jsonLevelPackData) { SetCachedString("cachedLevelPackData", jsonLevelPackData); }
	public string GetCachedLevelPackData() { return GetCachedString("cachedLevelPackData"); }
	// 선택할 팩 리스트. 현재 보여지고 있는 3개는 리스트에 저장해두고 다음에 똑같이 보여준다.
	public void OnChangedRandomLevelPackData(string jsonLevelPackData) { SetCachedString("randomLevelPackData", jsonLevelPackData); }
	public string GetCachedRandomLevelPackData() { return GetCachedString("randomLevelPackData"); }

	public void OnChangedHpRatio(float hpRatio) { SetCachedFloat("cachedHp", hpRatio); }
	public float GetCachedHpRatio() { return GetCachedFloat("cachedHp"); }
	public void OnChangedSpRatio(float spRatio) { SetCachedFloat("cachedSp", spRatio); }
	public float GetCachedSpRatio() { return GetCachedFloat("cachedSp"); }

	// 출전 캐릭터 리스트
	public void OnChangedBattleActorData(string jsonBattleActorData) { SetCachedString("cachedBattleActorData", jsonBattleActorData); }
	public string GetCachedBattleActorData() { return GetCachedString("cachedBattleActorData"); }
	// 마지막 캐릭터도 따로 저장해놔야한다. A->B->A 로 전환시 BattleActorList에는 A와 B만 들어있기 때문에 마지막 요소가 마지막 캐릭터와 다를 수 있기 때문이다.
	public void OnChangedBattleActor(string battleActorId) { SetCachedString("cachedBattleActor", battleActorId); }
	public string GetCachedBattleActor() { return GetCachedString("cachedBattleActor"); }

	// 드랍아이템 리스트. DropProcessor와 획득 시점이 달라서 이렇게 별도로 관리하는거다. 잃어버리면 안되서 이렇게 시점이 다른거다.
	void OnChangedDropItemData(string jsonDropItemData) { SetCachedString("cachedDropItemData", jsonDropItemData); }
	string GetCachedDropItemData() { return GetCachedString("cachedDropItemData"); }
	List<string> _listDropEquipId = new List<string>();
	public void ClearDropItemList()
	{
		_listDropEquipId.Clear();
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		OnChangedDropItemData(serializer.SerializeObject(_listDropEquipId));
	}
	public void OnAddedDropItemId(string dropItemId)
	{
		_listDropEquipId.Add(dropItemId);
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		OnChangedDropItemData(serializer.SerializeObject(_listDropEquipId));
	}
	public List<string> GetCachedDropItemList()
	{
		string jsonDropItemData = GetCachedDropItemData();
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listDropEquipId = serializer.DeserializeObject<List<string>>(jsonDropItemData);
		return _listDropEquipId;
	}

	// 골드와 인장
	public void OnChangedDropGold(float dropGold) { SetCachedFloat("cachedDropGold", dropGold); }
	public float GetCachedDropGold() { return GetCachedFloat("cachedDropGold"); }
	public void OnChangedDropSeal(int dropSeal) { SetCachedInt("cachedDropSeal", dropSeal); }
	public int GetCachedDropSeal() { return GetCachedInt("cachedDropSeal"); }
	public void OnChangedDropChaosFragment(int dropChaosFragment) { SetCachedInt("cachedChaosFragment", dropChaosFragment); }
	public int GetCachedDropChaosFragment() { return GetCachedInt("cachedChaosFragment"); }

	// 패널티 버프 디버프
	public void OnChangedStagePenalty(string stagePenaltyId) { SetCachedString("cachedStagePenalty", stagePenaltyId); }
	public string GetCachedStagePenalty() { return GetCachedString("cachedStagePenalty"); }

	// 엘리트 몬스터 리스트
	void OnChangedEliteMonsterData(string jsonEliteMonsterData) { SetCachedString("cachedEliteMonsterData", jsonEliteMonsterData); }
	string GetCachedEliteMonsterData() { return GetCachedString("cachedEliteMonsterData"); }
	List<int> _listEliteMonsterIndex = new List<int>();
	public void ClearEliteMonsterIndexList()
	{
		_listEliteMonsterIndex.Clear();
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		OnChangedEliteMonsterData(serializer.SerializeObject(_listEliteMonsterIndex));
	}
	public void OnAddedEliteMonsterIndex(int index)
	{
		_listEliteMonsterIndex.Add(index);
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		OnChangedEliteMonsterData(serializer.SerializeObject(_listEliteMonsterIndex));
	}
	public List<int> GetCachedEliteMonsterIndexList()
	{
		string jsonEliteMonsterData = GetCachedEliteMonsterData();
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listEliteMonsterIndex = serializer.DeserializeObject<List<int>>(jsonEliteMonsterData);
		return _listEliteMonsterIndex;
	}

	// 배틀 클리어 포인트
	public void OnChangedClearPoint(int clearPoint) { SetCachedInt("cachedClearPoint", clearPoint); }
	public int GetCachedClearPoint() { return GetCachedInt("cachedClearPoint"); }
	public void OnChangedRefreshStackCount(int refreshStackCount) { SetCachedInt("cachedRefreshStackCount", refreshStackCount); }
	public int GetCachedRefreshStackCount() { return GetCachedInt("cachedRefreshStackCount"); }

	// 귀환 주문서 세이브 포인트
	public void OnChangedLastPowerSourceSaved(bool enable) { SetCachedInt("cachedLastPowerSource", enable ? 1 : 0); }
	public bool GetCachedLastPowerSourceSaved() { return GetCachedInt("cachedLastPowerSource") == 1; }
	public void OnChangedLastPowerSourceActorId(string stagePenaltyId) { SetCachedString("cachedLastPowerSourceActor", stagePenaltyId); }
	public string GetCachedLastPowerSourceActorId() { return GetCachedString("cachedLastPowerSourceActor"); }
	public void OnChangedLastPowerSourceStage(int dropSeal) { SetCachedInt("cachedLastPowerSourceStage", dropSeal); }
	public int GetCachedLastPowerSourceStage() { return GetCachedInt("cachedLastPowerSourceStage"); }
	public void OnChangedReturnScroll(bool enable) { SetCachedInt("cachedReturnScroll", enable ? 1 : 0); }
	public bool GetCachedReturnScroll() { return GetCachedInt("cachedReturnScroll") == 1; }

	// 퀘스트 진행 카운트
	public void OnChangedQuestTemporaryAddCount(int questTemporaryAddCount) { SetCachedInt("cachedQuestTempAddCount", questTemporaryAddCount); }
	public int GetCachedQuestTemporaryAddCount() { return GetCachedInt("cachedQuestTempAddCount"); }
	// 가이드 퀘스트 진행 카운트
	public void OnChangedGuideQuestTemporaryAddCount(int guideQuestTemporaryAddCount) { SetCachedInt("cachedGuideQuestTempAddCount", guideQuestTemporaryAddCount); }
	public int GetCachedGuideQuestTemporaryAddCount() { return GetCachedInt("cachedGuideQuestTempAddCount"); }


	// 어펙터 전용 변수들
	public void OnChangedAllyContinuousKillCount(int allyContinuousKillCount) { SetCachedInt("cachedAllyContinuousKillCount", allyContinuousKillCount); }
	public int GetCachedAllyContinuousKillCount() { return GetCachedInt("cachedAllyContinuousKillCount"); }
	


	#region Helper
	void SetCachedInt(string key, int value)
	{
		ObscuredPrefs.SetString(key, string.Format("{0}_{1}", value, _cachedEnterFlag));
	}

	int GetCachedInt(string key)
	{
		// _cachedEnterFlag가 비워져있을때는 읽어야할 타이밍이 아니다.
		if (string.IsNullOrEmpty(_cachedEnterFlag))
			return 0;

		string cachedString = ObscuredPrefs.GetString(key);
		if (cachedString == "")
			return 0;
		string result = cachedString.Replace(string.Format("_{0}", _cachedEnterFlag), "");
		int value = 0;
		int.TryParse(result, out value);
		return value;
	}

	void SetCachedFloat(string key, float value)
	{
		ObscuredPrefs.SetString(key, string.Format("{0}_{1}", value, _cachedEnterFlag));
	}

	float GetCachedFloat(string key)
	{
		// _cachedEnterFlag가 비워져있을때는 읽어야할 타이밍이 아니다.
		if (string.IsNullOrEmpty(_cachedEnterFlag))
			return 0.0f;

		string cachedString = ObscuredPrefs.GetString(key);
		if (cachedString == "")
			return 0.0f;
		string result = cachedString.Replace(string.Format("_{0}", _cachedEnterFlag), "");
		float value = 0.0f;
		float.TryParse(result, out value);
		return value;
	}

	void SetCachedString(string key, string value)
	{
		ObscuredPrefs.SetString(key, string.Format("{0}___{1}", value, _cachedEnterFlag));
	}

	string GetCachedString(string key)
	{
		// _cachedEnterFlag가 비워져있을때는 읽어야할 타이밍이 아니다.
		if (string.IsNullOrEmpty(_cachedEnterFlag))
			return "";

		string cachedString = ObscuredPrefs.GetString(key);
		if (cachedString == "")
			return "";

		int lastIndex = cachedString.LastIndexOf(string.Format("___{0}", _cachedEnterFlag));
		if (lastIndex == -1)
			return "";

		return cachedString.Replace(string.Format("___{0}", _cachedEnterFlag), "");
	}
	#endregion
}