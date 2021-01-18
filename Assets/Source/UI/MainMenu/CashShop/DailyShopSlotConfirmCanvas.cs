using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class DailyShopSlotConfirmCanvas : MonoBehaviour
{
	public static DailyShopSlotConfirmCanvas instance;

	public Transform subTitleTextTransform;

	public Text priceText;
	public GameObject buttonObject;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshInfo();	
	}

	int _price = 0;
	void RefreshInfo()
	{
		int price = 0;
		int currentShopUnlockLevel = DailyShopData.instance.unlockLevel;
		switch (currentShopUnlockLevel)
		{
			case 0: price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendPeriodShopOne"); break;
			case 1: price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendPeriodShopTwo"); break;
			case 2: price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendPeriodShopThree"); break;
		}
		_price = price;

		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < _price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		buttonObject.SetActive(true);
	}

	public void OnClickMoreButton()
	{
		int currentShopUnlockLevel = DailyShopData.instance.unlockLevel;
		string tooltipStringId = "";
		switch (currentShopUnlockLevel)
		{
			case 0: tooltipStringId = "ShopUIName_ExtendPeriodicFirstMore"; break;
			case 1: tooltipStringId = "ShopUIName_ExtendPeriodicSecondMore"; break;
			case 2: tooltipStringId = "ShopUIName_ExtendPeriodicThirdMore"; break;
		}
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(tooltipStringId), 300, subTitleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickOkButton()
	{
		if (CurrencyData.instance.dia < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestPurchaseDailyShopSlot(_price, () =>
		{
			CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

			// 슬롯을 갱신해야한다.
			CashShopCanvas.instance.dailyShopMinorInfo.RefreshShopItemInfo();
			CashShopCanvas.instance.dailyShopMinorInfo.gameObject.SetActive(false);
			CashShopCanvas.instance.dailyShopMinorInfo.gameObject.SetActive(true);

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUIName_ExtendComplete"), 2.0f);
			gameObject.SetActive(false);
		});

		buttonObject.SetActive(false);
	}
}