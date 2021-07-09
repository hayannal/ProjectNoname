using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class ContentsData : MonoBehaviour
{
	public static ContentsData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("ContentsData")).AddComponent<ContentsData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static ContentsData _instance = null;

	// 서브 컨텐츠가 많아지면서 PlayerData에 다 담을 수 없어서 따로 파서 관리하기로 한다.

	// BossBattle 관련 변수
	public ObscuredInt bossBattleId { get; set; }
	Dictionary<string, int> _dicBossBattleClearDifficulty = new Dictionary<string, int>();
	Dictionary<string, int> _dicBossBattleSelectedDifficulty = new Dictionary<string, int>();
	Dictionary<string, int> _dicBossBattleCount = new Dictionary<string, int>();
	// 클라 전용 변수. 보스가 갱신되었음을 다음번 창 열릴때 알린다.
	public ObscuredBool newBossRefreshed { get; set; }
	// 보스전 결과창 후 로비로 되돌아올때 로딩을 위한 변수
	public ObscuredBool readyToReopenBossEnterCanvas { get; set; }

	// Invasion 관련 변수
	List<ObscuredString> _listInvasionEnteredActorId = new List<ObscuredString>();
	public List<ObscuredString> listInvasionEnteredActorId { get { return _listInvasionEnteredActorId; } }
	// 오늘 1회라도 입장했다면 true
	public ObscuredBool invasionTodayEntered { get; set; }
	public DateTime lastInvasionEnteredTime { get; private set; }
	// 요일전 결과창 후 로비로 되돌아올때 로딩을 위한 변수
	public ObscuredBool readyToReopenInvasionEnterCanvas { get; set; }


	public void OnRecvContentsData(Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		#region Boss Battle
		newBossRefreshed = false;
		readyToReopenBossEnterCanvas = false;

		bossBattleId = 0;
		if (userReadOnlyData.ContainsKey("bossId"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["bossId"].Value, out intValue))
				bossBattleId = intValue;
		}

		// difficulty
		string bossBattleRecord = "";
		if (userReadOnlyData.ContainsKey("bossClLv"))
			bossBattleRecord = userReadOnlyData["bossClLv"].Value;
		OnRecvBossBattleClearData(bossBattleRecord);

		bossBattleRecord = "";
		if (userReadOnlyData.ContainsKey("bossSeLv"))
			bossBattleRecord = userReadOnlyData["bossSeLv"].Value;
		OnRecvBossBattleSelectData(bossBattleRecord);

		bossBattleRecord = "";
		if (userReadOnlyData.ContainsKey("bossCnt"))
			bossBattleRecord = userReadOnlyData["bossCnt"].Value;
		OnRecvBossBattleCountData(bossBattleRecord);
		#endregion

		#region Invasion
		readyToReopenInvasionEnterCanvas = false;
		invasionTodayEntered = false;
		if (userReadOnlyData.ContainsKey("invLasDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["invLasDat"].Value) == false)
			{
				DateTime lastTime = new DateTime();
				if (DateTime.TryParse(userReadOnlyData["invLasDat"].Value, out lastTime))
				{
					lastInvasionEnteredTime = lastTime.ToUniversalTime();
					if (ServerTime.UtcNow.Year == lastInvasionEnteredTime.Year && ServerTime.UtcNow.Month == lastInvasionEnteredTime.Month && ServerTime.UtcNow.Day == lastInvasionEnteredTime.Day)
						invasionTodayEntered = true;
					else
						invasionTodayEntered = false;
				}
			}
		}

		_listInvasionEnteredActorId.Clear();
		if (invasionTodayEntered)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			if (userReadOnlyData.ContainsKey("invActLst"))
			{
				List<string> listInvasionEnteredActor = null;
				string invasionEnteredActorListString = userReadOnlyData["invActLst"].Value;
				if (string.IsNullOrEmpty(invasionEnteredActorListString) == false)
					listInvasionEnteredActor = serializer.DeserializeObject<List<string>>(invasionEnteredActorListString);

				for (int i = 0; i < listInvasionEnteredActor.Count; ++i)
					_listInvasionEnteredActorId.Add(listInvasionEnteredActor[i]);
			}
		}
		#endregion
	}

	public void ClearData()
	{
		// 계정 데이터 초기화
		bossBattleId = 0;
		newBossRefreshed = false;
		readyToReopenBossEnterCanvas = false;

		OnRecvBossBattleClearData("");
		OnRecvBossBattleSelectData("");
		OnRecvBossBattleCountData("");
	}

	public void ResetContentsInfo()
	{
		// 다음날이 되서 갱신되어야하는 것들 초기화
		_listInvasionEnteredActorId.Clear();
		invasionTodayEntered = false;
	}


	#region Boss Battle
	void OnRecvBossBattleClearData(string json)
	{
		_dicBossBattleClearDifficulty.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicBossBattleClearDifficulty = serializer.DeserializeObject<Dictionary<string, int>>(json);
	}

	void OnRecvBossBattleSelectData(string json)
	{
		_dicBossBattleSelectedDifficulty.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicBossBattleSelectedDifficulty = serializer.DeserializeObject<Dictionary<string, int>>(json);
	}

	void OnRecvBossBattleCountData(string json)
	{
		_dicBossBattleCount.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicBossBattleCount = serializer.DeserializeObject<Dictionary<string, int>>(json);
	}

	public int GetBossBattleClearDifficulty(string id)
	{
		if (_dicBossBattleClearDifficulty.ContainsKey(id))
			return _dicBossBattleClearDifficulty[id];
		return 0;
	}

	public int GetBossBattleSelectedDifficulty(string id)
	{
		if (_dicBossBattleSelectedDifficulty.ContainsKey(id))
			return _dicBossBattleSelectedDifficulty[id];

		// 선택 데이터가 없으면 분명 처음 열린걸꺼다. 이때는 0을 리턴해준다.
		return 0;
	}

	public int GetBossBattleCount(string id)
	{
		if (_dicBossBattleCount.ContainsKey(id))
			return _dicBossBattleCount[id];
		return 0;
	}

	public void ClearBossBattleDifficulty(int difficulty)
	{
		int id = bossBattleId;
		if (id == 0) id = 1;
		string key = id.ToString();
		if (_dicBossBattleClearDifficulty.ContainsKey(key))
			_dicBossBattleClearDifficulty[key] = difficulty;
		else
			_dicBossBattleClearDifficulty.Add(key, difficulty);
	}

	public void SelectBossBattleDifficulty(int difficulty)
	{
		int id = bossBattleId;
		if (id == 0) id = 1;
		string key = id.ToString();
		if (_dicBossBattleSelectedDifficulty.ContainsKey(key))
			_dicBossBattleSelectedDifficulty[key] = difficulty;
		else
			_dicBossBattleSelectedDifficulty.Add(key, difficulty);
	}

	public void AddBossBattleCount()
	{
		int id = bossBattleId;
		if (id == 0) id = 1;
		string key = id.ToString();
		if (_dicBossBattleCount.ContainsKey(key))
		{
			int value = _dicBossBattleCount[key];
			if (value < GetMaxXpExp())
				_dicBossBattleCount[key]++;
		}
		else
			_dicBossBattleCount.Add(key, 1);
	}

	int GetMaxXpExp()
	{
		for (int i = 1; i < TableDataManager.instance.bossExpTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.bossExpTable.dataArray[i].xpLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleLevel"))
				return TableDataManager.instance.bossExpTable.dataArray[i].requiredAccumulatedExp;
		}
		return 0;
	}


	List<int> _listNextRandomBossId = new List<int>();
	public int GetNextRandomBossId()
	{
		_listNextRandomBossId.Clear();

		// 현재 설정된 보스를 제외한 나머지 중에서 
		int prevBossBattleId = bossBattleId;
		if (prevBossBattleId == 0)
			prevBossBattleId = 1;
		for (int i = 0; i < TableDataManager.instance.bossBattleTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.bossBattleTable.dataArray[i].chapter < PlayerData.instance.highestPlayChapter)
			{
				if (prevBossBattleId != TableDataManager.instance.bossBattleTable.dataArray[i].num)
					_listNextRandomBossId.Add(TableDataManager.instance.bossBattleTable.dataArray[i].num);
			}
			else
				break;
		}
		if (_listNextRandomBossId.Count == 0)
			return prevBossBattleId;
		return _listNextRandomBossId[UnityEngine.Random.Range(0, _listNextRandomBossId.Count)];
	}

	public void OnClearBossBattle(int selectedDifficulty, int clearDifficulty, int nextBossId)
	{
		// 현재 선택한 레벨이 최고레벨일때랑 아닐때랑 나뉜다.
		if (selectedDifficulty <= clearDifficulty)
		{
			// 최고 클리어 난이도보다 낮거나 같은 난이도를 클리어. 이미 클리어한 곳을 클리어하는거니 아무것도 하지 않는다.
		}
		else
		{
			// record
			bool firstClear = false;
			if (clearDifficulty == 0)
				firstClear = true;
			else if (selectedDifficulty == (clearDifficulty + 1))
				firstClear = true;

			if (firstClear)
			{
				ClearBossBattleDifficulty(selectedDifficulty);

				int currentBossId = bossBattleId;
				if (currentBossId == 0)
					currentBossId = 1;
				if (GetBossBattleClearDifficulty(currentBossId.ToString()) == selectedDifficulty)
				{
					// 난이도의 최대 범위를 넘지않는 한도 내에서
					// 그러나 최대 범위 넘지 않더라도 7챕터를 깨지 않으면 난이도 8 이상으로는 올릴 수 없도록 해야한다.
					int nextDifficulty = selectedDifficulty + 1;
					if (nextDifficulty > 14)
						nextDifficulty = 14;
					else if (nextDifficulty > 7 && PlayerData.instance.highestPlayChapter <= 7)
						nextDifficulty = 7;

					if (selectedDifficulty != nextDifficulty)
					{
						selectedDifficulty = nextDifficulty;
						SelectBossBattleDifficulty(selectedDifficulty);
					}
				}
			}
		}

		// 난이도 처리가 다 끝난 후 다음 보스 아이디를 갱신해야한다.
		bossBattleId = nextBossId;
		newBossRefreshed = true;
	}
	#endregion

	#region Invasion
	public void OnRecvInvasionClearDateTime(string lastInvasionTimeString, string selectedActorId)
	{
		DateTime lastInvasionTime = new DateTime();
		if (DateTime.TryParse(lastInvasionTimeString, out lastInvasionTime))
		{
			DateTime universalTime = lastInvasionTime.ToUniversalTime();
			if (ServerTime.UtcNow.Year == universalTime.Year && ServerTime.UtcNow.Month == universalTime.Month && ServerTime.UtcNow.Day == universalTime.Day)
			{
				lastInvasionEnteredTime = universalTime;
				invasionTodayEntered = true;
				if (_listInvasionEnteredActorId.Contains(selectedActorId) == false)
					_listInvasionEnteredActorId.Add(selectedActorId);
			}
			else
			{
				// 정말 이상한 경우인데 클리어한 시점이라고 날아온 값이 어제인거다.
				// 이건 아무래도 날짜갱신 타이밍에 맞춰서 클리어했는데 하필 서버에 어제꺼로 기록이 된건데..
				// 이땐 서버에 어쨌든 어제꺼로 되어있을테니 그냥 클리어 시키는 쪽으로 처리해둔다.
				lastInvasionEnteredTime = universalTime;
				invasionTodayEntered = false;
				_listInvasionEnteredActorId.Clear();
			}
		}
	}
	#endregion
}