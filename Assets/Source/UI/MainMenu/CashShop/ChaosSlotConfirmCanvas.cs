using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChaosSlotConfirmCanvas : MonoBehaviour
{
	public static ChaosSlotConfirmCanvas instance;

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
		int currentUnlockLevel = DailyShopData.instance.chaosSlotUnlockLevel;
		switch (currentUnlockLevel)
		{
			case 0: price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendChaosShopOne"); break;
			case 1: price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendChaosShopTwo"); break;
		}
		_price = price;

		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.gold < _price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		buttonObject.SetActive(true);
	}

	// 툴팁 없이 가기로 한다.
	//public void OnClickMoreButton()
	//{
	//	int currentUnlockLevel = DailyShopData.instance.unlockLevel;
	//	string tooltipStringId = "";
	//	switch (currentUnlockLevel)
	//	{
	//		case 0: tooltipStringId = "ShopUIName_ExtendPeriodicFirstMore"; break;
	//		case 1: tooltipStringId = "ShopUIName_ExtendPeriodicSecondMore"; break;
	//	}
	//	TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(tooltipStringId), 300, subTitleTextTransform, new Vector2(0.0f, -35.0f));
	//}

	public void OnClickOkButton()
	{
		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestPurchaseChaosSlot(_price, () =>
		{
			CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

			// 슬롯을 갱신해야한다.
			CashShopCanvas.instance.dailyShopChaosInfo.gameObject.SetActive(false);
			CashShopCanvas.instance.dailyShopChaosInfo.gameObject.SetActive(true);

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUIName_ExtendComplete"), 2.0f);
			gameObject.SetActive(false);
		});

		buttonObject.SetActive(false);
	}
}