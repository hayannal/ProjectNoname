using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class QuestInfoCanvas : MonoBehaviour
{
	public static QuestInfoCanvas instance;

	public Transform subTitleTransform;
	public Text descText;
	public Text remainTimeText;
	public QuestInfoItem info;
	public Image gaugeImage;
	public Text gaugeText;
	public Text completeText;

	public Text priceText;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public Text claimText;
	public Image claimButtonImage;

	void Awake()
	{
		instance = this;
	}

	ObscuredBool _complete;
	int _price;
	int _addGold;
	void OnEnable()
	{
		CurrencySmallInfoCanvas.Show(true);

		// 진행중인 퀘스트 역시 시간제한이 걸려있다. 오늘 안하면 모든게 초기화.
		_questResetTime = QuestData.instance.todayQuestResetTime;
		_needUpdate = true;

		QuestData.QuestInfo questInfo = QuestData.instance.FindQuestInfoByIndex(QuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		info.RefreshInfo(questInfo);
		_addGold = questInfo.rwd;

		// 진행도 표시
		int currentCount = QuestData.instance.currentQuestProceedingCount;
		int maxCount = questInfo.cnt;

		// 완료 체크
		_complete = (currentCount >= maxCount);

		if (_complete)
		{
			descText.SetLocalizedText(UIString.instance.GetString("QuestUI_OneDone"));
			gaugeImage.color = DailyFreeItem.GetGoldTextColor();
			completeText.text = UIString.instance.GetString("QuestUI_QuestCompleteNoti");
		}
		else
		{
			if (PlayerData.instance.currentChaosMode)
				descText.SetLocalizedText(UIString.instance.GetString("QuestUI_NowQuest"));
			else
				descText.SetLocalizedText(string.Format("{0}\n{1}", UIString.instance.GetString("QuestUI_NowQuest"), UIString.instance.GetString("QuestUI_NotChaos")));
			gaugeImage.color = Color.white;
		}
		gaugeImage.fillAmount = (float)(currentCount) / maxCount;
		gaugeText.text = string.Format("{0} / {1}", currentCount, maxCount);
		completeText.gameObject.SetActive(_complete);

		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("SubQuestGoldDoubleDiamond");
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < price || !_complete);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		bool disableButton = !_complete;
		claimButtonImage.color = !disableButton ? Color.white : ColorUtil.halfGray;
		claimText.color = !disableButton ? Color.white : Color.gray;

		_price = price;
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

	public void OnClickDoubleClaimButton()
	{
		if (!_complete)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_CompleteFirst"), 2.0f);
			return;
		}

		if (CurrencyData.instance.dia < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		_addGold *= 2;
		PlayFabApiManager.instance.RequestCompleteQuest(true, _price, _addGold, OnRecvCompleteQuest);
	}

	public void OnClickClaimButton()
	{
		if (!_complete)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_CompleteFirst"), 2.0f);
			return;
		}

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		PlayFabApiManager.instance.RequestCompleteQuest(false, 0, _addGold, OnRecvCompleteQuest);
	}

	void OnRecvCompleteQuest()
	{
		// 우선 보상처리를 위해 골드 연출부터 하고
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 닫는건 RandomBoxScreenCanvas에서 확인하고 알아서 닫아줄거다.
			//gameObject.SetActive(false);

			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "QuestGold", "", true, true);
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Origin, dropProcessor, 0, 0, () =>
			{
				// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
				DropManager.instance.ClearLobbyDropInfo();

				// 결과로는 공용 재화 획득창을 띄워준다.
				UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
				{
					CurrencyBoxResultCanvas.instance.RefreshInfo(_addGold, 0, 0, false, true);
				});
			});
		});
	}

	public void OnCompleteRandomBox()
	{
		// 연출 후 아직 3회 전부한게 아니라면 퀘스트 선택창을 띄운다.
		if (QuestData.instance.todayQuestRewardedCount < QuestData.DailyMaxCount)
		{
			UIInstanceManager.instance.ShowCanvasAsync("QuestSelectCanvas", null);
			return;
		}

		// 3회 전부 한거라면 토스트 띄울까 했는데 그냥 패스
	}
}