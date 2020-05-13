using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopMainInfo : MonoBehaviour
{
	public Text remainTimeText;
	public DailyShopListItem dailyShopListItem1;
	public DailyShopListItem dailyShopListItem2;

	void Update()
	{
		UpdateRemainTime();
		UpdateRefresh();
	}

	public void RefreshInfo()
	{
		// 7번 8번이 최상단에 나오는 큰 아이템 두개다. 기간이 긴 아이템을 위로 빼서 보여주는데 쓴다.
		// 둘다 없으면 이 항목 통쨰로 숨겨둔다.
		// 5번 6번과 달리 예외처리가 하나 있는데 두 아이템 모두 보여줄 필요가 없을땐 통째로 숨긴다는거다.
		int showCount = 0;
		DailyShopData.DailyShopSlotInfo dailyShopSlotInfo1 = DailyShopData.instance.GetTodayShopData(7);
		DailyShopData.DailyShopSlotInfo dailyShopSlotInfo2 = DailyShopData.instance.GetTodayShopData(8);
		DailyShopData.DailyShopSlotInfo selectedSlotInfo = null;
		dailyShopListItem1.gameObject.SetActive(dailyShopSlotInfo1 != null);
		dailyShopListItem2.gameObject.SetActive(dailyShopSlotInfo2 != null);
		if (dailyShopSlotInfo1 != null)
		{
			selectedSlotInfo = dailyShopSlotInfo1;
			if (dailyShopListItem1.RefreshInfo(dailyShopSlotInfo1))
				++showCount;
		}
		if (dailyShopSlotInfo2 != null)
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
		// 몇일까지 같은 항목인지 계산해서 3일 남았으면 72:20:15 이런식으로 표시해야한다.
		if (selectedSlotInfo == null)
			return;

		_dailyResetTime = DailyShopData.instance.dailyShopSlotPurchasedResetTime;
		_needUpdate = true;

		int sameDayCount = 0;
		for (int i = 1; i < 30; ++i)
		{
			DailyShopData.DailyShopSlotInfo info = DailyShopData.instance.GetShopSlotData(selectedSlotInfo.day + i, selectedSlotInfo.slotId);
			if (info == null)
				break;
			if (selectedSlotInfo.type != info.type || selectedSlotInfo.value != info.value)
				break;
			++sameDayCount;
		}

		if (sameDayCount > 0)
			_dailyResetTime += TimeSpan.FromDays(sameDayCount);
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
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours + remainTime.Days * 24, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
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