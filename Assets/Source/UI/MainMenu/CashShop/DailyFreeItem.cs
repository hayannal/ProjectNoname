﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DailyFreeItem : MonoBehaviour
{
	public DOTweenAnimation iconTweenAnimation;
	public GameObject goldIconObject;
	public GameObject diaIconObject;
	public GameObject energyIconObject;
	public Text countText;
	public Text freeText;
	public Text completeText;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	bool _started = false;
	void Start()
	{
		_started = true;
	}

	bool _reserveAnimation;
	void Update()
	{
		if (_reserveAnimation)
		{
			iconTweenAnimation.DORestart();
			_reserveAnimation = false;
		}

		UpdateRemainTime();
		UpdateRefresh();
	}

	public static Color GetGoldTextColor()
	{
		return new Color(0.905f, 0.866f, 0.098f);
	}

	public static Color GetDiaTextColor()
	{
		return new Color(0.211f, 0.905f, 0.098f);
	}

	int _addDia, _addGold, _addEnergy;
	public void RefreshInfo()
	{
		// 일일 무료 아이템을 수령하지 않아도 갱신은 하루 단위로 해야하는거라 DailyShopData의 갱신 시간을 따르지 않고 강제로 1일을 넣어둔다.
		// 데이터 갱신과 캔버스 갱신이 다른 유일한 예.
		//_dailyResetTime = DailyShopData.instance.dailyFreeItemResetTime;
		_dailyResetTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
		_needUpdate = true;

		_addDia = _addGold = _addEnergy = 0;
		DailyShopData.DailyFreeItemInfo info = DailyShopData.instance.GetTodayFreeItemData();
		if (info == null)
			return;

		if (info.cd == CurrencyData.GoldCode())
		{
			_addGold = info.cn;
			goldIconObject.SetActive(true);
			diaIconObject.SetActive(false);
			energyIconObject.SetActive(false);
			countText.color = GetGoldTextColor();
		}
		else if (info.cd == CurrencyData.DiamondCode())
		{
			_addDia = info.cn;
			goldIconObject.SetActive(false);
			diaIconObject.SetActive(true);
			energyIconObject.SetActive(false);
			countText.color = GetDiaTextColor();
		}
		else
		{
			_addEnergy = info.cn;
			goldIconObject.SetActive(false);
			diaIconObject.SetActive(false);
			energyIconObject.SetActive(true);
			countText.color = Color.white;
		}
		countText.text = info.cn.ToString("N0");

		// 이미 오늘자 보상을 받았는지 판단해야한다.
		bool received = DailyShopData.instance.dailyFreeItemReceived;
		freeText.gameObject.SetActive(!received);
		completeText.gameObject.SetActive(received);
		blackObject.SetActive(received);

		if (received)
		{
			iconTweenAnimation.DOPause();
			AlarmObject.Hide(alarmRootTransform);
		}
		else
		{
			if (_started)
				iconTweenAnimation.DORestart();
			else
				_reserveAnimation = true;
			AlarmObject.Show(alarmRootTransform);
		}
	}

	DateTime _dailyResetTime;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		//if (DailyShopData.instance.dailyFreeItemReceived == false)
		//	return;
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _dailyResetTime)
		{
		}
		else
		{
			// 일퀘과 달리 패킷 없이 클라가 선처리 하기로 했으니 PlayerData쪽에서 sharedDailyPackageOpened 값을 false로 바꿔두길 기다렸다가 갱신한다.
			// 시간을 변조했다면 받을 수 있는거처럼 보이게 될거다.
			_needUpdate = false;
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (DailyShopData.instance.dailyFreeItemReceived == false)
		{
			RefreshInfo();
			_needRefresh = false;
		}
	}

	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyFreeItem"), 2.0f);
			return;
		}

		// 일일 무료아이템 획득 요청
		PlayFabApiManager.instance.RequestGetFreeItem(_addDia, _addGold, _addEnergy, (serverFailure) =>
		{
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.FreeItem);

			RefreshInfo();
			CashShopCanvas.instance.currencySmallInfo.RefreshInfo();
			DotMainMenuCanvas.instance.RefreshCashShopAlarmObject();
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);
		});
	}
}