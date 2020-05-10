using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopMainInfo : MonoBehaviour
{
	public Text remainTimeText;
	public DailyShopListItem dailyShopListItem1;
	public DailyShopListItem dailyShopListItem2;

	public void RefreshInfo()
	{
		// 7번 8번이 최상단에 나오는 큰 아이템 두개다. 기간이 긴 아이템을 위로 빼서 보여주는데 쓴다.
		// 둘다 없으면 이 항목 통쨰로 숨겨둔다.
		// 5번 6번과 달리 예외처리가 하나 있는데
		// 여기 아이템들은 만약 구매했다면 blackObject처리를 하는게 아니라 아예 숨겨버린다는거다.
		// 그래서 두 아이템 모두 구매해서 표시할게 없다면 탭 자체를 숨겨야한다.
		int showCount = 0;
		DailyShopData.DailyShopSlotInfo dailyShopSlotInfo1 = DailyShopData.instance.GetTodayShopData(7);
		DailyShopData.DailyShopSlotInfo dailyShopSlotInfo2 = DailyShopData.instance.GetTodayShopData(8);
		DailyShopData.DailyShopSlotInfo selectedSlotInfo = null;
		if (dailyShopSlotInfo1 != null && DailyShopData.instance.IsPurchasedTodayShopData(dailyShopSlotInfo1.slotId) == false)
		{
			selectedSlotInfo = dailyShopSlotInfo1;
			if (dailyShopListItem1.RefreshInfo(dailyShopSlotInfo1))
				++showCount;
		}
		if (dailyShopSlotInfo2 != null && DailyShopData.instance.IsPurchasedTodayShopData(dailyShopSlotInfo2.slotId) == false)
		{
			selectedSlotInfo = dailyShopSlotInfo2;
			if (dailyShopListItem2.RefreshInfo(dailyShopSlotInfo2))
				++showCount;
		}
		if (showCount == 0)
		{
			gameObject.SetActive(false);
			return;
		}
		gameObject.SetActive(true);

		// 그리고 또 하나 예외처리도 있는데
		// 시간표시가 1일짜리로 고정되지 않는다는거다.
		// 7번이든 8번이든 아무거나 하나 골라서
		// 몇일까지 같은 항목인지 계산해서 5일 남았으면 5day 20hour 이런식으로 표시해야한다.
		if (selectedSlotInfo == null)
			return;

	}
}