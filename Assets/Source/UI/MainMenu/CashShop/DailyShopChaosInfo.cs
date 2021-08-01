using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopChaosInfo : MonoBehaviour
{
	public Text remainTimeText;
	public ChaosSlotListItem[] chaosSlotListItemList;

	public RectTransform slotAddItemTransform;
	public Text slotAddSubText;
	public Text slotAddPriceText;
	int[] _slotAddIdList = { 1, 2 };

	void OnEnable()
	{
		RefreshInfo();
	}

	void Update()
	{
		UpdateRemainTime();
		UpdateRefresh();
	}

	void RefreshInfo()
	{
		_dailyResetTime = DailyShopData.instance.dailyShopSlotPurchasedResetTime;
		_needUpdate = true;

		for (int i = 0; i < chaosSlotListItemList.Length; ++i)
			chaosSlotListItemList[i].RefreshInfo(i);

		// 슬롯 확장이 되는 상태인지를 봐야한다.
		bool showShopSlotAddButton = (DailyShopData.instance.chaosSlotUnlockLevel < _slotAddIdList.Length);
		if (showShopSlotAddButton)
		{
			string subTextStringId = "";
			int price = 0;
			switch (DailyShopData.instance.chaosSlotUnlockLevel)
			{
				case 0:
					subTextStringId = "ShopUIName_ExtendPeriodicFirstSub";
					price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendChaosShopOne");
					break;
				case 1:
					subTextStringId = "ShopUIName_ExtendPeriodicSecondSub";
					price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendChaosShopTwo");
					break;
			}
			slotAddSubText.SetLocalizedText(UIString.instance.GetString(subTextStringId));
			slotAddPriceText.text = price.ToString("N0");
			slotAddItemTransform.gameObject.SetActive(true);
		}
		else
			slotAddItemTransform.gameObject.SetActive(false);
	}

	public void OnClickDailySlotAddButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ChaosSlotConfirmCanvas", null);
	}




	DateTime _dailyResetTime;
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

			// DailyShop 상품들과 달리 다음날에도 파는게 같기때문에 굳이 창을 닫을 이유는 없다.
			//if (ChaosFragmentConfirmCanvas.instance != null && ChaosFragmentConfirmCanvas.instance.gameObject.activeSelf)
			//	ChaosFragmentConfirmCanvas.instance.gameObject.SetActive(false);
		}
	}

	bool _needRefresh = false;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (DailyShopData.instance.IsClearedShopSlotPurchasedInfo())
		{
			RefreshInfo();
			_needRefresh = false;
		}
	}
}