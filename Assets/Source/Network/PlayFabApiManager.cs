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

	void Update()
	{
		UpdateCliSusQueue();
		UpdateServerUtc();
	}

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
			case PlayFabErrorCode.NotAuthenticated:
				OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_LoseSession"), () =>
				{
					// NotAuthenticated 뜨면 다시 로그인해야만 한다.
					PlayerData.instance.ResetData();
					SceneManager.LoadScene(0);
				});
				return;
		}

		if (error.Error == PlayFabErrorCode.ServiceUnavailable || error.Error == PlayFabErrorCode.DownstreamServiceUnavailable || error.Error == PlayFabErrorCode.APIRequestLimitExceeded || error.HttpCode == 400)
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_DisconnectServer"), () =>
			{
				// 모든 정보를 다시 받아야하기 때문에 로그인부터 하는게 맞다.
				PlayerData.instance.ResetData();
				SceneManager.LoadScene(0);
			}, 100);
		}
	}

	// PlayFabError가 아닌 상황에서도 튕겨내기 위한 함수
	public void HandleCommonError()
	{
		WaitingNetworkCanvas.Show(false);

		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_DisconnectServer"), () =>
		{
			PlayerData.instance.ResetData();
			SceneManager.LoadScene(0);
		}, 100);
	}

	bool enableCliSusQueue { get; set; }
	int lastSendFrameCount { get; set; }
	public void RequestIncCliSus(eClientSuspectCode clientSuspectCode, bool sendBattleInfo = false, int param2 = 0)
	{
		int param1 = 0;
		if (sendBattleInfo)
		{
			int powerLevel = 1;
			CharacterData characterData = PlayerData.instance.GetCharacterData(BattleInstanceManager.instance.playerActor.actorId);
			if (characterData != null) powerLevel = characterData.powerLevel;
			int selected = StageManager.instance.playChapter;
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
				selected = BattleManager.instance.GetSelectedNodeWarTableData().level;
			param1 = selected * 100 + powerLevel;
		}

		if (enableCliSusQueue && !sendBattleInfo)
		{
			// 한 프레임에 수십개의 IncCliSus 패킷을 보내니 일부 실패하고 값 덮어쓰고 난리다.
			// 그래서 로그인처럼 같은 프레임에 다수의 패킷을 보내야하는 상황이라면 같은 프레임에 체크되는지 검사해서 큐에 넣고 천천히 보내기로 한다.
			if (lastSendFrameCount == Time.frameCount)
			{
				if (_queueCliSusInfo == null)
					_queueCliSusInfo = new Queue<sCliSusInfo>();
				sCliSusInfo info = new sCliSusInfo();
				info.code = (int)clientSuspectCode;
				info.param1 = param1;
				info.param2 = param2;
				_queueCliSusInfo.Enqueue(info);
				return;
			}
		}

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "IncCliSus",
			FunctionParameter = new { Er = (int)clientSuspectCode, Pa1 = param1, Pa2 = param2 },
			GeneratePlayStreamEvent = true
		}, null, (errorCallback) =>
		{
			HandleCliSusError(errorCallback, clientSuspectCode);
		});

		if (enableCliSusQueue && !sendBattleInfo)
			lastSendFrameCount = Time.frameCount;
	}

	void HandleCliSusError(PlayFabError errorCallback, eClientSuspectCode clientSuspectCode)
	{
		switch (clientSuspectCode)
		{
			case eClientSuspectCode.OneShotKillBoss:
				HandleCommonError(errorCallback);
				break;
		}
	}

	struct sCliSusInfo
	{
		public int code;
		public int param1;
		public int param2;
	}
	Queue<sCliSusInfo> _queueCliSusInfo;
	const float SendCliSusQueueDelay = 0.333f;
	float _cliSusSendRemainTime;
	void UpdateCliSusQueue()
	{
		if (_queueCliSusInfo == null)
			return;
		if (_queueCliSusInfo.Count == 0)
			return;

		_cliSusSendRemainTime -= Time.deltaTime;
		if (_cliSusSendRemainTime < 0.0f)
		{
			sCliSusInfo info = _queueCliSusInfo.Dequeue();

			PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
			{
				FunctionName = "IncCliSus",
				FunctionParameter = new { Er = info.code, Pa1 = info.param1, Pa2 = info.param2 },
				GeneratePlayStreamEvent = true
			}, null, (errorCallback) =>
			{
				HandleCliSusError(errorCallback, (eClientSuspectCode)info.code);
			});

			_cliSusSendRemainTime += SendCliSusQueueDelay;
		}
	}

	void ClearCliSusQueue()
	{
		if (_queueCliSusInfo == null)
			return;
		_queueCliSusInfo.Clear();
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
		_loginResult = loginResult;

#if USE_TITLE_PLAYER_ENTITY
		_titlePlayerEntityKey = new PlayFab.DataModels.EntityKey { Id = loginResult.EntityToken.Entity.Id, Type = loginResult.EntityToken.Entity.Type };
#endif

		// 로그인 처리를 진행하기 전에 서버상태라던가 버전정보를 확인한다.
		if (CheckServerMaintenance(loginResult.InfoResultPayload.TitleData))
			return;

		bool needCheckResourceVersion = false;
		if (CheckVersion(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.PlayerStatistics, out needCheckResourceVersion) == false)
			return;

		// 리소스 체크를 해야하는 상황에서만 번들 체크를 한다.
		if (needCheckResourceVersion)
		{
			DownloadManager.instance.CheckDownloadProcess();
		}
		else
			OnLogin();
	}

	public void OnLogin()
	{
		LoginResult loginResult = _loginResult;

		ApplyGlobalTable(loginResult.InfoResultPayload.TitleData);
		AuthManager.instance.OnRecvAccountInfo(loginResult.InfoResultPayload.AccountInfo);
		CurrencyData.instance.OnRecvCurrencyData(loginResult.InfoResultPayload.UserVirtualCurrency, loginResult.InfoResultPayload.UserVirtualCurrencyRechargeTimes);
		DailyShopData.instance.OnRecvShopData(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData);
		MailData.instance.OnRecvMailData(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.NewlyCreated);
		SupportData.instance.OnRecvSupportData(loginResult.InfoResultPayload.UserReadOnlyData);

		if (loginResult.NewlyCreated)
		{
			// 이때도 서버 utcTime을 받아와야하긴 하는데 서버응답 기다리는거 없이 백그라운드에서 진행하기로 한다.
			_waitOnlyServerUtc = true;
			_getServerUtcSendTime = Time.time;
			PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetServerUtc",
			}, OnGetServerUtc, OnRecvPlayerDataFailure);

			// 처음 만든 계정이면 어차피 읽어올게 없다.
			// 오히려 서버 rules에 넣어둔 OnCreatedPlayer cloud script가 돌고있을텐데
			// 이게 비동기라서 로그인과 동시에 날아온 인벤 리스트에는 들어있지 않게 된다.
			// 그래서 직접 캐릭터를 인벤토리에 넣어주고 넘어가면 된다.
			PlayerData.instance.OnNewlyCreatedPlayer();

			// 더이상 쓰이는 곳 없다.
			_loginResult = null;
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
			GeneratePlayStreamEvent = true,
		}, OnGetServerUtc, OnRecvPlayerDataFailure);
		++_requestCountForGetPlayerData;
	}

	void ApplyGlobalTable(Dictionary<string, string> titleData)
	{
		// 이미 이 시점에서 TableDataManager의 내용물은 어드레서블로 받은 데이터를 로드한 상태일거다.
		// 그러니 로그인 즉시 덮어씌우면 서버값을 사용하게 될거다.
		if (titleData.ContainsKey("int"))
		{
			string tableDataString = titleData["int"];
			if (string.IsNullOrEmpty(tableDataString) == false)
			{
				var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
				Dictionary<string, int> globalConstantIntServerTable = serializer.DeserializeObject<Dictionary<string, int>>(tableDataString);
				Dictionary<string, int>.Enumerator e = globalConstantIntServerTable.GetEnumerator();
				GlobalConstantIntTable table = TableDataManager.instance.globalConstantIntTable;
				while (e.MoveNext())
				{
					string key = e.Current.Key;
					for (int i = 0; i < table.dataArray.Length; ++i)
					{
						if (table.dataArray[i].id == key)
						{
							table.dataArray[i].value = e.Current.Value;
							break;
						}
					}
				}
			}
		}
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

	bool _waitOnlyServerUtc = false;
	float _getServerUtcSendTime;
	TimeSpan _timeSpanForServerUtc;
	void OnGetServerUtc(ExecuteCloudScriptResult success)
	{
		PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
		jsonResult.TryGetValue("date", out object date);
		jsonResult.TryGetValue("ms", out object ms);

		DateTime serverUtcTime = new DateTime();
		if (DateTime.TryParse((string)date, out serverUtcTime))
		{
			double millisecond = 0.0;
			double.TryParse(ms.ToString(), out millisecond);
			serverUtcTime = serverUtcTime.AddMilliseconds(millisecond);

			DateTime universalTime = serverUtcTime.ToUniversalTime();
			// 클라 시간을 변경했으면 DateTime.UtcNow도 달라지기 때문에 그냥 믿으면 안된다. 서버 타임이랑 비교해서 차이값을 기록해둔다.
			// DateTime.UtcNow에다가 offset을 더해서 예측하는 방식이므로 universalTime에서 DateTime.UtcNow를 빼서 기록해둔다.
			// 정확하게는 클라가 고친 시간 오프셋값에다가 서버에서 클라까지 오는 패킷 딜레이까지 포함된 시간이다.
			_timeSpanForServerUtc = DateTime.UtcNow - universalTime;
			_serverUtcRefreshTime = GetServerUtcTime() + TimeSpan.FromMinutes(ServerRefreshFastDelay);

			// for latency
			// 원래는 latency 보정용으로 하려고 했는데, 패킷의 가는 시간이 길고 오는 시간이 짧아지면
			// 클라가 생각하는 서버가 실제 서버타임보다 빨라질 수 있다.
			// 이 경우 요청하지 말아야하는데 요청하는 경우가 생기므로 sus를 믿을 수 없게 된다. 그러니 아예 보정처리는 하지 않기로 한다.
			//_timeSpanForServerUtc += TimeSpan.FromSeconds((Time.time - _getServerUtcSendTime) * 0.5f);
		}
		if (_waitOnlyServerUtc)
		{
			_waitOnlyServerUtc = false;
			return;
		}

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	public DateTime GetServerUtcTime()
	{
		return DateTime.UtcNow - _timeSpanForServerUtc;
	}

	#region Refresh ServerUtc
	// 위에서 이어지는 내용이긴 한데
	// _timeSpanForServerUtc 값이 실상은 서버에서 클라까지 오는 패킷 딜레이를 포함하다보니 로그인때 하필 이 오차가 크게 저장될 경우
	// 이후 GetServerUtcTime() 값을 비교해서 서버에 요청할때 시간이 틀어질 수 있다는걸 의미했다.
	// 그래서 이 오차를 최대한 줄이기 위해 중간중간 패킷을 다시 보내서 서버와의 오차가 가장 적어지도록 갱신하기로 한다.
	// 초반 10회는 2분 간격으로 보내고 그 이후부터는 5분 간격으로 보낸다.
	// 이건 계정 전환을 해도 리셋할 필요가 없으니 그냥 앱이 가동되고 나서부터 제일 오차가 적은 값을 사용하면 된다.
	DateTime _serverUtcRefreshTime;
	const int ServerRefreshFastDelay = 1;
	const int ServerRefreshDelay = 5;
	int _serverUtcRefreshFastRemainCount = 10;
	void UpdateServerUtc()
	{
		if (PlayerData.instance.loginned == false)
			return;

		if (DateTime.Compare(GetServerUtcTime(), _serverUtcRefreshTime) < 0)
			return;

		int minutes = ServerRefreshDelay;
		if (_serverUtcRefreshFastRemainCount > 0)
		{
			minutes = ServerRefreshFastDelay;
			--_serverUtcRefreshFastRemainCount;
		}
		_serverUtcRefreshTime = GetServerUtcTime() + TimeSpan.FromMinutes(minutes);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetServerUtc",
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("date", out object date);
			jsonResult.TryGetValue("ms", out object ms);

			DateTime serverUtcTime = new DateTime();
			if (DateTime.TryParse((string)date, out serverUtcTime))
			{
				double millisecond = 0.0;
				double.TryParse(ms.ToString(), out millisecond);
				serverUtcTime = serverUtcTime.AddMilliseconds(millisecond);

				DateTime universalTime = serverUtcTime.ToUniversalTime();

				// 위의 파싱은 로그인때 했던거와 같지만 갱신할때는 이전에 저장된거와 비교해서 패킷 딜레이가 더 짧아질때만 적용하면 된다.
				// 패킷 딜레이가 짧아질수록 TimeSpan값이 커지기 때문에 아래와 같이 클때 덮으면 된다.
				// 클라가 타임을 변조해서 느리게 하든 빠르게 하든 동일하다.
				TimeSpan timeSpanForServerUtc = DateTime.UtcNow - universalTime;
				if (timeSpanForServerUtc < _timeSpanForServerUtc)
				{
					_timeSpanForServerUtc = timeSpanForServerUtc;
					Debug.LogFormat("ServerUtc TimeSpan : {0}", _timeSpanForServerUtc.TotalMilliseconds);
				}
			}
		}, (error) =>
		{
			// 주기적으로 보내는거라 에러 핸들링 하면 안된다.
			//HandleCommonError(error);
		});
	}
	#endregion

	bool CheckServerMaintenance(Dictionary<string, string> titleData)
	{
		if (titleData.ContainsKey("down") && string.IsNullOrEmpty(titleData["down"]) == false)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			Dictionary<string, string> dicInfo = serializer.DeserializeObject<Dictionary<string, string>>(titleData["down"]);
			if (dicInfo.ContainsKey("0") && dicInfo["0"] == "1" && dicInfo.Count >= 3)
			{
				DateTime startTime = new DateTime();
				DateTime endTime = new DateTime();
				if (DateTime.TryParse(dicInfo["1"], out startTime) && DateTime.TryParse(dicInfo["2"], out endTime))
				{
					DateTime localStartTime = startTime.ToLocalTime();
					DateTime localEndTime = endTime.ToLocalTime();
					string startArgment = string.Format("{0:00}:{1:00}", localStartTime.Hour, localStartTime.Minute);
					string endArgment = string.Format("{0:00}:{1:00}", localEndTime.Hour, localEndTime.Minute);
					StartCoroutine(AuthManager.instance.RestartProcess(null, "SystemUI_ServerDown", startArgment, endArgment));
					return true;
				}
			}
		}
		return false;
	}

	bool CheckVersion(Dictionary<string, string> titleData, List<StatisticValue> playerStatistics, out bool needCheckResourceVersion)
	{
		needCheckResourceVersion = false;

		// 이 시점에서는 아직 PlayerData를 구축하기 전이니 이렇게 직접 체크한다.
		// highestPlayChapter로 체크해야 기기를 바꾸든 앱을 재설치 하든 데이터를 삭제하든 모든 상황에 대응할 수 있다.
		// 현재 계정 상태에 따라 다운로드 진행을 결정하는 것.
		int highestPlayChapter = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "highestPlayChapter")
			{
				highestPlayChapter = playerStatistics[i].Value;
				break;
			}
		}

		// 튜토리얼을 마치지 않았다면 앱 업뎃이든 번들패치든 할 필요 없다. 바로 리턴.
		if (highestPlayChapter == 0)
			return true;

		// 빌드번호를 서버에 적혀있는 빌드번호와 비교해야한다.
		BuildVersionInfo versionInfo = null;
