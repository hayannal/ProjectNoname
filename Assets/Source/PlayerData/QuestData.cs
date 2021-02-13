using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class QuestData : MonoBehaviour
{
	public static QuestData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("QuestData")).AddComponent<QuestData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static QuestData _instance = null;


	public enum eQuestStep
	{
		Select,
		Proceeding,
	}

	public enum eQuestClearType
	{
		KillMonster,
		ClearBossStage,
		Swap,
		NoHitClear,
		PowerSource,
		Critical,
		FastClear,
		Ultimate,
	}

	public enum eQuestCondition
	{
		None,
		PowerSource,
		Grade,
	}

	public const int DailyMaxCount = 3;

	public class QuestInfo
	{
		public int idx;     // index 0 ~ 5
		public string tp;   // type
		public int cnt;     // need count
		public int rwd;     // reward

		public int cdtn;    // sub condition
		public int param;	// condition parameter
	}
	List<QuestInfo> _listQuestInfo;

	// 퀘스트 리셋 시간
	public DateTime todayQuestResetTime { get; private set; }

	// 보상까지 받은 퀘스트 횟수
	public ObscuredInt todayQuestRewardedCount { get; set; }

	// 현재 진행중인 퀘스트. 오리진 박스를 열때 Index는 0으로 Step은 
	public eQuestStep currentQuestStep { get; set; }
	public ObscuredInt currentQuestIndex { get; set; }
	public ObscuredInt currentQuestProceedingCount { get; set; }

	public static eQuestClearType Type2ClearType(string type)
	{
		switch (type)
		{
			case "1": return eQuestClearType.KillMonster;
			case "2": return eQuestClearType.ClearBossStage;
			case "3": return eQuestClearType.Swap;
			case "4": return eQuestClearType.NoHitClear;
			case "5": return eQuestClearType.PowerSource;
			case "6": return eQuestClearType.Critical;
			case "7": return eQuestClearType.FastClear;
			case "8": return eQuestClearType.Ultimate;
		}
		return eQuestClearType.KillMonster;
	}

	public QuestInfo FindQuestInfoByIndex(int index)
	{
		if (CheckValidQuestList() == false)
			return null;

		for (int i = 0; i < _listQuestInfo.Count; ++i)
		{
			if (_listQuestInfo[i].idx == index)
				return _listQuestInfo[i];
		}
		return null;
	}

	

	public void OnRecvQuestData(Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		// PlayerData.ResetData 호출되면 다시 여기로 들어올테니 플래그들 초기화 시켜놓는다.
		_checkUnfixedQuestListInfo = false;

		// 다른 Unfixed 처럼 하루에 할 수 있는 퀘스트 목록 총 6개를 서버로 보내서 등록하게 된다.
		if (userReadOnlyData.ContainsKey("lasUnfxQstDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasUnfxQstDat"].Value) == false)
				_lastUnfixedDateTimeString = userReadOnlyData["lasUnfxQstDat"].Value;
		}

		if (userReadOnlyData.ContainsKey("qstLst"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["qstLst"].Value) == false)
				_questListDataString = userReadOnlyData["qstLst"].Value;
		}

		// 현재 진행중인 퀘스트의 상태. 동시에 1개만 진행가능하다.
		currentQuestStep = eQuestStep.Select;
		currentQuestIndex = 0;
		if (userReadOnlyData.ContainsKey("qstIdx"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["qstIdx"].Value, out intValue))
			{
				currentQuestStep = eQuestStep.Proceeding;
				currentQuestIndex = intValue;
			}
		}

		currentQuestProceedingCount = 0;
		if (userReadOnlyData.ContainsKey("qstPrcdCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["qstPrcdCnt"].Value, out intValue))
				currentQuestProceedingCount = intValue;
		}

		todayQuestRewardedCount = 0;
		if (userReadOnlyData.ContainsKey("qstRwdCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["qstRwdCnt"].Value, out intValue))
				todayQuestRewardedCount = intValue;
		}

		// UI에서 사용할 리셋타임. 정시에 리셋이다.
		todayQuestResetTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
		_lastCachedQuestIndex = -1;
		_listQuestInfo = null;
		_temporaryAddCount = 0;
}

	public void LateInitialize()
	{
		CheckUnfixedQuestListInfo();
	}

	bool _checkIndicatorOnRecvQuestList;
	public bool CheckValidQuestList(bool checkIndicatorOnRecvQuestList = false)
	{
		if (checkIndicatorOnRecvQuestList)
			_checkIndicatorOnRecvQuestList = true;

		return (_listQuestInfo != null && _listQuestInfo.Count > 0);
	}

	#region Quest List
	string _lastUnfixedDateTimeString = "";
	string _questListDataString = "";
	// 클라 구동 후 퀘스트는 하루에 한번 미리 정해둔다.
	bool _checkUnfixedQuestListInfo = false;
	void CheckUnfixedQuestListInfo()
	{
		if (_checkUnfixedQuestListInfo)
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
					var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
					_listQuestInfo = serializer.DeserializeObject<List<QuestInfo>>(_questListDataString);

					OnCompleteRecvQuestList();
				}
				else
					needRegister = true;
			}
		}
		_checkUnfixedQuestListInfo = true;

		if (needRegister == false)
			return;
		RegisterQuestList();
	}

	List<QuestInfo> _listQuestInfoForSend = new List<QuestInfo>();
	void RegisterQuestList()
	{
		_listQuestInfoForSend.Clear();

		// 두개씩 세트로 만들거다. 두개 안에서는 다른 퀘스트를 만들어야한다.
		for (int i = 0; i < DailyMaxCount; ++i)
		{
			string questType1 = "";
			string questType2 = "";
			int questNeedCountIndex1 = 0;
			int questNeedCountIndex2 = 0;

			CreateQuestPair(ref questType1, ref questNeedCountIndex1, ref questType2, ref questNeedCountIndex2);

			QuestInfo questInfo = new QuestInfo();
			questInfo.idx = i * 2;
			questInfo.tp = questType1;
			SubQuestTableData subQuestTableData = TableDataManager.instance.FindSubQuestTableData(questType1);
			questInfo.cnt = subQuestTableData.needCount[questNeedCountIndex1];
			questInfo.rwd = subQuestTableData.rewardGold[questNeedCountIndex1];
			SetSubCondition(questInfo);
			_listQuestInfoForSend.Add(questInfo);

			questInfo = new QuestInfo();
			questInfo.idx = i * 2 + 1;
			questInfo.tp = questType2;
			subQuestTableData = TableDataManager.instance.FindSubQuestTableData(questType2);
			questInfo.cnt = subQuestTableData.needCount[questNeedCountIndex2];
			questInfo.rwd = subQuestTableData.rewardGold[questNeedCountIndex2];
			SetSubCondition(questInfo);
			_listQuestInfoForSend.Add(questInfo);
		}

		PlayFabApiManager.instance.RequestRegisterQuestList(_listQuestInfoForSend, () =>
		{
			// 성공이 오면 새로 만든거로 덮어씌운다.
			_listQuestInfo = _listQuestInfoForSend;

			// 성공했다면 오늘의 최초 로그인에서 퀘스트 리스트를 등록했다는거니
			// 퀘스트 스텝을 초기화 해도 되지 않을까.
			// 여기에서 처리하면 오리진박스 오픈하고나서 설정하지 않아도 되기때문에 하는게 좋을거 같다.
			currentQuestIndex = 0;
			currentQuestStep = eQuestStep.Select;
			currentQuestProceedingCount = 0;
			todayQuestRewardedCount = 0;
			_lastCachedQuestIndex = -1;

			OnCompleteRecvQuestList();
		});
	}

	void OnCompleteRecvQuestList()
	{
		// 패킷 받고나서 바로 확인하도록 처리
		if (_checkIndicatorOnRecvQuestList)
		{
			if (TreasureChest.instance != null && TreasureChest.instance.gameObject.activeSelf && IsCompleteQuest())
				TreasureChest.instance.SetAutoShowIndicatorRemainTime(0.1f);
			_checkIndicatorOnRecvQuestList = false;
		}
	}

	List<int> _listTempIndex = new List<int>();
	void CreateQuestPair(ref string questType1, ref int questNeedCountIndex1, ref string questType2, ref int questNeedCountIndex2)
	{
		// 타입도 달라야하고 난이도도 달라야한다. 첫번째꺼는 테이블에서 랜덤으로 뽑고
		int firstIndex = UnityEngine.Random.Range(0, TableDataManager.instance.subQuestTable.dataArray.Length);
		questType1 = TableDataManager.instance.subQuestTable.dataArray[firstIndex].type;
		questNeedCountIndex1 = UnityEngine.Random.Range(0, TableDataManager.instance.subQuestTable.dataArray[firstIndex].needCount.Length);

		// 두번째꺼는 첫번째껄 제외한 리스트를 만들어서 이 안에서 뽑는다.
		_listTempIndex.Clear();
		for (int i = 0; i < TableDataManager.instance.subQuestTable.dataArray.Length; ++i)
		{
			if (firstIndex == i)
				continue;
			_listTempIndex.Add(i);
		}
		int secondIndex = _listTempIndex[UnityEngine.Random.Range(0, _listTempIndex.Count)];
		questType2 = TableDataManager.instance.subQuestTable.dataArray[secondIndex].type;

		_listTempIndex.Clear();
		for (int i = 0; i < TableDataManager.instance.subQuestTable.dataArray[secondIndex].needCount.Length; ++i)
		{
			if (questNeedCountIndex1 == i)
				continue;
			_listTempIndex.Add(i);
		}
		questNeedCountIndex2 = _listTempIndex[UnityEngine.Random.Range(0, _listTempIndex.Count)];
	}

	List<int> _listTempCondition = new List<int>();
	List<float> _listGradeWeight = new List<float>();
	void SetSubCondition(QuestInfo questInfo)
	{
		if (Type2ClearType(questInfo.tp) == eQuestClearType.Swap)
			return;

		// 추가 조건은 페어랑 상관없이 개별로 하면 된다.
		// 먼저 추가 조건이 쓰이는지부터 굴려본다.
		_listTempCondition.Clear();
		_listTempCondition.Add((int)eQuestCondition.None);
		_listTempCondition.Add((int)eQuestCondition.None);
		_listTempCondition.Add((int)eQuestCondition.PowerSource);
		_listTempCondition.Add((int)eQuestCondition.Grade);

		questInfo.cdtn = _listTempCondition[UnityEngine.Random.Range(0, _listTempCondition.Count)];
		switch (questInfo.cdtn)
		{
			case (int)eQuestCondition.None:
				break;
			case (int)eQuestCondition.PowerSource:
				questInfo.param = UnityEngine.Random.Range(0, 4);
				break;
			case (int)eQuestCondition.Grade:
				_listGradeWeight.Clear();
				float sumWeight = 1.5f; _listGradeWeight.Add(sumWeight);
				sumWeight += 1.25f; _listGradeWeight.Add(sumWeight);
				sumWeight += 1.0f; _listGradeWeight.Add(sumWeight);
				float randomResult = UnityEngine.Random.Range(0.0f, sumWeight);
				for (int i = 0; i < _listGradeWeight.Count; ++i)
				{
					if (randomResult <= _listGradeWeight[i])
					{
						questInfo.param = i;
						break;
					}
				}
				break;
		}
	}
	#endregion

	public void ResetQuestStepInfo()
	{
		// 갱신 타이밍은 항상 동일
		todayQuestResetTime += TimeSpan.FromDays(1);

		todayQuestRewardedCount = 0;

		// 퀘스트의 진행 내역을 초기화 해야한다.
		currentQuestIndex = 0;
		currentQuestStep = eQuestStep.Select;
		currentQuestProceedingCount = 0;

		// 이 타이밍에 다음날 새로 열리는 퀘스트 갱신처리도 함께 해준다.
		RegisterQuestList();
	}

	public bool IsCompleteQuest()
	{
		// 이미 보상받은 퀘스트는 Complete로 판단하지 않고 rewarded count에 포함되어있다.
		if (currentQuestStep != eQuestStep.Proceeding)
			return false;

		QuestInfo questInfo = FindQuestInfoByIndex(currentQuestIndex);
		if (questInfo == null)
			return false;
		return (currentQuestProceedingCount >= questInfo.cnt);
	}

	int _lastCachedQuestIndex = -1;
	QuestInfo _cachedQuestInfo = null;
	// 이거는 게임 진행중에 누적하고있는 값이다. 서버에 보내서 확인처리되면 currentQuestProceedingCount 값에 반영시킨다.
	ObscuredInt _temporaryAddCount;
	public void OnQuestEvent(eQuestClearType questClearType)
	{
		if (PlayerData.instance.currentChaosMode == false)
			return;
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
			return;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			return;
		if (currentQuestStep != eQuestStep.Proceeding)
			return;

		// 자주 호출되는 부분이라서 캐싱을 사용한다.
		QuestInfo selectedQuestInfo = null;
		if (_lastCachedQuestIndex == currentQuestIndex && _cachedQuestInfo != null)
			selectedQuestInfo = _cachedQuestInfo;
		else
		{
			selectedQuestInfo = FindQuestInfoByIndex(currentQuestIndex);
			_lastCachedQuestIndex = currentQuestIndex;
			_cachedQuestInfo = selectedQuestInfo;
		}

		// 설마 이랬는데도 null이면 이상한거다.
		if (selectedQuestInfo == null)
			return;

		// 이미 완료한 상태라면 더 할 필요도 없다.
		if (currentQuestProceedingCount + _temporaryAddCount >= selectedQuestInfo.cnt)
			return;

		// 조건들 체크
		if (Type2ClearType(selectedQuestInfo.tp) != questClearType)
			return;
		switch (selectedQuestInfo.cdtn)
		{
			case (int)eQuestCondition.PowerSource:
				if (BattleInstanceManager.instance.playerActor.targetingProcessor.cachedActorTableData.powerSource != selectedQuestInfo.param)
					return;
				break;
			case (int)eQuestCondition.Grade:
				if (BattleInstanceManager.instance.playerActor.targetingProcessor.cachedActorTableData.grade != selectedQuestInfo.param)
					return;
				break;
		}

		// 여기까지 통과했으면 임시변수에 누적해두고
		// 플레이 종료시 갱신 함수를 날려서 갱신하면 된다.
		_temporaryAddCount += 1;

		// 이 변수 역시 서버는 모르는 값이기때문에 재진입에서 복구를 해야하는 점수다.
		ClientSaveData.instance.OnChangedQuestTemporaryAddCount(_temporaryAddCount);
	}

	public void SetQuestInfoForInProgressGame()
	{
		_temporaryAddCount = ClientSaveData.instance.GetCachedQuestTemporaryAddCount();
	}

	public void OnEndGame(bool cancelGame = false)
	{
		// 하나도 달성 못했으면 패스
		if (_temporaryAddCount == 0)
			return;

		// 취소가 아닌 정상적인 EndGame에서 처리할게 있는지 확인하면 된다.
		if (cancelGame)
		{
			_temporaryAddCount = 0;
			return;
		}

		PlayFabApiManager.instance.RequestQuestProceedingCount(_temporaryAddCount, () =>
		{
			_temporaryAddCount = 0;
		});
	}
}