using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using MEC;

public class TreasureChestIndicatorCanvas : ObjectIndicatorCanvas
{
	public static TreasureChestIndicatorCanvas instance;

	enum eButtonType
	{
		Shop,
		DailyBox,
		SubQuest,
	}
	eButtonType _buttonType;

	public GameObject buttonRootObject;
	public Text[] buttonTextList;
	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		InitializeTarget(targetTransform);
		RefreshButtonText();
	}

	void OnDisable()
	{
		buttonRootObject.SetActive(false);

		// 6각버튼 애니메이션 끝나고나서 알람 느낌표가 보여야 자연스러워서 애니하고나서 Tween Complete 이벤트로 켜게 되어있다. 그래서 OnDisable때마다 다시 꺼놔야한다.
		alarmRootTransform.gameObject.SetActive(false);
	}

	void RefreshButtonText()
	{
		_buttonType = eButtonType.Shop;

		if (IsDailyBoxType())
			_buttonType = eButtonType.DailyBox;
		else if (IsSubQuestBoxType())
			_buttonType = eButtonType.SubQuest;

		AlarmObject.Hide(alarmRootTransform);

		string stringId = "";
		switch (_buttonType)
		{
			case eButtonType.Shop:
				stringId = "GameUI_Shop";
				break;
			case eButtonType.DailyBox:
				AlarmObject.Show(alarmRootTransform);
				stringId = "GameUI_OneCharBox";
				break;
			case eButtonType.SubQuest:
				if (QuestData.instance.IsCompleteQuest())
					AlarmObject.Show(alarmRootTransform);
				stringId = "GameUI_SubQuestBig";
				break;
		}
		for (int i = 0; i < buttonTextList.Length; ++i)
			buttonTextList[i].SetLocalizedText(UIString.instance.GetString(stringId));
	}

	public static bool IsDailyBoxType()
	{
		if (PlayerData.instance.sharedDailyBoxOpened == false && PlayerData.instance.sealCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("SealMaxCount"))
			return true;
		return false;
	}

	public static bool IsSubQuestBoxType()
	{
		if (PlayerData.instance.sharedDailyBoxOpened == true && PlayerData.instance.chaosModeOpened)
			return true;
		return false;
	}

	public bool IsShowQuestBoxType()
	{
		return (_buttonType == eButtonType.SubQuest);
	}

	public void OnClickButton()
	{
		if (GatePillar.instance.processing)
			return;
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.processing)
			return;

		if (ContentsManager.IsTutorialChapter())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterTraining"), 2.0f);
			return;
		}

		if (PlayerData.instance.lobbyDownloadState)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterDownload"), 2.0f);
			return;
		}

		switch (_buttonType)
		{
			case eButtonType.Shop:
				OnClickOpenShop();
				break;
			case eButtonType.DailyBox:
				OnClickOpenDailyBox();
				break;
			case eButtonType.SubQuest:
				OnClickSubQuest();
				break;
		}
	}

	void OnClickOpenShop()
	{
		Timing.RunCoroutine(ShowCashShopAsync());
	}

	IEnumerator<float> ShowCashShopAsync()
	{
		DelayedLoadingCanvas.Show(true);

		while (CodelessIAPStoreListener.initializationComplete == false)
			yield return Timing.WaitForOneFrame;

		UIInstanceManager.instance.ShowCanvasAsync("CashShopCanvas", () =>
		{
			DelayedLoadingCanvas.Show(false);
		}, false);
	}

	void OnClickOpenDailyBox()
	{
		if (GatePillar.instance.processing)
			return;
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.processing)
			return;
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.processing)
			return;
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.enteredPortal)
			return;

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		bool useSecond = ((PlayerData.instance.secondDailyBoxFillCount + 1) == BattleInstanceManager.instance.GetCachedGlobalConstantInt("SealBigCount"));

		// 가장 핵심은 드랍부터 굴려서 보상정보를 얻어오는거다.
		DropProcessor dropProcessor = DropProcessor.Drop(targetTransform, useSecond ? "Zozoflr" : "Zoflr", "", true, true);
		dropProcessor.AdjustDropRange(3.7f);
		if (CheatingListener.detectedCheatTable)
			return;
		PlayFabApiManager.instance.RequestOpenDailyBox((serverFailure) =>
		{
			if (serverFailure)
			{
				// 뭔가 잘못된건데 응답을 할 필요가 있을까.
			}
			else
			{
				// 연출 및 보상 처리.

				// TreasureChest는 숨겨도 하단 일퀘 갱신은 즉시 보여준다.
				DailyBoxGaugeCanvas.instance.RefreshGauge();

				UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
				{
					RandomBoxScreenCanvas.instance.SetInfo(useSecond ? RandomBoxScreenCanvas.eBoxType.Origin_Big : RandomBoxScreenCanvas.eBoxType.Origin, dropProcessor, 0, 0, () =>
					{
						CharacterBoxConfirmCanvas.OnCompleteRandomBoxScreen(DropManager.instance.GetGrantCharacterInfo(), DropManager.instance.GetTranscendPointInfo(), CharacterBoxConfirmCanvas.OnResult);
					});
				});
			}
		});
	}

	void OnClickSubQuest()
	{
		if (GatePillar.instance.processing)
			return;
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.processing)
			return;
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.processing)
			return;
		if (NodeWarPortal.instance != null && NodeWarPortal.instance.enteredPortal)
			return;

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		// 퀘스트는 현재 진행상황에 따라 다르게 처리되어야한다.
		// 모든걸 완료한 상태라면
		if (QuestData.instance.todayQuestRewardedCount >= QuestData.DailyMaxCount)
		{
			UIInstanceManager.instance.ShowCanvasAsync("QuestEndCanvas", null);
			return;
		}

		// 진행할 수 있는 상황에서 둘중에 하나 선택해야하는 상태
		// 선택 후 진행중이거나 보상을 받을 수 있는 상태
		// 이렇게 2가지다.
		switch (QuestData.instance.currentQuestStep)
		{
			case QuestData.eQuestStep.Select:
				// 생성은 했는데 아직 선택을 하지 않은 상태
				// 생성되어있는 정보로 창을 열기만 하면 된다.
				UIInstanceManager.instance.ShowCanvasAsync("QuestSelectCanvas", null);
				break;
			case QuestData.eQuestStep.Proceeding:
				// 진행중이거나 완료되서 일때도 받은 정보로 보여주기만 하면 된다. 하지만 창이 Select때와 다르다.
				UIInstanceManager.instance.ShowCanvasAsync("QuestInfoCanvas", null);
				break;
		}
	}
}