#if UNITY_ANDROID
		versionInfo = Resources.Load<BuildVersionInfo>("Build/BuildVersionInfo_Android");
#elif UNITY_IOS
		versionInfo = Resources.Load<BuildVersionInfo>("Build/BuildVersionInfo_iOS");
#endif
		Debug.LogFormat("Build Version _.{0}.{1}", versionInfo.updateVersion, versionInfo.addressableVersion);
		if (titleData.ContainsKey(versionInfo.serverKeyName) && string.IsNullOrEmpty(titleData[versionInfo.serverKeyName]) == false)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			int serverVersion = 0;
			int.TryParse(titleData[versionInfo.serverKeyName], out serverVersion);
			if (versionInfo.updateVersion < serverVersion)
			{
				// 업데이트가 있음을 알려야한다.
				StartCoroutine(AuthManager.instance.RestartProcess(() =>
				{
#if UNITY_EDITOR
					// 에디터에서는 마켓창 열필요 없으니 종료한다.
					UnityEditor.EditorApplication.isPlaying = false;
#endif
					if (Application.platform == RuntimePlatform.IPhonePlayer)
					{
						Application.OpenURL(titleData["iosUrl"]);
					}
					else if (Application.platform == RuntimePlatform.Android)
					{
						Application.OpenURL("market://details?id=" + Application.identifier);
					}
				}, "SystemUI_NeedUpdate"));
				return false;
			}
		}

		// 빌드 업데이트 확인이 끝나면 리소스 체크를 해야하는데,
		// 리소스 버전은 빌드번호와 달리 직접 서버에 적어두고 비교하는 형태가 아니다.
		// 어드레서블을 사용하기 때문에 GetDownloadSizeAsync 호출해서 있으면 패치할게 있는거고 0이면 패치할게 없는거다.
		// 당연히 Async구조이기 때문에 코루틴으로 바꿔서 대기해야한다.
		//int resourceNumber = 0;
		//int.TryParse(split[2], out resourceNumber);
		needCheckResourceVersion = true;

		return true;
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
		StartCoroutine(AuthManager.instance.RestartProcess(null, stringId));
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

		// 혹시 다 못보냈더라도 어쩔 수 없다. 이전에 로그인 했던 계정의 정보를 보낼순 없다.
		ClearCliSusQueue();
		enableCliSusQueue = true;
		PlayerData.instance.OnRecvPlayerStatistics(_loginResult.InfoResultPayload.PlayerStatistics);
		TimeSpaceData.instance.OnRecvEquipInventory(_loginResult.InfoResultPayload.UserInventory, _loginResult.InfoResultPayload.UserData);
		PlayerData.instance.OnRecvPlayerData(_loginResult.InfoResultPayload.UserData, _loginResult.InfoResultPayload.UserReadOnlyData, _loginResult.InfoResultPayload.CharacterList);
		PlayerData.instance.OnRecvCharacterList(_loginResult.InfoResultPayload.CharacterList, _dicCharacterStatisticsResult, _listCharacterEntityObject);
		enableCliSusQueue = false;

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

	public void RequestNetworkOnce(Action successCallback, Action failureCallback, bool showWaitingNetworkCanvas)
	{
		if (showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(true);

		GetUserDataRequest request = new GetUserDataRequest() { Keys = new List<string> { "mainCharacterId" } };
		PlayFabClientAPI.GetUserData(request, (success) =>
		{
			if (showWaitingNetworkCanvas)
				WaitingNetworkCanvas.Show(false);

			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			if (showWaitingNetworkCanvas)
				WaitingNetworkCanvas.Show(false);

			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
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
			// 갑자기 전투중에 튕기니 이상하다. 이거는 에러처리 하지 않기로 한다.
			//HandleCommonError(error);
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
	ObscuredString _serverEnterKey;

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
			if (string.IsNullOrEmpty(_serverEnterKey) == false)
				ClientSaveData.instance.OnEnterGame(retryByCrash, _serverEnterKey);
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

		ClientSaveData.instance.OnEndGame();
	}

	public void RequestCancelChallenge(Action successCallback)
	{
		if (PlayerData.instance.clientOnly)
		{
			if (PlayerData.instance.currentChallengeMode && ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chaos))
			{
				PlayerData.instance.chaosMode = true;
				PlayerData.instance.purifyCount = 0;
			}
			if (successCallback != null) successCallback.Invoke();
			return;
		}

		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "CancelChallenge",
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				if (PlayerData.instance.currentChallengeMode && ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chaos))
				{
					PlayerData.instance.chaosMode = true;
					PlayerData.instance.purifyCount = 0;
				}
				// 도전모드 취소할땐 서버 처리가 다 끝나야지만 재진입 저장 데이터를 초기화 시켜야한다. 이래야 네트워크 상황 안좋아서 재시도할때도 제대로 처리할 수 있게 된다.
				ClientSaveData.instance.OnEndGame();
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true, true);
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
				ClientSaveData.instance.OnEndGame();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true, true);
	}
	#endregion

	#region NodeWar
	// NodeWar역시 입장시마다 랜덤으로 된 숫자키를 하나 받는다.
	ObscuredString _serverEnterKeyForNodeWar;
	public void RequestEnterNodeWar(Action<bool> successCallback, Action failureCallback)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EnterNodeWar",
			FunctionParameter = new { Enter = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			_serverEnterKeyForNodeWar = failure ? "" : resultString;
			if (successCallback != null) successCallback.Invoke(failure);
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestCancelNodeWar()
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CancelNodeWar",
			GeneratePlayStreamEvent = true,
		}, null, null);
	}

	public void RequestEndNodeWar(bool clear, int playLevel, List<ObscuredString> listDropItemId, Action<bool, string> successCallback)
	{
		string checkSum = "";
		List<TimeSpaceData.ItemGrantRequest> listItemGrantRequest = TimeSpaceData.instance.GenerateGrantRequestInfo(listDropItemId, ref checkSum);
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndNodeWar",
			FunctionParameter = new { Flg = (string)_serverEnterKeyForNodeWar, Cl = (clear ? 1 : 0), PlLv = playLevel, Lst = listItemGrantRequest, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
				jsonResult.TryGetValue("retErr", out object retErr);
				jsonResult.TryGetValue("itmRet", out object itmRet);
				bool failure = ((retErr.ToString()) == "1");
				_serverEnterKeyForNodeWar = "";
				if (!failure)
				{
					RetrySendManager.instance.OnSuccess();

					// 성공시에만 date파싱을 한다.
					jsonResult.TryGetValue("date", out object date);
					PlayerData.instance.OnRecvNodeWarInfo((string)date);
				}
				if (successCallback != null) successCallback.Invoke(clear, (string)itmRet);
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true, true);
	}

	public void RequestDownNodeWarLevel(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "DownNodeWar",
			FunctionParameter = new { Down = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestPurchaseNodeWarBoost(int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "BuyNodeBoost",
			FunctionParameter = new { Buy = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.dia -= price;
				PlayerData.instance.nodeWarBoostRemainCount += BattleInstanceManager.instance.GetCachedGlobalConstantInt("RefillBoostCount");
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestRegisterNodeWarBonusPowerSource(int powerSource)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SetNodeWarBonus",
			FunctionParameter = new { Inf = powerSource },
			GeneratePlayStreamEvent = true
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (failure)
				HandleCommonError();
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestOpenAgainNodeWar(int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenAgainNodeWar",
			FunctionParameter = new { Ent = 1 },
			GeneratePlayStreamEvent = true
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.dia -= price;

				// 특이한점은 nodeWarCleared를 강제로 안깬거처럼 되돌려놔야 한다는거다.
				// 이렇게해서 오늘 한번 더 클리어할 기회를 준다. 서버에서는 클리어 날짜를 하루전으로 변경하는 방식으로 같은 일을 수행한다. 그래서 재접해도 적용되게 처리한다.
				PlayerData.instance.nodeWarCleared = false;

				// 성공시에만 date파싱을 한다.
				jsonResult.TryGetValue("date", out object date);
				PlayerData.instance.OnRecvNodeWarOpenAgainInfo((string)date);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
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
		int addBalancePp = DropManager.instance.GetLobbyBalancePpAmount();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterTrpRequest> listTrpInfo = DropManager.instance.GetTranscendPointInfo();

		int ppCount = listPpInfo.Count;
		int originCount = listGrantInfo.Count + listTrpInfo.Count;
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
		string jsonListTrp = serializer.SerializeObject(listTrpInfo);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}_{3}_{4}_{5}", jsonListPp, jsonListGr, jsonListTrp, addGold, addDia, addBalancePp));

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenDailyBox",
			FunctionParameter = new { Go = addGold, Di = addDia, LstPp = listPpInfo, Bpp = addBalancePp, LstGr = listGrantInfo, LstTrp = listTrpInfo, LstCs = checkSum },
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
				if ((listTrpInfo.Count + listGrantInfo.Count) == 0)
					++PlayerData.instance.notStreakCharCount;
				else
					PlayerData.instance.notStreakCharCount = 0;

				// 성공시에는 서버에서 방금 기록한 마지막 오픈 타임이 날아온다.
				PlayerData.instance.OnRecvDailyBoxInfo((string)date, true);

				// 뽑기쪽 처리와 동일한 함수들
				PlayerData.instance.OnRecvUpdateCharacterStatistics(listPpInfo, listTrpInfo, addBalancePp);
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

	#region Experience
	public void RequestExperience(string actorId, bool onlyRecordPlay, bool addDia, bool showWaitingNetworkCanvas, Action successCallback)
	{
		if (showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Experience",
			FunctionParameter = new { Id = actorId, Ply = onlyRecordPlay ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				if (showWaitingNetworkCanvas)
					WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.dia += addDia ? 1 : 0;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Chaos
	public void RequestSelectFullChaos(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectFullChaos",
			FunctionParameter = new { Chl = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				PlayerData.instance.chaosMode = false;
				PlayerData.instance.purifyCount = 0;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSelectFullChaosRevert(Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// RequestSelectFullChaos함수는 도전모드일때 쓰는 함수고 RequestSelectFullChaosRevert 함수는 환원시에 사용하는 함수다.
		// 도전모드 전환과 달리 드랍 아이디 굴려서 드랍템 및 골드 보상 정보까지 보내야해서 별도의 패킷으로 처리하기로 한다.
		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();
		List<ObscuredString> listDropItemId = DropManager.instance.GetLobbyDropItemInfo();

		string checkSum = "";
		List<TimeSpaceData.ItemGrantRequest> listItemGrantRequest = TimeSpaceData.instance.GenerateGrantRequestInfo(listDropItemId, ref checkSum);

		// 지금까지 장비 체크섬은 장비만 따로 했었다. 여기서는 드랍 골드 다이아도 보내야하므로 체크섬을 나눠서 처리하기로 한다.
		string checkSum2 = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		checkSum2 = CheckSum(string.Format("{0}_{1}_{2}", addGold, addDia, "ibqpxu"));
		
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectFullChaosRevert",
			FunctionParameter = new { Go = addGold, Di = addDia, CrcyCs = checkSum2, Lst = listItemGrantRequest, LstCs = checkSum },
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
				PlayerData.instance.purifyCount = 0;
				jsonResult.TryGetValue("itmRet", out object itmRet);
				if ((string)itmRet != "")
					TimeSpaceData.instance.OnRecvItemGrantResult((string)itmRet, false);
				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestPurifyChaos(int priceGold, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PurifyChaos",
			FunctionParameter = new { Puri = 0 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				PlayerData.instance.chaosMode = false;
				CurrencyData.instance.gold -= priceGold;
				PlayerData.instance.purifyCount = 0;
				jsonResult.TryGetValue("retFre", out object freeApplied);
				if ((freeApplied.ToString()) == "1")
				{
					jsonResult.TryGetValue("date", out object date);
					PlayerData.instance.OnRecvFreePurifyInfo((string)date);
				}
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
				CurrencyData.instance.dia -= price;
				characterData.OnLimitBreak();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCharacterTranscend(CharacterData characterData, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Transcend",
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
				characterData.OnTranscend();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestApplyCharacterStats(CharacterData characterData, int strAddPoint, int dexAddPoint, int intAddPoint, int vitAddPoint, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ApplyStats",
			FunctionParameter = new { ChrId = characterData.entityKey.Id, Str = strAddPoint, Dex = dexAddPoint, Int = intAddPoint, Vit = vitAddPoint },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				characterData.OnApplyStats(strAddPoint, dexAddPoint, intAddPoint, vitAddPoint);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestResetCharacterStats(CharacterData characterData, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ResetStats",
			FunctionParameter = new { ChrId = characterData.entityKey.Id },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.dia -= price;
				characterData.OnResetStats();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCharacterTraining(CharacterData characterData, int addTrainingPoint, int priceGold, int priceDia, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Training",
			FunctionParameter = new { ChrId = characterData.entityKey.Id, Add = addTrainingPoint },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= priceGold;
				CurrencyData.instance.dia -= priceDia;
				characterData.OnTraining(addTrainingPoint);

				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 마지막 훈련 시간이 날아온다.
				if (priceGold > 0)
					PlayerData.instance.OnRecvDailyTrainingGoldInfo((string)date);
				else if (priceDia > 0)
					PlayerData.instance.OnRecvDailyTrainingDiaInfo((string)date);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestChangeWing(CharacterData characterData, int changeType, int wingLookId, int gradeIndex0, int gradeIndex1, int gradeIndex2, int gradeIndex3, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string checkSum = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}", characterData.entityKey.Id, changeType, wingLookId, gradeIndex0, gradeIndex1, gradeIndex2, gradeIndex3));
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ChangeWing",
			FunctionParameter = new { ChrId = characterData.entityKey.Id, ChTp = changeType, Look = wingLookId, Gr0 = gradeIndex0, Gr1 = gradeIndex1, Gr2 = gradeIndex2, Gr3 = gradeIndex3, InCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.dia -= price;
				characterData.OnChangeWing(changeType, wingLookId, gradeIndex0, gradeIndex1, gradeIndex2, gradeIndex3);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestHideWing(CharacterData characterData, bool hideState, Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "HideWing",
			FunctionParameter = new { ChrId = characterData.entityKey.Id, Hid = hideState ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				characterData.HideWing(hideState);
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestUseBalancePp(CharacterData characterData, int useBalancePp, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Balance",
			FunctionParameter = new { ChrId = characterData.entityKey.Id, Bpp = useBalancePp },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				PlayerData.instance.balancePp -= useBalancePp;
				characterData.pp += useBalancePp;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestPurchaseBalancePp(int addBalancePp, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PurchaseBalance",
			FunctionParameter = new { Bpp = addBalancePp },
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
				PlayerData.instance.balancePp += addBalancePp;

				// 상점에서 pp구매한거처럼 마찬가지로 balancePpBuyCount 증가시켜놔야한다.
				PlayerData.instance.balancePpBuyCount += addBalancePp;

				// 성공시에는 구매시간이 날아온다.
				jsonResult.TryGetValue("date", out object date);
				PlayerData.instance.OnRecvPurchaseBalance((string)date);

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
				TimeSpaceData.instance.OnEquip(equipData, true);
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
					TimeSpaceData.instance.OnEquip(listEquipData[i], false);
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
					TimeSpaceData.instance.OnEquip(equipData, false);
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

	public void RequestSellEquip(List<EquipData> listMaterialEquipData, int price, Action successCallback)
	{
		string checkSum = "";
		List<TimeSpaceData.RevokeInventoryItemRequest> listRevokeRequest = TimeSpaceData.instance.GenerateRevokeInfo(listMaterialEquipData, price, "", ref checkSum);

		// 선이펙트 없이 일반 패킷처럼 처리
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SellEquip",
			FunctionParameter = new { Lst = listRevokeRequest, Pri = price, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold += price;
				TimeSpaceData.instance.OnRevokeInventory(listMaterialEquipData);
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
		int addBalancePp = DropManager.instance.GetLobbyBalancePpAmount();
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterTrpRequest> listTrpInfo = DropManager.instance.GetTranscendPointInfo();

		int ppCount = listPpInfo.Count;
		int originCount = listGrantInfo.Count + listTrpInfo.Count;
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
		apiCallCount += listTrpInfo.Count;

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
		string jsonListLbp = serializer.SerializeObject(listTrpInfo);

		// notStreakLegendChar는 OriginBox에서는 체크하지 않고 CharBox에서만 체크한다. 이건 클라가 해서 checkSum에 포함시켜야해서 여기서 체크한다.
		bool existLegendChar = false;
		for (int i = 0; i < listTrpInfo.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listTrpInfo[i].actorId);
			if (actorTableData == null)
				continue;
			if (CharacterData.IsUseLegendWeight(actorTableData))
				existLegendChar = true;
		}
		for (int i = 0; i < listGrantInfo.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listGrantInfo[i]);
			if (actorTableData == null)
				continue;
			if (CharacterData.IsUseLegendWeight(actorTableData))
				existLegendChar = true;
		}

		checkSum = CheckSum(string.Format("{0}_{1}_{2}_{3}_{4}", jsonListPp, jsonListGr, jsonListLbp, addBalancePp, existLegendChar ? 1 : 0));

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenCharBox",
			FunctionParameter = new { LstPp = listPpInfo, Bpp = addBalancePp, LstGr = listGrantInfo, LstTrp = listTrpInfo, LeCh = (existLegendChar ? 1 : 0), LstCs = checkSum },
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
				if ((listTrpInfo.Count + listGrantInfo.Count) == 0)
					PlayerData.instance.notStreakCharCount += 2;
				else
					PlayerData.instance.notStreakCharCount = 0;
				if (existLegendChar == false)
					PlayerData.instance.notStreakLegendCharCount += 2;
				else
					PlayerData.instance.notStreakLegendCharCount = 0;

				// update
				PlayerData.instance.OnRecvUpdateCharacterStatistics(listPpInfo, listTrpInfo, addBalancePp);
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

#if UNITY_ANDROID
	public void RequestValidateDiaBox(string isoCurrencyCode, uint price, string receiptJson, string signature, int buyingDia, Action successCallback, Action<PlayFabError> failureCallback)
	{
		PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptJson = receiptJson,
			Signature = signature
#elif UNITY_IOS
	public void RequestValidateDiaBox(string isoCurrencyCode, int price, string receiptData, int buyingDia, Action successCallback, Action<PlayFabError> failureCallback)
	{
		PlayFabClientAPI.ValidateIOSReceipt(new ValidateIOSReceiptRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptData = receiptData
#endif
		}, (success) =>
		{
			CurrencyData.instance.dia += buyingDia;
			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke(error);
		});
	}

#if UNITY_ANDROID
	public void RequestValidateLevelPackage(string isoCurrencyCode, uint price, string receiptJson, string signature, ShopLevelPackageTableData shopLevelPackageTableData, Action successCallback, Action<PlayFabError> failureCallback)
	{
		PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptJson = receiptJson,
			Signature = signature
#elif UNITY_IOS
	public void RequestValidateLevelPackage(string isoCurrencyCode, int price, string receiptData, ShopLevelPackageTableData shopLevelPackageTableData, Action successCallback, Action<PlayFabError> failureCallback)
	{
		PlayFabClientAPI.ValidateIOSReceipt(new ValidateIOSReceiptRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptData = receiptData
#endif
		}, (success) =>
		{
			CurrencyData.instance.dia += shopLevelPackageTableData.buyingGems;
			CurrencyData.instance.gold += shopLevelPackageTableData.buyingGold;
			CurrencyData.instance.equipBoxKey += shopLevelPackageTableData.buyingEquipKey;
			CurrencyData.instance.legendEquipKey += shopLevelPackageTableData.buyingLegendEquipKey;

			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke(error);
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

#if UNITY_ANDROID
	public void RequestValidateDailyPackage(string isoCurrencyCode, uint price, string receiptJson, string signature, int dayCount, int buyingDia, Action successCallback, Action<PlayFabError> failureCallback)	
	{
		PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptJson = receiptJson,
			Signature = signature
#elif UNITY_IOS
	public void RequestValidateDailyPackage(string isoCurrencyCode, int price, string receiptData, int dayCount, int buyingDia, Action successCallback, Action<PlayFabError> failureCallback)
	{
		PlayFabClientAPI.ValidateIOSReceipt(new ValidateIOSReceiptRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptData = receiptData
#endif
		}, (success) =>
		{
			CurrencyData.instance.dailyDiaRemainCount += dayCount;
			CurrencyData.instance.dia += buyingDia;

			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke(error);
		});
	}

	public void RequestReceiveDailyPackage(int addDia, Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ReceiveDailyPackage",
			FunctionParameter = new { Di = addDia },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dailyDiaRemainCount -= 1;
				CurrencyData.instance.dia += addDia;

				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 마지막 수령 시간이 날아온다.
				PlayerData.instance.OnRecvDailyPackageInfo((string)date);

				if (successCallback != null) successCallback.Invoke(failure);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetFreeItem(int addDia, int addGold, int addEnergy, Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetFreeItem",
			FunctionParameter = new { Go = addGold },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia += addDia;
				CurrencyData.instance.gold += addGold;
				if (addEnergy > 0)
					CurrencyData.instance.OnRecvRefillEnergy(addEnergy);

				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 마지막 수령 시간이 날아온다.
				DailyShopData.instance.OnRecvDailyFreeItemInfo((string)date);

				if (successCallback != null) successCallback.Invoke(failure);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetTitleData(List<string> keys, Action<Dictionary<string, string>> successCallback)
	{
		PlayFabClientAPI.GetTitleData(new GetTitleDataRequest()
		{
			Keys = keys
		}, (success) =>
		{
			if (successCallback != null) successCallback.Invoke(success.Data);
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestPurchaseDailyShopItem(int slotId, string type, string equipId, string actorId, int priceDia, int priceGold, Action<bool, string, string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		ExecuteCloudScriptRequest request = null;
		if (equipId != "")
		{
			string checkSum = "";
			List<TimeSpaceData.ItemGrantRequest> listItemGrantRequest = TimeSpaceData.instance.GenerateGrantRequestInfo(equipId, ref checkSum);
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "BuyDailyShop",
				FunctionParameter = new { Sl = slotId, Tp = type, EqpLst = listItemGrantRequest, EqpLstCs = checkSum },
				GeneratePlayStreamEvent = true,
			};
		}
		else
		{
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "BuyDailyShop",
				FunctionParameter = new { Sl = slotId, Tp = type, Id = actorId },
				GeneratePlayStreamEvent = true,
			};
		}
		
		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia -= priceDia;
				CurrencyData.instance.gold -= priceGold;

				jsonResult.TryGetValue("date", out object date);
				jsonResult.TryGetValue("adChrId", out object adChrId);
				jsonResult.TryGetValue("itmRet", out object itmRet);

				// 성공시에는 서버에서 방금 기록한 마지막 수령 시간이 날아온다.
				DailyShopData.instance.OnRecvDailyShopSlotInfo((string)date, slotId);

				if (successCallback != null) successCallback.Invoke(failure, (string)adChrId, (string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestRegisterDailyShopUnfixedInfo(string jsonUnfixedData)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SetUnfixed",
			FunctionParameter = new { Inf = jsonUnfixedData },
			GeneratePlayStreamEvent = true
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (failure)
				HandleCommonError();
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestPurchaseDailyShopSlot(int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "BuyShopSlot",
			FunctionParameter = new { Lv = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.dia -= price;
				DailyShopData.instance.unlockLevel += 1;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Mail
	public void RequestRefreshMailList(int mailTableDataCount, Action<bool, bool, bool, string, string> successCallback)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "RefreshMail",
			FunctionParameter = new { Mtc = mailTableDataCount },
			GeneratePlayStreamEvent = true
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("del", out object del);
			jsonResult.TryGetValue("add", out object add);
			jsonResult.TryGetValue("mod", out object mod);
			jsonResult.TryGetValue("dat", out object jsonDateTime);
			jsonResult.TryGetValue("mtd", out object jsonMailTable);
			bool deleted = ((del.ToString()) == "1");
			bool added = ((add.ToString()) == "1");
			bool modified = ((mod.ToString()) == "1");
			if (successCallback != null) successCallback.Invoke(deleted, added, modified, (string)jsonDateTime, (string)jsonMailTable);
		}, (error) =>
		{
			// 5분마다 주기적으로 보내는거라 에러 핸들링 하면 안된다.
			//HandleCommonError(error);
		});
	}

	public void RequestReceiveMailPresent(string id, int receiveDay, string type, int addDia, int addGold, int addEnergy, string equipId, Action<bool, string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		ExecuteCloudScriptRequest request = null;
		if (equipId != "")
		{
			string checkSum = "";
			List<TimeSpaceData.ItemGrantRequest> listItemGrantRequest = TimeSpaceData.instance.GenerateGrantRequestInfo(equipId, ref checkSum);
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetMail",
				FunctionParameter = new { Id = id, Dy = receiveDay, Tp = type, EqpLst = listItemGrantRequest, EqpLstCs = checkSum },
				GeneratePlayStreamEvent = true,
			};
		}
		else
		{
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetMail",
				FunctionParameter = new { Id = id, Dy = receiveDay, Tp = type },
				GeneratePlayStreamEvent = true,
			};
		}

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				bool result = MailData.instance.OnRecvGetMail(id, receiveDay, type);
				if (result)
				{
					WaitingNetworkCanvas.Show(false);

					CurrencyData.instance.dia += addDia;
					CurrencyData.instance.gold += addGold;
					if (addEnergy > 0)
						CurrencyData.instance.OnRecvRefillEnergy(addEnergy);

					jsonResult.TryGetValue("dat", out object dat);
					jsonResult.TryGetValue("itmRet", out object itmRet);

					if (successCallback != null) successCallback.Invoke(failure, (string)itmRet);
				}
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestResearchLevelUp(int targetLevel, int price, int addDia, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);
		string input = string.Format("{0}_{1}", targetLevel, "rthqzobj");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Research",
			FunctionParameter = new { Ta = targetLevel, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				CurrencyData.instance.gold -= price;
				CurrencyData.instance.dia += addDia;
				PlayerData.instance.researchLevel += 1;
				PlayerData.instance.ReinitializeActorStatus();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Terms
	public void RequestConfirmTerms(Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "Terms",
			FunctionParameter = new { Terms = 1 },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.termsConfirmed = true;
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}
	#endregion

	#region Support
	public void RequestRefreshInquiryList(Action<string> successCallback)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "RefreshInquiry",
			FunctionParameter = new { Inq = 1 },
			GeneratePlayStreamEvent = true
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("dat", out object jsonInquiryData);
			if (successCallback != null) successCallback.Invoke((string)jsonInquiryData);
		}, (error) =>
		{
			// 5분마다 주기적으로 보내는거라 에러 핸들링 하면 안된다.
			//HandleCommonError(error);
		});
	}

	public void ReqeustWriteInquiry(string body, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Inquiry",
			FunctionParameter = new { Body = body },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				SupportData.instance.OnRecvWriteInquiry(body);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
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