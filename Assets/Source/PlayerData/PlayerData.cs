using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.DataModels;

public class PlayerData : MonoBehaviour
{
	public static PlayerData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PlayerData")).AddComponent<PlayerData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PlayerData _instance = null;

	public bool loginned { get; private set; }
	public bool clientOnly { get; private set; }

	// 변수 이름이 헷갈릴 수 있는데 로직상 이게 가장 필요한 정보라 그렇다.
	// 하나는 최대로 플레이한 챕터 번호고 하나는 최대로 클리어한 스테이지 번호다.
	// 무슨 말이냐 하면
	// 0-20 하다 죽었으면 highestPlayChapter는 0 / highestClearStage는 19가 저장되는 형태다.
	// 1-50 하다 죽었으면 highestPlayChapter는 1 / highestClearStage는 49가 저장되는 형태다.
	// 1-50 의 보스를 잡고 게이트 필라 치기 전에 죽었으면 위와 마찬가지로 highestPlayChapter는 1 / highestClearStage는 49가 저장되는 형태다.
	// 1-50 의 보스를 잡고 게이트 필라를 쳐서 결과창이 나왔으면 highestPlayChapter는 2 / highestClearStage는 0이 저장되는 형태다.
	// 이래야 깔끔하게 두개만 저장해서 모든걸 처리할 수 있다.
	public ObscuredInt highestPlayChapter { get; set; }
	public ObscuredInt highestClearStage { get; set; }
	public ObscuredInt selectedChapter { get; set; }
	// 이 카오스는 마지막 챕터의 카오스 상태를 저장하는 값이다. 이건 4챕터 이후에 도전모드 상태에서 질때 바뀌며 유저가 선택으로 바꾸는 값이 아니다.
	public ObscuredBool chaosMode { get; set; }
	public ObscuredInt purifyCount { get; set; }
	public ObscuredInt sealCount { get; set; }
	public ObscuredBool sharedDailyBoxOpened { get; set; }
	public DateTime dailyBoxResetTime { get; private set; }
	// 두번째 상자 게이지. sealCount와 달리 서버가 센다. 그래서 ReadOnlyUserData에 있다.
	public ObscuredInt secondDailyBoxFillCount { get; set; }
	public ObscuredInt researchLevel { get; set; }

	// 뽑기 관련 변수
	public ObscuredInt notStreakCount { get; set; }
	public ObscuredInt notStreakCharCount { get; set; }
	public ObscuredInt originOpenCount { get; set; }
	public ObscuredInt characterBoxOpenCount { get; set; }
	// pp 총합산 검증을 위해 상점에서 구매한 pp 카운트를 저장해두는 변수
	public ObscuredInt ppBuyCount { get; set; }

	// 인앱결제 상품 관련 변수
	List<int> _listLevelPackage;    // 레벨패키지 구매했음을 알리는 용도인데 어차피 일반 플레이어 데이터에 저장하는거라 Obscured도 안쓰기로 한다.
	public ObscuredBool sharedDailyPackageOpened { get; set; }
	public DateTime dailyPackageResetTime { get; private set; }

	// Training 관련 변수
	public ObscuredBool dailyTrainingGoldCompleted { get; set; }
	public DateTime dailyTrainingGoldResetTime { get; private set; }
	public ObscuredBool dailyTrainingDiaCompleted { get; set; }
	public DateTime dailyTrainingDiaResetTime { get; private set; }

	// NodeWar 관련 변수
	public ObscuredBool nodeWarCleared { get; set; }
	public DateTime nodeWarResetTime { get; private set; }
	public ObscuredInt nodeWarClearLevel { get; set; }

	// 이 카오스가 현재 카오스 상태로 스테이지가 셋팅되어있는지를 알려주는 값이다.
	// 이전 챕터로 내려갈 경우 서버에 저장된 chaosMode는 1이더라도 스테이지 구성은 도전모드로 셋팅하게 되며
	// 이땐 false를 리턴하게 될 것이다.
	public bool currentChaosMode
	{
		get
		{
			if (selectedChapter < highestPlayChapter)
				return false;
			return chaosMode;
		}
	}

