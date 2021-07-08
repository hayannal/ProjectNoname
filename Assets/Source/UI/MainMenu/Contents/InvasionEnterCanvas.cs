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
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("InvasionUI_PopupName"), UIString.instance.GetString("InvasionUI_PopupMore"), UIString.instance.GetString("InvasionUI_PopupDesc"), null, 0.785f);
			});
			EventManager.instance.reservedOpenInvasionEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.invasion);
		}
	}

	void OnEnable()
	{
		// RefreshGrid를 먼저 호출해서 현재 진입할 수 있는 캐릭터들을 추려야한다. 나머지는 그 이후에 가능.
		RefreshGrid(true);
		RefreshInfo();

		if (LobbyCanvas.instance != null)
		{
			LobbyCanvas.instance.subMenuCanvasGroup.alpha = 0.0f;
			LobbyCanvas.instance.dotMainMenuButton.gameObject.SetActive(false);
		}

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
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



	List<CharacterData> _listCharacterData = new List<CharacterData>();
	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	void RefreshGrid(bool onEnable)
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

		if (onEnable)
		{
			if (string.IsNullOrEmpty(firstSelectableActorId))
			{ }
			else
				OnClickListItem(firstSelectableActorId);
		}
		else
			OnClickListItem(_selectedActorId);
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
		if (powerLevel < 3)
			return 1;
		else if (powerLevel < 5)
			return 2;
		else if (powerLevel < 7)
			return 3;
		else if (powerLevel < 9)
			return 4;
		else
			return 5;
	}

	ObscuredInt _selectedDifficulty;
	ObscuredInt _limitPowerLevel;
	void RefreshInfo()
	{
		// RefreshGrid(true)를 통해 진입해야할 캐릭터들이 골라진 상태일거다.
		// 두번째 할일은 현재 가지고 있는 캐릭터들 중 최고레벨을 골라서 어느 난이도까지 표시할지 정해야한다.
		RefreshHighestPowerLevel();

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

		// EnterCount
		RefreshEnterCount();

		RefreshDifficultyInfo();
	}

	GameObject _cachedPreviewObject;
	StageTableData _invasionStageTableData;
	MapTableData _invasionMapTableData;
	void RefreshDifficultyInfo()
	{
		difficultyText.text = ChangeInvasionDifficultyCanvasListItem.GetDifficultyText(_selectedDifficulty);

		// 난이도까지 선택이 됐다면 이제 나머지 정보들을 불러올 수 있다.
		_invasionTableData = TableDataManager.instance.FindInvasionTableData((int)ServerTime.UtcNow.DayOfWeek, _selectedDifficulty);
		if (_invasionTableData == null)
			return;

		// 파워레벨은 제한으로 표시
		_limitPowerLevel = _invasionTableData.limitPower;
		limitPowerLevelText.SetLocalizedText(string.Format("{0} {1}", UIString.instance.GetString("InvasionUI_LimitedPowerLevel"), _limitPowerLevel));

		// Reward 표기
		RefreshReward();

		StageTableData invasionStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_invasionTableData.chapter, _invasionTableData.stage, false);
		if (invasionStageTableData == null)
			return;
		MapTableData invasionMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(invasionStageTableData.firstFixedMap);
		if (invasionMapTableData == null)
			return;

		if (_cachedPreviewObject != null)
		{
			_cachedPreviewObject.SetActive(false);
			_cachedPreviewObject = null;
		}

		_invasionStageTableData = invasionStageTableData;
		_invasionMapTableData = invasionMapTableData;

		if (string.IsNullOrEmpty(invasionMapTableData.bossName) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", invasionMapTableData.bossName), "Preview", (prefab) =>
			{
				_cachedPreviewObject = UIInstanceManager.instance.GetCachedObject(prefab, previewRootTransform);
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
		/*
		else if (_invasionTableData.rewardType == "be")
		{
			if (guideQuestTableData.rewardValue == "3" && guideQuestTableData.rewardCount >= 3)
				equipBigBoxObject.SetActive(true);
			else
				equipBoxObject.SetActive(true);
			rewardCountText.text = "";
		}
		else if (guideQuestTableData.rewardType == "bm")
		{
			if (guideQuestTableData.rewardCount >= 5)
				equipBigBoxObject.SetActive(true);
			else
				equipBoxObject.SetActive(true);
			rewardCountText.text = "";
		}
		*/
		else if (_invasionTableData.rewardType == "bc")
		{
			characterBoxObject.SetActive(true);
		}
	}

	public void OnClickRewardInfoButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.InvasionRewardInfo, UIString.instance.GetString(_invasionTableData.rewardMore), 300, goldIconObject.transform, new Vector2(0.0f, -40.0f));
	}

	const int ENTER_COUNT_MAX = 3;
	void RefreshEnterCount()
	{
		// 오늘 입장한 캐릭터 리스트를 구해오면 입장 횟수를 구할 수 있다.
		int currentEnterCount = PlayerData.instance.listInvasionEnteredActorId.Count;
		if (currentEnterCount < ENTER_COUNT_MAX)
		{
			enterButtonObject.SetActive(true);
			priceButtonObject.SetActive(false);
			bool disablePrice = (_selectedActorPowerLevel < _limitPowerLevel);
			enterButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			enterText.color = !disablePrice ? Color.white : Color.gray;
			enterCountText.text = string.Format("{0} / {1}", currentEnterCount, ENTER_COUNT_MAX);
			enterCountText.color = !disablePrice ? Color.white : Color.gray;
			oneMoreChanceTextObject.SetActive(false);
		}
		else if (currentEnterCount == ENTER_COUNT_MAX)
		{
			enterButtonObject.SetActive(false);
			priceButtonObject.SetActive(true);
			enterCountText.text = "";
			oneMoreChanceTextObject.SetActive(true);
			RefreshPrice();
		}
		else if ((currentEnterCount + 1) == ENTER_COUNT_MAX)
		{
			enterButtonObject.SetActive(true);
			priceButtonObject.SetActive(false);
			enterButtonImage.color = ColorUtil.halfGray;
			enterText.color = Color.gray;
			enterCountText.text = string.Format("{0} / {1}", ENTER_COUNT_MAX, ENTER_COUNT_MAX);
			enterCountText.color = Color.gray;
			oneMoreChanceTextObject.SetActive(false);
		}
	}

	void RefreshPrice()
	{
		// 가격
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("InvasionDiamond");
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < price || _selectedActorPowerLevel < _limitPowerLevel);
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
		OnClickListItem(_selectedActorId);
	}

	public void OnClickListItem(string actorId)
	{
		if (PlayerData.instance.ContainsActor(actorId) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_NotJoin"), 2.0f);
			return;
		}

		// 이미 출전했던 캐릭터라면
		if (PlayerData.instance.listInvasionEnteredActorId.Contains(actorId))
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
		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}
		if (_selectedActorPowerLevel < _limitPowerLevel)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("InvasionUI_CannotEnterPowerToast"), 2.0f);
			return;
		}
		// check lobby energy
		//if (CheckEnergy() == false)
		//	return;



		if (string.IsNullOrEmpty(_selectedActorId))
			return;

		// 진입처리
		// 인풋부터 막고
		DelayedLoadingCanvas.Show(true);

		// 입장 패킷 보내기전에 필수로 해야하는 것들 위주로 셋팅한다.
		// 나머진 패킷 받고 재진입 다 완료한 후에 셋팅하는거로 한다.

		// 이동해야할 스테이지의 로비를 미리 로드
		//StageManager.instance.ReloadBossBattle(_bossStageTableData, _bossMapTableData);

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
		//Timing.RunCoroutine(MoveProcess());
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

	/*

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
				PlayerData.instance.SelectBossBattleDifficulty(_selectedDifficulty);
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
	*/

	#region Record Last Character
	void RecordLastDifficulty()
	{
		string key = string.Format("{0}_{1}", (int)ServerTime.UtcNow.DayOfWeek, _selectedActorId);
		int value = _selectedDifficulty;
		ObscuredPrefs.SetInt(string.Format("_ivEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId), value);
	}

	int GetCachedLastDifficulty()
	{
		string key = string.Format("{0}_{1}", (int)ServerTime.UtcNow.DayOfWeek, _selectedActorId);
		return ObscuredPrefs.GetInt(string.Format("_ivEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId));
	}

	void DeleteLastDifficulty()
	{
		string key = string.Format("{0}_{1}", (int)ServerTime.UtcNow.DayOfWeek, _selectedActorId);
		ObscuredPrefs.DeleteKey(string.Format("_ivEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId));
	}
	#endregion
}