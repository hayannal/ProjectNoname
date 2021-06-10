﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using ActorStatusDefine;

public class DropProcessor : MonoBehaviour
{
	public enum eDropType
	{
		None,
		Exp,
		Gold,
		LevelPack,
		Heart,
		Gacha,
		Ultimate,
		Seal,
		Diamond,
		Origin,
		PowerPoint,
		Balance,
		ReturnScroll,
	}

	#region Static Fuction
	static bool FloatRange(eDropType dropType)
	{
		switch (dropType)
		{
			case eDropType.Gold:
			case eDropType.Ultimate:
				return true;
		}
		return false;
	}

	public static DropProcessor Drop(Transform rootTransform, string dropId, string addDropId, bool onAfterBattle, bool ignoreStartDrop)
	{
		//Debug.Log("dropId : " + dropId + " / addDropId : " + addDropId);

		Vector3 dropPosition = rootTransform.position;
		dropPosition.y = 0.0f;
		DropProcessor dropProcess = BattleInstanceManager.instance.GetCachedDropProcessor(dropPosition);
		dropProcess.onAfterBattle = onAfterBattle;

		if (!string.IsNullOrEmpty(dropId))
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(dropId);
			if (dropTableData != null)
				Drop(dropProcess, dropTableData);
		}

		bool invalid = false;
		if (addDropId == "9752476" && (ExperienceCanvas.instance == null || ExperienceCanvas.instance.gameObject == null || ExperienceCanvas.instance.gameObject.activeSelf == false))
			invalid = true;

