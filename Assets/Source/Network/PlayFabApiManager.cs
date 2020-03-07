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


public class PlayFabApiManager : MonoBehaviour
{
	public static PlayFabApiManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("PlayFabApiManager")).AddComponent<PlayFabApiManager>();
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

		if (error.Error == PlayFabErrorCode.ServiceUnavailable || error.HttpCode == 400)
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_DisconnectServer"), () =>
			{
				SceneManager.LoadScene(0);
			});
		}
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
	List<ObjectResult> _characterEntityObjects = new List<ObjectResult>();
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
			GetObjectsRequest getCharacterEntityRequest = new GetObjectsRequest { Entity = new PlayFab.DataModels.EntityKey { Id = characterId, Type = "character" } };
			PlayFabDataAPI.GetObjects(getCharacterEntityRequest, OnGetObjectsCharacter, OnRecvPlayerDataFailure);
			++_requestCountForGetPlayerData;
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

	void OnGetObjectsCharacter(GetObjectsResponse result)
	{
		Dictionary<string, ObjectResult>.Enumerator e = result.Objects.GetEnumerator();
		ObjectResult objectResult = null;
		while (e.MoveNext())
		{
			// 분명 첫번째 EntityObjects에 Actor001 처럼 캐릭 본인의 아이디로 된 key value가 들어있을거다. "Actor"스트링으로 검사해서 추출해낸다.
			if (e.Current.Key.Contains("Actor"))
			{
				objectResult = e.Current.Value;
				break;
			}
		}

		if (objectResult != null)
			_characterEntityObjects.Add(objectResult);

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	void OnRecvPlayerDataFailure(PlayFabError error)
	{
		// 로그인이 성공한 이상 실패할거 같진 않지만 그래도 혹시 모르니 해둔다.
		Debug.LogError(error.GenerateErrorReport());
		StartCoroutine(AuthManager.instance.RestartProcess());
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
		PlayerData.instance.OnRecvPlayerData(_loginResult.InfoResultPayload.PlayerStatistics, _loginResult.InfoResultPayload.UserData);
		PlayerData.instance.OnRecvCharacterList(_loginResult.InfoResultPayload.CharacterList, _characterEntityObjects);

		_loginResult = null;
#if USE_TITLE_PLAYER_ENTITY
		_titlePlayerEntityObject = null;
#endif
		_characterEntityObjects.Clear();
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
	string _requestMainCharacterId;
	public void RequestSelectMainCharacter(string mainCharacterId, Action successCallback, Action failureCallback = null)
	{
		_requestMainCharacterId = mainCharacterId;
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { "mainCharacterId", mainCharacterId } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.mainCharacterId = _requestMainCharacterId;
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
				if (failureCallback != null) failureCallback.Invoke();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	int _requestChangeChapter;
	bool _requestChangeChaos;
	public void RequestChangeChapter(int chapter, bool chaos, Action successCallback, Action failureCallback = null)
	{
		_requestChangeChapter = chapter;
		_requestChangeChaos = chaos;
		UpdateUserDataRequest request = new UpdateUserDataRequest() { Data = new Dictionary<string, string>() { { "selectedChapter", chapter.ToString() }, { "chaos", chaos ? "1" : "0" } } };
		Action action = () =>
		{
			PlayFabClientAPI.UpdateUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.selectedChapter = _requestChangeChapter;
				PlayerData.instance.chaosMode = _requestChangeChaos;
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

	#region InGame End
	public void RequestEndGame(int highestPlayChapter, int highestClearStage, int addGold)
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
	}
	#endregion

	#region Modify CharacterData
	// 이것도 서버에 저장되는 Entity Object
	public class CharacterDataEntity1
	{
		public int pow;
	}

	public void RequestPowerUpCharacter(CharacterData characterData, Action successCallback)
	{
		RequestCharacterData(characterData, characterData.powerLevel + 1, successCallback);
	}

	void RequestCharacterData(CharacterData characterData, int powerLevel, Action successCallback)
	{
		// 몰아서 저장하기 때문에 모든 정보를 다 적어야한다.
		CharacterDataEntity1 entity1Object = new CharacterDataEntity1
		{
			pow = powerLevel
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