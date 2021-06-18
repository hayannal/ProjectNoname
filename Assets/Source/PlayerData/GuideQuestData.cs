using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class GuideQuestData : MonoBehaviour
{
	public static GuideQuestData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("GuideQuestData")).AddComponent<GuideQuestData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static GuideQuestData _instance = null;

	// QuestData에 쓰던거 그대로 가져와서 더 많이 추가해서 쓴다. Swap같은건 완전 겹치는거라 안쓰기로 한다. 대신 인덱스 밀리면 안되서 지우진 않는다.
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

	// 현재 진행중인 퀘스트.
	public ObscuredInt currentGuideQuestIndex { get; set; }
	public ObscuredInt currentGuideQuestProceedingCount { get; set; }

	public static eQuestClearType Type2ClearType(int typeId)
	{
		switch (typeId)
		{
			case 1: return eQuestClearType.KillMonster;
			case 2: return eQuestClearType.ClearBossStage;
			case 3: return eQuestClearType.Swap;
			case 4: return eQuestClearType.NoHitClear;
			case 5: return eQuestClearType.PowerSource;
			case 6: return eQuestClearType.Critical;
			case 7: return eQuestClearType.FastClear;
			case 8: return eQuestClearType.Ultimate;
		}
		return eQuestClearType.KillMonster;
	}

	public void OnRecvGuideQuestData(Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		// 현재 진행중인 퀘스트의 상태. 동시에 1개만 진행가능하다.
		currentGuideQuestIndex = 0;
		if (userReadOnlyData.ContainsKey("gQstIdx"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["gQstIdx"].Value, out intValue))
				currentGuideQuestIndex = intValue;			
		}

		currentGuideQuestProceedingCount = 0;
		if (userReadOnlyData.ContainsKey("gQstPrcdCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["gQstPrcdCnt"].Value, out intValue))
				currentGuideQuestProceedingCount = intValue;
		}

		_lastCachedGuideQuestIndex = -1;
		_temporaryAddCount = 0;
	}

	int _lastCachedGuideQuestIndex = -1;
	GuideQuestTableData _cachedTableData = null;
	public GuideQuestTableData GetCurrentGuideQuestTableData()
	{
		// 자주 호출되는 부분이라서 캐싱을 사용한다.
		if (_lastCachedGuideQuestIndex == currentGuideQuestIndex && _cachedTableData != null)
			return _cachedTableData;

		_lastCachedGuideQuestIndex = currentGuideQuestIndex;
		_cachedTableData = TableDataManager.instance.FindGuideQuestTableData(currentGuideQuestIndex);
		return _cachedTableData;
	}

	public bool IsCompleteQuest()
	{
		GuideQuestTableData guideQuestTableData = GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return false;
		return (currentGuideQuestProceedingCount >= guideQuestTableData.needCount);
	}

	ObscuredInt _temporaryAddCount;
	public void OnQuestEvent(eQuestClearType questClearType)
	{
		GuideQuestTableData guideQuestTableData = GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		// viewInBattle에 따라 조건 검사가 달라진다.
		if (guideQuestTableData.viewInBattle)
		{
			if (PlayerData.instance.selectedChapter == PlayerData.instance.highestPlayChapter)
			{
			}
			else
				return;

			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
				return;
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
				return;
		}
		
		// 이미 완료한 상태라면 더 할 필요도 없다.
		if (currentGuideQuestProceedingCount + _temporaryAddCount >= guideQuestTableData.needCount)
			return;

		// 조건들 체크
		if (Type2ClearType(guideQuestTableData.typeId) != questClearType)
			return;

		_temporaryAddCount += 1;

		// 이 변수 역시 서버는 모르는 값이기때문에 재진입에서 복구를 해야하는 점수다.
		ClientSaveData.instance.OnChangedGuideQuestTemporaryAddCount(_temporaryAddCount);

		// 이제 UI도 생겼다. UI도 같이 갱신
		GuideQuestInfo.instance.OnAddCount(_temporaryAddCount);
	}

	public void SetGuideQuestInfoForInProgressGame()
	{
		GuideQuestTableData guideQuestTableData = GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;
		if (guideQuestTableData.viewInBattle == false)
			return;

		_temporaryAddCount = ClientSaveData.instance.GetCachedGuideQuestTemporaryAddCount();
		GuideQuestInfo.instance.OnAddCount(_temporaryAddCount);
	}

	public void OnEndGame(bool cancelGame = false)
	{
		if (_temporaryAddCount == 0)
			return;

		if (cancelGame)
		{
			_temporaryAddCount = 0;
			return;
		}

		// 플레이 도중에 다음날이 되어도 가이드 퀘스트는 리셋되지 않기 때문에 QuestData에서 체크하던건 필요없다.
		//if (currentQuestStep != eQuestStep.Proceeding)
		//{
		//	_temporaryAddCount = 0;
		//	return;
		//}

		PlayFabApiManager.instance.RequestGuideQuestProceedingCount(currentGuideQuestIndex, _temporaryAddCount, () =>
		{
			_temporaryAddCount = 0;
		});
	}
}