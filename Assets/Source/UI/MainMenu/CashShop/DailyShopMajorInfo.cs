using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopMajorInfo : MonoBehaviour
{
	public RectTransform rectTransform;
	public Text remainTimeText;
	public DailyShopListItem dailyShopListItem1;
	public DailyShopListItem dailyShopListItem2;

	float _defaultHeight = 220.0f;
	float _defaultItemHeight = 168.0f + 14.0f;
	void Awake()
	{
		// 부모 트랜스폼에 vertial layout group이 있었더니 값이 0으로 초기화 되서 생성된다. 지금와서 다 뜯기엔 너무 많아서 강제로 초기화값을 넣기로 한다.
		// Awake 보다 RefreshInfo 가 먼저 호출되길래 아예 디폴트 값으로 넣어둔다.
		//_defaultHeight = rectTransform.sizeDelta.y;
		//_defaultItemHeight = dailyShopListItem1.cachedRectTransform.sizeDelta.y;
	}

	void Update()
	{
		UpdateRemainTime();
		UpdateRefresh();
	}

	public void RefreshInfo()
	{
		// 기간제 상품 하단 영역 Timer는 항상 하루 단위다.
		// 아무것도 구매하지 않아도 타이머가 떠야하는거라서 이전의 기한 표시와 달리 강제로 보여주는 처리가 필요하다.
		_dailyResetTime = DailyShopData.instance.dailyShopSlotPurchasedResetTime;
		_needUpdate = true;

		// 5번 6번이 상단에 나오는 큰 아이템 두개다.
		// 7번 8번과 달리 구매했어도 blackObject로 가려져서 보여져야한다.
		int showCount = 0;
		DailyShopData.DailyShopSlotInfo dailyShopSlotInfo1 = DailyShopData.instance.GetTodayShopData(5);
		DailyShopData.DailyShopSlotInfo dailyShopSlotInfo2 = DailyShopData.instance.GetTodayShopData(6);
		if (dailyShopSlotInfo1 != null)
		{
			if (dailyShopListItem1.RefreshInfo(dailyShopSlotInfo1))
				++showCount;
		}
		if (dailyShopSlotInfo2 != null)
		{
			if (dailyShopListItem2.RefreshInfo(dailyShopSlotInfo2))
				++showCount;
		}
		if (showCount == 0)
		{
			// 없으면 영역 조정
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight - _defaultItemHeight);
		}
		else
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, _defaultHeight);
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
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
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

		// 상점데이터는 다른 일퀘나 일일 다이아 패키지와 달리 구매(혹은 수령, 진행)여부와는 상관없이 항상 갱신되야하는거라서
		// 리셋타임 5분전에 미리 서버 데이터를 긁어와서 새 데이터를 들고있을거다.
		// 구매 리셋만 확인되면 Refresh를 호출한다.
		if (DailyShopData.instance.IsClearedShopSlotPurchasedInfo())
		{
			// 여기서 사실 보통이라면 자기 혼자만 리프레쉬 하면 되는데
			// 이러면 Minor아이템도 스스로 또 체크해서 리프레쉬를 해야만 한다.
			// 굳이 양쪽에서 이렇게 따로 체크할 필요가 없기 때문에 Minor항목도 여기서 리프레쉬를 호출해주기로 한다.
			// 메인항목은 스스로 갱신타이밍을 재고있을텐데 여기서 해야하나..?
			RefreshInfo();
			CashShopCanvas.instance.dailyShopMinorInfo.RefreshShopItemInfo();
			_needRefresh = false;
		}
	}
}