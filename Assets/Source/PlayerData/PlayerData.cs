using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
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
		characterData.actorId = "Actor001";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor002";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor003";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor004";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor005";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor007";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor008";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor009";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor010";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor011";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor012";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor013";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor014";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor015";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor016";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor017";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor018";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor019";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor020";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor021";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor022";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor024";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor025";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor026";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor028";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor029";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor030";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor031";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor033";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor035";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor036";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor037";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor038";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor039";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor040";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
		characterData = new CharacterData();
		characterData.actorId = "Actor041";
		characterData.powerLevel = 1;
		_listCharacterData.Add(characterData);
	}
	#endregion


	#region Server
	void Update()
	{
		UpdateDailyBoxResetTime();
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

		// 나중에 지울 코드이긴 한데 MainSceneBuilder에서 NEWPLAYER_LEVEL1 디파인 켜둔채로 생성하는 테스트용 루틴일땐
		// 1챕터에서 시작하게 처리해둔다.
		// NEWPLAYER_LEVEL1 디파인 지울때 같이 지우면 된다.
		if (MainSceneBuilder.instance.playAfterInstallation == false)
		{
			highestPlayChapter = 1;
			selectedChapter = 1;
		}

		_listCharacterData.Clear();
		AddNewCharacter("Actor001", "", 1);

		// 임의로 생성한거라 EntityKey를 만들어둘수가 없다.
		// 그렇다고 loginned 를 풀어서 통째로 받으면 괜히 커져서 EntityKey 리프레쉬 함수 하나 만들어서 호출하기로 한다.
		StartCoroutine(DelayedSyncCharacterEntity(5.0f));

		_mainCharacterId = "Actor001";
		loginned = true;

		if (newPlayerAddKeep)
		{
			AddNewCharacter("Actor002", "", 1);
			_mainCharacterId = "Actor002";
		}
	}

	public void ResetData()
	{
		// 이게 가장 중요. 다른 것들은 받을때 알아서 다 비우고 다시 셋팅한다.
		loginned = false;

		// OnRecvPlayerData 함수들 두번 받아도 아무 문제없게 짜두면 여기서 딱히 할일은 없을거다.
	}

	public void AddNewCharacter(string actorId, string serverCharacterId, int powerLevel)
	{
		CharacterData characterData = new CharacterData();
		characterData.actorId = actorId;
		characterData.powerLevel = powerLevel;
		if (string.IsNullOrEmpty(serverCharacterId) == false)
			characterData.entityKey = new PlayFab.DataModels.EntityKey() { Id = serverCharacterId, Type = "character" };
		_listCharacterData.Add(characterData);
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

	public void OnRecvPlayerData(List<StatisticValue> playerStatistics, Dictionary<string, UserDataRecord> userData, List<CharacterResult> characterList)
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
				_mainCharacterId = "Actor001";
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

		if (userData.ContainsKey("chaos"))
		{
			int intValue = 0;
			if (int.TryParse(userData["chaos"].Value, out intValue))
				chaosMode = (intValue == 1);
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
		if (userData.ContainsKey("SHlstBxDat"))
		{
			if (string.IsNullOrEmpty(userData["SHlstBxDat"].Value) == false)
				OnRecvDailyBoxInfo(userData["SHlstBxDat"].Value);
		}

		loginned = true;
	}

	public void OnRecvCharacterList(List<CharacterResult> characterList, List<ObjectResult> characterEntityObjectList)
	{
		_listCharacterData.Clear();
		for (int i = 0; i < characterList.Count; ++i)
		{
			CharacterData newCharacterData = new CharacterData();
			newCharacterData.actorId = characterList[i].CharacterName;
			newCharacterData.entityKey = new PlayFab.DataModels.EntityKey { Id = characterList[i].CharacterId, Type = "character" };

			PlayFabApiManager.CharacterDataEntity1 dataObject = null;
			for (int j = 0; j < characterEntityObjectList.Count; ++j)
			{
				if (characterEntityObjectList[j].ObjectName == newCharacterData.actorId)
				{
					dataObject = JsonUtility.FromJson<PlayFabApiManager.CharacterDataEntity1>(characterEntityObjectList[j].DataObject.ToString());
					break;
				}
			}
			if (dataObject == null)
				continue;

			newCharacterData.powerLevel = dataObject.pow;
			_listCharacterData.Add(newCharacterData);
		}
	}

	void OnRecvDailyBoxInfo(DateTime lastDailyBoxOpenTime)
	{
		if (ServerTime.UtcNow < lastDailyBoxOpenTime)
		{
			// 어떻게 미래로 설정되어있을 수가 있나. 이건 무효.
			sharedDailyBoxOpened = false;
			return;
		}

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
			sealCount = 0;
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

	string _mainCharacterId = "Actor001";
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
	#endregion
}
