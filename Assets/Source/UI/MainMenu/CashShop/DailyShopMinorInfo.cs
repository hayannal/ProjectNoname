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

	public RectTransform slotAddButtonRectTransform;
	public Image slotAddBlurImage;
	public Image slotAddBackgroundImage;
	public Text slotAddSubText;

	int _itemCountOfLine = 3;
	float _defaultHeight = 512.0f;
	int[] _slotAddChapterList = {4, 6, 8};

	public void RefreshInfo()
	{
		// 6그리드의 가장 첫번재 항목은 항상 일일 무료 아이템이다. 이건 사라지지 않는다.
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
		float additionalHeight = 0.0f;
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
			switch (currentShopUnlockLevel)
			{
				case 0: subTextStringId = "ShopUIName_ExtendPeriodicFirstSub"; break;
				case 1: subTextStringId = "ShopUIName_ExtendPeriodicSecondSub"; break;
				case 2: subTextStringId = "ShopUIName_ExtendPeriodicThirdSub"; break;
			}
			slotAddSubText.SetLocalizedText(UIString.instance.GetString(subTextStringId));
			slotAddBlurImage.color = DailyShopListItem.s_basicBlurImageColor;
			slotAddBackgroundImage.color = DailyShopListItem.s_basicBackgroundImageColor;
			additionalHeight = slotAddButtonRectTransform.sizeDelta.y + gridLayoutGroup.spacing.y;
			slotAddButtonRectTransform.gameObject.SetActive(true);
		}
		else
			slotAddButtonRectTransform.gameObject.SetActive(false);

		// 0번 아이템은 일일 무료라 항상 존재하기 때문에 아에 없어지는 경우는 없고 최소 한줄은 나온다.
		int lineCount = (activeItemCount / _itemCountOfLine) + 1;
		switch (lineCount)
		{
			case 1:
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight - (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y) * 2.0f + additionalHeight);
				break;
			case 2:
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight - (gridLayoutGroup.cellSize.y + gridLayoutGroup.spacing.y) + additionalHeight);
				break;
			case 3:
				rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight + additionalHeight);
				break;
		}
	}

	public void OnClickDailySlotAddButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("DailyShopSlotConfirmCanvas", null);
	}
}