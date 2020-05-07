//#define USE_TITLE_PLAYER_ENTITY

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.DataModels;
using ClientSuspect;


public class PlayFabApiManager : MonoBehaviour
{
	public static PlayFabApiManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PlayFabApiManager")).AddComponent<PlayFabApiManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PlayFabApiManager _instance = null;

	string playFabId { get { return _playFabId; } }
	ObscuredString _playFabId;
#if USE_TITLE_PLAYER_ENTITY
	PlayFab.DataModels.EntityKey _titlePlayerEntityKey;
#endif

	// 네트워크 함수의 특징인데
	// 로그인이나 로그인 직후 받는 플레이어 데이터(인벤부터 캐릭터 리스트 등등) 등에는
	// 보통 UI의 인풋-아웃풋 처리로 되는게 아니라서 콜백이 필요없지만
	// UI에서 진행되는 요청들(캐릭변경, 강화, 장착 등등)에는 거의 대부분 콜백이 필요하게 된다.
	// 
	// 이거와 비슷하게
	// 몇몇 항목들은 재전송이 필요하지만(메인 캐릭터 교체, 인게임 결과 반영)
	// 재화를 소비하는 항목들은 재전송하기엔 두번 재화가 나가서 위험할때가 많다.(구매, 하트소모 등등)
	// 그래서 RetrySendManager는 모든 항목에 붙이는 대신 필요한 곳에만 적용하기로 한다.

	#region Time Record
	Dictionary<string, float> _dicTimeRecord = new Dictionary<string, float>();
	public void StartTimeRecord(string recordId)
	{
		if (_dicTimeRecord.ContainsKey(recordId))
			_dicTimeRecord[recordId] = Time.time;
		else
			_dicTimeRecord.Add(recordId, Time.time);
	}

	public void EndTimeRecord(string recordId)
	{
		if (_dicTimeRecord.ContainsKey(recordId) == false)
			return;

		float deltaTime = Time.time - _dicTimeRecord[recordId];
		Debug.LogFormat("Packet Delay - {0} : {1:0.###}", recordId, deltaTime);
	}
	#endregion

	void HandleCommonError(PlayFabError error)
	{
		Debug.LogError(error.GenerateErrorReport());

		if (WaitingNetworkCanvas.IsShow())
			WaitingNetworkCanvas.Show(false);

		switch (error.Error)
		{
			case PlayFabErrorCode.InsufficientFunds:
				PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
				{
					FunctionName = "IncCliSus",
					FunctionParameter = new { Er = (int)error.Error, Pa1 = 0, Pa2 = 0 },
					GeneratePlayStreamEvent = true
				}, null, null);
				break;
		}

		if (error.Error == PlayFabErrorCode.ServiceUnavailable || error.HttpCode == 400)
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_DisconnectServer"), () =>
			{
				// 모든 정보를 다시 받아야하기 때문에 로그인부터 하는게 맞다.
				PlayerData.instance.ResetData();
				SceneManager.LoadScene(0);
			}, 100);
		}
	}

	public void RequestIncCliSus(eClientSuspectCode clientSuspectCode, bool sendBattleInfo = false, int param2 = 0)
	{
		int param1 = 0;
		if (sendBattleInfo)
		{
			int powerLevel = 1;
			CharacterData characterData = PlayerData.instance.GetCharacterData(BattleInstanceManager.instance.playerActor.actorId);
			if (characterData != null) powerLevel = characterData.powerLevel;
			param1 = PlayerData.instance.selectedChapter * 100 + powerLevel;
		}

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "IncCliSus",
			FunctionParameter = new { Er = (int)clientSuspectCode, Pa1 = param1, Pa2 = param2 },
			GeneratePlayStreamEvent = true
		}, null, (errorCallback) =>
		{
			switch (clientSuspectCode)
			{
				case eClientSuspectCode.OneShotKillBoss:
					HandleCommonError(errorCallback);
					break;
			}
		});
	}

	public static string CheckSum(string input)
	{
		int chk = 0x68319547;
		int length = input.Length;
		for (int i = 0; i < length; ++i)
		{
			chk += (Convert.ToInt32((int)input[i]) * (i + 1));
		}
		return Convert.ToString((chk & 0xffffffff), 16);
	}

	#region Login with PlayerData, Entity Objects
	// 자주 사용되는 걸 UserData로 보냈더니 15초당 10개 제한에 걸려서 위험하기도 해서
	// 차라리 Entity Objects를 사용하기로 한다.
	// 그런데 Entity Objects는 로그인의 추가 데이터로 받을 수 없기 때문에
	// 로그인 즉시 요청함수를 날릴거고
	// 어차피 이때 추가로 날리는 겸 해서 캐릭터 커스텀 데이터도 같이 받는게 좋을거 같아서
	// 각종 필요한 모든 Entity Objects들을 요청해두기로 한다.
	int _requestCountForGetPlayerData = 0;
	LoginResult _loginResult;
#if USE_TITLE_PLAYER_ENTITY
	GetObjectsResponse _titlePlayerEntityObject;
