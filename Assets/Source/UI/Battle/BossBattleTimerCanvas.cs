using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BossBattleTimerCanvas : MonoBehaviour
{
	public static BossBattleTimerCanvas instance;

	public Text remainTimeText;
	public DOTweenAnimation tweenAnimation;

	void Awake()
	{
		instance = this;
	}

	BossBattleProcessor _processor;
	public void Initialize(BossBattleProcessor processor)
	{
		_processor = processor;
	}

	void Update()
	{
		UpdateRemainTime();
	}

	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (_processor == null)
			return;

		float remainTime = _processor.remainTime;
		if (remainTime > 0.0f)
		{
			if (_lastRemainTimeSecond != (int)remainTime)
			{
				int visualTime = (int)remainTime + 1;

				int minutes = visualTime / 60;
				int seconds = visualTime % 60;
				remainTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

				_lastRemainTimeSecond = (int)remainTime;

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
			remainTimeText.text = string.Format("{0:00}:{1:00}", 0, 0);
			_processor = null;
		}
	}

	void TweenRestart()
	{
		tweenAnimation.DORestart();
		remainTimeText.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
		remainTimeText.transform.DOScale(1.0f, 0.5f);
	}
}