using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class AnalysisLevelUpCanvas : MonoBehaviour
{
	public static AnalysisLevelUpCanvas instance;

	public Transform subTitleTransform;

	public Text currentLevelText;
	public Text nextLevelText;
	public Text needText;
	public Text maxTimeText;
	public Text nextMaxTimeText;

	// 10단위 레벨업 버튼
	public Text tenCurrentLevelText;
	public Text tenNextLevelText;
	public Text tenNeedText;
	public Text tenMaxTimeText;
	public Text tenNextMaxTimeText;
	public Text tenLevelUpButtonDescText;

	public Text priceText;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public Text tenPriceText;
	public Image tenPriceButtonImage;
	public Coffee.UIExtensions.UIEffect tenPriceGrayscaleEffect;

	void Awake()
	{
		instance = this;
	}

	ObscuredInt _nextLevel;
	ObscuredInt _jumpLevel;
	ObscuredInt _price;
	ObscuredInt _tenPrice;
	void OnEnable()
	{
		int currentLevel = AnalysisData.instance.analysisLevel;
		tenCurrentLevelText.text = currentLevelText.text = UIString.instance.GetString("GameUI_Lv", currentLevel);

		int currentLevelMaxTime = 0;
		AnalysisTableData currentLevelTableData = TableDataManager.instance.FindAnalysisTableData(currentLevel);
		if (currentLevelTableData != null)
		{
			maxTimeText.text = tenMaxTimeText.text = string.Format("Max {0}", GetMaxTimeText(currentLevelTableData.maxTime));
			currentLevelMaxTime = currentLevelTableData.maxTime;
		}

		AnalysisTableData nextLevelTableData = TableDataManager.instance.FindAnalysisTableData(currentLevel + 1);
		if (nextLevelTableData != null)
		{
			nextLevelText.text = UIString.instance.GetString("GameUI_Lv", currentLevel + 1);

			int remainExp = nextLevelTableData.requiredAccumulatedTime - AnalysisData.instance.analysisExp;
			needText.text = GetNeedTimeText(remainExp);

			nextMaxTimeText.text = GetMaxTimeText(nextLevelTableData.maxTime);
			nextMaxTimeText.color = (currentLevelMaxTime == nextLevelTableData.maxTime) ? Color.white : tenNextMaxTimeText.color;
			_price = nextLevelTableData.forceLeveling;
			_nextLevel = currentLevel + 1;
		}

		int jumpLevel = 0;
		if (currentLevel >= 1 && currentLevel <= 8)
			jumpLevel = 10;
		else if (currentLevel >= 9 && currentLevel <= 18)
			jumpLevel = 20;
		else if (currentLevel >= 19 && currentLevel <= 28)
			jumpLevel = 30;
		else if (currentLevel >= 29 && currentLevel <= 38)
			jumpLevel = 40;
		else if (currentLevel >= 39 && currentLevel <= 48)
			jumpLevel = 50;
		else if (currentLevel >= 49 && currentLevel <= 58)
			jumpLevel = 60;
		else if (currentLevel >= 59 && currentLevel <= 68)
			jumpLevel = 70;
		else if (currentLevel >= 69 && currentLevel <= 78)
			jumpLevel = 80;
		else if (currentLevel >= 79 && currentLevel <= 88)
			jumpLevel = 90;
		else if (currentLevel >= 89 && currentLevel <= 98)
			jumpLevel = 100;
		else if (currentLevel >= 99 && currentLevel <= 108)
			jumpLevel = 110;
		else if (currentLevel >= 109 && currentLevel <= 118)
			jumpLevel = 120;
		else if (currentLevel >= 119 && currentLevel <= 128)
			jumpLevel = 130;
		else if (currentLevel >= 129 && currentLevel <= 138)
			jumpLevel = 140;

		int maxLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel");
		if (jumpLevel > maxLevel)
			jumpLevel = maxLevel;

		AnalysisTableData jumpLevelTableData = TableDataManager.instance.FindAnalysisTableData(jumpLevel);
		if (jumpLevelTableData != null)
		{
			tenNextLevelText.text = UIString.instance.GetString("GameUI_Lv", jumpLevel);

			int remainExp = jumpLevelTableData.requiredAccumulatedTime - AnalysisData.instance.analysisExp;
			tenNeedText.text = GetNeedTimeText(remainExp);

			tenNextMaxTimeText.text = GetMaxTimeText(jumpLevelTableData.maxTime);
			tenLevelUpButtonDescText.SetLocalizedText(UIString.instance.GetString("AnalysisUI_JumpLevel", jumpLevel));

			_tenPrice = 0;
			for (int i = currentLevel + 1; i <= jumpLevel; ++i)
			{
				AnalysisTableData iTableData = TableDataManager.instance.FindAnalysisTableData(i);
				if (iTableData == null)
					continue;
				_tenPrice += iTableData.forceLeveling;
			}
			_jumpLevel = jumpLevel;
		}

		int price = _price;
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		price = _tenPrice;
		tenPriceText.text = price.ToString("N0");
		disablePrice = (CurrencyData.instance.dia < price);
		tenPriceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		tenPriceText.color = !disablePrice ? Color.white : Color.gray;
		tenPriceGrayscaleEffect.enabled = disablePrice;
	}

	public static string GetMaxTimeText(int tableMaxTime)
	{
		int maxTimeMinute = tableMaxTime / 60;
		if (maxTimeMinute < 60)
			return string.Format("{0}m", maxTimeMinute);
		else
			return string.Format("{0}h", maxTimeMinute / 60);
	}

	string GetNeedTimeText(int remainTime)
	{
		int remainTimeMinute = remainTime / 60;
		int remainTimeHour = remainTimeMinute / 60;
		int remainTimeDay = remainTimeHour / 24;
		if (remainTimeDay > 0)
			return string.Format("NEEDED : {0}d {1}h", remainTimeDay, remainTimeHour % 24);
		else
			return string.Format("NEEDED : {0}h {1}m", remainTimeHour, remainTimeMinute % 60);
	}

	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AnalysisUI_LevelUpMore"), 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickLevelUpButton()
	{
		if (CurrencyData.instance.dia < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestLevelUpAnalysis(AnalysisData.instance.analysisLevel, _nextLevel, _price, OnRecvLevelUpAnalysis);
	}

	public void OnClickJumpLevelUpButton()
	{
		if (CurrencyData.instance.dia < _tenPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestLevelUpAnalysis(AnalysisData.instance.analysisLevel, _jumpLevel, _tenPrice, OnRecvLevelUpAnalysis);
	}

	void OnRecvLevelUpAnalysis()
	{
		ResearchCanvas.instance.currencySmallInfo.RefreshInfo();

		// 알람 같은거 초기화 할거 본체 캔버스꺼로 초기화 해주고
		if (ResearchInfoAnalysisCanvas.instance != null)
			ResearchInfoAnalysisCanvas.instance.OnAnalysisResult();

		gameObject.SetActive(false);

		Timing.RunCoroutine(ResearchInfoAnalysisCanvas.instance.LevelUpAnalysisProcess());
	}
}