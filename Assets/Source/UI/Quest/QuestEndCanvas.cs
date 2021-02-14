using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestEndCanvas : MonoBehaviour
{
	public static QuestEndCanvas instance;

	public Transform subTitleTransform;
	public Text remainTimeText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		CurrencySmallInfoCanvas.Show(true);

		_questResetTime = QuestData.instance.todayQuestResetTime;
		_needUpdate = true;
	}

	void OnDisable()
	{
		CurrencySmallInfoCanvas.Show(false);
	}

	void Update()
	{
		UpdateRemainTime();
	}

	DateTime _questResetTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _questResetTime)
		{
			TimeSpan remainTime = _questResetTime - ServerTime.UtcNow;
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

			// 서브퀘스트라고 떠있는 인디케이터 닫아버린다.
			if (TreasureChest.instance != null)
				TreasureChest.instance.HideIndicatorCanvas(true);

			// 진행중이던게 아니니 그냥 토스트 없이 닫아본다.
			//_needRefresh = true;
			gameObject.SetActive(false);
		}
	}

	public void OnClickMoreButton()
	{
		string text = string.Format("{0}{1} / {2}", UIString.instance.GetString("QuestUI_SubQuestMore"), QuestData.instance.todayQuestRewardedCount, QuestData.DailyMaxCount);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}
}