#endif
	Dictionary<string, GetCharacterStatisticsResult> _dicCharacterStatisticsResult = new Dictionary<string, GetCharacterStatisticsResult>();
	List<ObjectResult> _listCharacterEntityObject = new List<ObjectResult>();
	public void OnRecvLoginResult(LoginResult loginResult)
	{
		_playFabId = loginResult.PlayFabId;
#if USE_TITLE_PLAYER_ENTITY
		_titlePlayerEntityKey = new PlayFab.DataModels.EntityKey { Id = loginResult.EntityToken.Entity.Id, Type = loginResult.EntityToken.Entity.Type };
#endif

		CurrencyData.instance.OnRecvCurrencyData(loginResult.InfoResultPayload.UserVirtualCurrency, loginResult.InfoResultPayload.UserVirtualCurrencyRechargeTimes);

		if (loginResult.NewlyCreated)
		{
			// 처음 만든 계정이면 어차피 읽어올게 없다.
			// 오히려 서버 rules에 넣어둔 OnCreatedPlayer cloud script가 돌고있을텐데
			// 이게 비동기라서 로그인과 동시에 날아온 인벤 리스트에는 들어있지 않게 된다.
			// 그래서 직접 캐릭터를 인벤토리에 넣어주고 넘어가면 된다.
			PlayerData.instance.OnNewlyCreatedPlayer();
			return;
		}

		_loginResult = loginResult;
		_dicCharacterStatisticsResult.Clear();
		_listCharacterEntityObject.Clear();

		StartTimeRecord("PlayerData");

#if USE_TITLE_PLAYER_ENTITY
		// 최초 생성 이후부터는 로그인 했을때 값이 들어있을테니 읽어와서 처리한다.
		// limit을 보다보니 Player Entity쓰는거보다 UserData갱신하는게 훨씬 더 편하고 개별로 다 조각조각 내도 되서 안쓰기로 한다.
		GetObjectsRequest getObjectsRequest = new GetObjectsRequest { Entity = _titlePlayerEntityKey };
		PlayFabDataAPI.GetObjects(getObjectsRequest, OnGetObjects, OnRecvPlayerDataFailure);
		++_requestCountForGetPlayerData;
#endif

		// 이것 저것 더 요청할 수 있다. 지금 필요한건 캐릭터마다의 엔티티다. 이게 있어야 캐릭터의 파워레벨을 받을 수 있다.
		for (int i = 0; i < loginResult.InfoResultPayload.CharacterList.Count; ++i)
		{
			string characterId = loginResult.InfoResultPayload.CharacterList[i].CharacterId;
			GetCharacterStatisticsRequest getCharacterStatisticsRequest = new GetCharacterStatisticsRequest { CharacterId = characterId };
			PlayFabClientAPI.GetCharacterStatistics(getCharacterStatisticsRequest, OnGetCharacterStatistics, OnRecvPlayerDataFailure);
			++_requestCountForGetPlayerData;
			GetObjectsRequest getCharacterEntityRequest = new GetObjectsRequest { Entity = new PlayFab.DataModels.EntityKey { Id = characterId, Type = "character" } };
			PlayFabDataAPI.GetObjects(getCharacterEntityRequest, OnGetObjectsCharacter, OnRecvPlayerDataFailure);
			++_requestCountForGetPlayerData;
		}

		// 서버의 utcTime도 받아두기로 한다. 그래서 클라가 가지고 있는 utcTime과 차이를 구해놓고 이후 클라의 utcNow를 얻을때 보정에 쓰도록 한다.
		_getServerUtcSendTime = Time.time;
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetServerUtc",
		}, OnGetServerUtc, OnRecvPlayerDataFailure);
		++_requestCountForGetPlayerData;
	}

#if USE_TITLE_PLAYER_ENTITY
	void OnGetObjects(GetObjectsResponse result)
	{
		_titlePlayerEntityObject = result;
		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}
