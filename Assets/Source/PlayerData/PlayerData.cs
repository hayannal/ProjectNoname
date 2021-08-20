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
	public bool newlyCreated { get; private set; }
	public bool clientOnly { get; private set; }

#if UNITY_IOS
	// 심사빌드인지 체크해두는 변수
	public bool reviewVersion { get; set; }
#endif

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
	public ObscuredInt highestValue { get; set; }
	public ObscuredInt selectedChapter { get; set; }
	// 이 카오스는 마지막 챕터의 카오스 상태를 저장하는 값이다. 이건 4챕터 이후에 도전모드 상태에서 질때 바뀌며 유저가 선택으로 바꾸는 값이 아니다.
	public ObscuredBool chaosMode { get; set; }
	// 이건 한번이라도 카오스가 열렸었는지를 기억하는 변수
	public ObscuredBool chaosModeOpened { get; set; }
	public ObscuredInt purifyCount { get; set; }
	public ObscuredBool todayFreePurifyApplied { get; set; }
	public DateTime todayFreePurifyResetTime { get; private set; }
	public ObscuredInt sealCount { get; set; }
	public ObscuredBool sharedDailyBoxOpened { get; set; }
	public DateTime dailyBoxResetTime { get; private set; }
	// 두번째 상자 게이지. sealCount와 달리 서버가 센다. 그래서 ReadOnlyUserData에 있다.
	public ObscuredInt secondDailyBoxFillCount { get; set; }
	public ObscuredInt researchLevel { get; set; }

	// 일일 정보 갱신 타임. 여러개 몰아서 한다.
	public DateTime unfixedResetTime { get; private set; }
	public ObscuredBool unfixedResetInitialized { get; private set; }

	// 균형의 PP
	public ObscuredInt balancePp { get; set; }
	public bool balancePpAlarmState { get; set; }
	public ObscuredBool balancePpPurchased { get; set; }
	public DateTime balancePpResetTime { get; private set; }
	public ObscuredInt balancePpBuyCount { get; set; }

	// 뽑기 관련 변수
	public ObscuredInt notStreakCount { get; set; }
	public ObscuredInt notStreakCharCount { get; set; }
	public ObscuredInt notStreakLegendCharCount { get; set; }
	public ObscuredInt originOpenCount { get; set; }
	public ObscuredInt characterBoxOpenCount { get; set; }
	public ObscuredInt questCharacterBoxOpenCount { get; set; }
	// pp 총합산 검증을 위해 상점에서 구매한 pp 카운트를 저장해두는 변수
	public ObscuredInt ppBuyCount { get; set; }
	// 컨텐츠 등에서 제공하는 pp추가 수량 총합. 서버에선 클라가 주는대로 합산해둔다.
	public ObscuredInt ppContentsAddCount { get; set; }

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
	public ObscuredInt nodeWarCurrentLevel { get; set; }
	public ObscuredInt nodeWarBoostRemainCount { get; set; }
	public ObscuredInt nodeWarBonusPowerSource { get; set; }
	public ObscuredBool nodeWarAgainOpened { get; set; }

	// 카오스 파편
	public ObscuredInt chaosFragmentCount { get; set; }

	// sealCount 획득 연출용 변수. 클라에만 저장해두고 로비 돌아갈때 보여준다.
	// 이벤트 처리가 아니라서 PlayerData에 넣어두기로 한다.
	public ObscuredInt sealGainCount { get; set; }

	// 이용약관 확인용 변수. 값이 있으면 기록된거로 간주하고 true로 해둔다.
	public ObscuredBool termsConfirmed { get; set; }

	// 디스플레이 네임
	public string displayName { get; set; }

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

	// 네트워크 오류로 인해 씬을 재시작할때는 타이틀 떠서 진입하듯 초기 프로세스들을 검사해야한다.
	public bool checkRestartScene { get; set; }

	// 튜토가 끝나고 다운로드 받을 데이터가 있는지 확인 후 0보다 크다면 로비에서 다운받는 절차를 진행하는 모드다.
	// 두번째 캐릭터로 플레이 하기전에 나가는걸 방지하기 위해 로비에서 받게 해주는 유일한 스탭.
	// 혹시라도 멀리 넘어간 계정으로 하다가 로그아웃해서 게스트로 튜토 진행할때는 데이터가 있으니 들어오지 않는다.
	public bool lobbyDownloadState { get; set; }
	public long lobbyDownloadSize { get; set; }

	#region Player Info For Client
	public void OnRecvPlayerInfoForClient()
	{
		// 디비 및 훈련챕터 들어가기 전까지 임시로 쓰는 값이다. 1챕터 정보를 부른다.
		highestPlayChapter = 2;
		highestClearStage = 0;
		selectedChapter = 1;
		chaosMode = false;
		chaosModeOpened = false;

		// temp
		loginned = true;
		newlyCreated = false;
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
		//UpdateDailyBoxResetTime();
		UpdateFreePurifyResetTime();
		UpdateDailyPackageResetTime();
		UpdateDailyTrainingGoldResetTime();
		UpdateDailyTrainingDiaResetTime();
		UpdateNodeWarResetTime();
		UpdateBalancePpPurchaseResetTime();
		UpdateUnfixedTime();
	}

	public bool newPlayerAddKeep { get; set; }
	public void OnNewlyCreatedPlayer()
	{
		highestPlayChapter = 0;
		highestClearStage = 0;
		selectedChapter = 0;
		chaosMode = false;
		chaosModeOpened = false;
		sealCount = 0;
		sharedDailyBoxOpened = false;
		purifyCount = 0;
		todayFreePurifyApplied = false;
		balancePp = 0;
		balancePpAlarmState = false;
		balancePpPurchased = false;
		balancePpBuyCount = 0;
		notStreakCount = 0;
		notStreakCharCount = 0;
		notStreakLegendCharCount = 0;
		originOpenCount = 0;
		characterBoxOpenCount = 0;
		questCharacterBoxOpenCount = 0;
		ppBuyCount = 0;
		ppContentsAddCount = 0;
		_listLevelPackage = null;
		sharedDailyPackageOpened = false;
		secondDailyBoxFillCount = 0;
		researchLevel = 0;
		dailyTrainingGoldCompleted = false;
		dailyTrainingDiaCompleted = false;
		nodeWarClearLevel = 0;
		nodeWarCleared = false;
		nodeWarCurrentLevel = 0;
		nodeWarBoostRemainCount = 0;
		nodeWarAgainOpened = false;
		chaosFragmentCount = 0;
		termsConfirmed = false;

		// Obscured 아니지만 함께 처리
		_parsedLastLevelPackageResetDateTime = false;
		_parsedServerLevelPackageResetDateTime = false;

		// 나중에 지울 코드이긴 한데 MainSceneBuilder에서 NEWPLAYER_LEVEL1 디파인 켜둔채로 생성하는 테스트용 루틴일땐 1챕터에서 시작하게 처리해둔다.
		// NEWPLAYER_LEVEL1 디파인 지울때 같이 지우면 된다.
		//highestPlayChapter = 1;
		//selectedChapter = 1;

		EventManager.instance.OnRecvServerEvent("");
		ExperienceData.instance.OnRecvData("");
		TimeSpaceData.instance.ClearInventory();
		ContentsData.instance.ClearData();
		_listCharacterData.Clear();
		AddNewCharacter("Actor0201", "", 1);
		_mainCharacterId = "Actor0201";
		displayName = "";

		if (newPlayerAddKeep)
		{
			AddNewCharacter("Actor1002", "", 1);
			_mainCharacterId = "Actor1002";
		}

		// 임의로 전투맵부터 시작하는거라 EnterFlag조차 안받은 상태다. EnterFlag가 없으면 튜토리얼 EndGame패킷도 처리할 수 없기 때문에 발급받아야한다.
		StartCoroutine(DelayedEnterGame(4.0f));

		// 임의로 생성한거라 EntityKey를 만들어둘수가 없다.
		// 그렇다고 loginned 를 풀어서 통째로 받으면 괜히 커져서 EntityKey 리프레쉬 함수 하나 만들어서 호출하기로 한다.
		// 너무 빨리 호출하면 아직 디비에서 만들어지는 도중일수도 있으니 튜토 끝나기 전에만 받으면 되서 30초는 기다렸다가 호출해보기로 한다.
		StartCoroutine(DelayedSyncCharacterEntity(30.0f));

		// newlyCreated는 새로 생성된 계정에서만 true일거고 재접하거나 로그아웃 할때 false로 돌아와서 유지될거다.
		newlyCreated = true;
		loginned = true;
	}

	public void ResetData()
	{
		// 이게 가장 중요. 다른 것들은 받을때 알아서 다 비우고 다시 셋팅한다.
		loginned = false;
		newlyCreated = false;
		lobbyDownloadState = false;

		// OnRecvPlayerData 함수들 두번 받아도 아무 문제없게 짜두면 여기서 딱히 할일은 없을거다.
		// 두번 받는거 뿐만 아니라 모든 변수를 다 덮어서 기록하는지도 확인하면 완벽하다.(건너뛰면 이전값이 남을테니 위험)
		//
		// 대신 진입시에 앱구동처럼 처리하기 위해 재시작 플래그를 여기서 걸어둔다.
		checkRestartScene = true;
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

			// 이 타이밍에 맞춰서 가이드 퀘스트 검사할게 생겼다. 파워레벨 달성퀘면 여기서 1로 해줘야한다.
			if (GuideQuestData.instance.CheckPowerLevelUp(actorId))
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.PowerLevel);
		}
	}

	IEnumerator DelayedSyncCharacterEntity(float delay)
	{
		yield return new WaitForSeconds(delay);

		PlayFabApiManager.instance.RequestSyncCharacterEntity();
	}

	IEnumerator DelayedEnterGame(float delay)
	{
		yield return new WaitForSeconds(delay);

		// 튜토리얼에서 사용하는거라 에너지를 검사할 일은 없다.
		// 게다가 계정이 제대로 생성되었다면 네트워크에 문제가 없단 얘기일테니
		// 몰래하는거라 생각하고 별다른 처리는 하지 않는다.
		PlayFabApiManager.instance.RequestEnterGame(false, "", null, null);
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

	// 장비 데이터 처리하기 전에 highestPlayChapter 를 먼저 파싱해야해서 함수를 분리해둔다.
	public void OnRecvPlayerStatistics(List<StatisticValue> playerStatistics)
	{
		// nodeWarClearLevel은 디비에 없을 수 있으므로 초기화가 필요.
		nodeWarClearLevel = 0;
		chaosFragmentCount = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				case "highestPlayChapter": highestPlayChapter = playerStatistics[i].Value; break;
				case "highestClearStage": highestClearStage = playerStatistics[i].Value; break;
				case "highestValue": highestValue = playerStatistics[i].Value; break;
				case "nodClLv": nodeWarClearLevel = playerStatistics[i].Value; break;
				case "chaosFragment": chaosFragmentCount = playerStatistics[i].Value; break;
			}
		}
	}

	public void OnRecvPlayerData(Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData, List<CharacterResult> characterList, PlayerProfileModel playerProfile)
	{
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

		chaosModeOpened = false;
		if (userData.ContainsKey("SHchaOpn"))
		{
			int intValue = 0;
			if (int.TryParse(userData["SHchaOpn"].Value, out intValue))
				chaosModeOpened = (intValue == 1);
		}

		purifyCount = 0;
		if (userData.ContainsKey("SHpur"))
		{
			int intValue = 0;
			if (int.TryParse(userData["SHpur"].Value, out intValue))
				purifyCount = intValue;
		}

		todayFreePurifyApplied = false;
		if (userReadOnlyData.ContainsKey("lasFrePurDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasFrePurDat"].Value) == false)
				OnRecvFreePurifyInfo(userReadOnlyData["lasFrePurDat"].Value);
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

		// experience
		string experienceState = "";
		if (userReadOnlyData.ContainsKey("expr"))
			experienceState = userReadOnlyData["expr"].Value;
		ExperienceData.instance.OnRecvData(experienceState);

		// Etc
		balancePp = 0;
		balancePpAlarmState = false;
		if (userReadOnlyData.ContainsKey("balancePp"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["balancePp"].Value, out intValue))
				balancePp = intValue;
		}
		balancePpPurchased = false;
		if (userReadOnlyData.ContainsKey("lasBppDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasBppDat"].Value) == false)
				OnRecvPurchaseBalance(userReadOnlyData["lasBppDat"].Value);
		}

		balancePpBuyCount = 0;
		if (userReadOnlyData.ContainsKey("bppBuyCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["bppBuyCnt"].Value, out intValue))
				balancePpBuyCount = intValue;
		}

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

		notStreakLegendCharCount = 0;
		if (userReadOnlyData.ContainsKey("strLeCh"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["strLeCh"].Value, out intValue))
				notStreakLegendCharCount = intValue;
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

		questCharacterBoxOpenCount = 0;
		if (userReadOnlyData.ContainsKey("qstChrBxCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["qstChrBxCnt"].Value, out intValue))
				questCharacterBoxOpenCount = intValue;
		}

		ppBuyCount = 0;
		if (userReadOnlyData.ContainsKey("ppBuyCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["ppBuyCnt"].Value, out intValue))
				ppBuyCount = intValue;
		}

		ppContentsAddCount = 0;
		if (userReadOnlyData.ContainsKey("ppCtsAddCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["ppCtsAddCnt"].Value, out intValue))
				ppContentsAddCount = intValue;
		}

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listLevelPackage = null;
		if (userData.ContainsKey("lvPckLst"))
		{
			string lvPckLstString = userData["lvPckLst"].Value;
			if (string.IsNullOrEmpty(lvPckLstString) == false)
				_listLevelPackage = serializer.DeserializeObject<List<int>>(lvPckLstString);
		}

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

		nodeWarCurrentLevel = 0;
		if (userReadOnlyData.ContainsKey("nodCuLv"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["nodCuLv"].Value, out intValue))
				nodeWarCurrentLevel = intValue;
		}

		nodeWarBoostRemainCount = 0;
		if (userReadOnlyData.ContainsKey("nodBst"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["nodBst"].Value, out intValue))
				nodeWarBoostRemainCount = intValue;
		}

		nodeWarAgainOpened = false;
		if (userReadOnlyData.ContainsKey("lasNodAgDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasNodAgDat"].Value) == false)
				OnRecvNodeWarOpenAgainInfo(userReadOnlyData["lasNodAgDat"].Value);
		}

		#region Unfixed NodeWar Bonus Info
		// NodeWar의 보너스 파워소스는 클라가 결정해서 서버에 기록해두는 형태다.
		if (userReadOnlyData.ContainsKey("lasUnfxNodDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasUnfxNodDat"].Value) == false)
				_lastUnfixedDateTimeString = userReadOnlyData["lasUnfxNodDat"].Value;
		}

		if (userReadOnlyData.ContainsKey("nodBnsPs"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["nodBnsPs"].Value) == false)
				_nodeWarBonusString = userReadOnlyData["nodBnsPs"].Value;
		}
		#endregion

		termsConfirmed = false;
		if (userReadOnlyData.ContainsKey("termsDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["termsDat"].Value) == false)
				termsConfirmed = true;
		}

		ContentsData.instance.OnRecvContentsData(userData, userReadOnlyData);

		displayName = "";
		if (string.IsNullOrEmpty(playerProfile.DisplayName) == false)
			displayName = playerProfile.DisplayName;

		newlyCreated = false;
		loginned = true;
	}

	const int PPMaxPerOriginBox = 80;
	const int PPMaxPerCharacterBox = 102;
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
		// balancePp도 합산시켜줘야한다.
		totalPp += balancePp;
		if (totalPp > (originOpenCount * PPMaxPerOriginBox + characterBoxOpenCount * PPMaxPerCharacterBox + questCharacterBoxOpenCount * PPMaxPerCharacterBox + ppBuyCount + balancePpBuyCount + ppContentsAddCount))
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidTotalPp, false, totalPp);

		// 연구레벨 체크
		if (researchLevel > 0)
		{
			ResearchTableData researchTableData = TableDataManager.instance.FindResearchTableData(researchLevel);
			if (researchTableData == null || ResearchInfoGrowthCanvas.CheckResearch(researchLevel) == false)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidResearchLevel, false, researchLevel);
		}
	}

	public void OnRecvUpdateCharacterStatistics(List<DropManager.CharacterPpRequest> listPpInfo, List<DropManager.CharacterTrpRequest> listTrpInfo, int addBalancePp)
	{
		for (int i = 0; i < listPpInfo.Count; ++i)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(listPpInfo[i].actorId);
			if (characterData == null)
				continue;
			characterData.pp = listPpInfo[i].pp;
		}

		for (int i = 0; i < listTrpInfo.Count; ++i)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(listTrpInfo[i].actorId);
			if (characterData == null)
				continue;
			characterData.transcendPoint = listTrpInfo[i].trp;
		}

		// 위의 pp와 달리 추가해야할 값이 들어있다. 얻을때 획득용 알람 처리도 해준다.
		this.balancePp += addBalancePp;
		if (addBalancePp > 0) this.balancePpAlarmState = true;

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.RefreshBalanceAlarmObject();
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
				{
					secondDailyBoxFillCount = 0;
					originOpenCount += 1;
				}
			}
		}
	}

	//bool _waitServerResponseForDailyBoxResetTime;
	//int _dailyBoxRefreshRetryRemainCount = 2;
	//void UpdateDailyBoxResetTime()
	//{
	//	if (_waitServerResponseForDailyBoxResetTime)
	//		return;
	//
	//	if (_dailyBoxRefreshRetryRemainCount == 0)
	//		return;
	//
	//	if (sharedDailyBoxOpened == false)
	//		return;
	//
	//	if (DateTime.Compare(ServerTime.UtcNow, dailyBoxResetTime) < 0)
	//		return;
	//
	//	// Energy와 달리 여긴 서버응답 꼭 받고 넘겨야해서 클라가 선처리 하지 않는다.
	//	_waitServerResponseForDailyBoxResetTime = true;
	//	PlayFabApiManager.instance.RequestRefreshDailyInfo((serverFailure) =>
	//	{
	//		_waitServerResponseForDailyBoxResetTime = false;
	//		if (serverFailure)
	//		{
	//			// 뭔가 잘못 된거다. 재접해서 새로 받기 전까진 15초마다 다시 보내보자. 재시도는 2회만 하도록 한다.
	//			dailyBoxResetTime += TimeSpan.FromSeconds(15);
	//			_dailyBoxRefreshRetryRemainCount--;
	//		}
	//		else
	//		{
	//			// 날짜 바꾸는거에 대해 ok가 떨어졌다. 데일리상자가 초기화 된거로 처리해둔다.
	//			sharedDailyBoxOpened = false;
	//			dailyBoxResetTime += TimeSpan.FromDays(1);
	//			_dailyBoxRefreshRetryRemainCount = 2;
	//		}
	//	});
	//}

	// 일반적인 DateTime 리셋과 달리 서버에다가 데이터를 올려서 기록시켜두는 로직들이 여러개 추가되었다.
	// 제일 기본적으로는 일일퀘이고 그 담에 추가된게 Unfixed 상점 리스트고 그 다음에 추가된게 NodeWar 보너스 타입이다.
	// 이 세가지 모두 공통적인 문제가 있었는데
	// 아무리 패킷 딜레이를 오차 안생기게 여러번 체크한다해도
	// 59분 59초 535ms 이런식으로 날짜가 갱신되기 직전 서버에 도착하는 경우가 있어서 에러가 나는거였다.
	// (ServerUtc 를 사용하더라도 발생하는 문제였다. 이 안에는 서버에서 클라로 오는 시간까지 포함되어있기 때문)
	//
	// 그래서 차라리 서버한테 시간 받아서 다음날 넘어간건지 확인한 후 갱신하는 것들을 모아서 한번에 하면
	// 조금 늦게 처리되더라도 안전하게 갱신할 수 있겠다 싶어서 하단의 로직을 추가하기로 했다.
	bool _waitServerResponseForUnfixedResetTime;
	int _unfixedRefreshRetryRemainCount = 30;
	// 그런데 한번 Send해두고 응답이 없을 경우도 있었더니 이 패킷의 응답이 올때까지는 계속 00:00:00 인채로 대기하게 된다.
	// 지금은 갱신이 필요한 상황이니 Send하고도 1초안에 응답이 없으면 바로 재시도하고
	// 이후 혹시 중복해서 응답이 오면 빨리 오는거만 처리하고 나머지는 무시하기로 한다.
	float _retryRefreshRemainTime = 0.0f;
	int _lastRefreshedUniversalTimeDay = 0;
	void UpdateUnfixedTime()
	{
		if (loginned == false)
			return;
		if (unfixedResetInitialized == false)
			return;

		if (_waitServerResponseForUnfixedResetTime)
		{
			// GetServerUtc 호출한거에 대한 응답을 기다리고 있는데 1초 동안 오지 않는다면 다시 보내본다.
			if (_retryRefreshRemainTime > 0.0f)
			{
				_retryRefreshRemainTime -= Time.deltaTime;
				if (_retryRefreshRemainTime <= 0.0f)
				{
					_waitServerResponseForUnfixedResetTime = false;
					_retryRefreshRemainTime = 0.0f;

					_unfixedRefreshRetryRemainCount--;
					if (_unfixedRefreshRetryRemainCount <= 0)
						PlayFabApiManager.instance.HandleCommonError();
				}
			}
			return;
		}
		if (_unfixedRefreshRetryRemainCount <= 0)
			return;

		// ServerTime.UtcNow와 비교하는 진짜 의미는 이쯤 보내면 서버의 24:00쯤에 딱 맞춰서 도착하겠지 라는 의미다.
		// 즉 패킷이 갑자기 빨리 가면 59분 59초 xxx ms에도 도착할 수 있다는 얘기다.
		if (DateTime.Compare(ServerTime.UtcNow, unfixedResetTime) < 0)
			return;

		// 여긴 서버응답 꼭 받고 처리를 진행할거라서 서버 응답을 기다려야한다.
		_waitServerResponseForUnfixedResetTime = true;
		_retryRefreshRemainTime = 1.0f;
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetServerUtc",
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			_waitServerResponseForUnfixedResetTime = false;

			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("date", out object date);

			bool refreshed = false;
			double nextSecond = 0.5;
			DateTime serverUtcTime = new DateTime();
			if (DateTime.TryParse((string)date, out serverUtcTime))
			{
				DateTime universalTime = serverUtcTime.ToUniversalTime();

				// 혹시 여러개 보낸거로 인해서 중복 처리 될 샹황이라면 그냥 리턴
				if (_lastRefreshedUniversalTimeDay == universalTime.Day)
				{
					_unfixedRefreshRetryRemainCount--;
					if (_unfixedRefreshRetryRemainCount <= 0)
						PlayFabApiManager.instance.HandleCommonError();
					return;
				}

				if (universalTime.Year == unfixedResetTime.Year && universalTime.Month == unfixedResetTime.Month && universalTime.Day == unfixedResetTime.Day)
				{
					// 이러면 확실히 서버에서도 다음날이 된걸 확인할 수 있다는거다.
					Debug.Log("Daily Unfixed Refreshed Start");
					refreshed = true;
					_lastRefreshedUniversalTimeDay = universalTime.Day;
					unfixedResetTime = new DateTime(universalTime.Year, universalTime.Month, universalTime.Day) + TimeSpan.FromDays(1);
					_unfixedRefreshRetryRemainCount = 30;

					// 여기서 각종 갱신 처리 및 패킷들을 보내면 문제없을거다.
					if (sharedDailyBoxOpened)
						sharedDailyBoxOpened = false;
					if (todayFreePurifyApplied)
						todayFreePurifyApplied = false;
					DailyShopData.instance.ResetDailyShopSlotPurchaseInfo();
					DailyShopData.instance.ResetDailyFreeItemInfo();
					RegisterNodeWarBonusPowerSource();
					QuestData.instance.ResetQuestStepInfo();
					CheckLevelPackageResetInfo();
					CumulativeEventData.instance.ResetEventInfo();
					ContentsData.instance.ResetContentsInfo();
				}
				else
				{
					// 다음날이 아니라면 얼마나 남았는지 판단하고 차이에 따라 다르게 처리한다. 3초 이상이면 3초까지 땡겨놓고 그 이하라면 0.5초 단위로 다시 보내서 확인하기로 한다.
					TimeSpan remainTime = unfixedResetTime - universalTime;
					if (remainTime > TimeSpan.FromSeconds(3))
					{
						nextSecond = remainTime.TotalSeconds - 3;
						Debug.LogFormat("Too different : {0} seconds", nextSecond);
					}
				}
			}

			if (refreshed == false)
			{
				// 뭔가 잘못 된거다. 재접해서 새로 받기 전까진 0.5초마다 다시 보내보자. 재시도는 30회
				unfixedResetTime += TimeSpan.FromSeconds(nextSecond);
				_unfixedRefreshRetryRemainCount--;
				if (_unfixedRefreshRetryRemainCount <= 0)
					PlayFabApiManager.instance.HandleCommonError();
			}
		}, (error) =>
		{
			// 입력막는 캔버스 없이 보내는거라 에러 핸들링 하면 안된다.
			//HandleCommonError(error);

			// 시간 갱신만 해두면 될듯.
			_waitServerResponseForUnfixedResetTime = false;
			unfixedResetTime += TimeSpan.FromSeconds(0.5);

			// 네트워크 오류여도 진행불가 상태일거라 판단하고 재접속을 시킨다.
			_unfixedRefreshRetryRemainCount--;
			if (_unfixedRefreshRetryRemainCount <= 0)
				PlayFabApiManager.instance.HandleCommonError();
		});
	}

	public bool IsWaitingRefreshDailyInfo()
	{
		if (_waitServerResponseForUnfixedResetTime)
			return true;
		if (unfixedResetInitialized && DateTime.Compare(ServerTime.UtcNow, unfixedResetTime) >= 0)
			return true;
		return false;
	}
	#endregion

	#region Today Free Purify
	void OnRecvFreePurifyInfo(DateTime lastTodayFreePurifyApplyTime)
	{
		if (ServerTime.UtcNow.Year == lastTodayFreePurifyApplyTime.Year && ServerTime.UtcNow.Month == lastTodayFreePurifyApplyTime.Month && ServerTime.UtcNow.Day == lastTodayFreePurifyApplyTime.Day)
		{
			todayFreePurifyApplied = true;
			todayFreePurifyResetTime = new DateTime(lastTodayFreePurifyApplyTime.Year, lastTodayFreePurifyApplyTime.Month, lastTodayFreePurifyApplyTime.Day) + TimeSpan.FromDays(1);
		}
		else
			todayFreePurifyApplied = false;
	}

	public void OnRecvFreePurifyInfo(string lastTodayFreePurifyApplyTimeString)
	{
		DateTime lastTodayFreePurifyApplyTime = new DateTime();
		if (DateTime.TryParse(lastTodayFreePurifyApplyTimeString, out lastTodayFreePurifyApplyTime))
		{
			DateTime universalTime = lastTodayFreePurifyApplyTime.ToUniversalTime();
			OnRecvFreePurifyInfo(universalTime);
		}
	}

	void UpdateFreePurifyResetTime()
	{
		if (todayFreePurifyApplied == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, todayFreePurifyResetTime) < 0)
			return;

		// 해당 창에서는 팝업창이라 타이머 표시를 하지 않을거라서 여기서 처리해준다.
		if (ChaosPurifierConfirmCanvas.instance != null && ChaosPurifierConfirmCanvas.instance.gameObject.activeSelf)
			ChaosPurifierConfirmCanvas.instance.gameObject.SetActive(false);
		todayFreePurifyApplied = false;
		todayFreePurifyResetTime += TimeSpan.FromDays(1);
	}
	#endregion

	public void LateInitialize()
	{
		unfixedResetTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
		unfixedResetInitialized = true;

		CheckUnfixedNodeWarInfo();
		CheckHighestValue();
	}

	void CheckHighestValue()
	{
		if (highestPlayChapter > 0 || highestClearStage > 0)
		{
			if (highestValue == 0)
			{
				PlayFabApiManager.instance.RequestRegisterHighestValue(() =>
				{
					highestValue = highestPlayChapter * 100 + highestClearStage;
				});
			}
		}
	}

	#region Unifxed NodeWar Bonus Info
	string _lastUnfixedDateTimeString = "";
	string _nodeWarBonusString = "";
	// 클라 구동 후 노드워 들어가기 전에 1회만 체크하면 된다.
	bool _checkedUnfixedNodeWarInfo = false;
	void CheckUnfixedNodeWarInfo()
	{
		if (_checkedUnfixedNodeWarInfo)
			return;
		if (ContentsManager.IsTutorialChapter())
			return;

		bool needRegister = false;
		if (_lastUnfixedDateTimeString == "")
			needRegister = true;
		if (needRegister == false)
		{
			DateTime lastUnfixedItemDateTime = new DateTime();
			if (DateTime.TryParse(_lastUnfixedDateTimeString, out lastUnfixedItemDateTime))
			{
				DateTime universalTime = lastUnfixedItemDateTime.ToUniversalTime();
				if (ServerTime.UtcNow.Year == universalTime.Year && ServerTime.UtcNow.Month == universalTime.Month && ServerTime.UtcNow.Day == universalTime.Day)
				{
					int result = 0;
					int.TryParse(_nodeWarBonusString, out result);
					nodeWarBonusPowerSource = result;
				}
				else
					needRegister = true;
			}
		}
		_checkedUnfixedNodeWarInfo = true;

		if (needRegister == false)
			return;
		RegisterNodeWarBonusPowerSource();
	}

	void RegisterNodeWarBonusPowerSource()
	{
		nodeWarBonusPowerSource = 0;
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.NodeWar))
			nodeWarBonusPowerSource = UnityEngine.Random.Range(0, 4);

		PlayFabApiManager.instance.RequestRegisterNodeWarBonusPowerSource(nodeWarBonusPowerSource);
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

	#region Reset LevelPackage
	public void OnRecvLevelPackageResetInfo(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, bool newlyCreated)
	{
		// 계정 생성시에는 굳이 할필요가 없다. 패스해보자.
		if (newlyCreated)
			return;

		OnRecvLevelPackageResetInfo(titleData);

		_parsedLastLevelPackageResetDateTime = false;
		if (userReadOnlyData.ContainsKey("lasLvRstDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasLvRstDat"].Value) == false)
				OnRecvLastLevelPackageResetInfo(userReadOnlyData["lasLvRstDat"].Value);
		}

		CheckLevelPackageResetInfo();
	}

	bool _parsedServerLevelPackageResetDateTime = false;
	DateTime _serverLevelPackageResetTime;
	public void OnRecvLevelPackageResetInfo(Dictionary<string, string> titleData)
	{
		_parsedServerLevelPackageResetDateTime = false;

		if (titleData.ContainsKey("lvRst") == false)
			return;

		string serverLevelPackageResetTimeString = titleData["lvRst"];
		if (string.IsNullOrEmpty(serverLevelPackageResetTimeString))
			return;

		DateTime serverLevelPackageResetTime = new DateTime();
		if (DateTime.TryParse(serverLevelPackageResetTimeString, out serverLevelPackageResetTime))
		{
			_serverLevelPackageResetTime = serverLevelPackageResetTime.ToUniversalTime();
			_parsedServerLevelPackageResetDateTime = true;
		}
	}

	bool _parsedLastLevelPackageResetDateTime = false;
	DateTime _lastLevelPackageResetTime;
	void OnRecvLastLevelPackageResetInfo(string lastLevelPackageResetTimeString)
	{
		_parsedLastLevelPackageResetDateTime = false;

		DateTime lastlevelPackageResetTime = new DateTime();
		if (DateTime.TryParse(lastLevelPackageResetTimeString, out lastlevelPackageResetTime))
		{
			_lastLevelPackageResetTime = lastlevelPackageResetTime.ToUniversalTime();
			_parsedLastLevelPackageResetDateTime = true;
		}
	}

	void CheckLevelPackageResetInfo()
	{
		if (_parsedServerLevelPackageResetDateTime == false)
			return;

		// 파싱이 되어있는 상태라면
		// 현재 등록되어있는걸 구해서 없거나 그거보다 과거면 패킷을 보내야한다.
		bool needRequest = false;
		if (_parsedLastLevelPackageResetDateTime == false)
			needRequest = true;
		if (_parsedLastLevelPackageResetDateTime && _lastLevelPackageResetTime < _serverLevelPackageResetTime)
			needRequest = true;

		if (needRequest == false)
			return;

		PlayFabApiManager.instance.RequestResetLevelPackage(() =>
		{
			// 타이머가 있는 캔버스도 아니라서 정확한 시간을 필요로 하지 않는다.
			// 그냥 지금 시간으로 해두면 다음날까지 켜놔도 새로 리셋되지 않을테니 오늘 날짜로만 넣어두기로 한다.
			_listLevelPackage = null;
			_lastLevelPackageResetTime = ServerTime.UtcNow;
			_parsedLastLevelPackageResetDateTime = true;
		});
	}
	#endregion


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

	void OnRecvNodeWarOpenAgainInfo(DateTime lastNodeWarOpenAgainTime)
	{
		if (ServerTime.UtcNow.Year == lastNodeWarOpenAgainTime.Year && ServerTime.UtcNow.Month == lastNodeWarOpenAgainTime.Month && ServerTime.UtcNow.Day == lastNodeWarOpenAgainTime.Day)
		{
			nodeWarAgainOpened = true;

			// ResetTime은 따로 변수 추가하지 않고 nodeWarCleared가 썼던 변수를 같이 사용한다.
			// 둘 중 하나 셋팅된거중에 아무거나 쓰면 된다. 혹은 둘다 되어있더라도 같은 값이 나올테니 아무거나 쓰면 된다.
			nodeWarResetTime = new DateTime(lastNodeWarOpenAgainTime.Year, lastNodeWarOpenAgainTime.Month, lastNodeWarOpenAgainTime.Day) + TimeSpan.FromDays(1);
		}
		else
			nodeWarAgainOpened = false;
	}

	public void OnRecvNodeWarOpenAgainInfo(string lastNodeWarOpenAgainTimeString)
	{
		DateTime lastNodeWarOpenAgainTime = new DateTime();
		if (DateTime.TryParse(lastNodeWarOpenAgainTimeString, out lastNodeWarOpenAgainTime))
		{
			DateTime universalTime = lastNodeWarOpenAgainTime.ToUniversalTime();
			OnRecvNodeWarOpenAgainInfo(universalTime);
		}
	}

	void UpdateNodeWarResetTime()
	{
		// 오늘자 노드워를 클리어 하거나 재오픈을 했다면 리셋이 필요하다.
		if (nodeWarCleared == false && nodeWarAgainOpened == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, nodeWarResetTime) < 0)
			return;

		// 클라 선처리로 갱신
		nodeWarCleared = false;
		nodeWarAgainOpened = false;
		nodeWarResetTime += TimeSpan.FromDays(1);

		// 이 타이밍에 사실 서버의 시간이 다음날이라고 확정된 타임은 아니다.
		//RegisterNodeWarBonusPowerSource();
	}
	#endregion

	#region Balance Purchase
	void OnRecvPurchaseBalance(DateTime lastBalancePpPurchaseTime)
	{
		if (ServerTime.UtcNow.Year == lastBalancePpPurchaseTime.Year && ServerTime.UtcNow.Month == lastBalancePpPurchaseTime.Month && ServerTime.UtcNow.Day == lastBalancePpPurchaseTime.Day)
		{
			balancePpPurchased = true;
			balancePpResetTime = new DateTime(lastBalancePpPurchaseTime.Year, lastBalancePpPurchaseTime.Month, lastBalancePpPurchaseTime.Day) + TimeSpan.FromDays(1);
		}
		else
			balancePpPurchased = false;
	}

	public void OnRecvPurchaseBalance(string lastBalancePpPurchaseTimeString)
	{
		DateTime lastBalancePpPurchaseTime = new DateTime();
		if (DateTime.TryParse(lastBalancePpPurchaseTimeString, out lastBalancePpPurchaseTime))
		{
			DateTime universalTime = lastBalancePpPurchaseTime.ToUniversalTime();
			OnRecvPurchaseBalance(universalTime);
		}
	}

	void UpdateBalancePpPurchaseResetTime()
	{
		if (balancePpPurchased == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, balancePpResetTime) < 0)
			return;

		// 클라 선처리로 갱신
		balancePpPurchased = false;
		balancePpResetTime += TimeSpan.FromDays(1);
	}
	#endregion
}