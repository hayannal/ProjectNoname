using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopMinorInfo : MonoBehaviour
{
	public RectTransform rectTransform;
	public DailyFreeItem dailyFreeItem;
	public DailyShopListItem[] dailyShopListItemList;
	public GridLayoutGroup gridLayoutGroup;

	public RectTransform slotAddItemTransform;
	public Text slotAddSubText;
	public Text slotAddPriceText;

	int _itemCountOfLine = 3;
	float _defaultHeight = 512.0f;
	int[] _slotAddChapterList = {4, 6, 8};

	public void RefreshInfo()
	{
		// 9그리드의 가장 첫번재 항목은 항상 일일 무료 아이템이다. 이건 사라지지 않는다.
		// 그래서 데이터도 DailyShopData를 받아오는게 아니라 클라가 가지고 있는 테이블에서 가져와서 셋팅한다.
		dailyFreeItem.RefreshInfo();
		RefreshShopItemInfo();
	}

	public void RefreshShopItemInfo()
	{
		int activeItemCount = 0;
		for (int i = 0; i < dailyShopListItemList.Length; ++i)
		{
			DailyShopData.DailyShopSlotInfo dailyShopSlotInfo = DailyShopData.instance.GetTodayShopData(i);
			if (dailyShopSlotInfo == null)
			{
				dailyShopListItemList[i].gameObject.SetActive(false);
				continue;
			}

			if (dailyShopListItemList[i].RefreshInfo(dailyShopSlotInfo))
				++activeItemCount;
		}

		// 각각의 슬롯 정보를 확인했으면 슬롯 확장이 되는 상태인지를 봐야한다.
		bool showShopSlotAddButton = false;
		int currentShopUnlockLevel = DailyShopData.instance.unlockLevel;
		for (int i = 0; i < _slotAddChapterList.Length; ++i)
		{
			if (PlayerData.instance.highestPlayChapter >= _slotAddChapterList[i])
			{
				if (currentShopUnlockLevel < (i + 1))
				{
					showShopSlotAddButton = true;
					break;
				}
			}
		}
		if (showShopSlotAddButton)
		{
			string subTextStringId = "";
			int price = 0;
			switch (currentShopUnlockLevel)
			{
				case 0:
					subTextStringId = "ShopUIName_ExtendPeriodicFirstSub";
					price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendPeriodShopOne");
					break;
				case 1:
					subTextStringId = "ShopUIName_ExtendPeriodicSecondSub";
					price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendPeriodShopTwo");
					break;
				case 2:
					subTextStringId = "ShopUIName_ExtendPeriodicThirdSub";
					price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ExtendPeriodShopThree");
					break;
			}
			slotAddSubText.SetLocalizedText(UIString.instance.GetString(subTextStringId));
			slotAddPriceText.text = price.ToString("N0");
			slotAddItemTransform.gameObject.SetActive(true);
			++activeItemCount;
		}
		else
			slotAddItemTransform.gameObject.SetActive(false);

		// 0번 아이템은 일일 무료라 항상 존재하기 때문에 아에 없어지는 경우는 없고 최소 한줄은 나온다.
		int lineCount = (activeItemCount / _itemCountOfLine) + 1;
		switch (lineCount)
		{
			case 1:
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight - (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y) * 2.0f);
				break;
			case 2:
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight - (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y));
				break;
			case 3:
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight);
				break;
		}
	}

	public void OnClickDailySlotAddButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("DailyShopSlotConfirmCanvas", null);
	}
}