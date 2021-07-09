using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class BossBattleEnterCanvas : MonoBehaviour
{
	public static BossBattleEnterCanvas instance;

	public Transform titleTextTransform;
	public Text levelText;
	public GameObject newObject;
	public GameObject changeDifficultyButtonObject;
	public Transform previewRootTransform;
	public Text bossNameText;
	public Button bossInfoButton;
	public Transform xpLevelButtonTransform;
	public Text xpLevelText;
	public Text xpLevelExpText;
	public Image xpLevelExpImage;

	public Text suggestPowerLevelText;
	public Text stagePenaltyText;
	public Text selectResultText;

	public Text priceText;
	public GameObject buttonObject;
	public Image priceButtonImage;
	public GameObject priceOnIconImageObject;
	public GameObject priceOffIconImageObject;
	//public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public Text remainEnergyText;

	public SortButton sortButton;
	SortButton.eSortType _currentSortType;

	public RectTransform alarmRootTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SwapCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);

		if (EventManager.instance.reservedOpenBossBattleEvent)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("BossBattleUI_PopupName"), UIString.instance.GetString("BossBattleUI_PopupDesc"), UIString.instance.GetString("BossBattleUI_PopupMore"), null, 0.785f);
			});
			EventManager.instance.reservedOpenBossBattleEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.boss);
		}
	}

	void OnEnable()
	{
		if (sortButton.onChangedCallback == null)
		{
			int sortType = PlayerPrefs.GetInt("_SwapSort", 0);
			_currentSortType = (SortButton.eSortType)sortType;
			sortButton.SetSortType(_currentSortType);
			sortButton.onChangedCallback = OnChangedSortType;
		}

		RefreshInfo();
		RefreshGrid(true);

		if (LobbyCanvas.instance != null)
		{
			LobbyCanvas.instance.subMenuCanvasGroup.alpha = 0.0f;
			LobbyCanvas.instance.dotMainMenuButton.gameObject.SetActive(false);
		}

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (ContentsData.instance.newBossRefreshed)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BossUI_NewAppear"), 3.0f);
			ContentsData.instance.newBossRefreshed = false;
		}
	}

	void OnDisable()
	{
		if (LobbyCanvas.instance != null)
		{
			LobbyCanvas.instance.subMenuCanvasGroup.alpha = 1.0f;
			LobbyCanvas.instance.dotMainMenuButton.gameObject.SetActive(true);
		}

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
	}

	void Update()
	{
		UpdateEnergy();
	}

	GameObject _cachedPreviewObject;
	StageTableData _bossStageTableData;
	MapTableData _bossMapTableData;
	ChapterTableData _bossChapterTableData;
	ObscuredInt _selectedDifficulty;
	ObscuredInt _clearDifficulty;
	void RefreshInfo()
	{
		if (_cachedPreviewObject != null)
		{
			_cachedPreviewObject.SetActive(false);
			_cachedPreviewObject = null;
		}

		int currentBossId = ContentsData.instance.bossBattleId;
		if (currentBossId == 0)
		{
			// 0이라면 처음 보스배틀을 시작하는 유저일거다.
			// 1번 몬스터를 가져와서 셋팅한다.
			currentBossId = 1;
		}

		_bossBattleTableData = TableDataManager.instance.FindBossBattleData(currentBossId);
		if (_bossBattleTableData == null)
			return;

		int clearDifficulty = ContentsData.instance.GetBossBattleClearDifficulty(currentBossId.ToString());
		_selectedDifficulty = ContentsData.instance.GetBossBattleSelectedDifficulty(currentBossId.ToString());
		if (_selectedDifficulty == 0)
		{
			// _selectedDifficulty이면 한번도 플레이 안했다는거니 bossBattleTable에서 시작 챕터를 가져와야한다.
			_selectedDifficulty = _bossBattleTableData.chapter;
		}
		// 선택한게 클리어난이도+1 보다 크면 뭔가 이상한거다. 조정해준다.
		// 이제 챕터의 난이도에서 시작하게 되면서 이 로직을 사용할 수 없게 되었다.
		//if (_selectedDifficulty > (clearDifficulty + 1))
		//	_selectedDifficulty = (clearDifficulty + 1);

		int bossBattleCount = ContentsData.instance.GetBossBattleCount(currentBossId.ToString());


		StageTableData bossStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_bossBattleTableData.chapter, _bossBattleTableData.stage, false);
		if (bossStageTableData == null)
			return;
		MapTableData bossMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(bossStageTableData.firstFixedMap);
		if (bossMapTableData == null)
			return;
		// 챕터 테이블은 권장 레벨 표기를 위한거라 선택된 난이도로 구해오는게 맞다.
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(_selectedDifficulty);
		if (chapterTableData == null)
			return;

		_bossStageTableData = bossStageTableData;
		_bossMapTableData = bossMapTableData;
		_bossChapterTableData = chapterTableData;
		levelText.text = string.Format("<size=24>DIFFICULTY</size> {0}", _selectedDifficulty);
		newObject.SetActive(_selectedDifficulty > clearDifficulty);

		int selectableDifficultyCount = clearDifficulty - _bossBattleTableData.chapter + 2;
		changeDifficultyButtonObject.SetActive(selectableDifficultyCount > 1);
		_clearDifficulty = clearDifficulty;
		if (changeDifficultyButtonObject.activeSelf)
		{
			AlarmObject.Hide(alarmRootTransform);
			if (_selectedDifficulty != (clearDifficulty + 1) && ChangeDifficultyCanvasListItem.CheckSelectable(_clearDifficulty + 1) == 0)
				AlarmObject.Show(alarmRootTransform, false, false, true);
		}

		if (string.IsNullOrEmpty(bossMapTableData.bossName) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", bossMapTableData.bossName), "Preview", (prefab) =>
			{
				_cachedPreviewObject = UIInstanceManager.instance.GetCachedObject(prefab, previewRootTransform);
			});
		}
		bossNameText.SetLocalizedText(UIString.instance.GetString(bossMapTableData.nameId));

		RefreshBossBattleCount(bossBattleCount);

		// 패널티를 구할땐 그냥 스테이지 테이블에서 구해오면 안되고 선택된 난이도의 1층을 구해와서 처리해야한다.
		StageTableData penaltyStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_selectedDifficulty, 1, false);
		if (penaltyStageTableData == null)
			return;

		stagePenaltyText.gameObject.SetActive(false);
		string penaltyString = ChapterCanvas.GetPenaltyString(penaltyStageTableData);
		if (string.IsNullOrEmpty(penaltyString) == false)
		{
			stagePenaltyText.SetLocalizedText(penaltyString);
			stagePenaltyText.gameObject.SetActive(true);
		}

		selectResultText.text = "";

		// 파워레벨은 항상 표시
		string rangeString = UIString.instance.GetString("GameUI_NumberRange", chapterTableData.suggestedPowerLevel, chapterTableData.suggestedMaxPowerLevel);
		suggestPowerLevelText.SetLocalizedText(string.Format("{0} {1}", UIString.instance.GetString("GameUI_SuggestedPowerLevel"), rangeString));

		RefreshEnergy();
		RefreshPrice();
	}

	BossBattleTableData _bossBattleTableData;
	public BossBattleTableData GetBossBattleTableData() { return _bossBattleTableData; }

	#region Preload Reopen
	public static void PreloadReadyToReopen()
	{
		// 보스전 하고와서 되돌아오자마자 바로 보스전 열때 끊기는거 같아서 넣는 프리로드
		// 위 RefreshInfo에서 하는 코드와 비슷해서 근처에 둔다.
		AddressableAssetLoadManager.GetAddressableGameObject("BossBattleEnterCanvas", "Canvas");

		int currentBossId = ContentsData.instance.bossBattleId;
		BossBattleTableData bossBattleTableData = TableDataManager.instance.FindBossBattleData(currentBossId);
		if (bossBattleTableData == null)
			return;
		StageTableData bossStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(bossBattleTableData.chapter, bossBattleTableData.stage, false);
		if (bossStageTableData == null)
			return;
		MapTableData bossMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(bossStageTableData.firstFixedMap);
		if (bossMapTableData == null)
			return;
		if (string.IsNullOrEmpty(bossMapTableData.bossName) == false)
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", bossMapTableData.bossName), "Preview");
	}
	#endregion

	ObscuredInt _xpLevel = 1;
	public int GetXpLevel() { return _xpLevel; }
	ObscuredInt _xp = 0;
	public int GetXp() { return _xp; }
	void RefreshBossBattleCount(int count)
	{
		// 현재 카운트가 속하는 테이블 구해와서 레벨 및 경험치로 표시.
		_xp = count;
		_xpLevel = 1;
		int maxXpLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleLevel");
		int level = 0;
		float percent = 0.0f;
		int currentPeriodExp = 0;
		int currentPeriodExpMax = 0;
		for (int i = _xpLevel; i < TableDataManager.instance.bossExpTable.dataArray.Length; ++i)
		{
			if (count < TableDataManager.instance.bossExpTable.dataArray[i].requiredAccumulatedExp)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				percent = (float)currentPeriodExp / (float)currentPeriodExpMax;
				level = TableDataManager.instance.bossExpTable.dataArray[i].xpLevel - 1;
				break;
			}
			if (TableDataManager.instance.bossExpTable.dataArray[i].xpLevel >= maxXpLevel)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				level = maxXpLevel;
				percent = 1.0f;
				break;
			}
		}

		_xpLevel = level;
		string xpLevelString = "";
		if (level == maxXpLevel)
		{
			xpLevelString = UIString.instance.GetString("GameUI_Lv", "Max");
			xpLevelExpImage.color = DailyFreeItem.GetGoldTextColor();
		}
		else
		{
			xpLevelString = UIString.instance.GetString("GameUI_Lv", level);
			xpLevelExpImage.color = Color.white;
		}
		xpLevelText.text = string.Format("XP {0}", xpLevelString);
		xpLevelExpText.text = string.Format("{0} / {1}", currentPeriodExp, currentPeriodExpMax);
		xpLevelExpImage.fillAmount = percent;
	}

	void RefreshPrice()
	{
		// 가격
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToPlay");
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.energy < price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		//priceGrayscaleEffect.enabled = disablePrice;
		priceOnIconImageObject.SetActive(!disablePrice);
		priceOffIconImageObject.SetActive(disablePrice);
		_price = price;
	}

	public void OnChangeDifficulty(int difficulty)
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("BossUI_ChangeDifficulty"), 2.0f);
		ContentsData.instance.SelectBossBattleDifficulty(difficulty);
		RefreshInfo();
		RefreshGrid(false);
		OnClickListItem(_selectedActorId);
	}


	void OnChangedSortType(SortButton.eSortType sortType)
	{
		_currentSortType = sortType;
		int sortTypeValue = (int)sortType;
		PlayerPrefs.SetInt("_SwapSort", sortTypeValue);
		RefreshGrid(false);
	}

	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	void RefreshGrid(bool onEnable)
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();

		if (_bossChapterTableData == null || _bossMapTableData == null)
			return;

		string[] suggestedActorIdList = _bossMapTableData.suggestedActorId;
		RefreshPenaltyPowerSource();

		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		switch (_currentSortType)
		{
			case SortButton.eSortType.PowerLevel:
				listCharacterData.Sort(sortButton.comparisonPowerLevel);
				break;
			case SortButton.eSortType.PowerLevelDescending:
				listCharacterData.Sort(sortButton.comparisonPowerLevelDescending);
				break;
			case SortButton.eSortType.Transcend:
				listCharacterData.Sort(sortButton.comparisonTranscendLevel);
				break;
			case SortButton.eSortType.PowerSource:
				listCharacterData.Sort(sortButton.comparisonPowerSource);
				break;
			case SortButton.eSortType.Grade:
				listCharacterData.Sort(sortButton.comparisonGrade);
				break;
		}

		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(listCharacterData[i].actorId, listCharacterData[i].powerLevel, SwapCanvasListItem.GetPowerLevelColorState(listCharacterData[i]), listCharacterData[i].transcendLevel, _bossChapterTableData.suggestedPowerLevel, suggestedActorIdList, _listCachedPenaltyPowerSource, OnClickListItem);
			_listSwapCanvasListItem.Add(swapCanvasListItem);
		}

		if (onEnable)
		{
			string cachedLastCharacter = GetCachedLastCharacter();
			if (string.IsNullOrEmpty(cachedLastCharacter))
				OnClickListItem(BattleInstanceManager.instance.playerActor.actorId);
			else
				OnClickListItem(cachedLastCharacter);
		}
		else
			OnClickListItem(_selectedActorId);
	}

	List<int> _listCachedPenaltyPowerSource = new List<int>();
	void RefreshPenaltyPowerSource()
	{
		_listCachedPenaltyPowerSource.Clear();

		if (_bossStageTableData == null)
			return;

		// 이미 랜덤하게 부여받은 패널티를 바탕으로 스왑창에서 패널티 부여받은 상태니 고를때 조심해라 띄우는 처리인데
		// 아직 로비에 있는 상태라 결정이 안된 상태다.

		StageTableData penaltyStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_selectedDifficulty, 1, false);
		if (penaltyStageTableData == null)
			return;

		stagePenaltyText.gameObject.SetActive(false);
		string penaltyString = ChapterCanvas.GetPenaltyString(penaltyStageTableData);
		if (string.IsNullOrEmpty(penaltyString) == false)
		{
			stagePenaltyText.SetLocalizedText(penaltyString);
			stagePenaltyText.gameObject.SetActive(true);
		}

		for (int k = 0; k < penaltyStageTableData.stagePenaltyId.Length; ++k)
		{
			StagePenaltyTableData stagePenaltyTableData = TableDataManager.instance.FindStagePenaltyTableData(penaltyStageTableData.stagePenaltyId[k]);
			if (stagePenaltyTableData == null)
				continue;

			for (int i = 0; i < stagePenaltyTableData.affectorValueId.Length; ++i)
			{
				AffectorValueLevelTableData affectorValueLevelTableData = TableDataManager.instance.FindAffectorValueLevelTableData(stagePenaltyTableData.affectorValueId[i], 1);
				if (affectorValueLevelTableData == null)
					continue;

				for (int j = 0; j < affectorValueLevelTableData.conditionValueId.Length; ++j)
				{
					ConditionValueTableData conditionValueTableData = TableDataManager.instance.FindConditionValueTableData(affectorValueLevelTableData.conditionValueId[j]);
					if (conditionValueTableData == null)
						continue;

					if ((Condition.eConditionType)conditionValueTableData.conditionId == Condition.eConditionType.DefenderPowerSource && (Condition.eCompareType)conditionValueTableData.compareType == Condition.eCompareType.Equal)
					{
						int.TryParse(conditionValueTableData.value, out int intValue);
						if (_listCachedPenaltyPowerSource.Contains(intValue) == false)
							_listCachedPenaltyPowerSource.Add(intValue);
					}
				}
			}
		}
	}

	public void OnClickListItem(string actorId)
	{
		_selectedActorId = actorId;

		bool recommanded = false;
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
		{
			bool showSelectObject = (_listSwapCanvasListItem[i].actorId == actorId);
			_listSwapCanvasListItem[i].ShowSelectObject(showSelectObject);
			if (showSelectObject)
				recommanded = _listSwapCanvasListItem[i].recommandedText.gameObject.activeSelf;
		}

		string firstText = "";
		if (MainSceneBuilder.instance.lobby == false && BattleInstanceManager.instance.IsInBattlePlayerList(actorId))
			firstText = UIString.instance.GetString("GameUI_FirstSwapHealNotApplied");

		string secondText = "";
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar()) { }
		else if (_bossChapterTableData != null)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
			if (characterData.powerLevel > _bossChapterTableData.suggestedMaxPowerLevel)
				secondText = UIString.instance.GetString("BossUI_TooPowerfulToReward"); // GameUI_TooPowerfulToReward
			else if (characterData.powerLevel < _bossChapterTableData.suggestedPowerLevel && recommanded)
				secondText = UIString.instance.GetString("GameUI_TooWeakToBoss");
		}
		bool firstResult = string.IsNullOrEmpty(firstText);
		bool secondResult = string.IsNullOrEmpty(secondText);
		if (firstResult && secondResult)
			selectResultText.text = "";
		else if (firstResult == false && secondResult)
			selectResultText.SetLocalizedText(firstText);
		else if (firstResult && secondResult == false)
			selectResultText.SetLocalizedText(secondText);
		else
			selectResultText.SetLocalizedText(string.Format("{0}\n{1}", firstText, secondText));
	}

	public void OnClickTitleInfoButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("BossBattleUI_TitleMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickBossInfoButton()
	{
		if (_bossMapTableData == null)
			return;

		string suggestString = GetSuggestString(_bossMapTableData.descriptionId, _bossMapTableData.suggestedActorId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, suggestString, 200, bossInfoButton.transform, new Vector2(0.0f, -35.0f));
	}

	// SwapCanvas에서 그대로 가져옴
	StringBuilder _stringBuilderFull = new StringBuilder();
	StringBuilder _stringBuilderActor = new StringBuilder();
	string GetSuggestString(string descriptionId, string[] suggestedActorIdList)
	{
		_stringBuilderFull.Remove(0, _stringBuilderFull.Length);
		_stringBuilderActor.Remove(0, _stringBuilderActor.Length);
		for (int i = 0; i < suggestedActorIdList.Length; ++i)
		{
			string actorId = suggestedActorIdList[i];
			string actorName = CharacterData.GetLowNameByActorId(actorId);
			if (string.IsNullOrEmpty(actorName))
				continue;

			bool applyPenalty = false;
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
			if (_listCachedPenaltyPowerSource != null && _listCachedPenaltyPowerSource.Contains(actorTableData.powerSource)) applyPenalty = true;

			//if (PlayerData.instance.ContainsActor(actorId) == false)
			//	continue;
			if (_stringBuilderActor.Length > 0)
				_stringBuilderActor.Append(", ");
			_stringBuilderActor.Append(applyPenalty ? "<color=#707070>" : "<color=#00AB00>");
			_stringBuilderActor.Append(actorName);
			_stringBuilderActor.Append("</color>");
		}
		if (_stringBuilderActor.Length == 0)
		{
			bool applyPenalty = false;
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(suggestedActorIdList[0]);
			if (_listCachedPenaltyPowerSource != null && _listCachedPenaltyPowerSource.Contains(actorTableData.powerSource)) applyPenalty = true;

			_stringBuilderActor.Append(applyPenalty ? "<color=#707070>" : "<color=#00AB00>");
			_stringBuilderActor.Append(CharacterData.GetLowNameByActorId(suggestedActorIdList[0]));
			_stringBuilderActor.Append("</color>");
		}
		_stringBuilderFull.AppendFormat(UIString.instance.GetString(descriptionId), _stringBuilderActor.ToString());
		return _stringBuilderFull.ToString();
	}

	#region Energy
	void RefreshEnergy()
	{
		remainEnergyText.text = CurrencyData.instance.energy.ToString();
		_updateEnergy = false;
		if (CurrencyData.instance.energy < CurrencyData.instance.energyMax)
			_updateEnergy = true;
	}

	bool _updateEnergy = false;
	float _remainEnergyTime = 0.0f;
	void UpdateEnergy()
	{
		if (_updateEnergy == false)
			return;

		// 밖에 게이트필라가 있다보니 1초에 한번씩만 갱신시켜주기로 한다.
		_remainEnergyTime -= Time.deltaTime;
		if (_remainEnergyTime < 0.0f)
		{
			_remainEnergyTime = 1.0f;
			RefreshEnergy();
		}
	}
	#endregion

	#region Sub Menu
	public void OnClickRefreshButton()
	{
		if (CurrencyData.instance.energy == 0)
		{
			ShowRefillEnergyCanvas(true);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("BossBattleRefreshCanvas", null);
	}

	public void OnClickChangeDifficultyButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ChangeDifficultyCanvas", () =>
		{
			ChangeDifficultyCanvas.instance.RefreshInfo(_bossStageTableData.chapter, _selectedDifficulty, _clearDifficulty);
		});
	}

	public void OnClickXpLevelInfoButton()
	{
		string xpLevelString1 = UIString.instance.GetString("BossBattleUI_XpLevelMore1");
		string xpLevelString2 = UIString.instance.GetString("BossBattleUI_XpLevelMore2", _xpLevel);

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0}\n\n{1}", xpLevelString1, xpLevelString2), 300, xpLevelButtonTransform, new Vector2(0.0f, -35.0f));
	}
	#endregion










	int _price;
	string _selectedActorId;
	public void OnClickYesButton()
	{
		if (DelayedLoadingCanvas.IsShow())
			return;
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}
		// check lobby energy
		if (CheckEnergy() == false)
			return;



		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		// 진입처리
		// 인풋부터 막고
		DelayedLoadingCanvas.Show(true);

		// 입장 패킷 보내기전에 필수로 해야하는 것들 위주로 셋팅한다.
		// 나머진 패킷 받고 재진입 다 완료한 후에 셋팅하는거로 한다.

		// 이동해야할 스테이지의 로비를 미리 로드
		StageManager.instance.ReloadBossBattle(_bossStageTableData, _bossMapTableData);

		// 선택한 캐릭이 현재 캐릭터와 다르다면 바꾸는 작업을 수행해야하는데
		// 이미 만들었던 플레이어 캐릭터라면 다시 만들필요 없으니 가져다쓰고 없으면 어드레스 로딩을 시작해야한다.
		// 재진입과는 달리 캐릭터가 이미 만들었다가 꺼있을수도 있으니 로드하기전에 확인해야한다.
		if (BattleInstanceManager.instance.playerActor.actorId != _selectedActorId)
		{
			standbySwapBattleActor = true;

			// 생성할 필요가 없을땐
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_selectedActorId);
			if (playerActor != null)
				_cachedPlayerActorForChange = playerActor;
			else
			{
				// 생성되어있지 않은 캐릭이라면 로드를 걸어둔다. 여기서 해야 패킷 보내서 받는 시간까지 로딩에 쓸 수 있다.
				AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(_selectedActorId), "Character", OnLoadedPlayerActor);
			}
		}

		// 한가지 추가로 해줄게 있는데 레벨팩 관련 이펙트다.
		PreloadLevelPackOnStartStageEffect();

		// 이동 프로세스
		Timing.RunCoroutine(MoveProcess());
	}

	bool IsLoadedPlayerActor { get { return _playerActorPrefab != null; } }
	GameObject _playerActorPrefab;
	void OnLoadedPlayerActor(GameObject prefab)
	{
		_playerActorPrefab = prefab;
	}

	bool standbySwapBattleActor { get; set; }
	PlayerActor _cachedPlayerActorForChange;
	void ChangeBattleActor()
	{
		// 이미 생성되어있는 상태였다면 이 캐릭으로 바꿔주면 끝이다.
		if (_cachedPlayerActorForChange != null)
		{
			_cachedPlayerActorForChange.gameObject.SetActive(true);
			BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
			_cachedPlayerActorForChange.OnChangedMainCharacter();
			_cachedPlayerActorForChange = null;
			standbySwapBattleActor = false;
			return;
		}


#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(_playerActorPrefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(_playerActorPrefab);
#endif

		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
		playerActor.OnChangedMainCharacter();
		standbySwapBattleActor = false;
	}

	List<string> _listCachedLevelPackForEffectPreload = null;
	void PreloadLevelPackOnStartStageEffect()
	{
		// 다른 전투들과 달리 보스전에서는 레벨팩을 랜덤으로 부여받게 되는데 부여받는 시점이 하필 OnStartBattle 시점이라서
		// OnSpawnFlag랑 거의 비슷하게 호출되면서 레벨팩 어펙터 적용되기 전에 이펙트가 로딩되지 않는 문제가 발생했다.
		// 그렇다고 이제와서 랜덤으로 부여받는걸 바꾸기도 뭐하고
		// 랜덤으로 부여받는 시점을 땡겨서 미리 정해놓고 들어가려면 또 로직에 손대야해서
		// 차라리 스테이지 시작 조건으로 되어있는 레벨팩(현재는 두개뿐이다. 조우시 힐장판, 조우시 공속업)의 이펙트만 미리 선별해서 프리로드 해두기로 한다.

		if (_listCachedLevelPackForEffectPreload != null)
			return;

		_listCachedLevelPackForEffectPreload = new List<string>();
		_listCachedLevelPackForEffectPreload.Add("HealAreaOnEncounter");
		_listCachedLevelPackForEffectPreload.Add("AtkSpeedUpOnEncounter");
		for (int i = 0; i < _listCachedLevelPackForEffectPreload.Count; ++i)
		{
			LevelPackTableData levelPackTableData = TableDataManager.instance.FindLevelPackTableData(_listCachedLevelPackForEffectPreload[i]);
			if (levelPackTableData == null)
				continue;

			for (int j = 0; j < levelPackTableData.effectAddress.Length; ++j)
			{
				AddressableAssetLoadManager.GetAddressableGameObject(levelPackTableData.effectAddress[j], "CommonEffect", (prefab) =>
				{
					BattleInstanceManager.instance.AddCommonPoolPreloadObjectList(prefab);
				});
			}
		}
	}



	bool CheckEnergy()
	{
		// GatePillar에서 했던거 가져와서 쓴다.
		if (CurrencyData.instance.energy < BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToPlay"))
		{
			ShowRefillEnergyCanvas();
			return false;
		}

		return true;
	}

	void ShowRefillEnergyCanvas(bool refreshRefill = false)
	{
		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () => {

			if (this == null) return;
			if (gameObject == null) return;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false) return;

			string title = UIString.instance.GetString("SystemUI_Info");
			string message = "";
			int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RefillEnergyDiamond");
			int energyToPlay = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToBoss");
			if (refreshRefill)
				message = UIString.instance.GetString("BossUI_RefillEnergy", BossBattleRefreshCanvas.REFRESH_PRICE, energyToPlay);
			else
				message = UIString.instance.GetString("GameUI_RefillEnergy", energyToPlay, energyToPlay);
			ConfirmSpendCanvas.instance.ShowCanvas(true, title, message, CurrencyData.eCurrencyType.Diamond, price, true, () =>
			{
				if (CurrencyData.instance.dia < price)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
					return;
				}
				PlayFabApiManager.instance.RequestRefillEnergy(price, energyToPlay, () =>
				{
					CurrencySmallInfoCanvas.RefreshInfo();
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					RefreshPrice();
					RefreshEnergy();
				});
			});
		});
	}

	// 패킷을 날려놓고 페이드아웃쯤에 오는 서버 응답에 따라 처리가 나뉜다. 
	bool _waitEnterServerResponse;
	bool _enterBossBattleServerFailure;
	bool _networkFailure;
	void PrepareBossBattle()
	{
		int useAmount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RequiredEnergyToPlay");
		// 클라이언트에서 먼저 삭제한 다음
		CurrencyData.instance.UseEnergy(useAmount);
		if (EnergyGaugeCanvas.instance != null)
			EnergyGaugeCanvas.instance.RefreshEnergy();

		// 입장패킷 보내서 서버로부터 제대로 응답오는지 기다려야한다.
		PlayFabApiManager.instance.RequestEnterBossBattle(_selectedDifficulty, (serverFailure) =>
		{
			DelayedLoadingCanvas.Show(false);
			if (_waitEnterServerResponse)
			{
				// 에너지가 없는데 도전
				_enterBossBattleServerFailure = serverFailure;
				_waitEnterServerResponse = false;
				ContentsData.instance.SelectBossBattleDifficulty(_selectedDifficulty);
			}
		}, () =>
		{
			DelayedLoadingCanvas.Show(false);
			if (_waitEnterServerResponse)
			{
				// 그외 접속불가 네트워크 에러
				_networkFailure = true;
				_waitEnterServerResponse = false;
			}
		});
		_waitEnterServerResponse = true;
	}

	List<GameObject> _listCachingObject;
	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> MoveProcess()
	{
		if (_processing)
			yield break;
		if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf && GatePillar.instance.processing)
			yield break;

		_processing = true;

		// 보안 이슈로 Enter Flag는 받아둔다. 기존꺼랑 겹치지 않게 별도의 enterFlag다.
		PrepareBossBattle();

