using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopListItem : MonoBehaviour
{
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
	public GameObject addObject;
	public Text addText;
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
				RefreshEquipIconImage();
				break;
			case "bn":  // normal Character Box
				RefreshCharacterBoxIconImage("ShopUI_OnlyGradeNormal");
				break;
			case "bh":  // heroic Character Box
				RefreshCharacterBoxIconImage("ShopUI_OnlyGradeHeroic");
				break;
			case "fc":  // fixed Character
				RefreshCharacterIconImage();
				break;
			case "fp":  // fixed Character PP
				RefreshCharacterPpImage();
				break;
			case "fl1": // fixed Character Limit1
				RefreshCharacterIconImage();
				break;
			case "fl2": // fixed Character Limit2
				RefreshCharacterIconImage();
				break;
			case "fl3": // fixed Character Limit3
				RefreshCharacterIconImage();
				break;
			case "uch":	// unfixed Heroic Character
				break;
			case "upn":	// unfixed Normal Character PP
				break;
			case "uph":	// unfixed Heroic Character PP
				break;
		}

		RefreshCurrencyMode(_slotInfo.prevPrice, _slotInfo.price, _slotInfo.priceType);

		bool purchased = DailyShopData.instance.IsPurchasedTodayShopData(dailyShopSlotInfo.slotId);
		blackObject.SetActive(purchased);
		gameObject.SetActive(true);
		return true;
	}

	bool CheckVisible(DailyShopData.DailyShopSlotInfo dailyShopSlotInfo)
	{
		CharacterData characterData = null;
		switch (dailyShopSlotInfo.type)
		{
			case "fe":  // fixed Equip
				return true;
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
				for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
				{
					if (TableDataManager.instance.actorTable.dataArray[i].grade != 1)
						continue;
					if (PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
						return true;
				}
				break;
			case "upn": // unfixed Normal Character PP
				if (PlayerData.instance.ContainsActorByGrade(0))
					return true;
				break;
			case "uph": // unfixed Heroic Character PP
				if (PlayerData.instance.ContainsActorByGrade(1))
					return true;
				break;
		}
		return false;
	}

	void RefreshEquipIconImage()
	{
		if (equipGroupObject) equipGroupObject.SetActive(true);
		if (characterGroupObject) characterGroupObject.SetActive(false);
		if (characterBoxGroupObject) characterBoxGroupObject.SetActive(false);

		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(_slotInfo.value);
		if (equipTableData == null)
			return;

		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString(equipTableData.nameId));
		nameText.gameObject.SetActive(true);
		addObject.SetActive(false);
	}

	void RefreshCharacterIconImage()
	{
		if (equipGroupObject) equipGroupObject.SetActive(false);
		if (characterGroupObject) characterGroupObject.SetActive(true);
		if (characterBoxGroupObject) characterBoxGroupObject.SetActive(false);

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_slotInfo.value);
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});

		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		nameText.gameObject.SetActive(true);
		addObject.SetActive(false);
	}

	void RefreshCharacterPpImage()
	{
		
	}

	void RefreshCharacterBoxIconImage(string addTextStringId)
	{
		if (equipGroupObject) equipGroupObject.SetActive(false);
		if (characterGroupObject) characterGroupObject.SetActive(false);
		if (characterBoxGroupObject) characterBoxGroupObject.SetActive(true);

		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString("ShopUIName_CharacterBox"));
		nameText.gameObject.SetActive(true);
		addText.SetLocalizedText(UIString.instance.GetString(addTextStringId));
		addObject.SetActive(true);
	}

	void RefreshCurrencyMode(int prevPrice, int price, string priceType)
	{
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
