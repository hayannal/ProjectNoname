using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointShopEventInfo : MonoBehaviour
{
	public Text remainTimeText;

	public Text pointText;

	int _addPoint;
	void OnEnable()
	{
		// 정보가 없다면 창을 열수가 없었을거다.
		CumulativeEventData.RepeatEventTypeInfo info = CumulativeEventData.instance.FindRepeatEventTypeInfo(CumulativeEventData.eEventType.PointShop);
		if (info == null)
			return;

		_eventEndDateTime = info.endDateTime;

		if (CumulativeEventData.instance.pointShopChecked == false)
			_viewCheckRemainTime = 0.5f;

		pointText.text = "0 p";
		_addPoint = CumulativeEventData.instance.pointShopPoint;
		if (_addPoint > 0)
		{
			_pointChangeRemainTime = pointChangeTime;
			_pointChangeSpeed = _addPoint / _pointChangeRemainTime;
			_currentPoint = 0.0f;
			_updatePointText = true;
		}
	}

	void Update()
	{
		UpdatePointText();
		Update5Second();
		UpdateRemainTime();
	}

	const float pointChangeTime = 0.6f;
	float _pointChangeRemainTime;
	float _pointChangeSpeed;
	float _currentPoint;
	int _lastPoint;
	bool _updatePointText;
	void UpdatePointText()
	{
		if (_updatePointText == false)
			return;

		_currentPoint += _pointChangeSpeed * Time.deltaTime;
		int currentPointInt = (int)_currentPoint;
		if (currentPointInt >= _addPoint)
		{
			currentPointInt = _addPoint;
			_updatePointText = false;
		}
		if (currentPointInt != _lastPoint)
		{
			_lastPoint = currentPointInt;
			pointText.text = string.Format("{0:N0} p", _lastPoint);
		}
	}

	float _viewCheckRemainTime;
	void Update5Second()
	{
		// 창을 열어서 0.5초 이상 봤으면 몰래 봤다는 패킷을 날려서 다음에 느낌표가 안뜨게 해준다.
		if (_viewCheckRemainTime > 0.0f)
		{
			_viewCheckRemainTime -= Time.deltaTime;
			if (_viewCheckRemainTime <= 0.0f)
			{
				_viewCheckRemainTime = 0.0f;
				PlayFabApiManager.instance.RequestCheckPointShopEvent(() =>
				{
					CumulativeEventCanvas.instance.RefreshAlarmObject(CumulativeEventData.eEventType.PointShop);
				});
			}
		}
	}

	DateTime _eventEndDateTime;
	int _lastRemainTimeSecond = -1;
	//bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _eventEndDateTime)
		{
			TimeSpan remainTime = _eventEndDateTime - ServerTime.UtcNow;
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
			// 이벤트 기간이 끝났으면 닫아버리는게 제일 편하다.
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_EventExpired"), 2.0f);
			CumulativeEventCanvas.instance.gameObject.SetActive(false);

			//_needUpdate = false;
			//remainTimeText.text = "00:00:00";
			//_needRefresh = true;
		}
	}
}