#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(gameObject);
#endif
		CustomRenderer.instance.bloom.AdjustDirtIntensity(1.5f);

		yield return Timing.WaitForSeconds(0.5f);

		LobbyCanvas.instance.FadeOutQuestInfoGroup(0.0f, 0.2f, false, true);
		if (OptionManager.instance.darkMode == 1)
			FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		else
			FadeCanvas.instance.FadeOut(0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		// 여기서 캔버스는 닫는다.
		gameObject.SetActive(false);

		// DotMainMenu도 닫아야 조명부터 리셋시켜둘 수 있다.
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		while (_waitEnterServerResponse)
			yield return Timing.WaitForOneFrame;
		if (_enterBossBattleServerFailure || _networkFailure)
		{
			FadeCanvas.instance.FadeIn(0.4f);
			// 서버 에러 오면 안된다. 뭔가 잘못된거다.
			if (_enterBossBattleServerFailure)
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("BossBattleUI_Wrong"), 2.0f);
			_enterBossBattleServerFailure = false;
			_networkFailure = false;
			// 알파가 어느정도 빠지면 _processing을 풀어준다.
			yield return Timing.WaitForSeconds(0.2f);
			_processing = false;
			yield break;
		}
		if (standbySwapBattleActor)
		{
			if (_cachedPlayerActorForChange == null)
			{
				while (IsLoadedPlayerActor == false)
					yield return Timing.WaitForOneFrame;
			}
			ChangeBattleActor();
			// 한번도 여기서 캐릭터를 바꾼적이 없어서 그동안은 필요없던 코드긴 한데
			// 처음 캐릭을 생성하고나서 Start가 호출되고 나야 ActorStatus를 사용할 수 있다. 그러기 위해서 생성하고나서 한프레임 쉬고 가도록 한다.
			yield return Timing.WaitForOneFrame;
		}
		Timing.RunCoroutine(CurrencyData.instance.DelayedSyncEnergyRechargeTime(5.0f));
		while (MainSceneBuilder.instance.IsDoneLateInitialized() == false)
			yield return Timing.WaitForOneFrame;
		if (TitleCanvas.instance != null)
			TitleCanvas.instance.gameObject.SetActive(false);
		if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
			GatePillar.instance.gameObject.SetActive(false);
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.gameObject.activeSelf)
			NodeWarPortal.instance.gameObject.SetActive(false);
		MainSceneBuilder.instance.OnExitLobby();
		BattleManager.instance.Initialize(BattleManager.eBattleMode.BossBattle);
		BattleManager.instance.OnStartBattle();
		SoundManager.instance.PlayBattleBgm(BattleInstanceManager.instance.playerActor.actorId);
		RecordLastCharacter();

		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return Timing.WaitForOneFrame;
		CustomRenderer.instance.bloom.ResetDirtIntensity();
		StageManager.instance.DeactiveCurrentMap();
		StageManager.instance.MoveToBossBattle(_bossStageTableData, _bossMapTableData, _selectedDifficulty);

		#region Effect Preload
		// 캐릭터를 바꿔서 들어가면 프리로딩이 필요하다. 프레임 끊기는 경험이 좋지 않으니 막아야한다.
		SoundManager.instance.SetUiVolume(0.0f);
		yield return Timing.WaitForOneFrame;
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], Vector3.right, Quaternion.identity));
		}
		yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;
		if (_listCachingObject != null)
		{
			for (int i = 0; i < _listCachingObject.Count; ++i)
				_listCachingObject[i].SetActive(false);
			_listCachingObject.Clear();
		}
		// 캐싱 오브젝트 끌때 바로 복구
		SoundManager.instance.SetUiVolume(OptionManager.instance.systemVolume);
		#endregion

		// 레벨업 이펙트 없이 곧바로 보스 진입이니 
		FadeCanvas.instance.FadeIn(0.8f);

		_processing = false;
	}

	#region Record Last Character
	void RecordLastCharacter()
	{
		string key = _bossBattleTableData.num.ToString();
		string value = _selectedActorId;
		ObscuredPrefs.SetString(string.Format("_bbEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId), value);
	}

	string GetCachedLastCharacter()
	{
		string key = _bossBattleTableData.num.ToString();
		return ObscuredPrefs.GetString(string.Format("_bbEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId));
	}
	#endregion
}