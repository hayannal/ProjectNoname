using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class InvasionEnterCanvas : MonoBehaviour
{
	public static InvasionEnterCanvas instance;

	public Transform titleTextTransform;
	public Text remainTimeText;

	public Text difficultyText;
	public GameObject changeDifficultyButtonObject;
	public Transform previewRootTransform;

	public Text rewardDescText;
	public GameObject goldIconObject;
	public GameObject diaIconObject;
	public GameObject energyIconObject;
	public GameObject returnScrollIconObject;
	public GameObject equipBoxObject;
	public GameObject equipBigBoxObject;
	public GameObject characterBoxObject;

	public Text limitPowerLevelText;
	public Text selectResultText;

	public Text enterText;
	public GameObject enterButtonObject;
	public Image enterButtonImage;
	public Text priceText;
	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public Text enterCountText;
	public GameObject oneMoreChanceTextObject;

	// 로드해줄 곳이 없다. 여기에 둔다.
	public GameObject nodeWarEndPortalEffectPrefab;

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

		if (EventManager.instance.reservedOpenInvasionEvent)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("InvasionUI_PopupName"), UIString.instance.GetString("InvasionUI_PopupDesc"), UIString.instance.GetString("InvasionUI_PopupMore"), null, 0.785f);
			});
			EventManager.instance.reservedOpenInvasionEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.invasion);
			LobbyCanvas.instance.HideSubMenuAlarmObject(1);
		}
	}

	bool _onEnable = false;
	void OnEnable()
	{
		_onEnable = true;

		// RefreshGrid를 먼저 호출해서 현재 진입할 수 있는 캐릭터들을 추려야한다. 나머지는 그 이후에 가능.
		RefreshGrid();
		RefreshInfo();

		if (LobbyCanvas.instance != null)
		{
			LobbyCanvas.instance.subMenuCanvasGroup.alpha = 0.0f;
			LobbyCanvas.instance.dotMainMenuButton.gameObject.SetActive(false);
		}

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		_onEnable = false;
	}

	void OnDisable()
	{
		if (CurrencySmallInfoCanvas.IsShow())
		{
			CurrencySmallInfoCanvas.Show(false);
			_showCurrencySmallInfoCanvas = false;
		}

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
		// 날짜가 지나는 타이밍에는 캐릭터 사용 내역이 초기화 되었는지 확인하고 날짜를 갱신시켜야한다.
		UpdateRemainTime();
		UpdateRefresh();
	}



	List<CharacterData> _listCharacterData = new List<CharacterData>();
	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();

		// 현재 요일의 난이도 1짜리를 구해와서 후보 리스트부터 확인하는게 먼저다.
		InvasionTableData invasionTableData = TableDataManager.instance.FindInvasionTableData((int)ServerTime.UtcNow.DayOfWeek, 1);
		if (invasionTableData == null)
			return;

		_listCharacterData.Clear();
		for (int i = 0; i < invasionTableData.limitActorId.Length; ++i)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(invasionTableData.limitActorId[i]);
			if (characterData == null)
				continue;
			_listCharacterData.Add(characterData);
		}

		_listCharacterData.Sort(delegate (CharacterData x, CharacterData y)
		{
			// 제일 먼저 오늘 출전한 캐릭터인지를 
			if (ContentsData.instance.listInvasionEnteredActorId.Contains(x.actorId) == false && ContentsData.instance.listInvasionEnteredActorId.Contains(y.actorId)) return -1;
			else if (ContentsData.instance.listInvasionEnteredActorId.Contains(x.actorId) && ContentsData.instance.listInvasionEnteredActorId.Contains(y.actorId) == false) return 1;

			// 이후 검사는 기본적인 파워레벨 순서로 한다.
			if (x.powerLevel > y.powerLevel) return -1;
			else if (x.powerLevel < y.powerLevel) return 1;
			ActorTableData xActorTableData = TableDataManager.instance.FindActorTableData(x.actorId);
			ActorTableData yActorTableData = TableDataManager.instance.FindActorTableData(y.actorId);
			if (xActorTableData != null && yActorTableData != null)
			{
				if (xActorTableData.grade > yActorTableData.grade) return -1;
				else if (xActorTableData.grade < yActorTableData.grade) return 1;
			}
			if (x.transcendLevel > y.transcendLevel) return -1;
			else if (x.transcendLevel < y.transcendLevel) return 1;
			if (xActorTableData != null && yActorTableData != null)
			{
				if (xActorTableData.orderIndex < yActorTableData.orderIndex) return -1;
				else if (xActorTableData.orderIndex > yActorTableData.orderIndex) return 1;
			}
			return 0;
		});

		// 보유한 캐릭터 먼저 그리드에 표시하고
		string firstSelectableActorId = "";
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(_listCharacterData[i].actorId, _listCharacterData[i].powerLevel, SwapCanvasListItem.GetPowerLevelColorState(_listCharacterData[i]), _listCharacterData[i].transcendLevel, 0, null, null, OnClickListItem);
			_listSwapCanvasListItem.Add(swapCanvasListItem);

			if (swapCanvasListItem.blackObject.activeSelf == false && string.IsNullOrEmpty(firstSelectableActorId))
				firstSelectableActorId = _listCharacterData[i].actorId;
		}

		// 보유하지 않은 캐릭터를 그 뒤에 추가로 표시한다.
		for (int i = 0; i < invasionTableData.limitActorId.Length; ++i)
		{
			if (PlayerData.instance.ContainsActor(invasionTableData.limitActorId[i]))
				continue;

			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(invasionTableData.limitActorId[i]);
			if (actorTableData == null)
				continue;

			SwapCanvasListItem swapCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			swapCanvasListItem.Initialize(invasionTableData.limitActorId[i], 0, SwapCanvasListItem.ePowerLevelColorState.Normal, 0, 0, null, null, OnClickListItem);
			_listSwapCanvasListItem.Add(swapCanvasListItem);
		}

		if (string.IsNullOrEmpty(firstSelectableActorId))
			selectResultText.text = "";
		else
			OnClickListItem(firstSelectableActorId);
	}

	public int GetTodayClearPoint()
	{
		// 오늘 출전가능한 멤버들의 파워레벨 합산이 클리어포인트다.
		int sumLevel = 0;
		for (int i = 0; i < _listCharacterData.Count; ++i)
			sumLevel += _listCharacterData[i].powerLevel;
		return sumLevel;
	}

	ObscuredInt _highestDifficulty = 1;
	void RefreshHighestPowerLevel()
	{
		int max = 1;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
		{
			if (max < PlayerData.instance.listCharacterData[i].powerLevel)
				max = PlayerData.instance.listCharacterData[i].powerLevel;
		}
		_highestDifficulty = GetHighestPlayableDifficulty(max);
		changeDifficultyButtonObject.SetActive(_highestDifficulty > 1);
	}

	int GetHighestPlayableDifficulty(int powerLevel)
	{
		//if (powerLevel < 3)
		//	return 1;
		//else if (powerLevel < 5)
		//	return 2;
		//else if (powerLevel < 7)
		//	return 3;
		//else if (powerLevel < 9)
		//	return 4;
		//else
		//	return 5;
		// 이제 파워레벨과 difficulty가 1:1로 매칭되면서 그냥 리턴하면 된다.
		return powerLevel;
	}

	ObscuredInt _selectedDifficulty;
	ObscuredInt _limitPowerLevel;
	void RefreshInfo()
	{
		// 요일별 타이머가 떠야하는거라서 직접 이렇게 설정
		_dailyResetTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
		_lastDayOfWeek = ServerTime.UtcNow.DayOfWeek;
		_needUpdate = true;

		// RefreshGrid(true)를 통해 진입해야할 캐릭터들이 골라진 상태일거다.
		// 두번째 할일은 현재 가지고 있는 캐릭터들 중 최고레벨을 골라서 어느 난이도까지 표시할지 정해야한다.
		RefreshHighestPowerLevel();

		// EnterCount
		RefreshEnterCount();

		// Difficulty
		RefreshDifficultyInfo();
	}

	GameObject _cachedPreviewObject;
	bool _previewCachingProceed = false;
	StageTableData _invasionStageTableData;
	MapTableData _invasionMapTableData;
	void RefreshDifficultyInfo()
	{
		// 만약 하나도 진입할 수 없는 상태라면 _selectedActorId가 빈 상태일테니 예외처리 해주면 된다.
		if (string.IsNullOrEmpty(_selectedActorId))
		{
			_selectedDifficulty = _highestDifficulty;
		}
		else
		{
			// 진입할 수 있는 캐릭터가 선택되어있다면 해당 요일과 해당 캐릭터를 바탕으로 
			// 낮춰놨던 난이도가 있는지 캐싱데이터를 찾아보고
			int cachedLastDifficulty = GetCachedLastDifficulty();
			if (cachedLastDifficulty == 0)
			{
				// 캐싱된 난이도가 없을땐 해당 캐릭터로 들어갈 수 있는 최대 난이도를 선택하고
				_selectedDifficulty = GetHighestPlayableDifficulty(_selectedActorPowerLevel);
			}
			else
			{
				// 캐싱된 난이도가 있을땐
				_selectedDifficulty = cachedLastDifficulty;
			}
		}

		difficultyText.text = ChangeInvasionDifficultyCanvasListItem.GetDifficultyText(_selectedDifficulty);

		// 난이도까지 선택이 됐다면 이제 나머지 정보들을 불러올 수 있다.
		_invasionTableData = TableDataManager.instance.FindInvasionTableData((int)ServerTime.UtcNow.DayOfWeek, _selectedDifficulty);
		if (_invasionTableData == null)
			return;

		// 파워레벨은 제한으로 표시
		_limitPowerLevel = _invasionTableData.limitPower;
		limitPowerLevelText.SetLocalizedText(string.Format("{0} {1}", UIString.instance.GetString("InvasionUI_EntryPowerLevel"), _limitPowerLevel));

		// Reward 표기
		RefreshReward();

		StageTableData invasionStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_invasionTableData.chapter, _invasionTableData.stage, false);
		if (invasionStageTableData == null)
			return;
		MapTableData invasionMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(invasionStageTableData.firstFixedMap);
		if (invasionMapTableData == null)
			return;

		_invasionStageTableData = invasionStageTableData;
		_invasionMapTableData = invasionMapTableData;

		if (_cachedPreviewObject != null)
		{
			_cachedPreviewObject.SetActive(false);
			_cachedPreviewObject = null;
		}

		if (string.IsNullOrEmpty(invasionMapTableData.bossName) == false && _previewCachingProceed == false)
		{
			_previewCachingProceed = true;
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", invasionMapTableData.bossName), "Preview", (prefab) =>
			{
				_cachedPreviewObject = UIInstanceManager.instance.GetCachedObject(prefab, previewRootTransform);
				_previewCachingProceed = false;
			});
		}
	}

	InvasionTableData _invasionTableData;
	public InvasionTableData GetInvasionTableData() { return _invasionTableData; }

	#region Preload Reopen
	public static void PreloadReadyToReopen()
	{
		// 보스전 프리로드때와 비슷하게 처리
		// 위 RefreshInfo에서 하는 코드와 비슷해서 근처에 둔다.
		AddressableAssetLoadManager.GetAddressableGameObject("InvasionEnterCanvas", "Canvas");

		// 아무 난이도 구해와도 보스 프리뷰 같을거라서 강제로 1로 셋팅
		InvasionTableData invasionTableData = TableDataManager.instance.FindInvasionTableData((int)ServerTime.UtcNow.DayOfWeek, 1);
		if (invasionTableData == null)
			return;

		StageTableData invasionStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(invasionTableData.chapter, invasionTableData.stage, false);
		if (invasionStageTableData == null)
			return;
		MapTableData invasionMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(invasionStageTableData.firstFixedMap);
		if (invasionMapTableData == null)
			return;
		if (string.IsNullOrEmpty(invasionMapTableData.bossName) == false)
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", invasionMapTableData.bossName), "Preview");
	}
	#endregion

	void RefreshReward()
	{
		rewardDescText.SetLocalizedText(UIString.instance.GetString(_invasionTableData.rewardTitle));

		goldIconObject.SetActive(false);
		diaIconObject.SetActive(false);
		energyIconObject.SetActive(false);
		returnScrollIconObject.SetActive(false);
		equipBoxObject.SetActive(false);
		equipBigBoxObject.SetActive(false);
		characterBoxObject.SetActive(false);

		if (_invasionTableData.rewardType == "cu")
		{
			if (_invasionTableData.rewardValue == CurrencyData.GoldCode())
				goldIconObject.SetActive(true);
			else if (_invasionTableData.rewardValue == CurrencyData.DiamondCode())
				diaIconObject.SetActive(true);
			else if (_invasionTableData.rewardValue == CurrencyData.EnergyCode())
				energyIconObject.SetActive(true);
			else
				returnScrollIconObject.SetActive(true);
		}
		else if (_invasionTableData.rewardType == "be")
		{
			if (_invasionTableData.rewardValue == "3")
				equipBigBoxObject.SetActive(true);
			else
				equipBoxObject.SetActive(true);
		}
		else if (_invasionTableData.rewardType == "bm")
		{
			//if (_invasionTableData.rewardCount >= 5)
			//	equipBigBoxObject.SetActive(true);
			//else
				equipBoxObject.SetActive(true);
		}
		else if (_invasionTableData.rewardType == "bc")
		{
			characterBoxObject.SetActive(true);
		}
	}

	public void OnClickRewardInfoButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.InvasionRewardInfo, UIString.instance.GetString(_invasionTableData.rewardMore), 290, goldIconObject.transform, new Vector2(0.0f, -40.0f));
	}

	bool _showCurrencySmallInfoCanvas = false;
	public static int ENTER_COUNT_MAX = 3;
	void RefreshEnterCount()
	{
		// 오늘 입장한 캐릭터 리스트를 구해오면 입장 횟수를 구할 수 있다.
		int currentEnterCount = ContentsData.instance.listInvasionEnteredActorId.Count;
		if (currentEnterCount < ENTER_COUNT_MAX)
		{
			enterButtonObject.SetActive(true);
			priceButtonObject.SetActive(false);
			bool disablePrice = (_selectedActorPowerLevel < _limitPowerLevel || string.IsNullOrEmpty(_selectedActorId));
			enterButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			enterText.color = !disablePrice ? Color.white : Color.gray;
			enterCountText.text = string.Format("{0} / {1}", currentEnterCount, ENTER_COUNT_MAX);
			enterCountText.color = !disablePrice ? Color.white : Color.gray;
			oneMoreChanceTextObject.SetActive(false);

			if (CurrencySmallInfoCanvas.IsShow())
			{
				CurrencySmallInfoCanvas.Show(false);
				_showCurrencySmallInfoCanvas = false;
			}
		}
		else if (currentEnterCount == ENTER_COUNT_MAX)
		{
			enterButtonObject.SetActive(false);
			priceButtonObject.SetActive(true);
			enterCountText.text = "";
			oneMoreChanceTextObject.SetActive(true);
			RefreshPrice();

			if (CurrencySmallInfoCanvas.IsShow() == false && _showCurrencySmallInfoCanvas == false)
			{
				CurrencySmallInfoCanvas.Show(true);
				_showCurrencySmallInfoCanvas = true;
			}
		}
		else if (currentEnterCount == (ENTER_COUNT_MAX + 1))
		{
			enterButtonObject.SetActive(true);
			priceButtonObject.SetActive(false);
			enterButtonImage.color = ColorUtil.halfGray;
			enterText.color = Color.gray;
			enterCountText.text = string.Format("{0} / {1}", ENTER_COUNT_MAX, ENTER_COUNT_MAX);
			enterCountText.color = Color.gray;
			oneMoreChanceTextObject.SetActive(false);

			if (CurrencySmallInfoCanvas.IsShow())
			{
				CurrencySmallInfoCanvas.Show(false);
				_showCurrencySmallInfoCanvas = false;
			}
		}
	}

	void RefreshPrice()
	{
		// 가격
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("InvasionDiamond");
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < price || _selectedActorPowerLevel < _limitPowerLevel || string.IsNullOrEmpty(_selectedActorId));
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;
		_price = price;
	}

	public void OnChangeDifficulty(int difficulty)
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("BossUI_ChangeDifficulty"), 2.0f);

		_selectedDifficulty = difficulty;
		if (difficulty < GetHighestPlayableDifficulty(_selectedActorPowerLevel))
			RecordLastDifficulty();
		else if (difficulty == GetHighestPlayableDifficulty(_selectedActorPowerLevel))
			DeleteLastDifficulty();
		RefreshDifficultyInfo();
		//RefreshGrid(false);
		//OnClickListItem(_selectedActorId);

		RefreshSelectResultText();
		RefreshEnterCount();
	}

	public void OnClickListItem(string actorId)
	{
		if (string.IsNullOrEmpty(actorId))
		{
			// null이란건 아예 선택조차 안됐을 경우다. 이때는 합류하지 않았다고 뜨기보다 메세지 없는게 더 나아보인다.
			return;
		}

		if (PlayerData.instance.ContainsActor(actorId) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_NotJoin"), 2.0f);
			return;
		}

		// 이미 출전했던 캐릭터라면
		if (ContentsData.instance.listInvasionEnteredActorId.Contains(actorId))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_AlreadyEntered"), 2.0f);
			return;
		}

		_selectedActorId = actorId;
		_selectedActorPowerLevel = PlayerData.instance.GetCharacterData(actorId).powerLevel;

		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
		{
			bool showSelectObject = (_listSwapCanvasListItem[i].actorId == actorId);
			_listSwapCanvasListItem[i].ShowSelectObject(showSelectObject);
		}

		// 캐릭터에 맞는 난이도를 자동으로 선택해줘야하는데 이때 난이도가 변경된다면 난이도 변경 메세지를 표시해야한다.
		int selectedDifficulty = _selectedDifficulty;
		RefreshDifficultyInfo();
		if (_onEnable == false && selectedDifficulty != _selectedDifficulty)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("BossUI_ChangeDifficulty"), 2.0f);

		RefreshSelectResultText();
		RefreshEnterCount();
	}

	void RefreshSelectResultText()
	{
		selectResultText.text = "";

		// 캐릭을 바꾸거나 난이도를 바꾸거나 할때 서로의 선택에는 영향을 주진 않으나
		// 현재 고른것의 결과에 대해서는 결과 텍스트로 알려주는게 필요하다.
		if (_selectedActorPowerLevel < _limitPowerLevel)
			selectResultText.SetLocalizedText(UIString.instance.GetString("InvasionUI_CannotEnterPower"));
	}

	public void OnClickTitleInfoButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("InvasionUI_TitleMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	#region Sub Menu
	public void OnClickChangeDifficultyButton()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		UIInstanceManager.instance.ShowCanvasAsync("ChangeInvasionDifficultyCanvas", () =>
		{
			ChangeInvasionDifficultyCanvas.instance.RefreshInfo(_highestDifficulty, _selectedDifficulty, _selectedActorPowerLevel);
		});
	}
	#endregion





	

	int _price;
	string _selectedActorId;
	int _selectedActorPowerLevel;
	public void OnClickYesButton()
	{
		if (DelayedLoadingCanvas.IsShow())
			return;
		int currentEnterCount = ContentsData.instance.listInvasionEnteredActorId.Count;
		if (currentEnterCount == (ENTER_COUNT_MAX + 1))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_ThreeDone"), 2.0f);
			return;
		}
		if (string.IsNullOrEmpty(_selectedActorId))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_NoOneSelected"), 2.0f);
			return;
		}
		if (_selectedActorPowerLevel < _limitPowerLevel)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_CannotEnterPowerToast"), 2.0f);
			return;
		}
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}
		if (currentEnterCount == ENTER_COUNT_MAX && CurrencyData.instance.dia < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}
		if (currentEnterCount < (ENTER_COUNT_MAX + 1))
		{
			TimeSpan remainTimeSpan = _dailyResetTime - ServerTime.UtcNow;
			if (remainTimeSpan < TimeSpan.FromSeconds(30.0))
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_TooSoonInit"), 2.0f);
				return;
			}
		}




		// 진입처리
		// 인풋부터 막고
		DelayedLoadingCanvas.Show(true);

		// 입장 패킷 보내기전에 필수로 해야하는 것들 위주로 셋팅한다.
		// 나머진 패킷 받고 재진입 다 완료한 후에 셋팅하는거로 한다.

		// 이동해야할 스테이지의 로비를 미리 로드. 보스 배틀때 썼던 함수와 동일하다.
		StageManager.instance.ReloadBossBattle(_invasionStageTableData, _invasionMapTableData);

		if (BattleInstanceManager.instance.playerActor.actorId != _selectedActorId)
		{
			standbySwapBattleActor = true;
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_selectedActorId);
			if (playerActor != null)
				_cachedPlayerActorForChange = playerActor;
			else
			{
				// 생성되어있지 않은 캐릭이라면 로드를 걸어둔다. 여기서 해야 패킷 보내서 받는 시간까지 로딩에 쓸 수 있다.
				AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(_selectedActorId), "Character", OnLoadedPlayerActor);
			}
		}

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

	// 패킷을 날려놓고 페이드아웃쯤에 오는 서버 응답에 따라 처리가 나뉜다. 
	bool _waitEnterServerResponse;
	bool _enterInvasionServerFailure;
	bool _networkFailure;
	void PrepareInvasion()
	{
		// 입장패킷 보내서 서버로부터 제대로 응답오는지 기다려야한다.
		PlayFabApiManager.instance.RequestEnterInvasion(_selectedActorId, _selectedDifficulty, (serverFailure) =>
		{
			DelayedLoadingCanvas.Show(false);
			if (_waitEnterServerResponse)
			{
				// 제한 횟수 넘어서 도전
				_enterInvasionServerFailure = serverFailure;
				_waitEnterServerResponse = false;
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
		PrepareInvasion();

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
		if (_enterInvasionServerFailure || _networkFailure)
		{
			FadeCanvas.instance.FadeIn(0.4f);
			// 서버 에러 오면 안된다. 뭔가 잘못된거다.
			if (_enterInvasionServerFailure)
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_Wrong"), 2.0f);
			_enterInvasionServerFailure = false;
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
			yield return Timing.WaitForOneFrame;
		}
		while (MainSceneBuilder.instance.IsDoneLateInitialized() == false)
			yield return Timing.WaitForOneFrame;
		if (TitleCanvas.instance != null)
			TitleCanvas.instance.gameObject.SetActive(false);
		if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
			GatePillar.instance.gameObject.SetActive(false);
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.gameObject.activeSelf)
			NodeWarPortal.instance.gameObject.SetActive(false);
		MainSceneBuilder.instance.OnExitLobby();
		BattleManager.instance.Initialize(BattleManager.eBattleMode.Invasion);
		BattleManager.instance.OnStartBattle();
		SoundManager.instance.PlayBattleBgm(BattleInstanceManager.instance.playerActor.actorId);

		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return Timing.WaitForOneFrame;
		CustomRenderer.instance.bloom.ResetDirtIntensity();
		StageManager.instance.DeactiveCurrentMap();
		StageManager.instance.MoveToInvasion(_invasionStageTableData, _invasionMapTableData);

		#region Effect Preload
		// 캐릭터를 바꿔서 들어가면 프리로딩이 필요하다. 프레임 끊기는 경험이 좋지 않으니 막아야한다.
		SoundManager.instance.SetUiVolume(0.0f);
		yield return Timing.WaitForOneFrame;
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], Vector3.right, Quaternion.identity));

			_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.portalMoveEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(100.0f, 0.0f, 0.0f), Quaternion.identity));
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





	DateTime _dailyResetTime;
	DayOfWeek _lastDayOfWeek;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _dailyResetTime)
		{
			TimeSpan remainTime = _dailyResetTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				if (remainTime.Days > 0)
					remainTimeText.text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				else
					remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			_needRefresh = true;

			if (ChangeInvasionDifficultyCanvas.instance != null && ChangeInvasionDifficultyCanvas.instance.gameObject.activeSelf)
				ChangeInvasionDifficultyCanvas.instance.gameObject.SetActive(false);
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (ContentsData.instance.listInvasionEnteredActorId.Count == 0 && ServerTime.UtcNow.DayOfWeek != _lastDayOfWeek)
		{
			RefreshGrid();
			RefreshInfo();

			_needRefresh = false;
		}
	}

	#region Record Last Character
	void RecordLastDifficulty()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		string key = string.Format("{0}_{1}", (int)ServerTime.UtcNow.DayOfWeek, _selectedActorId);
		int value = _selectedDifficulty;
		ObscuredPrefs.SetInt(string.Format("_ivEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId), value);
	}

	int GetCachedLastDifficulty()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return 0;

		string key = string.Format("{0}_{1}", (int)ServerTime.UtcNow.DayOfWeek, _selectedActorId);
		return ObscuredPrefs.GetInt(string.Format("_ivEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId));
	}

	void DeleteLastDifficulty()
	{
		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		string key = string.Format("{0}_{1}", (int)ServerTime.UtcNow.DayOfWeek, _selectedActorId);
		ObscuredPrefs.DeleteKey(string.Format("_ivEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId));
	}
	#endregion
}