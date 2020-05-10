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

	float _defaultHeight = 332.0f;
	public void RefreshInfo()
	{
		// 6그리드의 가장 첫번재 항목은 항상 일일 무료 아이템이다. 이건 사라지지 않는다.
		// 그래서 데이터도 DailyShopData를 받아오는게 아니라 클라가 가지고 있는 테이블에서 가져와서 셋팅한다.
		dailyFreeItem.RefreshInfo();
		RefreshShopItemInfo();
	}

	public void RefreshShopItemInfo()
	{
		int activeItemCount = 1;
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

		// 0번 아이템은 일일 무료라 항상 존재하기 때문에 아에 없어지는 경우는 없고
		// 한줄이냐 두줄이냐만 결정하면 된다.
		int totalItemCount = dailyShopListItemList.Length + 1;
		bool twoLines = false;
		if (activeItemCount > (totalItemCount / 2)) twoLines = true;
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, twoLines ? _defaultHeight : (_defaultHeight - gridLayoutGroup.cellSize.y - gridLayoutGroup.spacing.y));
	}
}