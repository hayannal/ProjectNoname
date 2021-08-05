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

	public Slider expGaugeSlider;
	public Image expGaugeImage;
	public DOTweenAnimation expGaugeColorTween;
	public Image expGaugeEndPointImage;

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
			ResearchCanvas.instance.RefreshAlarmObjectList();
			DotMainMenuCanvas.instance.RefreshResearchAlarmObject();
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
		_defaultExpGaugeColor = expGaugeImage.color;
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

		// 화면 전환이 없다보니 제대로 캐싱할 시간은 없고 오브젝트만 만들었다가 꺼두는 캐싱이라도 해둔다.
		if (_disableButton == false)
		{
			GameObject effectObject = BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
			effectObject.SetActive(false);
		}
	}

	void OnDisable()
	{
		ObscuredPrefs.SetInt(OPTION_COMPLETE_ALARM, _onCompleteAlarmState ? 1 : 0);
	}

	void Update()
	{
		UpdateRemainTime();
		UpdatePercentText();
		UpdateExpGauge();
	}

	int _currentLevel;
	float _currentExpPercent;
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
		//bool maxReached = (_currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
		//if (maxReached) hideLevelUpButton = true;
		if (AnalysisData.instance.analysisStarted == false) hideLevelUpButton = true;
		levelUpButtonObject.SetActive(!hideLevelUpButton);

		// exp는 누적된 시간을 구해와서 현재 Required 에 맞게 변환해서 표시하면 된다.
		CalcExpPercent();
		expGaugeSlider.value = _currentExpPercent;
		expGaugeEndPointImage.gameObject.SetActive(false);

		int maxTimeMinute = analysisTableData.maxTime / 60;
		if (maxTimeMinute < 60)
			maxTimeText.text = string.Format("Max {0}m", maxTimeMinute);
		else
			maxTimeText.text = string.Format("Max {0}h", maxTimeMinute / 60);

		RefreshProcessGauge();
		RefreshGetButton();

		_needUpdate = false;
		_maxTimeReached = false;
		if (AnalysisData.instance.analysisStarted == false)
		{
			analyzingText.text = "";
			completeText.text = "";
			remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", maxTimeMinute / 60, maxTimeMinute % 60, 0);
			return;
		}

		DateTime finishTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromSeconds(_analysisTableData.maxTime);
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
			completeText.text = UIString.instance.GetString("AnalysisUI_ProgressFull");
			remainTimeText.text = "00:00:00";
			_maxTimeReached = true;
		}
	}

	void CalcExpPercent()
	{
		int level = 0;
		float percent = 0.0f;
		int maxLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel");
		for (int i = _currentLevel; i < TableDataManager.instance.analysisTable.dataArray.Length; ++i)
		{
			if (AnalysisData.instance.analysisExp < TableDataManager.instance.analysisTable.dataArray[i].requiredAccumulatedTime)
			{
				int currentPeriodExp = AnalysisData.instance.analysisExp - TableDataManager.instance.analysisTable.dataArray[i - 1].requiredAccumulatedTime;
				percent = (float)currentPeriodExp / (float)TableDataManager.instance.analysisTable.dataArray[i].requiredTime;
				level = TableDataManager.instance.analysisTable.dataArray[i].level - 1;
				break;
			}
			if (TableDataManager.instance.analysisTable.dataArray[i].level >= maxLevel)
			{
				level = maxLevel;
				percent = 1.0f;
				break;
			}
		}

		_currentExpPercent = percent;
	}

	bool _disableButton = false;
	void RefreshGetButton()
	{
		_disableButton = false;

		if (AnalysisData.instance.analysisStarted == false)
		{
			getButtonImage.color = ColorUtil.halfGray;
			getButtonText.color = ColorUtil.halfGray;
			_disableButton = true;
			return;
		}

		bool confirmable = false;
		if (_analysisTableData != null)
		{
			if (centerGaugeSlider.value >= 0.5f)
				confirmable = true;
			TimeSpan diffTime = ServerTime.UtcNow - AnalysisData.instance.analysisStartedTime;
			if (diffTime.TotalMinutes > 30)
				confirmable = true;
		}

		_disableButton = !confirmable;
		getButtonImage.color = _disableButton ? ColorUtil.halfGray : Color.white;
		getButtonText.color = _disableButton ? ColorUtil.halfGray : Color.white;
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
		float processRatio = (float)totalSeconds / _analysisTableData.maxTime;
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
		DateTime finishTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromSeconds(_analysisTableData.maxTime);
		if (ServerTime.UtcNow < finishTime)
		{
			TimeSpan remainTime = finishTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				RefreshProcessGauge();
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				analyzingText.text = string.Format("{0}{1}", _progressOngoingString, GetDotString(_lastRemainTimeSecond));
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;

				if (_disableButton)
					RefreshGetButton();
			}
		}
		else
		{
			_needUpdate = false;
			_maxTimeReached = true;
			RefreshProcessGauge();
			analyzingText.text = "";
			completeText.text = UIString.instance.GetString("AnalysisUI_ProgressFull");
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

	float _percentTextZeroRemainTime = 0.0f;
	void UpdatePercentText()
	{
		if (_percentTextZeroRemainTime > 0.0f)
		{
			_percentTextZeroRemainTime -= Time.deltaTime;
			percentText.text = string.Format("{0:0.00}%", centerGaugeSlider.value * 100.0f);

			if (_percentTextZeroRemainTime <= 0.0f)
			{
				_percentTextZeroRemainTime = 0.0f;
				percentText.text = "0.00%";
			}
		}
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
		AnalysisData.instance.ReserveAnalysisNotification();
#elif UNITY_IOS
		MobileNotificationWrapper.instance.CheckAuthorization(() =>
		{
			AnalysisData.instance.ReserveAnalysisNotification();
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

		AnalysisData.instance.CancelAnalysisNotification();
	}
	#endregion

	public static bool CheckAnalysis()
	{
		if (EventManager.instance.reservedOpenAnalysisEvent)
			return true;
		if (AnalysisData.instance.analysisStarted == false)
			return false;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(AnalysisData.instance.analysisLevel);
		if (analysisTableData == null)
			return false;

		DateTime completeTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromSeconds(analysisTableData.maxTime);
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
		if (_currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("AnalysisLevelUpCanvas", null);
	}

	public void OnClickRemainTimeButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AnalysisUI_LeftTimeMore"), 250, remainTimeText.transform, new Vector2(15.0f, -35.0f));
	}

	public void OnClickButton()
	{
		if (AnalysisData.instance.analysisStarted == false)
		{
			return;
		}

		if (_disableButton)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AnalysisUI_NotEnoughCondition"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestAnalysis(() =>
		{
			OnAnalysisResult();
			Timing.RunCoroutine(AnalysisResultProcess());
		});
	}

	public void OnAnalysisResult()
	{
		if (_maxTimeReached)
		{
			AlarmObject.Hide(alarmRootTransform);
			ResearchCanvas.instance.RefreshAlarmObjectList();
			DotMainMenuCanvas.instance.RefreshResearchAlarmObject();
		}
		else
		{
			if (_onCompleteAlarmState)
				AnalysisData.instance.CancelAnalysisNotification();
		}
	}

	#region Exp Percent Gauge
	public void RefreshExpPercent(float targetPercent, int levelUpCount)
	{
		_targetPercent = targetPercent;
		_levelUpCount = levelUpCount;

		float totalDiff = levelUpCount;
		totalDiff += (targetPercent - expGaugeSlider.value);
		_fillSpeed = totalDiff / LevelUpExpFillTime;
		_fillRemainTime = LevelUpExpFillTime;
		
		expGaugeColorTween.DORestart();
		expGaugeEndPointImage.color = new Color(expGaugeEndPointImage.color.r, expGaugeEndPointImage.color.g, expGaugeEndPointImage.color.b, _defaultExpGaugeColor.a);
		expGaugeEndPointImage.gameObject.SetActive(true);
	}

	Color _defaultExpGaugeColor;
	const float LevelUpExpFillTime = 0.6f;
	float _fillRemainTime;
	float _fillSpeed;
	float _targetPercent;
	int _levelUpCount;
	void UpdateExpGauge()
	{
		if (_fillRemainTime > 0.0f)
		{
			_fillRemainTime -= Time.deltaTime;
			expGaugeSlider.value += _fillSpeed * Time.deltaTime;
			if (expGaugeSlider.value >= 1.0f && _levelUpCount > 0)
			{
				expGaugeSlider.value -= 1.0f;
				_levelUpCount -= 1;
			}

			if (_fillRemainTime <= 0.0f)
			{
				_fillRemainTime = 0.0f;
				expGaugeSlider.value = _targetPercent;

				expGaugeColorTween.DOPause();
				expGaugeImage.color = _defaultExpGaugeColor;
				expGaugeEndPointImage.DOFade(0.0f, 1.0f).SetEase(Ease.OutQuad);
			}
		}
	}
	#endregion

	IEnumerator<float> AnalysisResultProcess()
	{
		// 인풋 차단
		ResearchCanvas.instance.inputLockObject.SetActive(true);
		getButtonImage.color = ColorUtil.halfGray;
		getButtonText.color = ColorUtil.halfGray;

		// 시간 업뎃을 멈추고 게이지부터 내린다.
		_needUpdate = false;
		completeText.text = "";
		analyzingText.text = "";
		_percentTextZeroRemainTime = LevelUpExpFillTime;
		DOTween.To(() => centerGaugeSlider.value, x => centerGaugeSlider.value = x, 0.0f, LevelUpExpFillTime).SetEase(Ease.Linear);

		// 경험치 슬라이더도 함께 움직여야한다.
		bool showLevelUp = (AnalysisData.instance.analysisLevel - _currentLevel > 0);
		CalcExpPercent();
		RefreshExpPercent(_currentExpPercent, AnalysisData.instance.analysisLevel - _currentLevel);
		//yield return Timing.WaitForSeconds(LevelUpExpFillTime - 0.3f);


		// 오브젝트 정지
		ResearchObjects.instance.objectTweenAnimation.DOTogglePause();
		yield return Timing.WaitForSeconds(0.3f);

		// 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
		yield return Timing.WaitForSeconds(2.0f);


		// 마지막에 알람도 다시 예약. 이 잠깐의 연출이 나오는동안 앱을 종료시키면 예약이 안될수도 있는데 이런 경우는 패스하기로 한다.
		if (_onCompleteAlarmState)
			AnalysisData.instance.ReserveAnalysisNotification();


		// 결과창 로딩 후 열리는 타이밍에 마지막 처리를 전달
		Action action = () =>
		{
			// 여기서 다이아 갱신까지 다시 되게 한다.
			ResearchCanvas.instance.currencySmallInfo.RefreshInfo();
			RefreshGetButton();
			RefreshLevelInfo();

			// 토글 복구
			ResearchObjects.instance.objectTweenAnimation.DOTogglePause();

			// 인풋 복구
			ResearchCanvas.instance.inputLockObject.SetActive(false);
		};


		// 보상 연출을 시작해야하는데 오리진이 있을때와 없을때로 구분된다.
		List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
		List<DropManager.CharacterTrpRequest> listTrpInfo = DropManager.instance.GetTranscendPointInfo();
		if (showLevelUp || listGrantInfo.Count + listTrpInfo.Count > 0)
		{
			// 이때는 풀스크린 결과창을 띄운다
			UIInstanceManager.instance.ShowCanvasAsync("AnalysisResultCanvas", () =>
			{
				action.Invoke();
			});
		}
		else
		{
			// 이때는 심플 결과창을 띄운다. 평소에는 이걸 제일 많이 보게될거다.
			UIInstanceManager.instance.ShowCanvasAsync("AnalysisSimpleResultCanvas", () =>
			{
				action.Invoke();
			});
		}
	}

	// UI는 다 여기있으니 여기서 처리하는게 맞다.
	public IEnumerator<float> LevelUpAnalysisProcess()
	{
		// 인풋 차단
		ResearchCanvas.instance.inputLockObject.SetActive(true);
		getButtonImage.color = ColorUtil.halfGray;
		getButtonText.color = ColorUtil.halfGray;

		// 시간 업뎃을 멈추고 여기선 게이지 내릴 필요 없으니 바로 퍼센트만 처리한다.
		_needUpdate = false;
		bool showLevelUp = (AnalysisData.instance.analysisLevel - _currentLevel > 0);
		CalcExpPercent();
		RefreshExpPercent(_currentExpPercent, AnalysisData.instance.analysisLevel - _currentLevel);
		//yield return Timing.WaitForSeconds(LevelUpExpFillTime - 0.3f);


		// 오브젝트 정지
		ResearchObjects.instance.objectTweenAnimation.DOTogglePause();
		yield return Timing.WaitForSeconds(0.3f);

		// 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
		yield return Timing.WaitForSeconds(2.0f);


		// 마지막에 알람도 다시 예약. 이 잠깐의 연출이 나오는동안 앱을 종료시키면 예약이 안될수도 있는데 이런 경우는 패스하기로 한다.
		if (_onCompleteAlarmState)
			AnalysisData.instance.ReserveAnalysisNotification();


		// 결과창 로딩 후 열리는 타이밍에 마지막 처리를 전달
		Action action = () =>
		{
			RefreshGetButton();
			RefreshLevelInfo();

			// 토글 복구
			ResearchObjects.instance.objectTweenAnimation.DOTogglePause();

			// 인풋 복구
			ResearchCanvas.instance.inputLockObject.SetActive(false);
		};


		// 보상 연출을 시작해야하는데 오리진이 있을때와 없을때로 구분된다.
		// 이때는 풀스크린 결과창을 띄운다
		UIInstanceManager.instance.ShowCanvasAsync("AnalysisResultCanvas", () =>
		{
			action.Invoke();
		});
	}
}