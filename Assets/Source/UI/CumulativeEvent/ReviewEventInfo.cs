using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReviewEventInfo : MonoBehaviour
{
	#region RemainTime
	public bool useRemainTimeText;
	public Text remainTimeText;
	#endregion

	void OnEnable()
	{
		#region RemainTime
		if (useRemainTimeText == false)
		{
			remainTimeText.gameObject.SetActive(false);
			return;
		}

		// 정보가 없다면 창을 열수가 없었을거다.
		CumulativeEventData.RepeatEventTypeInfo info = CumulativeEventData.instance.FindRepeatEventTypeInfo(CumulativeEventData.eEventType.Review);
		if (info == null)
			return;

		_eventEndDateTime = info.endDateTime;
		//_needUpdate = true;
		#endregion

		if (CumulativeEventData.instance.reviewEventChecked == false)
			_viewCheckRemainTime = 0.5f;
	}

	void Update()
	{
		Update5Second();

		#region RemainTime
		if (useRemainTimeText == false)
			return;

		UpdateRemainTime();
		#endregion
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
				PlayFabApiManager.instance.RequestReviewEvent(() =>
				{
					CumulativeEventCanvas.instance.RefreshAlarmObject(CumulativeEventData.eEventType.Review);
				});
			}
		}
	}

	#region RemainTime
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
	#endregion

	#region Market
	public void OnClickMarketButton()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			string url = CumulativeEventData.instance.iosUrl;
			Application.OpenURL(url);
		}
		else if (Application.platform == RuntimePlatform.Android)
		{
			Application.OpenURL("market://details?id=" + Application.identifier);
		}
	}
	#endregion
}