#endif

	void OnGetCharacterStatistics(GetCharacterStatisticsResult result)
	{
		string characterId = "";
		GetCharacterStatisticsRequest getCharacterStatisticsRequest = result.Request as GetCharacterStatisticsRequest;
		if (getCharacterStatisticsRequest != null)
			characterId = getCharacterStatisticsRequest.CharacterId;

		if (string.IsNullOrEmpty(characterId) == false)
			_dicCharacterStatisticsResult.Add(characterId, result);

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	void OnGetObjectsCharacter(GetObjectsResponse result)
	{
		Dictionary<string, ObjectResult>.Enumerator e = result.Objects.GetEnumerator();
		ObjectResult objectResult = null;
		while (e.MoveNext())
		{
			// 분명 첫번째 EntityObjects에 Actor0201 처럼 캐릭 본인의 아이디로 된 key value가 들어있을거다. "Actor"스트링으로 검사해서 추출해낸다.
			if (e.Current.Key.Contains("Actor"))
			{
				objectResult = e.Current.Value;
				break;
			}
		}

		if (objectResult != null)
			_listCharacterEntityObject.Add(objectResult);

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	float _getServerUtcSendTime;
	TimeSpan _timeSpanForServerUtc;
	void OnGetServerUtc(ExecuteCloudScriptResult success)
	{
		string serverUtcTimeString = (string)success.FunctionResult;
		DateTime serverUtcTime = new DateTime();
		if (DateTime.TryParse(serverUtcTimeString, out serverUtcTime))
		{
			DateTime universalTime = serverUtcTime.ToUniversalTime();
			_timeSpanForServerUtc = universalTime - DateTime.UtcNow;
			// for latency
			_timeSpanForServerUtc += TimeSpan.FromSeconds((Time.time - _getServerUtcSendTime) * 0.5f);
		}

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	public DateTime GetServerUtcTime()
	{
		return DateTime.UtcNow + _timeSpanForServerUtc;
	}

	void OnRecvPlayerDataFailure(PlayFabError error)
	{
		// 정말 이상하게도 갑자기 PlayFab로그인은 되는데 그 뒤 모든 패킷을 보내봐도(클라이언트측 함수나 클라우드 스크립트 둘다) 503 에러를 뱉어냈다.
		// 이때는 PlayFab 서버가 동작 안하는 상태라 종료시간같은걸 정할수도 없으니 전용 스트링 하나 만들어서 보여주기로 한다.
		string stringId = "SystemUI_DisconnectServer";
		if (error.Error == PlayFabErrorCode.ServiceUnavailable)
			stringId = "SystemUI_Error503";

		// 로그인이 성공한 이상 실패할거 같진 않지만 그래도 혹시 모르니 해둔다.
		Debug.LogError(error.GenerateErrorReport());
		StartCoroutine(AuthManager.instance.RestartProcess(stringId));
	}

	void CheckCompleteRecvPlayerData()
	{
		if (_requestCountForGetPlayerData > 0)
			return;

		EndTimeRecord("PlayerData");

		// LoginResult도 받았고 추가로 요청했던 Entity Objects도 전부 받았다. 진짜 로드를 시작하자.
#if USE_TITLE_PLAYER_ENTITY
		PlayerDataEntity1 entity1Object = null;
		if (_titlePlayerEntityObject.Objects.ContainsKey("PlayerData"))
		{
			ObjectResult playerDataObjectResult = _titlePlayerEntityObject.Objects["PlayerData"];
			entity1Object = JsonUtility.FromJson<PlayerDataEntity1>(playerDataObjectResult.DataObject.ToString());
		}
#endif
		TimeSpaceData.instance.OnRecvEquipInventory(_loginResult.InfoResultPayload.UserInventory, _loginResult.InfoResultPayload.UserData);
		PlayerData.instance.OnRecvPlayerData(_loginResult.InfoResultPayload.PlayerStatistics, _loginResult.InfoResultPayload.UserData, _loginResult.InfoResultPayload.UserReadOnlyData, _loginResult.InfoResultPayload.CharacterList);
		PlayerData.instance.OnRecvCharacterList(_loginResult.InfoResultPayload.CharacterList, _dicCharacterStatisticsResult, _listCharacterEntityObject);

		_loginResult = null;
#if USE_TITLE_PLAYER_ENTITY
		_titlePlayerEntityObject = null;
#endif
		_dicCharacterStatisticsResult.Clear();
		_listCharacterEntityObject.Clear();
	}
	#endregion

	public void RequestSyncCharacterEntity()
	{
		ListUsersCharactersRequest request = new ListUsersCharactersRequest() { PlayFabId = playFabId };
		PlayFabClientAPI.GetAllUsersCharacters(request, (success) =>
		{
			PlayerData.instance.OnRecvSyncCharacterEntity(success.Characters);
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestNetwork(Action successCallback)
	{
		GetUserDataRequest request = new GetUserDataRequest() { Keys = new List<string> { "mainCharacterId" } };
		Action action = () =>
		{
			PlayFabClientAPI.GetUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	#region Energy
	public void RequestSyncEnergyRechargeTime()
	{
		PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest()
		{
		}, (success) =>
		{
			CurrencyData.instance.OnRecvCurrencyData(success.VirtualCurrency, success.VirtualCurrencyRechargeTimes);
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestRefillEnergy(int price, int refillAmount, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest()
		{
			ItemId = "RefillEnergy",
			Price = price,
			VirtualCurrency = CurrencyData.DiamondCode()
		}, (success) =>
		{
			WaitingNetworkCanvas.Show(false);

			// 게시판에 사람들이 적은대로 bundle 안에 있는건 날아오지 않는다. 그래서 success만 오면 알아서 리필한다.
			//for (int i = 0; i < success.Items.Count; ++i)
			//{	
			//}
			CurrencyData.instance.dia -= price;
			CurrencyData.instance.OnRecvRefillEnergy(refillAmount);

			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region InGame
	// 입장시마다 랜덤으로 된 숫자키를 하나 받는다. 정산시 보내서 서버랑 비교하기 위함이다.
	public ObscuredString _serverEnterKey;

	// 게이트 필라 쳐서 들어가는 패킷. 에너지를 소모하지 않는 튜토때도 패킷은 보낸다.
	// 클라우드 스크립트로 처리해서 정산을 할 기회를 1회 올린다.
	public void RequestEnterGame(bool retryByCrash, string entFlgByCrash, Action<bool> successCallback, Action failureCallback)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EnterGame",
			FunctionParameter = new { ByCrash = (retryByCrash ? 1 : 0), Flg = entFlgByCrash },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			_serverEnterKey = failure ? "" : resultString;
			if (successCallback != null) successCallback.Invoke(failure);
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestCancelGame()
	{
		if (PlayerData.instance.clientOnly)
			return;

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CancelGame",
			GeneratePlayStreamEvent = true,
		}, null, null);
	}

	public void RequestEndGame(bool clear, bool currentChaos, int playChapter, int stage, int addGold, int addSeal, List<ObscuredString> listDropItemId, Action<bool, string, string> successCallback)    // List<Item>
	{
		// 인게임 플레이 하고 정산할때 호출되는 함수인데
		// Statistics 갱신과 인벤획득처리 골드 갱신 등으로 나뉘어져있다.
		// 그런데 셋다 한번에 보냈다가 누군 처리되고 누군 처리 안되면 어떻게 구별할 것이며 어떻게 재전송 할 것인가.
		// 그렇다고 하나 보내고 성공시 하나 또 보내는 방식은 시간도 너무 오래 걸리고 그러다가 실패시 앞에 로직을 복구할거냐는 의문도 생긴다.
		//
		// 결국 RetrySendManager의 RequestAction이 하나의 액션만을 저장할 수 있다는 점에서
		// 여러 패킷을 동시에 전달해야하는 상황을 커버할 수 없게 된거다.
		//
		// 근데 다른건 복구 안해도 인게임 정산은 무조건 복구해야하는데..
		// 위에 SelectMainCharacter따위는 홈가도 그만이다. 어차피 다시 켜서 바꾸면 되니까.. 그런데 50층 깨고 정산타이밍에 네트워크 오류 생겨서 다 날아가면 정말 큰일이다.
		// 층만 반영되고 템이 저장안되도 큰일이다. 즉 정산땐 모든게 다 제대로 복구되야한다.
		//
		// 이러려면 RetrySendManager의 RequestAction이 동시에 여러개 호출되어도 알아서 각자 복구되는 기능이 필요하다.
		// 사실은 cloud script의 제한이 없었다면 정산함수 캐릭강화함수 장비강화함수 초월함수 다 따로 만들었을거 같다.
		// 그런데 지금 PlayFab 한계때문에 그럴수 없는 상황이라 나눠서 전송하는 식으로 해본다.
		/*
		StatisticUpdate highestPlayChapterRecord = new StatisticUpdate() { StatisticName = "highestPlayChapter", Value = highestPlayChapter };
		StatisticUpdate highestClearStageRecord = new StatisticUpdate() { StatisticName = "highestClearStage", Value = highestClearStage };
		UpdatePlayerStatisticsRequest request0 = new UpdatePlayerStatisticsRequest() { Statistics = new List<StatisticUpdate>() { highestPlayChapterRecord, highestClearStageRecord } };
		Action action0 = () =>
		{
			PlayFabClientAPI.UpdatePlayerStatistics(request0, (success) =>
			{
				RetrySendManager.instance.OnSuccessForList(0);
				PlayerData.instance.highestPlayChapter = highestPlayChapter;
				PlayerData.instance.highestClearStage = highestClearStage;
			}, (error) =>
			{
				RetrySendManager.instance.OnFailureForList(0);
			});
		};
		List<Action> listAction = new List<Action>();
		listAction.Add(action0);
		RetrySendManager.instance.RequestActionList(listAction, true);
		*/
		// 위에꺼로 하려고 했다가 입장 카운트를 확인하고 처리하는 식으로 바꿔야해서 클라우드 스크립트 쓰기로 한다.
		// 위의 형태는 나중에 언젠가 필요한 곳에 쓰자.

		// 다른 정보와 달리 아이템은 리스트를 구축해서 서버로 넘겨야한다. 공용 로직.
		string checkSum = "";
		List<TimeSpaceData.ItemGrantRequest> listItemGrantRequest = TimeSpaceData.instance.GenerateGrantRequestInfo(listDropItemId, ref checkSum);
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndGame",
			// playChapter와 currentChaos는 서버 검증용이다.
			FunctionParameter = new { Flg = (string)_serverEnterKey, Cl = (clear ? 1 : 0), Cha = currentChaos ? "1" : "0", Plch = playChapter, St = stage, Go = addGold, Se = addSeal, Lst = listItemGrantRequest, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
				jsonResult.TryGetValue("retErr", out object retErr);
				jsonResult.TryGetValue("adChrId", out object adChrId);
				jsonResult.TryGetValue("itmRet", out object itmRet);
				bool failure = ((retErr.ToString()) == "1");
				_serverEnterKey = "";
				if (!failure)
				{
					// 서버에러가 떠도 정산창은 띄되 WaitingNetworkCanvas은 닫지 않기로 한다.
					// 골드나 인장 같은건 얻되 다음 챕터로 넘어가지 못하게 하기 위함이다.
					RetrySendManager.instance.OnSuccess();
				}
				if (successCallback != null) successCallback.Invoke(clear, (string)adChrId, (string)itmRet);
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}
	#endregion


	#region Daily
	public void RequestOpenDailyBox(Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// DropProcess를 1회 굴리고나면 DropManager에 정보가 쌓여있다. 이걸 보내면 된다.
		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();
		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterLbpRequest> listLbpInfo = DropManager.instance.GetLimitBreakPointInfo();

		int ppCount = listPpInfo.Count;
		int originCount = listGrantInfo.Count + listLbpInfo.Count;
		if (ppCount > 5 || originCount > 1)
		{
			// 수량 에러
			CheatingListener.OnDetectCheatTable();
			return;
		}

		string checkSum = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonListPp = serializer.SerializeObject(listPpInfo);
		string jsonListGr = serializer.SerializeObject(listGrantInfo);
		string jsonListLbp = serializer.SerializeObject(listLbpInfo);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}_{3}_{4}", jsonListPp, jsonListGr, jsonListLbp, addGold, addDia));

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenDailyBox",
			FunctionParameter = new { Go = addGold, Di = addDia, LstPp = listPpInfo, LstGr = listGrantInfo, LstLbp = listLbpInfo, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold += addGold;
				CurrencyData.instance.dia += addDia;

				jsonResult.TryGetValue("date", out object date);
				jsonResult.TryGetValue("adChrIdPay", out object adChrIdPayload);

				++PlayerData.instance.originOpenCount;
				if ((listLbpInfo.Count + listGrantInfo.Count) == 0)
					++PlayerData.instance.notStreakCharCount;
				else
					PlayerData.instance.notStreakCharCount = 0;

				// 성공시에는 서버에서 방금 기록한 마지막 오픈 타임이 날아온다.
				PlayerData.instance.OnRecvDailyBoxInfo((string)date, true);

				// 뽑기쪽 처리와 동일한 함수들
				PlayerData.instance.OnRecvUpdateCharacterStatistics(listPpInfo, listLbpInfo);
				PlayerData.instance.OnRecvGrantCharacterList(adChrIdPayload);

				// 보통은 failure해도 successCallback 호출을 해줬는데 여기선 아예 뽑기 연출로 가지도 않도록 호출하지 않는다.
				if (successCallback != null) successCallback.Invoke(failure);

				// 클리어는 여기서 바로 하면 안된다.
				// 장비와 달리 캐릭터쪽 정보는 DropManager가 들고있는걸 사용해서 보여주기 때문에 결과창에 대한 처리가 끝나면 해야한다.
				//DropManager.instance.ClearLobbyDropInfo();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestRefreshDailyInfo(Action<bool> successCallback)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "RefreshDailyInfo",
			FunctionParameter = new { Test = 0, },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (successCallback != null) successCallback.Invoke(failure);
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Modify PlayerData
#if USE_TITLE_PLAYER_ENTITY
	// 이런식으로 헬퍼를 만들어도 되고 너무 많을거 같으면 안만들어도 된다.
	// 나중을 위해 기록으로 남겨두긴 한다.
	// 지금은 Player Entity쓰는 거보다 Player Data (Title) 쓰는게 더 좋아보여서 디파인으로 묶어서 비활성화 해둔다.
	public void RequestChangeMainCharacter(string mainCharacterId)
	{
		RequestModifyPlayerData(mainCharacterId, PlayerData.instance.selectedChapter, PlayerData.instance.chaosMode);
	}
	public void RequestChangeChapter(int chapter)
	{
		RequestModifyPlayerData(PlayerData.instance.mainCharacterId, chapter, PlayerData.instance.chaosMode);
	}
	public void RequestChangeChaos(bool chaos)
	{
		RequestModifyPlayerData(PlayerData.instance.mainCharacterId, PlayerData.instance.selectedChapter, chaos);
	}

	// 서버로 보내서 EntityObjects에 Json으로 저장되는거라 변수명을 압축할수록 유리하다.
	// 키 밸류로 할수도 있었는데 스트링 비교 매번 해야해서 차라리 클래스 형태로 가기로 한다.
	public class PlayerDataEntity1
	{
		public string mainCharId;
		public int selecChap;
		public int chao;
	}
	void RequestModifyPlayerData(string mainCharacterId, int selectChapter, bool chaos)
	{
		PlayerDataEntity1 entity1Object = new PlayerDataEntity1
		{
			mainCharId = mainCharacterId,
			selecChap = selectChapter,
			chao = chaos ? 1 : 0
		};
		string entity1Data = JsonUtility.ToJson(entity1Object);

		var dataList = new List<SetObject>()
		{
			new SetObject()
			{
				ObjectName = "PlayerData",
				DataObject = entity1Data
			},
			// A free-tier customer may store up to 3 objects on each entit
		};

		PlayFabDataAPI.SetObjects(new SetObjectsRequest()
		{
			Entity = _titlePlayerEntityKey,	// Saved from GetEntityToken, or a specified key created from a titlePlayerId, CharacterId, etc
			Objects = dataList,
		}, (setResult) => {
			Debug.Log(setResult.ProfileVersion);
		}, HandleCommonError);
	}
#else
	public void RequestSelectMainCharacter(string mainCharacterId, Action successCallback)
	{
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { "mainCharacterId", mainCharacterId } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.mainCharacterId = mainCharacterId;
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestChangeChapter(int chapter, Action successCallback, Action failureCallback = null)
	{
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { "selectedChapter", chapter.ToString() } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.selectedChapter = chapter;
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
				if (failureCallback != null) failureCallback.Invoke();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestPushServerEvent(string jsonServerEvent, Action successCallback = null, Action failureCallback = null)
	{
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { "even", jsonServerEvent } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
				if (failureCallback != null) failureCallback.Invoke();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}
#endif
	#endregion

	#region Chaos
	public void RequestSelectFullChaos(bool challenge, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectFullChaos",
			FunctionParameter = new { Chl = challenge ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				if (challenge)
					PlayerData.instance.chaosMode = false;
				PlayerData.instance.purifyCount = 0;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Modify CharacterData
	// 파워레벨쪽은 정석대로 서버에서 처리된다.
	public void RequestCharacterPowerLevelUp(CharacterData characterData, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PowerLevelUp",
			FunctionParameter = new { ChrId = characterData.entityKey.Id },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				characterData.OnPowerLevelUp();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCharacterLimitBreak(CharacterData characterData, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "LimitBreak",
			FunctionParameter = new { ChrId = characterData.entityKey.Id },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				characterData.OnLimitBreak();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	// 이것도 서버에 저장되는 Entity Object
	public class CharacterDataEntity1
	{
		// 아마도 잠재부터 이쪽에 들어갈거 같다.
		public int poten;
	}

	void RequestCharacterObjectData(CharacterData characterData, int potential, Action successCallback)
	{
		// 몰아서 저장하기 때문에 모든 정보를 다 적어야한다.
		CharacterDataEntity1 entity1Object = new CharacterDataEntity1
		{
			poten = potential
		};
		string entity1Data = JsonUtility.ToJson(entity1Object);

		var dataList = new List<SetObject>()
		{
			new SetObject()
			{
				ObjectName = characterData.actorId,
				DataObject = entity1Data
			},
			// A free-tier customer may store up to 3 objects on each entit
		};

		// 아마 근데 이렇게 오브젝트를 통째로 밀면 checkSum 처리할 곳도 없어지기 때문에 이렇게는 안하고 클라우드 스크립트 통해서 할거 같다.
		// 우선 클라이언트에서 보낼 수도 있다는 걸 보여주기 위한 샘플로만 놔둔다.
		PlayFabDataAPI.SetObjects(new SetObjectsRequest()
		{
			Entity = characterData.entityKey, // Saved from GetEntityToken, or a specified key created from a titlePlayerId, CharacterId, etc
			Objects = dataList,
		}, (setResult) => {
			Debug.Log(setResult.ProfileVersion);
			if (successCallback != null) successCallback.Invoke();
		}, HandleCommonError);
	}
	#endregion


	#region Equip
	public void RequestEquip(EquipData equipData, Action successCallback)
	{
		string equipSlotKey = string.Format("eqPo{0}", equipData.cachedEquipTableData.equipType);
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { equipSlotKey, equipData.uniqueId } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				TimeSpaceData.instance.OnEquip(equipData);
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestUnequip(EquipData equipData, Action successCallback)
	{
		string equipSlotKey = string.Format("eqPo{0}", equipData.cachedEquipTableData.equipType);
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { equipSlotKey, "" } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				TimeSpaceData.instance.OnUnequip(equipData);
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	Dictionary<string, string> _dicEquipListInfo = new Dictionary<string, string>();
	public void RequestEquipList(List<EquipData> listEquipData, Action successCallback)
	{
		_dicEquipListInfo.Clear();
		for (int i = 0; i < listEquipData.Count; ++i)
		{
			string equipSlotKey = string.Format("eqPo{0}", listEquipData[i].cachedEquipTableData.equipType);
			_dicEquipListInfo.Add(equipSlotKey, listEquipData[i].uniqueId);
		}
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = _dicEquipListInfo };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				for (int i = 0; i < listEquipData.Count; ++i)
					TimeSpaceData.instance.OnEquip(listEquipData[i]);
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestLockEquip(EquipData equipData, bool lockState, Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "LockEquip",
			FunctionParameter = new { EqpId = (string)equipData.uniqueId, Lck = lockState ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				equipData.SetLock(lockState);
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestEnhance(EquipData equipData, int targetEnhanceLevel, List<EquipData> listMaterialEquipData, int price, Action successCallback)
	{
		string checkSum = "";
		List<TimeSpaceData.RevokeInventoryItemRequest> listRevokeRequest = TimeSpaceData.instance.GenerateRevokeInfo(listMaterialEquipData, price, targetEnhanceLevel.ToString(), ref checkSum);

		// 선이펙트와 함께 처리하는 형태라서 WaitingNetworkCanvas를 내부 코루틴에서 관리한다.
		//WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Enhance",
			FunctionParameter = new { EqpId = (string)equipData.uniqueId, T = targetEnhanceLevel, Lst = listRevokeRequest, Pri = price, LstCs = checkSum, EqpPos = "" },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				TimeSpaceData.instance.OnRevokeInventory(listMaterialEquipData);
				if (equipData.enhanceLevel != targetEnhanceLevel)
					equipData.OnEnhance(targetEnhanceLevel);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestTransfer(EquipData equipData, int targetEnhanceLevel, EquipData materialEquipData, int price, bool needEquip, Action successCallback)
	{
		string equipSlotKey = "";
		if (needEquip) equipSlotKey = string.Format("eqPo{0}", materialEquipData.cachedEquipTableData.equipType);

		string checkSum = "";
		List<TimeSpaceData.RevokeInventoryItemRequest> listRevokeRequest = TimeSpaceData.instance.GenerateRevokeInfo(materialEquipData, price, targetEnhanceLevel.ToString(), ref checkSum);

		// 선이펙트와 함께 처리하는 형태라서 WaitingNetworkCanvas를 내부 코루틴에서 관리한다.
		//WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			// 함수 형태가 비슷해서 Enhance에다가 인자 추가해서 쓰기로 한다.
			FunctionName = "Enhance",
			FunctionParameter = new { EqpId = (string)equipData.uniqueId, T = targetEnhanceLevel, Lst = listRevokeRequest, Pri = price, LstCs = checkSum, EqpPos = equipSlotKey },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				TimeSpaceData.instance.OnRevokeInventory();
				if (equipData.enhanceLevel != targetEnhanceLevel)
					equipData.OnEnhance(targetEnhanceLevel);
				if (needEquip)
					TimeSpaceData.instance.OnEquip(equipData);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestAmplifyMain(EquipData equipData, string mainOptionString, List<EquipData> listMaterialEquipData, int price, Action successCallback)
	{
		string checkSum = "";
		List<TimeSpaceData.RevokeInventoryItemRequest> listRevokeRequest = TimeSpaceData.instance.GenerateRevokeInfo(listMaterialEquipData, price, mainOptionString, ref checkSum);

		// 선이펙트 없이 일반 패킷처럼 처리
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "AmplifyMain",
			FunctionParameter = new { EqpId = (string)equipData.uniqueId, Op = mainOptionString, Lst = listRevokeRequest, Pri = price, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				TimeSpaceData.instance.OnRevokeInventory(listMaterialEquipData);
				equipData.OnAmplifyMain(mainOptionString);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestAmplifyRandom(EquipData equipData, int randomIndex, string randomOptionString, List<EquipData> listMaterialEquipData, int price, Action successCallback)
	{
		string checkSum = "";
		List<TimeSpaceData.RevokeInventoryItemRequest> listRevokeRequest = TimeSpaceData.instance.GenerateRevokeInfo(listMaterialEquipData, price, randomOptionString, ref checkSum);

		// 선이펙트 없이 일반 패킷처럼 처리
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			// 옵션변경쪽은 선이펙트 없이 후이펙트로만 간다. 옵션 변경과 구조가 동일해서 같은 패킷을 사용하기로 한다.
			FunctionName = "Transmute",
			FunctionParameter = new { EqpId = (string)equipData.uniqueId, Pos = randomIndex.ToString(), Op = randomOptionString, Lst = listRevokeRequest, Pri = price, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				TimeSpaceData.instance.OnRevokeInventory(listMaterialEquipData);
				equipData.OnTransmute(randomIndex, randomOptionString, true);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestTransmute(EquipData equipData, int randomIndex, string randomOptionString, EquipData materialEquipData, int price, Action successCallback)
	{
		string checkSum = "";
		// TransmuteRemainCount 삭제하라고 알리는 Cnt 인자는 해킹방지를 적용해야하는 인자라 additonal에 포함시켜야한다.
		string additionalString = string.Format("{0}_{1}", randomOptionString, 1);
		List<TimeSpaceData.RevokeInventoryItemRequest> listRevokeRequest = TimeSpaceData.instance.GenerateRevokeInfo(materialEquipData, price, additionalString, ref checkSum);

		// 선이펙트 없이 일반 패킷처럼 처리
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Transmute",
			FunctionParameter = new { EqpId = (string)equipData.uniqueId, Pos = randomIndex.ToString(), Op = randomOptionString, Lst = listRevokeRequest, Pri = price, LstCs = checkSum, UsC = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				TimeSpaceData.instance.OnRevokeInventory();
				equipData.OnTransmute(randomIndex, randomOptionString, false);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Gacha
	// RequestOpenDailyBox 과 상당히 비슷하다.
	public void RequestCharacterBox(int price, Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);
		
		// DropProcess를 1회 굴리고나면 DropManager에 정보가 쌓여있다. 이걸 보내면 된다.
		List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterLbpRequest> listLbpInfo = DropManager.instance.GetLimitBreakPointInfo();

		int ppCount = listPpInfo.Count;
		int originCount = listGrantInfo.Count + listLbpInfo.Count;
		if (ppCount > 6 || originCount > 2)
		{
			// 수량 에러
			CheatingListener.OnDetectCheatTable();
			return;
		}

		// default 4
		int apiCallCount = 4;
		apiCallCount += listPpInfo.Count;
		apiCallCount += listGrantInfo.Count * 2;
		apiCallCount += listLbpInfo.Count;

		if (apiCallCount > 15)
		{
			// 15회 넘어가면 예외처리로 처리방식을 바꿔야한다.
			// 기획을 바꾸면서 15회 넘어갈일이 안생기게 되었다. 사실상 넘으면 뭔가 잘못된거다.
			CheatingListener.OnDetectCheatTable();
			return;
		}

		string checkSum = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonListPp = serializer.SerializeObject(listPpInfo);
		string jsonListGr = serializer.SerializeObject(listGrantInfo);
		string jsonListLbp = serializer.SerializeObject(listLbpInfo);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}", jsonListPp, jsonListGr, jsonListLbp));

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenCharBox",
			FunctionParameter = new { LstPp = listPpInfo, LstGr = listGrantInfo, LstLbp = listLbpInfo, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.dia -= price;
				jsonResult.TryGetValue("adChrIdPay", out object adChrIdPayload);

				++PlayerData.instance.characterBoxOpenCount;
				if ((listLbpInfo.Count + listGrantInfo.Count) == 0)
					PlayerData.instance.notStreakCharCount += 2;
				else
					PlayerData.instance.notStreakCharCount = 0;

				// update
				PlayerData.instance.OnRecvUpdateCharacterStatistics(listPpInfo, listLbpInfo);
				PlayerData.instance.OnRecvGrantCharacterList(adChrIdPayload);
				if (successCallback != null) successCallback.Invoke(failure);

				// 장비와 달리 캐릭터쪽 정보는 DropManager가 들고있는걸 사용해서 보여주기 때문에 결과창에 대한 처리가 끝나면 해야한다.
				//DropManager.instance.ClearLobbyDropInfo();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestEquipBox(List<ObscuredString> listDropItemId, int price, int equipBoxKeyCount, int legendEquipKeyCount, string jsonLevelPackageList, Action<bool, string> successCallback)
	{
		if (equipBoxKeyCount == 0 && legendEquipKeyCount == 0)
			WaitingNetworkCanvas.Show(true);

		string checkSum = "";
		List<TimeSpaceData.ItemGrantRequest> listItemGrantRequest = TimeSpaceData.instance.GenerateGrantRequestInfo(listDropItemId, ref checkSum);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = (legendEquipKeyCount > 0) ? "OpenLEquipBox" : "OpenEquipBox",
			FunctionParameter = new { Lst = listItemGrantRequest, LstCs = checkSum, LvPck = jsonLevelPackageList },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			jsonResult.TryGetValue("itmRet", out object itmRet);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				if (equipBoxKeyCount == 0 && legendEquipKeyCount == 0)
					WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia -= price;
				if (equipBoxKeyCount > 0) CurrencyData.instance.equipBoxKey -= equipBoxKeyCount;
				if (legendEquipKeyCount > 0) CurrencyData.instance.legendEquipKey -= legendEquipKeyCount;
				TimeSpaceData.instance.OnRecvItemGrantResult((string)itmRet, false);
				// 캐릭터와 달리 장비는 드랍프로세서에서 정보를 뽑아쓰는게 아니라서 미리 클리어해도 상관없다.
				DropManager.instance.ClearLobbyDropInfo();
				// 패킷 실패시엔 뽑기 프로세스를 타지 않도록 여기에서 호출한다.
				if (successCallback != null) successCallback.Invoke(failure, (string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Shop
	public void RequestBuyGoldBox(string serverItemId, int price, int buyingGold, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest()
		{
			ItemId = serverItemId,
			Price = price,
			VirtualCurrency = CurrencyData.DiamondCode()
		}, (success) =>
		{
			WaitingNetworkCanvas.Show(false);

			// bundle 안에 있는건 날아오지 않는다. 그래서 success만 오면 알아서 올려줘야한다.
			CurrencyData.instance.dia -= price;
			CurrencyData.instance.gold += buyingGold;

			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestValidateLevelPackage(string serverItemId, ShopLevelPackageTableData shopLevelPackageTableData, Action successCallback)
	//public void RequestValidateLevelPackage(string receiptJson, int price, int buyingGold, Action successCallback)
	{
		//PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
		//{
		//	ReceiptJson = receiptJson,
		//}
		PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest()
		{
			ItemId = serverItemId,
			Price = 1,
			VirtualCurrency = CurrencyData.GoldCode()
		}, (success) =>
		{
			// bundle 안에 있는건 날아오지 않는다. 그래서 success만 오면 알아서 올려줘야한다.
			// 우선 골드1부터 차감해서 동기부터 맞춘다.(임시가격)
			CurrencyData.instance.gold -= 1;

			CurrencyData.instance.dia += shopLevelPackageTableData.buyingGems;
			CurrencyData.instance.gold += shopLevelPackageTableData.buyingGold;
			CurrencyData.instance.equipBoxKey += shopLevelPackageTableData.buyingEquipKey;
			CurrencyData.instance.legendEquipKey += shopLevelPackageTableData.buyingLegendEquipKey;

			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestUpdateLevelPackageList(string jsonLevelPackageList, Action successCallback)
	{
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { "lvPckLst", jsonLevelPackageList } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}
	#endregion






	#region Sample
	// Sample 1. 콜백도 없고 재전송도 없을땐 이렇게 간단하게 처리
	public void RequestPlayerProfile()
	{
		GetPlayerProfileRequest request = new GetPlayerProfileRequest() { PlayFabId = playFabId };
		PlayFabClientAPI.GetPlayerProfile(request, OnGetPlayerProfileSuccess, OnGetPlayerProfileFailure);
	}

	void OnGetPlayerProfileSuccess(GetPlayerProfileResult result)
	{
	}

	void OnGetPlayerProfileFailure(PlayFabError error)
	{
	}

	// Sample 2. UI에서는 callback 필요할테니 이런식으로 처리한다.
	// 게다가 메인 캐릭터 설정은 재화를 소모하는 요청이 아니기 때문에 Retry도 적용할 수 있다.
	public void RequestChangeMainCharacter(string mainCharacterId, Action successCallback, Action failureCallback = null)
	{
		// 직접 Send하는 대신 RetrySendManager에게 맡긴다.
		GetPlayerProfileRequest request = new GetPlayerProfileRequest() { PlayFabId = playFabId };
		Action action = () =>
		{
			PlayFabClientAPI.GetPlayerProfile(request, OnChangeMainCharacterSuccess, OnChangeMainCharacterFailure);
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	void OnChangeMainCharacterSuccess(GetPlayerProfileResult result)
	{
		RetrySendManager.instance.OnSuccess();

		// 나머지 처리
		//
	}

	void OnChangeMainCharacterFailure(PlayFabError error)
	{
		// 이때만 재전송 할건가? 고민했었는데
		//error.Error = PlayFabErrorCode.ServiceUnavailable;
		//error.HttpCode = 400;
		// 어차피 Retry를 해도 되는 패킷이라고 한 이상 꼭 제한을 걸필요는 없을거 같았다. 우선은 어떤 실패를 해도 재시도 하는거로 처리
		RetrySendManager.instance.OnFailure();
	}



	// Sample 3. PlayFab에서 제공하는 함수의 리턴값이 필요한 경우에는 Sample 2.와는 조금 다르게 결과값을 넘겨줘야한다.
	// 아무래도 이게 제일 비중이 많을거 같은데
	// 어차피 이렇게 짤거라면 UI쪽에서 직접 PlayFab함수를 호출해서 처리하는게 더 깔끔한거 아닌가.
	// Retry도 필요없을테고.. 이건 UI처리하는 부분 생길때 다시 고민해보자.
	public void RequestNeedReturn(string mainCharacterId, Action<GetPlayerProfileResult> successCallback)
	{

	}
	#endregion
}