	public bool currentChallengeMode
	{
		get
		{
			if (selectedChapter < highestPlayChapter)
				return false;
			if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chaos) == false)
				return false;
			return !chaosMode;
		}
	}

	#region Player Info For Client
	public void OnRecvPlayerInfoForClient()
	{
		// 디비 및 훈련챕터 들어가기 전까지 임시로 쓰는 값이다. 1챕터 정보를 부른다.
		highestPlayChapter = 2;
		highestClearStage = 0;
		selectedChapter = 1;
		chaosMode = false;

		// temp
		loginned = true;
		clientOnly = true;
	}

	public void OnRecvCharacterListForClient()
	{
		// 지금은 패킷 구조를 모르니.. 형태만 만들어두기로 한다.
		// list를 먼저 쭉 받아서 기억해두고 메인 캐릭터 설정하면 될듯

		// list

		// 

		CharacterData characterData = new CharacterData();
		characterData.actorId = "Actor0201";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1002";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2103";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0104";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1005";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0007";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1108";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1109";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2010";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2011";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor3212";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0113";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor3114";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2015";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1216";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor3117";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1218";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor3019";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2120";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor3021";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor3022";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0024";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0125";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1226";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2128";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1029";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0030";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor3231";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0233";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2235";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0236";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0037";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor2238";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1039";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor0240";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor1141";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
	}
	#endregion


	#region Server
	void Update()
	{
		UpdateDailyBoxResetTime();
		UpdateDailyPackageResetTime();
		UpdateDailyTrainingGoldResetTime();
		UpdateDailyTrainingDiaResetTime();
		UpdateNodeWarResetTime();
	}

	public bool newPlayerAddKeep { get; set; }
	public void OnNewlyCreatedPlayer()
	{
		highestPlayChapter = 0;
		highestClearStage = 0;
		selectedChapter = 0;
		chaosMode = false;
		sealCount = 0;
		sharedDailyBoxOpened = false;
		purifyCount = 0;
		notStreakCount = 0;
		notStreakCharCount = 0;
		originOpenCount = 0;
		characterBoxOpenCount = 0;
		ppBuyCount = 0;
		_listLevelPackage = null;
		sharedDailyPackageOpened = false;
		secondDailyBoxFillCount = 0;
		researchLevel = 0;
		dailyTrainingGoldCompleted = false;
		dailyTrainingDiaCompleted = false;

		// 나중에 지울 코드이긴 한데 MainSceneBuilder에서 NEWPLAYER_LEVEL1 디파인 켜둔채로 생성하는 테스트용 루틴일땐
		// 1챕터에서 시작하게 처리해둔다.
		// NEWPLAYER_LEVEL1 디파인 지울때 같이 지우면 된다.
		if (MainSceneBuilder.instance.playAfterInstallation == false)
		{
			highestPlayChapter = 1;
			selectedChapter = 1;
		}

		_listCharacterData.Clear();
		AddNewCharacter("Actor0201", "", 1);

		// 임의로 생성한거라 EntityKey를 만들어둘수가 없다.
		// 그렇다고 loginned 를 풀어서 통째로 받으면 괜히 커져서 EntityKey 리프레쉬 함수 하나 만들어서 호출하기로 한다.
		StartCoroutine(DelayedSyncCharacterEntity(5.0f));

		_mainCharacterId = "Actor0201";
		loginned = true;

		if (newPlayerAddKeep)
		{
			AddNewCharacter("Actor1002", "", 1);
			_mainCharacterId = "Actor1002";
		}
	}

	public void ResetData()
	{
		// 이게 가장 중요. 다른 것들은 받을때 알아서 다 비우고 다시 셋팅한다.
		loginned = false;

		// OnRecvPlayerData 함수들 두번 받아도 아무 문제없게 짜두면 여기서 딱히 할일은 없을거다.
		// 두번 받는거 뿐만 아니라 모든 변수를 다 덮어서 기록하는지도 확인하면 완벽하다.(건너뛰면 이전값이 남을테니 위험)
	}

	public void AddNewCharacter(string actorId, string serverCharacterId, int powerLevel, bool reinitializeActorStatus = false)
	{
		CharacterData characterData = new CharacterData();
		characterData.actorId = actorId;
		characterData.powerLevel = powerLevel;
		if (string.IsNullOrEmpty(serverCharacterId) == false)
			characterData.entityKey = new PlayFab.DataModels.EntityKey() { Id = serverCharacterId, Type = "character" };
		_listCharacterData.Add(characterData);

		if (reinitializeActorStatus)
		{
			// 플레이 중간에 캐릭터 인벤에 추가하는 곳은 여기 하나뿐이다.
			// 캐릭터를 획득하기 전에 체험이나 미리보기같은데서 먼저 보게되면 기본 스탯으로 생성되는데
			// 인벤에 들어오는 시점에 스탯을 리프레시 해놔야 장비나 연구까지 다 적용된 상태로 들어오게 된다.
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(actorId);
			if (playerActor != null)
				playerActor.actorStatus.InitializeActorStatus();
		}
	}

	IEnumerator DelayedSyncCharacterEntity(float delay)
	{
		yield return new WaitForSeconds(delay);

		PlayFabApiManager.instance.RequestSyncCharacterEntity();
	}

	public void OnRecvSyncCharacterEntity(List<CharacterResult> characterList)
	{
		for (int i = 0; i < characterList.Count; ++i)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(characterList[i].CharacterName);
			if (characterData == null)
				continue;
			characterData.entityKey = new PlayFab.DataModels.EntityKey() { Id = characterList[i].CharacterId, Type = "character" };
		}
	}

	public void OnRecvPlayerData(List<StatisticValue> playerStatistics, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData, List<CharacterResult> characterList)
	{
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				case "highestPlayChapter": highestPlayChapter = playerStatistics[i].Value; break;
				case "highestClearStage": highestClearStage = playerStatistics[i].Value; break;
			}
		}

		if (userData.ContainsKey("mainCharacterId"))
		{
			string actorId = userData["mainCharacterId"].Value;
			bool find = false;
			for (int i = 0; i < characterList.Count; ++i)
			{
				if (characterList[i].CharacterName == actorId)
				{
					find = true;
					break;
				}
			}
			if (find)
				_mainCharacterId = actorId;
			else
			{
				_mainCharacterId = "Actor0201";
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidMainCharacter);
			}
		}

		if (userData.ContainsKey("selectedChapter"))
		{
			int intValue = 0;
			if (int.TryParse(userData["selectedChapter"].Value, out intValue))
				selectedChapter = intValue;
			if (selectedChapter > highestPlayChapter)
			{
				selectedChapter = highestPlayChapter;
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidSelectedChapter);
			}
		}

		// 만약 디비에 정보가 없을 수 있다면(나중에 추가됐거나 하는 이유 등등) 이렇게 직접 초기화 하는게 안전하다.
		// 이 SHcha값은 항상 들어있을테지만 샘플로 이렇게 초기화 하는 형태를 보여주기 위해 남겨둔다.
		chaosMode = false;
		if (userData.ContainsKey("SHcha"))
		{
			int intValue = 0;
			if (int.TryParse(userData["SHcha"].Value, out intValue))
				chaosMode = (intValue == 1);
		}

		purifyCount = 0;
		if (userData.ContainsKey("SHpur"))
		{
			int intValue = 0;
			if (int.TryParse(userData["SHpur"].Value, out intValue))
				purifyCount = intValue;
		}

		if (userData.ContainsKey("seal"))
		{
			int intValue = 0;
			if (int.TryParse(userData["seal"].Value, out intValue))
				sealCount = intValue;
		}

		// 원래는 서버가 계산해서 넘겨받으려고 했는데
		// 계정 생성과 마찬가지로 로그인 이후에 rules사용해서 서버에서 클라우드 스크립트를 돌려도
		// 이미 로그인은 실행되고 있어서 그 안에 있는 UserData에는 실어줄 수가 없다.
		// 그래서 마지막 오픈 시간을 받아서 직접 계산하기로 한다.
		sharedDailyBoxOpened = false;
		if (userReadOnlyData.ContainsKey("lasBxDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasBxDat"].Value) == false)
				OnRecvDailyBoxInfo(userReadOnlyData["lasBxDat"].Value);
		}

		secondDailyBoxFillCount = 0;
		if (userReadOnlyData.ContainsKey("scDyCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["scDyCnt"].Value, out intValue))
				secondDailyBoxFillCount = intValue;
		}

		string eventState = "";
		if (userData.ContainsKey("even"))
			eventState = userData["even"].Value;
		EventManager.instance.OnRecvServerEvent(eventState);

		// Etc
		notStreakCount = 0;
		if (userReadOnlyData.ContainsKey("strCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["strCnt"].Value, out intValue))
				notStreakCount = intValue;
		}

		notStreakCharCount = 0;
		if (userReadOnlyData.ContainsKey("strCh"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["strCh"].Value, out intValue))
				notStreakCharCount = intValue;
		}

		originOpenCount = 0;
		if (userReadOnlyData.ContainsKey("orCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["orCnt"].Value, out intValue))
				originOpenCount = intValue;
		}

		characterBoxOpenCount = 0;
		if (userReadOnlyData.ContainsKey("chrBxCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["chrBxCnt"].Value, out intValue))
				characterBoxOpenCount = intValue;
		}

		ppBuyCount = 0;
		if (userReadOnlyData.ContainsKey("ppBuyCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["ppBuyCnt"].Value, out intValue))
				ppBuyCount = intValue;
		}

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listLevelPackage = null;
		if (userData.ContainsKey("lvPckLst"))
			_listLevelPackage = serializer.DeserializeObject<List<int>>(userData["lvPckLst"].Value);

		// 마지막 오픈 시간을 받는건 데일리패키지 역시 마찬가지다.
		sharedDailyPackageOpened = false;
		if (userReadOnlyData.ContainsKey("lasPckDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasPckDat"].Value) == false)
				OnRecvDailyPackageInfo(userReadOnlyData["lasPckDat"].Value);
		}

		researchLevel = 0;
		if (userReadOnlyData.ContainsKey("rsrLv"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["rsrLv"].Value, out intValue))
				researchLevel = intValue;
		}

		dailyTrainingGoldCompleted = false;
		if (userReadOnlyData.ContainsKey("lasTrGoDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasTrGoDat"].Value) == false)
				OnRecvDailyTrainingGoldInfo(userReadOnlyData["lasTrGoDat"].Value);
		}

		dailyTrainingDiaCompleted = false;
		if (userReadOnlyData.ContainsKey("lasTrDiDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasTrDiDat"].Value) == false)
				OnRecvDailyTrainingDiaInfo(userReadOnlyData["lasTrDiDat"].Value);
		}

		nodeWarCleared = false;
		if (userReadOnlyData.ContainsKey("lasNodDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasNodDat"].Value) == false)
				OnRecvNodeWarInfo(userReadOnlyData["lasNodDat"].Value);
		}

		nodeWarClearLevel = 0;
		if (userReadOnlyData.ContainsKey("nodLv"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["nodLv"].Value, out intValue))
				nodeWarClearLevel = intValue;
		}

		loginned = true;
	}

	const int PPMaxPerOriginBox = 125;
	const int PPMaxPerCharacterBox = 270;
	public void OnRecvCharacterList(List<CharacterResult> characterList, Dictionary<string, GetCharacterStatisticsResult> dicCharacterStatistics, List<ObjectResult> characterEntityObjectList)
	{
		_listCharacterData.Clear();
		for (int i = 0; i < characterList.Count; ++i)
		{
			string actorId = characterList[i].CharacterName;
			string serverCharacterId = characterList[i].CharacterId;
			if (dicCharacterStatistics.ContainsKey(serverCharacterId) == false)
				continue;
			if (dicCharacterStatistics[serverCharacterId].CharacterStatistics == null)
				continue;

			// 이건 필수항목이 아니라서 없을수도 있다.
			PlayFabApiManager.CharacterDataEntity1 dataObject = null;
			for (int j = 0; j < characterEntityObjectList.Count; ++j)
			{
				if (characterEntityObjectList[j].ObjectName == actorId)
				{
					dataObject = JsonUtility.FromJson<PlayFabApiManager.CharacterDataEntity1>(characterEntityObjectList[j].DataObject.ToString());
					break;
				}
			}

			CharacterData newCharacterData = new CharacterData();
			newCharacterData.actorId = actorId;
			newCharacterData.entityKey = new PlayFab.DataModels.EntityKey { Id = serverCharacterId, Type = "character" };
			newCharacterData.Initialize(dicCharacterStatistics[serverCharacterId].CharacterStatistics, dataObject);
			_listCharacterData.Add(newCharacterData);
		}

		// 전캐릭터의 pp 합산값 계산. max체크만 한다.
		int totalPp = 0;
		for (int i = 0; i < _listCharacterData.Count; ++i)
			totalPp += _listCharacterData[i].pp;
		if (totalPp > (originOpenCount * PPMaxPerOriginBox + characterBoxOpenCount * PPMaxPerCharacterBox + ppBuyCount))
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidTotalPp, false, totalPp);

		// 연구레벨 체크
		if (researchLevel > 0)
		{
			ResearchTableData researchTableData = TableDataManager.instance.FindResearchTableData(researchLevel);
			if (researchTableData == null || ResearchInfoGrowthCanvas.GetCurrentAccumulatedPowerLevel() < researchTableData.requiredAccumulatedPowerLevel)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidResearchLevel, false, researchLevel);
		}
	}

	public void OnRecvUpdateCharacterStatistics(List<DropManager.CharacterPpRequest> listPpInfo, List<DropManager.CharacterLbpRequest> listLbpInfo)
	{
		for (int i = 0; i < listPpInfo.Count; ++i)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(listPpInfo[i].actorId);
			if (characterData == null)
				continue;
			characterData.pp = listPpInfo[i].pp;
		}

		for (int i = 0; i < listLbpInfo.Count; ++i)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(listLbpInfo[i].actorId);
			if (characterData == null)
				continue;
			characterData.limitBreakPoint = listLbpInfo[i].lbp;
		}
	}

	public void OnRecvGrantCharacterList(object adCharIdPayload)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		Dictionary<string, string> dicGrantCharacter = serializer.DeserializeObject<Dictionary<string, string>>(adCharIdPayload.ToString());
		if (dicGrantCharacter == null)
			return;
		if (dicGrantCharacter.Count == 0)
			return;

		Dictionary<string, string>.Enumerator e = dicGrantCharacter.GetEnumerator();
		while (e.MoveNext())
		{
			string actorId = e.Current.Key;
			string characterId = e.Current.Value;
			AddNewCharacter(actorId, characterId, 1, true);
		}
	}

	void OnRecvDailyBoxInfo(DateTime lastDailyBoxOpenTime)
	{
		//if (ServerTime.UtcNow < lastDailyBoxOpenTime)
		//{
		//	// 어떻게 미래로 설정되어있을 수가 있나. 이건 무효.
		//	sharedDailyBoxOpened = false;
		//	return;
		//}

		if (ServerTime.UtcNow.Year == lastDailyBoxOpenTime.Year && ServerTime.UtcNow.Month == lastDailyBoxOpenTime.Month && ServerTime.UtcNow.Day == lastDailyBoxOpenTime.Day)
		{
			sharedDailyBoxOpened = true;
			dailyBoxResetTime = new DateTime(lastDailyBoxOpenTime.Year, lastDailyBoxOpenTime.Month, lastDailyBoxOpenTime.Day) + TimeSpan.FromDays(1);
		}
		else
			sharedDailyBoxOpened = false;
	}

	public void OnRecvDailyBoxInfo(string lastDailyBoxOpenTimeString, bool openResult = false)
	{
		DateTime lastDailyBoxOpenTime = new DateTime();
		if (DateTime.TryParse(lastDailyBoxOpenTimeString, out lastDailyBoxOpenTime))
		{
			DateTime universalTime = lastDailyBoxOpenTime.ToUniversalTime();
			OnRecvDailyBoxInfo(universalTime);
		}

		if (openResult && sharedDailyBoxOpened)
		{
			sealCount = 0;
			if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.SecondDailyBox))
			{
				secondDailyBoxFillCount += 1;
				if (secondDailyBoxFillCount == BattleInstanceManager.instance.GetCachedGlobalConstantInt("SealBigCount"))
					secondDailyBoxFillCount = 0;
			}
		}
	}

	bool _waitServerResponseForDailyBoxResetTime;
	int _dailyBoxRefreshRetryRemainCount = 2;
	void UpdateDailyBoxResetTime()
	{
		if (_waitServerResponseForDailyBoxResetTime)
			return;

		if (_dailyBoxRefreshRetryRemainCount == 0)
			return;

		if (sharedDailyBoxOpened == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dailyBoxResetTime) < 0)
			return;

		// Energy와 달리 여긴 서버응답 꼭 받고 넘겨야해서 클라가 선처리 하지 않는다.
		_waitServerResponseForDailyBoxResetTime = true;
		PlayFabApiManager.instance.RequestRefreshDailyInfo((serverFailure) =>
		{
			_waitServerResponseForDailyBoxResetTime = false;
			if (serverFailure)
			{
				// 뭔가 잘못 된거다. 재접해서 새로 받기 전까진 15초마다 다시 보내보자. 재시도는 2회만 하도록 한다.
				dailyBoxResetTime += TimeSpan.FromSeconds(15);
				_dailyBoxRefreshRetryRemainCount--;
			}
			else
			{
				// 날짜 바꾸는거에 대해 ok가 떨어졌다. 데일리상자가 초기화 된거로 처리해둔다.
				sharedDailyBoxOpened = false;
				dailyBoxResetTime += TimeSpan.FromDays(1);
				_dailyBoxRefreshRetryRemainCount = 2;
			}
		});
	}
	#endregion

	#region Character List
	List<CharacterData> _listCharacterData = new List<CharacterData>();
	public List<CharacterData> listCharacterData { get { return _listCharacterData; } }

	public CharacterData GetCharacterData(string actorId)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
				return _listCharacterData[i];
		}
		return null;
	}

	string _mainCharacterId = "Actor0201";
	public string mainCharacterId
	{
		get
		{
			// 디비에 저장되어있는 메인 캐릭터를 리턴
			// 우선은 임시
			return _mainCharacterId;
		}
		set
		{
			_mainCharacterId = value;
		}
	}

	public bool swappable { get { return _listCharacterData.Count > 1; } }

	public bool ContainsActor(string actorId)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
				return true;
		}
		return false;
	}

	public bool ContainsActorByGrade(int grade)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_listCharacterData[i].actorId);
			if (actorTableData.grade == grade)
				return true;
		}
		return false;
	}

	public void ReinitializeActorStatus()
	{
		// 모든 캐릭터의 스탯을 재계산 하도록 알려야한다.
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_listCharacterData[i].actorId);
			if (playerActor == null)
				continue;
			playerActor.actorStatus.InitializeActorStatus();
		}
	}
	#endregion




	#region Cash Shop
	public bool IsPurchasedLevelPackage(int level)
	{
		if (_listLevelPackage == null)
			return false;

		return _listLevelPackage.Contains(level);
	}

	public List<int> AddLevelPackage(int level)
	{
		if (_listLevelPackage == null)
			_listLevelPackage = new List<int>();
		if (_listLevelPackage.Contains(level) == false)
			_listLevelPackage.Add(level);
		return _listLevelPackage;
	}


	void OnRecvDailyPackageInfo(DateTime lastDailyPackageOpenTime)
	{
		// 이건 서버와의 패킷 주고받을땐 절대하면 안되는거다.
		// ServerTime.UtcNow는 사실 예측값이기 때문에 패킷 딜레이에 따라 매번 오차가 생길 수 있다.
		// 그러니 기록되어있는 데이터를 비교할때는 써도 되지만 실시간으로 주고받을때는 오차가 생길 수 있으므로 안하는게 맞다.
		// 게다가 서버가 이미 시간비교해서 ok한건데 왜 클라가 또 검사하나.
		// 그러니 처음 로그인해서 플레이어 데이터에 기록된걸 불러올때만 검사하면 될거다.
		//
		// 그런데.. 생각해보니 기록되어있는 데이터는 ReadOnly라 클라가 건드릴수도 없으니 클라우드 스크립트에서 적어줄때만 바뀌는데 절대 미래의 값이 들어있을리 없다.
		// 손으로 직접 운영툴에서 적지 않는 이상 잘못된 값이 들어있을리 없으니
		// 차라리 이 시간검사를 빼버리기로 한다.
		//if (ServerTime.UtcNow < lastDailyPackageOpenTime)
		//{
		//	// 어떻게 미래로 설정되어있을 수가 있나. 이건 무효.
		//	sharedDailyPackageOpened = false;
		//	return;
		//}

		if (ServerTime.UtcNow.Year == lastDailyPackageOpenTime.Year && ServerTime.UtcNow.Month == lastDailyPackageOpenTime.Month && ServerTime.UtcNow.Day == lastDailyPackageOpenTime.Day)
		{
			sharedDailyPackageOpened = true;
			dailyPackageResetTime = new DateTime(lastDailyPackageOpenTime.Year, lastDailyPackageOpenTime.Month, lastDailyPackageOpenTime.Day) + TimeSpan.FromDays(1);
		}
		else
			sharedDailyPackageOpened = false;
	}

	public void OnRecvDailyPackageInfo(string lastDailyPackageOpenTimeString)
	{
		DateTime lastDailyPackageOpenTime = new DateTime();
		if (DateTime.TryParse(lastDailyPackageOpenTimeString, out lastDailyPackageOpenTime))
		{
			DateTime universalTime = lastDailyPackageOpenTime.ToUniversalTime();
			OnRecvDailyPackageInfo(universalTime);
		}
	}

	void UpdateDailyPackageResetTime()
	{
		if (sharedDailyPackageOpened == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dailyPackageResetTime) < 0)
			return;

		// 일퀘와 달리 창을 열어야만 보이기도 하고 노출되는 횟수가 적을거 같아서 하루 갱신될때 서버에 알리지 않고 클라가 선처리 하기로 한다.
		sharedDailyPackageOpened = false;
		dailyPackageResetTime += TimeSpan.FromDays(1);
	}
	#endregion

	#region Training
	void OnRecvDailyTrainingGoldInfo(DateTime lastDailyTrainingGoldTime)
	{
		if (ServerTime.UtcNow.Year == lastDailyTrainingGoldTime.Year && ServerTime.UtcNow.Month == lastDailyTrainingGoldTime.Month && ServerTime.UtcNow.Day == lastDailyTrainingGoldTime.Day)
		{
			dailyTrainingGoldCompleted = true;
			dailyTrainingGoldResetTime = new DateTime(lastDailyTrainingGoldTime.Year, lastDailyTrainingGoldTime.Month, lastDailyTrainingGoldTime.Day) + TimeSpan.FromDays(1);
		}
		else
			dailyTrainingGoldCompleted = false;
	}

	public void OnRecvDailyTrainingGoldInfo(string lastDailyTrainingGoldTimeString)
	{
		DateTime lastDailyTrainingGoldTime = new DateTime();
		if (DateTime.TryParse(lastDailyTrainingGoldTimeString, out lastDailyTrainingGoldTime))
		{
			DateTime universalTime = lastDailyTrainingGoldTime.ToUniversalTime();
			OnRecvDailyTrainingGoldInfo(universalTime);
		}
	}

	void UpdateDailyTrainingGoldResetTime()
	{
		if (dailyTrainingGoldCompleted == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dailyTrainingGoldResetTime) < 0)
			return;

		// 클라 선처리로 갱신
		dailyTrainingGoldCompleted = false;
		dailyTrainingGoldResetTime += TimeSpan.FromDays(1);
	}

	void OnRecvDailyTrainingDiaInfo(DateTime lastDailyTrainingDiaTime)
	{
		if (ServerTime.UtcNow.Year == lastDailyTrainingDiaTime.Year && ServerTime.UtcNow.Month == lastDailyTrainingDiaTime.Month && ServerTime.UtcNow.Day == lastDailyTrainingDiaTime.Day)
		{
			dailyTrainingDiaCompleted = true;
			dailyTrainingDiaResetTime = new DateTime(lastDailyTrainingDiaTime.Year, lastDailyTrainingDiaTime.Month, lastDailyTrainingDiaTime.Day) + TimeSpan.FromDays(1);
		}
		else
			dailyTrainingDiaCompleted = false;
	}

	public void OnRecvDailyTrainingDiaInfo(string lastDailyTrainingDiaTimeString)
	{
		DateTime lastDailyTrainingDiaTime = new DateTime();
		if (DateTime.TryParse(lastDailyTrainingDiaTimeString, out lastDailyTrainingDiaTime))
		{
			DateTime universalTime = lastDailyTrainingDiaTime.ToUniversalTime();
			OnRecvDailyTrainingDiaInfo(universalTime);
		}
	}

	void UpdateDailyTrainingDiaResetTime()
	{
		if (dailyTrainingDiaCompleted == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dailyTrainingDiaResetTime) < 0)
			return;

		// 클라 선처리로 갱신
		dailyTrainingDiaCompleted = false;
		dailyTrainingDiaResetTime += TimeSpan.FromDays(1);
	}
	#endregion

	#region NodeWar
	void OnRecvNodeWarInfo(DateTime lastNodeWarTime)
	{
		if (ServerTime.UtcNow.Year == lastNodeWarTime.Year && ServerTime.UtcNow.Month == lastNodeWarTime.Month && ServerTime.UtcNow.Day == lastNodeWarTime.Day)
		{
			nodeWarCleared = true;
			nodeWarResetTime = new DateTime(lastNodeWarTime.Year, lastNodeWarTime.Month, lastNodeWarTime.Day) + TimeSpan.FromDays(1);
		}
		else
			nodeWarCleared = false;
	}

	public void OnRecvNodeWarInfo(string lastNodeWarTimeString)
	{
		DateTime lastNodeWarTime = new DateTime();
		if (DateTime.TryParse(lastNodeWarTimeString, out lastNodeWarTime))
		{
			DateTime universalTime = lastNodeWarTime.ToUniversalTime();
			OnRecvNodeWarInfo(universalTime);
		}
	}

	void UpdateNodeWarResetTime()
	{
		if (nodeWarCleared == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, nodeWarResetTime) < 0)
			return;

		// 클라 선처리로 갱신
		nodeWarCleared = false;
		nodeWarResetTime += TimeSpan.FromDays(1);
	}
	#endregion
}