		if (!string.IsNullOrEmpty(addDropId) && invalid == false)
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(addDropId);
			if (dropTableData != null)
				Drop(dropProcess, dropTableData);
		}

		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		// NodeWar 드랍은 lobby드랍처럼 처리해줘야한다.
		if (lobby == false && BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			lobby = true;
		if (lobby == false)
		{
			// drop event
			if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace) == false && ContentsManager.IsDropChapterStage(StageManager.instance.playChapter, StageManager.instance.playStage))
				dropProcess.AdjustDrop();
		}

		if (ignoreStartDrop == false)
			dropProcess.StartDrop();

		return dropProcess;
	}

	static void Drop(DropProcessor dropProcessor, DropTableData dropTableData)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);

		// NodeWar 드랍은 lobby드랍처럼 처리해줘야한다.
		if (lobby == false && BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			lobby = true;

		for (int i = 0; i < dropTableData.dropEnum.Length; ++i)
		{
			eDropType dropType = (eDropType)dropTableData.dropEnum[i];
			float probability = dropTableData.probability[i];

			// 보정처리 적용하는 것들.
			if (lobby)
			{
				// 드랍확률 보정처리.
				switch (dropType)
				{
					case eDropType.Origin:
						if (dropTableData.subValue[i] != "s" && dropTableData.subValue[i] != "x")
							break;

						// Origin의 경우 probability를 적혀있는대로 쓰면 안되고 현재 상황에 맞춰서 가공해야한다.
						probability = AdjustOriginDropProbability(probability, dropTableData.subValue[i] == "x");
						float weight = TableDataManager.instance.FindNotCharAdjustProb(DropManager.instance.GetCurrentNotSteakCharCount());
						// NotCharTable Adjust Prob 검증
						if (weight > 1.7f)
							CheatingListener.OnDetectCheatTable();
						probability *= weight;
						break;
					case eDropType.Gold:
					case eDropType.Diamond:
						if (dropTableData.dropId.Contains("Shop"))
							dropProcessor.AdjustDropDelay(0.05f);
						else if (dropTableData.dropId.Contains("Daily"))
							dropProcessor.AdjustDropDelay(0.1f);
						break;
					case eDropType.PowerPoint:
						// 천칭 메뉴가 열리고나서부터는 PowerPoint 마지막꺼 뽑을때 예외처리를 한다.
						if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Balance))
						{
							// 연속으로 붙어있는걸 고려해서 마지막인지 판단한다.
							bool lastPowerPoint = false;
							if ((i + 1) < dropTableData.dropEnum.Length)
							{
								eDropType nextDropType = (eDropType)dropTableData.dropEnum[i + 1];
								if (nextDropType != eDropType.PowerPoint)
									lastPowerPoint = true;
							}
							else
								lastPowerPoint = true;

							// 적용해도 되는 상황일때 55% 확률로 dropType을 강제로 교체한다.
							if (lastPowerPoint && Random.value < 0.55f)
								dropType = eDropType.Balance;
						}
						break;
				}
			}
			else
			{
				// suggested MaxPower Level 습득불가 처리
				switch (dropType)
				{
					case eDropType.Gold:
					case eDropType.Gacha:
						ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
						CharacterData mainCharacterData = PlayerData.instance.GetCharacterData(BattleInstanceManager.instance.playerActor.actorId);
						if (chapterTableData != null && mainCharacterData != null && mainCharacterData.powerLevel > chapterTableData.suggestedMaxPowerLevel)
							continue;
						break;
				}
				if (dropType == eDropType.Gacha)
				{
					// 최대량을 넘지 못하게 처리
					if (TimeSpaceData.instance.inventoryItemCount + DropManager.instance.droppedStageItemCount >= TimeSpaceData.InventoryRealMax)
						continue;
				}
				// 드랍확률 보정처리.
				switch (dropType)
				{
					case eDropType.Gacha:
						float itemDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.ItemDropRate);
						if (itemDropAdjust != 0.0f)
							probability *= (1.0f + itemDropAdjust);
						// 초반 플레이 예외처리.
						if (PlayerData.instance.highestPlayChapter <= 3 && PlayerData.instance.highestPlayChapter == PlayerData.instance.selectedChapter && DropManager.instance.droppedStageItemCount == 0)
							probability *= 2.3f;
						break;
					case eDropType.Heart:
						probability *= StageManager.instance.currentStageTableData.dropHeartAdjustment;
						float heartDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.HeartDropRate);
						if (heartDropAdjust != 0.0f)
							probability *= (1.0f + heartDropAdjust);
						break;
				}
			}
			if (Random.value > probability)
			{
				if (dropType == eDropType.Origin)
				{
					if (dropTableData.subValue[i] == "s" || dropTableData.subValue[i] == "x")
						++DropManager.instance.droppedNotStreakCharCount;
				}
				continue;
			}

			float floatValue = 0.0f;
			int intValue = 0;
			string stringValue = "";
			if (FloatRange(dropType))
				floatValue = Random.Range(dropTableData.minValue[i], dropTableData.maxValue[i]);
			else
			{
				int minValue = Mathf.RoundToInt(dropTableData.minValue[i]);
				int maxValue = Mathf.RoundToInt(dropTableData.maxValue[i]);
				intValue = Random.Range(minValue, maxValue + 1);

				// subValue 확인
				if (dropType == eDropType.Gacha)
				{
					switch (dropTableData.subValue[i])
					{
						case "e": stringValue = DropManager.instance.GetStageDropEquipId(); break;
						case "g": stringValue = DropManager.instance.GetGachaEquipId(); break;
						case "o": stringValue = DropManager.instance.GetGachaEquipIdByGrade(); break;
						case "n": stringValue = DropManager.instance.GetGachaEquipIdByGrade(1); break;
						case "j": stringValue = DropManager.instance.GetGachaEquipIdByGrade(2); break;
						case "q": stringValue = DropManager.instance.GetGachaEquipIdByGrade(3); break;
						case "k": stringValue = DropManager.instance.GetGachaEquipIdByType(8); break;
						case "w": stringValue = DropManager.instance.GetFullChaosRevertDropEquipId(); break;
					}
				}
				else if (dropType == eDropType.Origin)
				{
					switch (dropTableData.subValue[i])
					{
						case "s": stringValue = DropManager.instance.GetGachaCharacterId(false, true); break;
						case "x": stringValue = DropManager.instance.GetGachaCharacterId(true, false); break;
						case "l": stringValue = DropManager.instance.GetGachaCharacterId(false, false, 0); break;
						case "u": stringValue = DropManager.instance.GetGachaCharacterId(false, false, 1); break;
					}
					// Origin이나 아래 PowerPoint는 특정 조건에 의해(중복 방지라던지 등등) 안나올 수 있다. 이땐 건너뛰어야한다.
					if (stringValue == "")
						continue;
				}
				else if (dropType == eDropType.PowerPoint)
				{
					switch (dropTableData.subValue[i])
					{
						case "f": stringValue = DropManager.instance.GetGachaPowerPointId(true, false); break;
						default: stringValue = DropManager.instance.GetGachaPowerPointId(false, true); break;
					}
					if (stringValue == "")
						continue;
				}
				else if (dropType == eDropType.Diamond)
				{
					if (dropTableData.dropId.Contains("Reconstruct"))
					{
						if (EquipReconstructCanvas.instance != null)
						{
							int rewardAmount = EquipReconstructCanvas.instance.GetDiaAmount();
							if (rewardAmount == 1 || rewardAmount == 2)
								intValue = rewardAmount;
							else
								intValue = 3;
						}
					}
				}
			}

			switch (dropType)
			{
				case eDropType.Exp:
					DropExp(intValue);
					break;
				case eDropType.Gold:
					float goldDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.GoldDropAmount);
					if (goldDropAdjust != 0.0f)
						floatValue = floatValue * (1.0f + goldDropAdjust);
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddLobbyGold(floatValue);
					break;
				case eDropType.LevelPack:
					++DropManager.instance.reservedLevelPackCount;
					// check no hit levelpack
					if (BattleManager.instance.GetDamageCountOnStage() == 0)
						floatValue = 1.0f;
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					// 현재 레벨팩은 획득 가능인 상태로만 드랍되며 클리어 후 드랍되기 때문에
					// 드랍 오브젝트 획득에서 증가시키지 않고 여기서 증가시켜둔다.
					// 해당 층의 클리어 여부는 마지막 몹을 잡을때 바로 저장될테니 두번 잡을 순 없을거다.
					if (floatValue == 0.0f)
						ClientSaveData.instance.OnAddedRemainLevelPackCount(1);
					else
						ClientSaveData.instance.OnAddedRemainNoHitLevelPackCount(1);
					break;
				case eDropType.Heart:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
				case eDropType.Gacha:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddLobbyDropItemId(stringValue);
					else
					{
						// 사실 클라 세이브 기능 만들기전엔 템의 획득은 항상 DropObject에 가까이가서 먹을때만 처리했었는데
						// 이러다보니 획득 전에 강종되면 복구할 방법이 없었다.
						// 아이템은 가뜩이나 잘 나오지도 않는건데 나왔다가 사라져버리면 문의가 많이 올테니 세이브쪽만 예외처리하기로 한다.
						//
						// 마지막 몹을 잡는 시점에 클라 세이브가 이뤄지는데.. 이 타이밍에 해당 층에서 떨어진 템 혹은 떨어질 템에 획득 가능 표시가 적히게 된다.(onAfterBattle로 표시)
						// 이 템들을 리스트에 담아두기로 한다.
						//if (dropProcessor.onAfterBattle)
						//	ClientSaveData.instance.OnAddedDropItemId(stringValue);
						// 위의 코드다.
						// 그런데 여기서는 딱히 문제가 없었는데 BattleInstanceManager.OnDropLastMonsterInStage 함수에서 처리하기 애매한 문제가 발생했다.
						// 이미 드랍중인 프로세서의 경우 드랍되지 않은 요소들에만 적용해야하는데 이게 까다로웠던 것.
						//
						// 그래서 차라리 방향을 바꿔서..
						// 떨어져있는 드랍오브젝트를 클리어시점에 Add해둔다 정책과
						// 이후엔 onAfterBattle이 true인채로 생성되는 드랍오브젝트를 Add해둔다. 두 정책으로 가기로 한다.
						// 적어도 유저 눈에 드랍이 보이는 순간부터는 저장되니 목적은 충분히 달성됐다고 본다.
						// 이 구조의 최대 단점은 막타 때리자마자 끄면 드랍되어있는거 말곤 템을 못얻게 되는데 결국 이런 유저들의 대부분이 어뷰징일거라 이대로 가기로 한다.
					}
					break;
				case eDropType.Ultimate:
					DropSp(floatValue);
					break;
				case eDropType.Seal:
					if (PlayerData.instance.sharedDailyBoxOpened)
						break;
					if (PlayerData.instance.highestPlayChapter != PlayerData.instance.selectedChapter)
						break;
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
				case eDropType.Diamond:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddLobbyDia(intValue);
					break;
				case eDropType.Origin:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddOrigin(stringValue);
					break;
				case eDropType.PowerPoint:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddPowerPoint(stringValue, intValue);
					break;
				case eDropType.Balance:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddLobbyBalancePp(intValue);
					break;
				case eDropType.ReturnScroll:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
			}
		}
	}

	public static void DropSp(float dropSpValue)
	{
		if (dropSpValue == 0.0f)
			return;

		PlayerActor playerActor = BattleInstanceManager.instance.playerActor;
		if (ExperienceCanvas.instance != null && ExperienceCanvas.instance.gameObject.activeSelf)
			playerActor = CharacterListCanvas.instance.selectedPlayerActor;
		float spGainAddRate = playerActor.actorStatus.GetValue(eActorStatus.SpGainAddRate);
		if (spGainAddRate != 0.0f) dropSpValue *= (1.0f + spGainAddRate);
		playerActor.actorStatus.AddSP(dropSpValue);
	}

	static void DropExp(int dropExpValue)
	{
		if (dropExpValue == 0)
			return;

		//Debug.Log("dropExp : " + dropExpValue);

		DropManager.instance.StackDropExp(dropExpValue);
	}

	static float AdjustOriginDropProbability(float tableProbability, bool originDrop)
	{
		// 최초 1회는 무조건 캐릭터가 나와야한다. 이래야 간파울은 2렙에 제한걸릴테니 킵과 다른 일반캐릭터 1개가 pp를 나눠서 얻을 수 있게된다.
		int sum = PlayerData.instance.originOpenCount + PlayerData.instance.characterBoxOpenCount;
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		if (sum == 0 && listGrantInfo.Count == 0)
			return 1.0f;

		// Origin은 현재 캐릭터의 보유 여부에 따라 보정처리를 해서 드랍 확률이 결정된다.
		// 기본값은 0.046인데 그걸 공식 하나 적용해서 보정하는 형태. 공식은 다음과 같다.
		// 새 드랍확률 = 테이블 드랍확률 * 조정후가중치합 / 조정전가중치합
		//
		// 이건 FixedChar 를 다 뽑기 전이든 아니든 동일하기 때문에 로직이 나눠지지 않는다.
		// CharacterBoxConfirmCanvas 에 있는 돋보기 코드를 기반으로 만드니 최대한 비슷하게 해둔다.
		float sumWeight = 0.0f;
		float adjustSumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.actorTable.dataArray[i].charGachaWeight;
			if (weight <= 0.0f)
				continue;

			// LegendAdjustWeightByCount는 sumWeight에 반영되어야한다.
			float weightForSum = TableDataManager.instance.actorTable.dataArray[i].charGachaWeight;
			if (CharacterData.IsUseLegendWeight(TableDataManager.instance.actorTable.dataArray[i]))
				weightForSum *= CharacterData.GetLegendAdjustWeightByCount();
			sumWeight += weightForSum;

			bool useAdjustWeight = false;
			// 여기선 listGrantInfo도 체크해야해서 GetableOrigin를 사용하지 않는다.
			//if (DropManager.instance.GetableOrigin(TableDataManager.instance.actorTable.dataArray[i].actorId, ref useAdjustWeight) == false)
			//	weight = 0.0f;
			CharacterData characterData = PlayerData.instance.GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId);
			if (characterData == null && listGrantInfo.Contains(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
			{
				if (TableDataManager.instance.actorTable.dataArray[i].actorId == "Actor2103")
					weight = 0.0f;
				else
					useAdjustWeight = true;
			}
			else
			{
				if (characterData != null && characterData.transcendPoint < CharacterData.GetTranscendPoint(CharacterData.TranscendLevelMax))
				{ }
				else
					weight = 0.0f;
			}

			float adjustWeight = (useAdjustWeight ? (weight * TableDataManager.instance.actorTable.dataArray[i].noHaveTimes) : weight);

			if (CharacterData.IsUseLegendWeight(TableDataManager.instance.actorTable.dataArray[i]))
			{
				adjustWeight *= DropManager.GetGradeAdjust(TableDataManager.instance.actorTable.dataArray[i]);
				adjustWeight *= CharacterData.GetLegendAdjustWeightByCount();
				if (originDrop == false)
					adjustWeight *= TableDataManager.instance.FindNotLegendCharAdjustWeight(DropManager.instance.GetCurrentNotStreakLegendCharCount());
			}
			else
			{
				// 전설이 아닐때는 미보유인지 아닌지를 구분해서 특별한 보정처리를 한다.
				if (useAdjustWeight)
				{
					// 미보유
					if (originDrop) adjustWeight *= 3.0f;
					else adjustWeight *= 1.5f;
					adjustWeight += TableDataManager.instance.actorTable.dataArray[i].charGachaWeight * (DropManager.GetGradeAdjust(TableDataManager.instance.actorTable.dataArray[i]) - 1.0f);
				}
				else
					adjustWeight *= DropManager.GetGradeAdjust(TableDataManager.instance.actorTable.dataArray[i]);
			}

			adjustSumWeight += adjustWeight;
		}
		if (sumWeight == 0.0f)
			return 0.0f;

		return tableProbability * adjustSumWeight / sumWeight;
	}

	public static float GetOriginProbability(string dropId)
	{
		DropTableData dropTableData = TableDataManager.instance.FindDropTableData(dropId);
		if (dropTableData != null)
		{
			for (int i = 0; i < dropTableData.dropEnum.Length; ++i)
			{
				eDropType dropType = (eDropType)dropTableData.dropEnum[i];
				float probability = dropTableData.probability[i];

				// 드랍확률 보정처리.
				switch (dropType)
				{
					case eDropType.Origin:
						float weight = TableDataManager.instance.FindNotCharAdjustProb(DropManager.instance.GetCurrentNotSteakCharCount());
						probability *= weight;
						return probability;
				}
			}
		}
		return 0.0f;
	}
	#endregion

	class DropObjectInfo
	{
		public eDropType dropType;
		public float floatValue;
		public int intValue;
		public string stringValue;
	}
	List<DropObjectInfo> _listDropObjectInfo = new List<DropObjectInfo>();

	public void Add(eDropType dropType, float floatValue, int intValue, string stringValue)
	{
		DropObjectInfo newInfo = null;
		switch (dropType)
		{
			case eDropType.Exp:
				break;
			case eDropType.Gold:
				int randomCount = Random.Range(4, 7);
				if (_adjustDropDelay > 0.0f) randomCount = Random.Range(20, 30);
				float goldDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.GoldDropAmount);
				if (goldDropAdjust > 0.0f) randomCount += 1;
				for (int i = 0; i < randomCount; ++i)
				{
					newInfo = new DropObjectInfo();
					newInfo.dropType = dropType;
					newInfo.floatValue = floatValue / randomCount;
					_listDropObjectInfo.Add(newInfo);
				}
				break;
			case eDropType.LevelPack:
			case eDropType.Heart:
			case eDropType.Gacha:
			case eDropType.Seal:
			case eDropType.Origin:
			case eDropType.ReturnScroll:
				for (int i = 0; i < intValue; ++i)
				{
					newInfo = new DropObjectInfo();
					newInfo.dropType = dropType;
					if (newInfo.dropType == eDropType.LevelPack) newInfo.floatValue = floatValue;
					newInfo.intValue = 1;
					newInfo.stringValue = stringValue;
					_listDropObjectInfo.Add(newInfo);
				}
				if (dropType == eDropType.Gacha)
				{
					// 가차인 경우 사용할 오브젝트를 미리 로드 걸어둔다.
					// 앞에 골드들 떨어지는 동안 최대한 로딩이 끝나길 기대.
					EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(stringValue);
					if (equipTableData != null)
						AddressableAssetLoadManager.GetAddressableGameObject(equipTableData.prefabAddress, "Equip", null);
				}
				break;
			case eDropType.Ultimate:
				break;
			case eDropType.Diamond:
				int splitCount = Random.Range(3, 5);
				if (intValue < 3) splitCount = intValue;
				else if (intValue <= 10) splitCount = 3;
				if (_adjustDropDelay >= 0.1f) splitCount = Random.Range(5, 10);
				else if (_adjustDropDelay > 0.0f) splitCount = Random.Range(12, 20);
				int quotient = intValue / splitCount;
				int remainder = intValue % splitCount;
				for (int i = 0; i < splitCount; ++i)
				{
					int currentCount = quotient + ((i < remainder) ? 1 : 0);
					newInfo = new DropObjectInfo();
					newInfo.dropType = dropType;
					newInfo.intValue = currentCount;
					newInfo.stringValue = stringValue;
					_listDropObjectInfo.Add(newInfo);
				}
				break;
			case eDropType.PowerPoint:
			case eDropType.Balance:
				newInfo = new DropObjectInfo();
				newInfo.dropType = dropType;
				newInfo.intValue = intValue;
				newInfo.stringValue = stringValue;
				_listDropObjectInfo.Add(newInfo);
				break;
		}
	}

	void OnDisable()
	{
		_adjustDropDelay = 0.0f;
		_adjustDropRange = 0.0f;
		_listDropObjectInfo.Clear();
	}

	public void StartDrop()
	{
		if (_listDropObjectInfo.Count == 0)
		{
			if (BattleInstanceManager.instance.IsLastDropProcessorInStage(this))
			{
				if (DropManager.instance.IsExistReservedLastDropObject())
					DropManager.instance.ApplyLastDropObject();
			}

			gameObject.SetActive(false);
			return;
		}

		Timing.RunCoroutine(DropProcess());
	}

	
	IEnumerator<float> DropProcess()
	{
		float delay = 0.2f;
		if (_adjustDropDelay > 0.0f)
			delay = _adjustDropDelay;

		DropObject dropObjectForException = null;
		for (int i = 0; i < _listDropObjectInfo.Count; ++i)
		{
			string prefabName = string.Format("Drop{0}", _listDropObjectInfo[i].dropType.ToString());
			if (_listDropObjectInfo[i].dropType == eDropType.LevelPack && _listDropObjectInfo[i].floatValue > 0.0f)
				prefabName = string.Format("NoHit{0}", prefabName);

			GameObject dropObjectPrefab = GetDropObjectPrefab(prefabName);
			if (dropObjectPrefab == null)
				continue;

			DropObject cachedItem = BattleInstanceManager.instance.GetCachedDropObject(dropObjectPrefab, GetRandomDropPosition(), Quaternion.identity);
			cachedItem.Initialize(_listDropObjectInfo[i].dropType, _listDropObjectInfo[i].floatValue, _listDropObjectInfo[i].intValue, _listDropObjectInfo[i].stringValue, onAfterBattle);

			// 여러개의 드랍프로세서가 서로 다른 드랍오브젝트를 만들고 있을때는 누가 마지막 골드 드랍인지를 알수가 없게된다.
			// 그래서 생성시 라스트를 등록하고 있다가
			if (cachedItem.getAfterAllDropAnimationInStage && cachedItem.useIncreaseSearchRange == false)
				DropManager.instance.ReserveLastDropObject(cachedItem);

			// 저 아래 템을 못얻는 상황에 대한 예외처리를 위한 코드다.
			if (cachedItem.getAfterAllDropAnimationInStage && cachedItem.useIncreaseSearchRange)
				dropObjectForException = cachedItem;

			// 마지막 스폰이 끝날땐 드랍프로세서가 바로 사라지게 yield return 하지 않는다.
			if (i < _listDropObjectInfo.Count - 1)
				yield return Timing.WaitForSeconds(delay);

			if (this == null)
				yield break;
		}

		// 스테이지 내의 마지막 드랍 프로세서가 종료될때
		// 마지막으로 등록된 드랍 오브젝트를 진짜 라스트 드랍으로 지정하기로 한다.
		// 그럼 이게 발동될때 떨어져있던 골드나 레벨팩이 흡수될거다.
		if (BattleInstanceManager.instance.IsLastDropProcessorInStage(this))
		{
			if (DropManager.instance.IsExistReservedLastDropObject())
				DropManager.instance.ApplyLastDropObject();
			else
			{
				// 템을 못얻는 상황에서는 골드가 드랍되지 않기 때문에 _reservedLastDropObject값이 null인 상태일거다.
				// 이럴땐 어쩔 수 없이 마지막 드랍 프로세서의 드랍 오브젝트 리스트의 역 순서대로 검사해서
				// getAfterAllDropAnimationInStage 값이 true인 오브젝트를 강제로 LastDropObject로 설정해야한다.
				if (dropObjectForException != null)
				{
					DropManager.instance.ReserveLastDropObject(dropObjectForException);
					DropManager.instance.ApplyLastDropObject();
				}
			}
		}
		else
		{
			// EvilLich를 골드 보상 얻을 수 없는 높은 레벨로 깨면 ReservedLastDropObject는 없는데 인장은 얻어야할 상황이 생길 수 있다.
			// 이때를 대비해서 예외처리 코드 추가.
			// 이땐 소환된 몹보다 EvilLich를 먼저 잡을때라서 ApplyLastDropObject를 호출하면 안된다.
			if (dropObjectForException != null && DropManager.instance.IsExistReservedLastDropObject() == false)
				DropManager.instance.ReserveLastDropObject(dropObjectForException);
		}
		
		gameObject.SetActive(false);
	}

	// temp code
	GameObject GetDropObjectPrefab(string prefabName)
	{
		for (int i = 0; i < DropObjectGroup.instance.dropObjectPrefabList.Length; ++i)
		{
			if (DropObjectGroup.instance.dropObjectPrefabList[i].name == prefabName)
				return DropObjectGroup.instance.dropObjectPrefabList[i];
		}
		return null;
	}

	float _adjustDropRange = 0.0f;
	public void AdjustDropRange(float adjustRadius)
	{
		_adjustDropRange = adjustRadius;
	}

	Vector3 GetRandomDropPosition()
	{
		bool checkLocalPlayerPosition = false;
		float defaultRadius = 2.0f;
		if (_adjustDropRange > 0.0f)
		{
			defaultRadius = _adjustDropRange;
			checkLocalPlayerPosition = true;
		}

		Vector3 localPlayerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		int tryBreakCount = 0;
		while (true)
		{
			Vector2 randomOffset = Random.insideUnitCircle * Random.Range(0.2f, 1.0f) * defaultRadius;
			Vector3 desirePosition = cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);

			Vector3 diff = desirePosition - localPlayerPosition;
			if (checkLocalPlayerPosition == false || diff.x * diff.x + diff.z * diff.z > 1.5f)
				return desirePosition;

			++tryBreakCount;
			if (tryBreakCount > 200)
			{
				Debug.LogError("GetRandomDropPosition Error. Not found valid random position.");
				return desirePosition;
			}
		}
	}

	float _adjustDropDelay = 0.0f;
	public void AdjustDropDelay(float adjustDelay)
	{
		_adjustDropDelay = adjustDelay;
	}

	// 마지막 몹을 잡을때 기존몹의 드랍 프로세서가 진행중일 수 있다.
	// 이땐 afterBattle을 전달받아서 기록해두고 전달한다.
	public bool onAfterBattle { get; set; }




	#region Drop Event
	public void AdjustDrop()
	{
		// 최초로 2챕터 10스테이지를 깰때 강제 드랍처리를 해준다.
		// 먼저 리스트 내에 드랍할 장비가 있는지 판단.
		int gachaCount = 0;
		int firstGachaIndex = -1;
		for (int i = 0; i < _listDropObjectInfo.Count; ++i)
		{
			if (_listDropObjectInfo[i].dropType == eDropType.Gacha)
			{
				if (firstGachaIndex == -1)
					firstGachaIndex = i;
				++gachaCount;
			}
		}
		if (gachaCount == 0)
			Add(eDropType.Gacha, 0.0f, 1, "Equip3302");
		else if (gachaCount >= 1)
		{
			// 최초 드랍이니 한개만 드랍되게 마지막걸 지운다.
			for (int i = _listDropObjectInfo.Count - 1; i >= 0; --i)
			{
				if (_listDropObjectInfo[i].dropType == eDropType.Gacha)
				{
					if (firstGachaIndex != i)
						_listDropObjectInfo.RemoveAt(i);
					else
						_listDropObjectInfo[i].stringValue = "Equip3302";
				}
			}
		}

		// 이미 확정인 상태니 세이브 역시 Equip0001 하나 들어있도록 똑같은 정보로 해주면 되는데
		// 어차피 2-10 도달 전까진 템이 드랍되지 않기 때문에 Clear함수를 호출할 필요도 없다. 그래도 혹시 모르니 호출.
		ClientSaveData.instance.ClearDropItemList();

		// 이 장비템은 DropObject로 만들어질때 onAfterBattle 켜진채로 만들어질거라 거기서 Add처리 될거다.
		// 그러나 이 시점은 DropObject가 화면에 보일때라서 그 전에 클라를 강종하면 날아가게 된다.
		// 이걸 방지하기 위해 미리 Add시켜놔야하는데,
		// 문젠 여기서 한번 되고 또 DropObject가 추가될때 한번 더 해서 총 2개가 세이브데이터가 저장되게 된다.
		// 이걸 막기위해 AdjustDrop이 수행되는 시점의 DropObject는 무시하기로 한다.
		ClientSaveData.instance.OnAddedDropItemId("Equip3302");
	}
	#endregion



	



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
