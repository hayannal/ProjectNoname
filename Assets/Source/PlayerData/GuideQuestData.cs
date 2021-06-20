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
		IngameLevel,

		FreeItem,
		CumulativeLogin,
		ExperienceLevel1,
		ExperienceLevel2,
		PowerLevel,
		ChapterStage,
		ChangeMainCharacter,
		EnterTimeSpace,
		EquipType,
		EquipEnhance,
		ResearchLevel,
		OpenDailyBox,
		EnergyChargeAlarm,
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
			case 9: return eQuestClearType.IngameLevel;

			case 10: return eQuestClearType.FreeItem;
			case 11: return eQuestClearType.CumulativeLogin;
			case 12: return eQuestClearType.ExperienceLevel1;
			case 13: return eQuestClearType.ExperienceLevel2;
			case 14: return eQuestClearType.PowerLevel;
			case 15: return eQuestClearType.ChapterStage;
			case 16: return eQuestClearType.ChangeMainCharacter;
			case 17: return eQuestClearType.EnterTimeSpace;
			case 18: return eQuestClearType.EquipType;
			case 19: return eQuestClearType.EquipEnhance;
			case 20: return eQuestClearType.ResearchLevel;
			case 21: return eQuestClearType.OpenDailyBox;
			case 22: return eQuestClearType.EnergyChargeAlarm;
		}
		return (eQuestClearType)(typeId - 1);
	}

	public static bool IsImmediateQuest(eQuestClearType questClearType)
	{
		switch (questClearType)
		{
			case eQuestClearType.FreeItem:
			case eQuestClearType.CumulativeLogin:
			case eQuestClearType.ExperienceLevel1:
			case eQuestClearType.ExperienceLevel2:
			case eQuestClearType.PowerLevel:
			case eQuestClearType.ChangeMainCharacter:
			case eQuestClearType.EnterTimeSpace:
			case eQuestClearType.EquipType:
			case eQuestClearType.EquipEnhance:
			case eQuestClearType.ResearchLevel:
			case eQuestClearType.OpenDailyBox:
			case eQuestClearType.EnergyChargeAlarm:
				return true;
		}
		return false;
	}

	public void OnRecvGuideQuestData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		// 현재 진행중인 퀘스트의 상태. 동시에 1개만 진행가능하다.
		currentGuideQuestIndex = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "guideQuestIndex")
			{
				currentGuideQuestIndex = playerStatistics[i].Value;
				break;
			}
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
		if (ContentsManager.IsTutorialChapter())
			return;

		if (currentGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
			return;

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

		if (IsImmediateQuest(questClearType))
		{
			// 로비 퀘스트들은 행동 즉시 바로 보내는게 맞다.
			PlayFabApiManager.instance.RequestGuideQuestProceedingCount(currentGuideQuestIndex, 1, currentGuideQuestProceedingCount + 1, guideQuestTableData.key,() =>
			{
				GuideQuestInfo.instance.OnAddCount(0);
				if (currentGuideQuestProceedingCount + 1 >= guideQuestTableData.needCount)
					GuideQuestInfo.instance.RefreshAlarmObject();
			});
			return;
		}

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

		GuideQuestTableData guideQuestTableData = GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		PlayFabApiManager.instance.RequestGuideQuestProceedingCount(currentGuideQuestIndex, _temporaryAddCount, currentGuideQuestProceedingCount + _temporaryAddCount, guideQuestTableData.key, () =>
		{
			_temporaryAddCount = 0;
		});
	}

	public int CheckNextInitialProceedingCount()
	{
		int nextGuideQuestIndex = currentGuideQuestIndex + 1;
		if (nextGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
			return 0;

		GuideQuestTableData nextGuideQuestTableData = TableDataManager.instance.FindGuideQuestTableData(nextGuideQuestIndex);
		if (nextGuideQuestTableData == null)
			return 0;

		bool processed = false;
		eQuestClearType questClearType = Type2ClearType(nextGuideQuestTableData.typeId);
		switch (questClearType)
		{
			case eQuestClearType.FreeItem:
				// 일일 무료 아이템은 누적을 담당하는게 없으니 오늘 연 기록을 써서라도 해야하고
				if (DailyShopData.instance.dailyFreeItemReceived)
					processed = true;
				break;
			case eQuestClearType.CumulativeLogin:
				// 누적 로그인 같은건 기록이 있으니 여기서 조회하는게 안전하다.
				if (CumulativeEventData.instance.newAccountLoginEventCount >= 1)
					processed = true;
				break;
			case eQuestClearType.ExperienceLevel1:
				// 보상까지 받은 상태라면 플레이도 한거로 쳐준다.
				if (ExperienceData.instance.IsPlayed(nextGuideQuestTableData.param) || ExperienceData.instance.IsRewarded(nextGuideQuestTableData.param))
					processed = true;
				break;
			case eQuestClearType.ExperienceLevel2:
				if (ExperienceData.instance.IsRewarded(nextGuideQuestTableData.param))
					processed = true;
				break;
			case eQuestClearType.PowerLevel:
				CharacterData characterData = PlayerData.instance.GetCharacterData(nextGuideQuestTableData.param);
				if (characterData != null)
					return Mathf.Min(characterData.powerLevel, nextGuideQuestTableData.needCount);
				break;
			case eQuestClearType.ChapterStage:
				if (CheckChapterStage(PlayerData.instance.highestPlayChapter, PlayerData.instance.highestClearStage, nextGuideQuestTableData))
					processed = true;
				break;
			case eQuestClearType.EnterTimeSpace:
				if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
					processed = true;
				break;
			case eQuestClearType.EquipType:
				if (int.TryParse(nextGuideQuestTableData.param, out int paramEquipType))
				{
					if (TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)paramEquipType) != null)
						processed = true;
				}
				break;
			case eQuestClearType.ResearchLevel:
				return Mathf.Min(PlayerData.instance.researchLevel, nextGuideQuestTableData.needCount);
			case eQuestClearType.OpenDailyBox:
				if (PlayerData.instance.sharedDailyBoxOpened)
					processed = true;
				break;
			case eQuestClearType.EnergyChargeAlarm:
				if (OptionManager.instance.energyAlarm == 1)
					processed = true;
				break;
		}
		if (processed == false)
			return 0;

		// 이제 컴플릿 후에 별도 패킷으로 보내는게 아니라 컴플릿 안에다가 실어서 보내는거니 필요없다.
		//PlayFabApiManager.instance.RequestGuideQuestProceedingCount(currentGuideQuestIndex, 1, () =>
		//{
		//	GuideQuestInfo.instance.OnAddCount(0);
		//	if (currentGuideQuestProceedingCount + 1 >= guideQuestTableData.needCount)
		//		GuideQuestInfo.instance.RefreshAlarmObject();
		//});
		return nextGuideQuestTableData.needCount;
	}

	#region Check Condition Parameter
	// 파라미터 있는거마다 별도의 체크 함수 적용
	public bool CheckChapterStage(int playChapter, int playStage, GuideQuestTableData guideQuestTableData = null)
	{
		// 인자로 들어오면 인자 들어온거로 체크하고 인자로 안들어오면 현재 진행중인걸 구해와서 체크한다.
		if (guideQuestTableData == null)
		{
			guideQuestTableData = GetCurrentGuideQuestTableData();
			if (guideQuestTableData == null)
				return false;
			eQuestClearType questClearType = Type2ClearType(guideQuestTableData.typeId);
			if (questClearType != eQuestClearType.ChapterStage)
				return false;
		}

		if (int.TryParse(guideQuestTableData.param, out int paramStage))
		{
			int chapter = paramStage / 100;
			int stage = paramStage % 100;
			if (playChapter > chapter)
				return true;
			else if (playChapter == chapter && playStage >= stage)
				return true;
		}
		return false;
	}

	public bool CheckPowerLevelUp(string actorId, GuideQuestTableData guideQuestTableData = null)
	{
		// 인자로 들어오면 인자 들어온거로 체크하고 인자로 안들어오면 현재 진행중인걸 구해와서 체크한다.
		if (guideQuestTableData == null)
		{
			guideQuestTableData = GetCurrentGuideQuestTableData();
			if (guideQuestTableData == null)
				return false;
			eQuestClearType questClearType = Type2ClearType(guideQuestTableData.typeId);
			if (questClearType != eQuestClearType.PowerLevel)
				return false;
		}

		if (guideQuestTableData.param == actorId)
			return true;
		return false;
	}

	public bool CheckExperienceLevel(string actorId, bool onlyPlay, GuideQuestTableData guideQuestTableData = null)
	{
		if (guideQuestTableData == null)
		{
			guideQuestTableData = GetCurrentGuideQuestTableData();
			if (guideQuestTableData == null)
				return false;
			eQuestClearType questClearType = Type2ClearType(guideQuestTableData.typeId);
			if (onlyPlay)
			{
				if (questClearType != eQuestClearType.ExperienceLevel1)
					return false;
			}
			else
			{
				if (questClearType != eQuestClearType.ExperienceLevel2)
					return false;
			}
		}

		if (string.IsNullOrEmpty(guideQuestTableData.param))
			return true;
		else if (actorId == guideQuestTableData.param)
			return true;
		return false;
	}

	public bool CheckIngameLevel(int level, GuideQuestTableData guideQuestTableData = null)
	{
		// 인자로 들어오면 인자 들어온거로 체크하고 인자로 안들어오면 현재 진행중인걸 구해와서 체크한다.
		if (guideQuestTableData == null)
		{
			guideQuestTableData = GetCurrentGuideQuestTableData();
			if (guideQuestTableData == null)
				return false;
			eQuestClearType questClearType = Type2ClearType(guideQuestTableData.typeId);
			if (questClearType != eQuestClearType.IngameLevel)
				return false;
		}

		if (int.TryParse(guideQuestTableData.param, out int paramLevel))
		{
			if (level >= paramLevel)
				return true;
		}
		return false;
	}

	public bool CheckEquipType(TimeSpaceData.eEquipSlotType equipSlotType, GuideQuestTableData guideQuestTableData = null)
	{
		// 인자로 들어오면 인자 들어온거로 체크하고 인자로 안들어오면 현재 진행중인걸 구해와서 체크한다.
		if (guideQuestTableData == null)
		{
			guideQuestTableData = GetCurrentGuideQuestTableData();
			if (guideQuestTableData == null)
				return false;
			eQuestClearType questClearType = Type2ClearType(guideQuestTableData.typeId);
			if (questClearType != eQuestClearType.EquipType)
				return false;
		}

		if (int.TryParse(guideQuestTableData.param, out int paramEquipType))
		{
			if (equipSlotType == (TimeSpaceData.eEquipSlotType)paramEquipType)
				return true;
		}
		return false;
	}
	#endregion
}