using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Michsky.UI.Hexart;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class ResearchInfoAnalysisCanvas : MonoBehaviour
{
	public static ResearchInfoAnalysisCanvas instance;

	public Transform analysisTextTransform;
	public Text analysisText;

	public RectTransform positionRectTransform;
	public Text levelText;
	public GameObject levelUpButtonObject;
	public Slider expSlider;

	public GameObject switchGroupObject;
	public SwitchAnim alarmSwitch;
	public Text alarmOnOffText;

	public Slider centerGaugeSlider;
	public Image centerGaugeFillImage;
	public Text maxTimeText;
	public Text percentText;
	public Text analyzingText;
	public Text completeText;
	public Text remainTimeText;

	public GameObject getButtonObject;
	public Image getButtonImage;
	public Text getButtonText;
	public RectTransform alarmRootTransform;

	public GameObject effectPrefab;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		if (EventManager.instance.reservedOpenAnalysisEvent)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_AnalysisName"), UIString.instance.GetString("GameUI_AnalysisDesc"), UIString.instance.GetString("GameUI_AnalysisMore"), null, 0.785f);
			});
			EventManager.instance.reservedOpenAnalysisEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.analysis);
		}

		// 처음이라면 분석시작을 서버에 알려서 기록해야한다. 딱 한번만 날리는 패킷
		if (AnalysisData.instance.analysisLevel == 1 && AnalysisData.instance.analysisStarted == false)
		{
			PlayFabApiManager.instance.RequestStartAnalysis(() =>
			{
				RefreshLevelInfo();
			});
		}

		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_ignoreStartEvent = true;
	}

	Vector2 _leftTweenPosition = new Vector2(-150.0f, 0.0f);
	Vector2 _rightTweenPosition = new Vector2(150.0f, 0.0f);
	TweenerCore<Vector2, Vector2, VectorOptions> _tweenReferenceForMove;
	void MoveTween(bool left)
	{
		if (_tweenReferenceForMove != null)
			_tweenReferenceForMove.Kill();

		positionRectTransform.gameObject.SetActive(false);
		positionRectTransform.gameObject.SetActive(true);
		positionRectTransform.anchoredPosition = left ? _leftTweenPosition : _rightTweenPosition;
		_tweenReferenceForMove = positionRectTransform.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
	}

	
	void OnEnable()
	{
		if (ObscuredPrefs.HasKey(OPTION_COMPLETE_ALARM))
			_onCompleteAlarmState = ObscuredPrefs.GetInt(OPTION_COMPLETE_ALARM) == 1;

		RefreshInfo();
		MoveTween(true);

		// Refresh AlarmObject
		if (_maxTimeReached)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);
	}

	void OnDisable()
	{
		ObscuredPrefs.SetInt(OPTION_COMPLETE_ALARM, _onCompleteAlarmState ? 1 : 0);
	}

	void Update()
	{
		UpdateRemainTime();
	}

	int _currentLevel;
	public void RefreshInfo()
	{
		analysisText.text = UIString.instance.GetString("AnalysisUI_Analysis");

		RefreshAlarm();
		RefreshLevelInfo();
	}

	bool _onCompleteAlarmState = false;
	string OPTION_COMPLETE_ALARM = "_option_analysis_alarm_key";
	void RefreshAlarm()
	{
		_notUserSetting = true;
		alarmSwitch.isOn = _onCompleteAlarmState;
		_notUserSetting = false;
	}

	ObscuredBool _maxTimeReached = false;
	AnalysisTableData _analysisTableData;
	void RefreshLevelInfo()
	{
		_currentLevel = AnalysisData.instance.analysisLevel;
		levelText.text = UIString.instance.GetString("GameUI_Lv", _currentLevel);

		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(_currentLevel);
		if (analysisTableData == null)
			return;

		_analysisTableData = analysisTableData;

		bool hideLevelUpButton = false;
		bool maxReached = (_currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
		if (maxReached) hideLevelUpButton = true;
		if (AnalysisData.instance.analysisStarted == false) hideLevelUpButton = true;
		levelUpButtonObject.SetActive(!hideLevelUpButton);

		// exp는 누적된 시간을 구해와서 현재 Required 에 맞게 변환해서 표시하면 된다.
		expSlider.value = 0.0f;

		if (analysisTableData.maxTime < 60)
			maxTimeText.text = string.Format("Max {0}m", analysisTableData.maxTime);
		else
			maxTimeText.text = string.Format("Max {0}h", analysisTableData.maxTime / 60);

		RefreshGetButton();
		RefreshProcessGauge();

		_needUpdate = false;
		_maxTimeReached = false;
		if (AnalysisData.instance.analysisStarted == false)
		{
			analyzingText.text = "";
			completeText.text = "";
			remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", analysisTableData.maxTime / 60, analysisTableData.maxTime % 60, 0);
			return;
		}

		DateTime finishTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromMinutes(_analysisTableData.maxTime);
		if (ServerTime.UtcNow < finishTime)
		{
			analyzingText.text = "";
			completeText.text = "";
			_progressOngoingString = UIString.instance.GetString("AnalysisUI_ProgressOngoing");
			_needUpdate = true;
		}
		else
		{
			analyzingText.text = "";
			completeText.text = UIString.instance.GetString("QuestUI_QuestCompleteNoti");
			remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", 0, 0, 0);
			_maxTimeReached = true;
		}
	}

	void RefreshGetButton()
	{
		if (AnalysisData.instance.analysisStarted == false)
		{
			getButtonImage.color = ColorUtil.halfGray;
			getButtonText.color = ColorUtil.halfGray;
			return;
		}
	}

	void RefreshProcessGauge()
	{
		// centerGauge는 시작한 시간부터의 경과값을 변환해서 표시하면 된다.
		// 만약 아직 시작한 상태가 아니라면 0인 상태로 표시하면 될거다.
		int totalSeconds = 0;
		if (AnalysisData.instance.analysisStarted)
		{
			TimeSpan timeSpan = ServerTime.UtcNow - AnalysisData.instance.analysisStartedTime;
			totalSeconds = (int)timeSpan.TotalSeconds;
		}
		float processRatio = (float)totalSeconds / (_analysisTableData.maxTime * 60);
		if (processRatio > 1.0f) processRatio = 1.0f;
		centerGaugeSlider.value = processRatio;
		percentText.text = string.Format("{0:0.00}%", processRatio * 100.0f);
		centerGaugeFillImage.color = (processRatio >= 1.0f) ? new Color(1.0f, 1.0f, 0.0f, centerGaugeFillImage.color.a) : new Color(1.0f, 1.0f, 1.0f, centerGaugeFillImage.color.a);
	}

	string _progressOngoingString = "";
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (AnalysisData.instance.analysisStarted == false)
			return;
		if (_analysisTableData == null)
			return;
		if (_needUpdate == false)
			return;

		// 매프레임 계산하기엔 너무 부하가 심할수도 있으니 1초에 한번만 하기로 한다.
		DateTime finishTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromMinutes(_analysisTableData.maxTime);
		if (ServerTime.UtcNow < finishTime)
		{
			TimeSpan remainTime = finishTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				RefreshProcessGauge();
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				analyzingText.text = string.Format("{0}{1}", _progressOngoingString, GetDotString(_lastRemainTimeSecond));
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			_needUpdate = false;
			_maxTimeReached = true;
			RefreshProcessGauge();
			analyzingText.text = "";
			completeText.text = UIString.instance.GetString("QuestUI_QuestCompleteNoti");
			remainTimeText.text = "00:00:00";
			AlarmObject.Show(alarmRootTransform);
			ResearchCanvas.instance.RefreshAlarmObjectList();
		}
	}

	string GetDotString(int lastRemainTimeSecond)
	{
		int result = lastRemainTimeSecond % 3;
		switch (result)
		{
			case 0: return "...";
			case 1: return "..";
			case 2: return ".";
		}
		return ".";
	}

	#region Alarm
	bool _ignoreStartEvent = false;
	bool _notUserSetting = false;
	public void OnSwitchOnCompleteAlarm()
	{
		_onCompleteAlarmState = true;
		alarmOnOffText.text = "ON";
		alarmOnOffText.color = Color.white;

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

		// 최초 분석 시작도 안된상태에서 누르게 되면 리셋해둬야한다.
		if (AnalysisData.instance.analysisStarted == false)
		{
			//ToastCanvas.instance.ShowToast(UIString.instance.GetString("AnalysisUI_FirstStart"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
			return;
		}

#if UNITY_ANDROID
		//CurrencyData.instance.ReserveEnergyNotification();
#elif UNITY_IOS
		MobileNotificationWrapper.instance.CheckAuthorization(() =>
		{
			//CurrencyData.instance.ReserveEnergyNotification();
		}, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_EnergyNotiAppleLast"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
		});
