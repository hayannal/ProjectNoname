using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopListItem : MonoBehaviour
{
	public Image blurImage;
	public Image backgroundImge;
	public Sprite[] backgroundSpriteList;

	public GameObject equipGroupObject;
	public GameObject characterGroupObject;
	public GameObject characterBoxGroupObject;
	public Image equipIconImage;
	public Image characterImage;

	public Text countText;
	public Text nameText;
	public Text priceText;
	public Text prevPriceText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;
	public GameObject[] priceTypeObjectList;
	public GameObject onlyPriceObject;
	public Text onlyPriceText;
	public GameObject[] onlyPriceTypeObjectList;
	public GameObject purchasedObject;
	public GameObject addObject;
	public Text addText;
	public Slider ppSlider;
	public Text ppText;
	public GameObject blackObject;

	DailyShopData.DailyShopSlotInfo _slotInfo;
	public bool RefreshInfo(DailyShopData.DailyShopSlotInfo dailyShopSlotInfo)
	{
		// 각각의 타입마다 보여져야할 조건이 다르다.
		// 보여지면 안되는 상황이라면 숨겨야한다.
		if (CheckVisible(dailyShopSlotInfo) == false)
		{
			gameObject.SetActive(false);
			return false;
		}

		// 서버에서 날아온 데이터에는 어떤 상품을 얼마나 팔지가 적혀있다.
		// 이걸 가지고 클라 테이블을 열어서 보여줘야한다.
		_slotInfo = dailyShopSlotInfo;

		switch (_slotInfo.type)
		{
			case "fe":  // fixed Equip
				RefreshEquipIconImage(_slotInfo.value);
				break;
			case "bn":  // normal Character Box
				RefreshCharacterBoxIconImage("ShopUI_OnlyGradeNormal");
				break;
			case "bh":  // heroic Character Box
				RefreshCharacterBoxIconImage("ShopUI_OnlyGradeHeroic");
				break;
			case "fc":  // fixed Character
				RefreshCharacterIconImage(_slotInfo.value);
				break;
			case "fp":  // fixed Character PP
				RefreshCharacterPpImage(_slotInfo.value);
				break;
			case "fl1": // fixed Character Limit1
				RefreshCharacterIconImage(_slotInfo.value);
				break;
			case "fl2": // fixed Character Limit2
				RefreshCharacterIconImage(_slotInfo.value);
				break;
			case "fl3": // fixed Character Limit3
				RefreshCharacterIconImage(_slotInfo.value);
				break;
			case "uch": // unfixed Heroic Character
				RefreshUnfixedHeroicCharacterIconImage(_slotInfo.slotId);
				break;
			case "upn": // unfixed Normal Character PP
				RefreshUnfixedCharacterPpImage(_slotInfo.slotId);
				break;
			case "uph": // unfixed Heroic Character PP
				RefreshUnfixedCharacterPpImage(_slotInfo.slotId);
				break;
		}
		RefreshPrice();
		gameObject.SetActive(true);
		return true;
	}

	bool CheckVisible(DailyShopData.DailyShopSlotInfo dailyShopSlotInfo)
	{
		// 예외상황이 하나 있는데 영입하지 않은 노멀 캐릭터가 하나 남은 상태에서 bn 상품으로 마지막 노멀 캐릭터를 영입하면
		// 구매완료 대신 아예 Visible에서 false가 되면서 항목 자체가 사라져버린다.
		// 이걸 방지하기 위해 오늘 구매한거라면 강제로 visible true로 리턴하게 한다.
		if (DailyShopData.instance.IsPurchasedTodayShopData(dailyShopSlotInfo.slotId))
			return true;

		CharacterData characterData = null;
		string cachedActorId = "";
		switch (dailyShopSlotInfo.type)
		{
			case "fe":  // fixed Equip
				if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace))
					return true;
				break;
			case "bn":  // normal Character Box
				for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
				{
					if (TableDataManager.instance.actorTable.dataArray[i].grade != 0)
						continue;
					if (PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
						return true;
				}
				break;
			case "bh":  // heroic Character Box
				for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
				{
					if (TableDataManager.instance.actorTable.dataArray[i].grade != 1)
						continue;
					if (PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
						return true;
				}
				break;
			case "fc":  // fixed Character
				if (PlayerData.instance.ContainsActor(dailyShopSlotInfo.value) == false)
					return true;
				break;
			case "fp":  // fixed Character PP
				if (PlayerData.instance.ContainsActor(dailyShopSlotInfo.value))
					return true;
				break;
			case "fl1": // fixed Character Limit1
				characterData = PlayerData.instance.GetCharacterData(dailyShopSlotInfo.value);
				if (characterData == null)
					break;
				if (characterData.needLimitBreak && characterData.limitBreakPoint <= characterData.limitBreakLevel && characterData.limitBreakLevel == 0)
					return true;
				break;
			case "fl2": // fixed Character Limit2
				characterData = PlayerData.instance.GetCharacterData(dailyShopSlotInfo.value);
				if (characterData == null)
					break;
				if (characterData.needLimitBreak && characterData.limitBreakPoint <= characterData.limitBreakLevel && characterData.limitBreakLevel == 1)
					return true;
				break;
			case "fl3": // fixed Character Limit3
				characterData = PlayerData.instance.GetCharacterData(dailyShopSlotInfo.value);
				if (characterData == null)
					break;
				if (characterData.needLimitBreak && characterData.limitBreakPoint <= characterData.limitBreakLevel && characterData.limitBreakLevel == 2)
					return true;
				break;
			case "uch": // unfixed Heroic Character
				cachedActorId = DailyShopData.instance.GetUnfixedResult(dailyShopSlotInfo.slotId);
				if (cachedActorId == "")
					break;
				for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
				{
					if (TableDataManager.instance.actorTable.dataArray[i].grade != 1)
						continue;
					if (PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
						return true;
				}
				break;
			case "upn": // unfixed Normal Character PP
				cachedActorId = DailyShopData.instance.GetUnfixedResult(dailyShopSlotInfo.slotId);
				if (cachedActorId == "")
					break;
				// 캐싱한걸 검사하고 나서 진짜로 인벤에서 뽑을 수 있는지도 확인해야한다. 이래야 오리진에서 뽑아왔을때 못뽑는 것들을 숨길 수 있다.
				if (PlayerData.instance.ContainsActorByGrade(0))
					return true;
				break;
			case "uph": // unfixed Heroic Character PP
				cachedActorId = DailyShopData.instance.GetUnfixedResult(dailyShopSlotInfo.slotId);
				if (cachedActorId == "")
					break;
				if (PlayerData.instance.ContainsActorByGrade(1))
					return true;
				break;
		}
		return false;
	}

	void RefreshEquipIconImage(string value)
	{
		if (equipGroupObject) equipGroupObject.SetActive(true);
		if (characterGroupObject) characterGroupObject.SetActive(false);
		if (characterBoxGroupObject) characterBoxGroupObject.SetActive(false);

		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(value);
		if (equipTableData == null)
			return;

		RefreshBackground(equipTableData.grade == 4);

		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString(equipTableData.nameId));
		nameText.gameObject.SetActive(true);
		addObject.SetActive(false);
		ppSlider.gameObject.SetActive(false);
	}

	void RefreshCharacterIconImage(string value)
	{
		if (equipGroupObject) equipGroupObject.SetActive(false);
		if (characterGroupObject) characterGroupObject.SetActive(true);
		if (characterBoxGroupObject) characterBoxGroupObject.SetActive(false);

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(value);
		RefreshBackground(actorTableData.grade == 2);

		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});

		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		nameText.gameObject.SetActive(true);
		addObject.SetActive(false);
		ppSlider.gameObject.SetActive(false);
	}

	void RefreshCharacterPpImage(string value)
	{
		// pp 역시 캐릭터가 똑같이 보여지는데 그 위에 pp 정보를 출력하는 형태다.
		if (equipGroupObject) equipGroupObject.SetActive(false);
		if (characterGroupObject) characterGroupObject.SetActive(true);
		if (characterBoxGroupObject) characterBoxGroupObject.SetActive(false);

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(value);
		RefreshBackground(actorTableData.grade == 2);

		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});

		countText.text = _slotInfo.count.ToString("N0");
		countText.gameObject.SetActive(true);
		nameText.gameObject.SetActive(false);
		addObject.SetActive(false);

		int powerLevel = 1;
		int pp = 0;
		bool dontHave = true;
		CharacterData characterData = PlayerData.instance.GetCharacterData(value);
		if (characterData != null)
		{
			powerLevel = characterData.powerLevel;
			pp = characterData.pp;
			dontHave = false;
		}
		if (powerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
		{
			ppText.text = UIString.instance.GetString("GameUI_OverPp", pp - characterData.maxPp);
			ppSlider.value = 1.0f;
			ppSlider.gameObject.SetActive(true);
		}
		else
		{
			int current = 0;
			int max = 0;
			PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel);
			PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel + 1);
			current = pp - powerLevelTableData.requiredAccumulatedPowerPoint;
			max = nextPowerLevelTableData.requiredPowerPoint;

			if (!dontHave)
			{
				ppText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);
				ppSlider.value = Mathf.Min(1.0f, (float)current / (float)max);
				ppSlider.gameObject.SetActive(true);
			}
			else
				ppSlider.gameObject.SetActive(false);
		}
	}

	void RefreshCharacterBoxIconImage(string addTextStringId)
	{
		if (equipGroupObject) equipGroupObject.SetActive(false);
		if (characterGroupObject) characterGroupObject.SetActive(false);
		if (characterBoxGroupObject) characterBoxGroupObject.SetActive(true);

		RefreshBackground(false);
		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString("ShopUIName_CharacterBox"));
		nameText.gameObject.SetActive(true);
		addText.SetLocalizedText(UIString.instance.GetString(addTextStringId));
		addObject.SetActive(true);
		ppSlider.gameObject.SetActive(false);
	}

	void RefreshUnfixedHeroicCharacterIconImage(int slotId)
	{
		_selectedCharacterId = DailyShopData.instance.GetUnfixedResult(slotId);
		RefreshCharacterIconImage(_selectedCharacterId);
	}

	void RefreshUnfixedCharacterPpImage(int slotId)
	{
		_selectedCharacterId = DailyShopData.instance.GetUnfixedResult(slotId);
		RefreshCharacterPpImage(_selectedCharacterId);
	}

	string _selectedCharacterId;
	public string selectedCharacterId
	{
		get
		{
			switch (_slotInfo.type)
			{
				case "bn":
				case "bh":
					return "";
				case "uch":
				case "upn":
				case "uph":
					return _selectedCharacterId;
			}
			return _slotInfo.value;
		}
	}

	void RefreshBackground(bool isLightenBackground)
	{
		blurImage.color = isLightenBackground ? new Color(0.945f, 0.945f, 0.094f, 0.42f) : new Color(0.094f, 0.945f, 0.871f, 0.42f);
		backgroundImge.color = isLightenBackground ? new Color(1.0f, 1.0f, 1.0f, 0.42f) : new Color(0.0f, 1.0f, 0.749f, 0.42f);
		backgroundImge.sprite = backgroundSpriteList[isLightenBackground ? 0 : 1];
	}
	
	void RefreshPrice()
	{
		bool purchased = DailyShopData.instance.IsPurchasedTodayShopData(_slotInfo.slotId);
		blackObject.SetActive(purchased);
		purchasedObject.SetActive(purchased);
		if (purchased)
		{
			priceText.gameObject.SetActive(false);
			prevPriceText.gameObject.SetActive(false);
			onlyPriceObject.SetActive(false);
			return;
		}

		int prevPrice = _slotInfo.prevPrice;
		int price = _slotInfo.price;
		string priceType = _slotInfo.priceType;
		if (prevPrice > 0)
		{
			onlyPriceObject.SetActive(false);

			priceText.text = price.ToString("N0");
			prevPriceText.text = prevPrice.ToString("N0");
			priceText.gameObject.SetActive(true);
			prevPriceText.gameObject.SetActive(true);

			// 재화는 둘중에 하나다.
			CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
			if (priceType == CurrencyData.GoldCode())
				currencyType = CurrencyData.eCurrencyType.Gold;
			for (int i = 0; i < priceTypeObjectList.Length; ++i)
				priceTypeObjectList[i].SetActive((int)currencyType == i);

			RefreshLineImage();
			_updateRefreshLineImage = true;
		}
		else
		{
			priceText.gameObject.SetActive(false);
			prevPriceText.gameObject.SetActive(false);

			onlyPriceText.text = price.ToString("N0");
			onlyPriceObject.SetActive(true);

			// 재화는 둘중에 하나다.
			CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
			if (priceType == CurrencyData.GoldCode())
				currencyType = CurrencyData.eCurrencyType.Gold;
			for (int i = 0; i < onlyPriceTypeObjectList.Length; ++i)
				onlyPriceTypeObjectList[i].SetActive((int)currencyType == i);
		}
	}

	void RefreshLineImage()
	{
		Vector3 diff = rightTopRectTransform.position - lineImageRectTransform.position;
		lineImageRectTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);
		lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopCanvas.instance.lineLengthRatio);
	}

	bool _updateRefreshLineImage;
	void Update()
	{
		if (_updateRefreshLineImage)
		{
			RefreshLineImage();
			_updateRefreshLineImage = false;
		}
	}


	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyThatItem"), 2.0f);
			return;
		}

		if (_slotInfo.type == "fe")
		{
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			UIInstanceManager.instance.ShowCanvasAsync("DailyShopEquipConfirmCanvas", () =>
			{
				DailyShopEquipConfirmCanvas.instance.ShowCanvas(true, _slotInfo);
			});
		}
		else
		{
			UIInstanceManager.instance.ShowCanvasAsync("DailyShopCharacterConfirmCanvas", () =>
			{
				DailyShopCharacterConfirmCanvas.instance.ShowCanvas(true, _slotInfo, gameObject.name.Contains("_Big"));
			});
		}
	}








	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
}
