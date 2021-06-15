using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Image1EventInfo : MonoBehaviour
{
	public bool useRemainTimeText;
	public Text remainTimeText;

	void OnEnable()
	{
		if (useRemainTimeText == false)
		{
			remainTimeText.gameObject.SetActive(false);
			return;
		}

		// 정보가 없다면 창을 열수가 없었을거다.
		CumulativeEventData.RepeatEventTypeInfo info = CumulativeEventData.instance.FindRepeatEventTypeInfo(CumulativeEventData.eEventType.ImageEvent1);
		if (info == null)
			return;

		_eventEndDateTime = info.endDateTime;
		//_needUpdate = true;
	}

	void Update()
	{
		if (useRemainTimeText == false)
			return;

		UpdateRemainTime();
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