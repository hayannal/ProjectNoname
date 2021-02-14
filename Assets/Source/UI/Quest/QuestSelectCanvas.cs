using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestSelectCanvas : MonoBehaviour
{
	public static QuestSelectCanvas instance;

	public Transform subTitleTransform;
	public Text descText;
	public Text remainTimeText;
	public QuestInfoItem info1;
	public QuestInfoItem info2;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		CurrencySmallInfoCanvas.Show(true);

		if (PlayerData.instance.currentChaosMode)
			descText.SetLocalizedText(UIString.instance.GetString("QuestUI_SubQuestDesc"));
		else
			descText.SetLocalizedText(string.Format("{0}\n{1}", UIString.instance.GetString("QuestUI_SubQuestDesc"), UIString.instance.GetString("QuestUI_NotChaos")));

		// 퀘스트는 시간제한이 걸려있다. 오늘 안하면 모든게 초기화.
		_questResetTime = QuestData.instance.todayQuestResetTime;
		_needUpdate = true;

		int currentIndex = QuestData.instance.todayQuestRewardedCount * 2;
		QuestData.QuestInfo questInfo1 = QuestData.instance.FindQuestInfoByIndex(currentIndex);
		QuestData.QuestInfo questInfo2 = QuestData.instance.FindQuestInfoByIndex(currentIndex + 1);
		if (questInfo1 != null && questInfo2 != null)
		{
			info1.RefreshInfo(questInfo1);
			info2.RefreshInfo(questInfo2);
		}
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
				TreasureChest.instance.HideIndicatorCanvas(true, true);

			// 퀘스트는 다음날이 되면 바로 진행할 수 없고 오리진박스를 열고나서야 된다. 그러니 창을 닫아준다.
			//_needRefresh = true;
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_TimeOut"), 2.0f);
			gameObject.SetActive(false);
		}
	}

	public void OnClickMoreButton()
	{
		string text = string.Format("{0}{1} / {2}", UIString.instance.GetString("QuestUI_SubQuestMore"), QuestData.instance.todayQuestRewardedCount, QuestData.DailyMaxCount);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}
}