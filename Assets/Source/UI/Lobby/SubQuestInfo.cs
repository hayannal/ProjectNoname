using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SubQuestInfo : MonoBehaviour
{
	public static SubQuestInfo instance;

	public GameObject smallButtonRootObject;
	public DOTweenAnimation infoRootTweenAnimation;
	public GameObject smallBackButtonRootObject;

	public GameObject contentsRootObject;
	public Text descriptionText;
	public GameObject remainTimeRootObject;
	public Text remainTimeText;
	public GameObject smallBlinkObject;
	public GameObject blinkObject;
	public GameObject disableTextObject;

	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState)
			return;

		// 처음 로그인하면서 들어갈때는 GuideQuest와 달리 퀘스트 리스트가 제대로 처리되지 않은 상태라서 유효성 검사를 해야한다.
		if (QuestData.instance.CheckValidQuestList(false) == false)
			return;

		RefreshSmallButton();
		RefreshInfo();
	}

	// Update is called once per frame
	void Update()
	{
		UpdateRemainTime();

		if (_openRemainTime > 0.0f)
		{
			_openRemainTime -= Time.deltaTime;
			if (_openRemainTime <= 0.0f)
			{
				_openRemainTime = 0.0f;
				OnClickSmallBackButton();
			}
		}

		if (_closeRemainTime > 0.0f)
		{
			_closeRemainTime -= Time.deltaTime;
			if (_closeRemainTime <= 0.0f)
			{
				_closeRemainTime = 0.0f;
				infoRootTweenAnimation.gameObject.SetActive(false);
				smallButtonRootObject.SetActive(true);
			}
		}
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
			if (infoRootTweenAnimation.gameObject.activeSelf)
			{
				TimeSpan remainTime = _questResetTime - ServerTime.UtcNow;
				if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
				{
					if (remainTime.Hours == 0)
					{
						remainTimeText.text = string.Format("{0:00}:{1:00}", remainTime.Minutes, remainTime.Seconds);
						remainTimeRootObject.SetActive(true);
					}
					else
					{
						remainTimeText.text = "";
						remainTimeRootObject.SetActive(false);
					}

					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			_needUpdate = false;
			remainTimeText.text = "00:00:00";

			// Toast까지 띄울 필요는 없을듯
			gameObject.SetActive(false);
		}
	}

	void RefreshSmallButton()
	{
		bool show = true;

		// 서브퀘는 진행중이지 않을때가 많다.
		if (QuestData.instance.currentQuestStep != QuestData.eQuestStep.Proceeding)
			show = false;

		QuestData.QuestInfo questInfo = QuestData.instance.FindQuestInfoByIndex(QuestData.instance.currentQuestIndex);
		if (questInfo == null)
			show = false;

		smallButtonRootObject.SetActive(show);
	}

	void RefreshInfo()
	{
		if (QuestData.instance.currentQuestStep != QuestData.eQuestStep.Proceeding)
			return;
		QuestData.QuestInfo questInfo = QuestData.instance.FindQuestInfoByIndex(QuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		RefreshCountInfo(0);

		// QuestSelectCanvas 했던거 가져와서 쓴다.
		_questResetTime = QuestData.instance.todayQuestResetTime;
		_needUpdate = true;
		UpdateRemainTime();

		// 카오스가 아니면 쌓을 수 없음을 알려야하는데 만약 완료한 상태라면 그냥 두면 된다.
		if (QuestData.instance.IsCompleteQuest() == false)
		{
			if (PlayerData.instance.currentChaosMode)
			{ }
			else
			{
				contentsRootObject.SetActive(false);
				disableTextObject.SetActive(true);
			}
		}
	}

	void RefreshCountInfo(int temporaryAddCount)
	{
		QuestData.QuestInfo questInfo = QuestData.instance.FindQuestInfoByIndex(QuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		SubQuestTableData subQuestTableData = TableDataManager.instance.FindSubQuestTableData(questInfo.tp);
		if (questInfo.cdtn == (int)QuestData.eQuestCondition.None)
			descriptionText.SetLocalizedText(string.Format("{0} {1} / {2}", UIString.instance.GetString(subQuestTableData.shortDescriptionId), QuestData.instance.currentQuestProceedingCount + temporaryAddCount, questInfo.cnt));
		else
			descriptionText.SetLocalizedText(string.Format("{0} {1} / {2}\n{3}", UIString.instance.GetString(subQuestTableData.shortDescriptionId), QuestData.instance.currentQuestProceedingCount + temporaryAddCount, questInfo.cnt, QuestInfoItem.GetConditionText(questInfo)));

		bool isCompleteQuest = (QuestData.instance.currentQuestProceedingCount + temporaryAddCount >= questInfo.cnt);
		blinkObject.SetActive(isCompleteQuest);
		smallBlinkObject.SetActive(isCompleteQuest);
	}

	public void RefreshAlarmObject()
	{
		// TreasureChestIndicatorCanvas에 붙어있는게 있어서 여기껀 패스해보기로 한다.
		//bool showAlarm = QuestData.instance.IsCompleteQuest();
		//AlarmObject.Hide(alarmRootTransform);
		//if (showAlarm)
		//	AlarmObject.Show(alarmRootTransform, true, true);
	}

	public void CloseInfo()
	{
		smallButtonRootObject.SetActive(false);
		infoRootTweenAnimation.gameObject.SetActive(false);
		smallBackButtonRootObject.SetActive(false);
		_openRemainTime = _closeRemainTime = 0.0f;

		RefreshSmallButton();
	}

	public void RefreshCondition(bool stage)
	{
		if (QuestData.instance.currentQuestStep != QuestData.eQuestStep.Proceeding)
			return;

		QuestData.QuestInfo questInfo = QuestData.instance.FindQuestInfoByIndex(QuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		if (stage)
			AlarmObject.Hide(alarmRootTransform);
	}

	public void OnAddCount(int temporaryAddCount)
	{
		RefreshCountInfo(temporaryAddCount);
	}

	public void OnClickBlinkImage()
	{
		bool lobby = false;
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby) lobby = true;
		if (lobby)
			UIInstanceManager.instance.ShowCanvasAsync("QuestInfoCanvas", null);
		else
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_LobbyToast"), 2.0f);
	}



	#region Show Hide
	public void OnClickSmallButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		smallButtonRootObject.SetActive(false);
		infoRootTweenAnimation.gameObject.SetActive(true);
		smallBackButtonRootObject.SetActive(true);
		UpdateRemainTime();
	}

	float _closeRemainTime;
	public void OnClickSmallBackButton()
	{
		if (_closeRemainTime > 0.0f)
			return;
		if (smallBackButtonRootObject.activeSelf == false)
			return;

		smallBackButtonRootObject.SetActive(false);
		infoRootTweenAnimation.DOPlayBackwards();
		_closeRemainTime = 0.6f;
	}

	float _openRemainTime;
	public void OnCompleteInfoRootTweenAnimation()
	{
		smallBackButtonRootObject.SetActive(true);
		_openRemainTime = 4.0f;
	}
	#endregion
}