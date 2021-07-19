using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InvasionTimerCanvas : MonoBehaviour
{
	public static InvasionTimerCanvas instance;

	public GameObject textRootObject;
	public Text remainTimeText;
	public DOTweenAnimation tweenAnimation;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		_dailyResetTime = DailyShopData.instance.dailyShopSlotPurchasedResetTime;
		_needUpdate = true;
	}

	void Update()
	{
		UpdateRemainTime();
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
				int visualTime = (int)remainTime.TotalSeconds + 1;

				if (visualTime > 60)
				{
					if (textRootObject.activeSelf)
						textRootObject.SetActive(false);
				}
				else
				{
					if (textRootObject.activeSelf == false)
						textRootObject.SetActive(true);

					int minutes = visualTime / 60;
					int seconds = visualTime % 60;
					remainTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
				}
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;

				if (_lastRemainTimeSecond >= 0 && _lastRemainTimeSecond < 10)
				{
					TweenRestart();
					remainTimeText.color = new Color(0.925f, 0.52f, 0.52f);
				}
			}
		}
		else
		{
			TweenRestart();
			remainTimeText.text = "00:00";
			_needUpdate = false;
		}
	}

	void TweenRestart()
	{
		tweenAnimation.DORestart();
		remainTimeText.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
		remainTimeText.transform.DOScale(1.0f, 0.5f).SetUpdate(true);
	}
}