#endif
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		alarmSwitch.AnimateSwitch();
	}

	public void OnSwitchOffCompleteAlarm()
	{
		_onCompleteAlarmState = false;
		alarmOnOffText.text = "OFF";
		alarmOnOffText.color = new Color(0.176f, 0.176f, 0.176f);

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

		//CurrencyData.instance.CancelEnergyNotification();
	}
	#endregion

	public static bool CheckAnalysis()
	{
		if (AnalysisData.instance.analysisStarted == false)
			return false;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(AnalysisData.instance.analysisLevel);
		if (analysisTableData == null)
			return false;

		DateTime completeTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromMinutes(analysisTableData.maxTime);
		if (ServerTime.UtcNow < completeTime)
		{
		}
		else
			return true;

		return false;
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AnalysisUI_AnalysisMore"), 250, analysisTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickLevelUpButton()
	{

	}

	public void OnClickRemainTimeButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AnalysisUI_LeftTimeMore"), 250, remainTimeText.transform, new Vector2(15.0f, -35.0f));
	}

	public void OnClickButton()
	{
		/*
		if (_notEnough)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString(conditionSumLevelGroupObject.activeSelf ? "ResearchUI_NotEnoughLevel" : "ResearchUI_NotEnoughCharacter"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		priceButtonObject.SetActive(false);
		PlayFabApiManager.instance.RequestResearchLevelUp(_selectedLevel, _price, _rewardDia, () =>
		{
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ResearchLevel);

			// 다이아 보상 받는건 연출 뒤에 반영되게 하려고 예외처리 해둔다.
			//ResearchCanvas.instance.currencySmallInfo.RefreshInfo();
			ResearchCanvas.instance.currencySmallInfo.goldText.text = CurrencyData.instance.gold.ToString("N0");

			DotMainMenuCanvas.instance.RefreshResearchAlarmObject();
			Timing.RunCoroutine(ResearchLevelUpProcess());
		});
		*/
	}

	IEnumerator<float> ResearchLevelUpProcess()
	{
		// 인풋 차단
		ResearchCanvas.instance.inputLockObject.SetActive(true);

		// 오브젝트 정지
		ResearchObjects.instance.objectTweenAnimation.DOTogglePause();
		yield return Timing.WaitForSeconds(0.3f);

		// 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
		yield return Timing.WaitForSeconds(2.0f);

		// 여기서 다이아 갱신까지 다시 되게 한다.
		ResearchCanvas.instance.currencySmallInfo.RefreshInfo();

		/*
		// Toast 알림
		string stringId = diaObject.activeSelf ? "ResearchUI_RewardedCurrency" : "ResearchUI_RewardedStat";
		ToastCanvas.instance.ShowToast(UIString.instance.GetString(stringId), 3.0f);
		yield return Timing.WaitForSeconds(1.0f);

		// nextInfo
		priceButtonObject.SetActive(true);
		if (rightButton.gameObject.activeSelf)
		{
			OnClickRightButton();
			yield return Timing.WaitForSeconds(0.4f);
		}
		else
		{
			RefreshLevelInfo();
		}

		// 토글 복구
		ResearchObjects.instance.objectTweenAnimation.DOTogglePause();

		// 인풋 복구
		ResearchCanvas.instance.inputLockObject.SetActive(false);
		*/
	}
}