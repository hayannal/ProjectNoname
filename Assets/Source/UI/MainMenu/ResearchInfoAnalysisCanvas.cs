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

		// caching
		_defaultExpGaugeColor = expGaugeImage.color;
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

		MoveTween(true);
		RefreshInfo();

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
		bool maxReached = (_currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
		//if (maxReached) hideLevelUpButton = true;
		if (AnalysisData.instance.analysisStarted == false) hideLevelUpButton = true;
		levelUpButtonObject.SetActive(!hideLevelUpButton);

		// exp는 누적된 시간을 구해와서 현재 Required 에 맞게 변환해서 표시하면 된다.
		CalcExpPercent();
		expGaugeSlider.value = _currentExpPercent;
		expGaugeImage.color = maxReached ? new Color(1.0f, 1.0f, 0.25f, 1.0f) : _defaultExpGaugeColor;
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
		AlarmObject.Hide(alarmRootTransform);
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
			AlarmObject.Show(alarmRootTransform);
		}
	}

	void CalcExpPercent()
	{
		int level = 0;
		float percent = 0.0f;
		int maxLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel");
		for (int i = _currentLevel - 1; i < TableDataManager.instance.analysisTable.dataArray.Length; ++i)
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
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours + ((remainTime.Days > 0) ? remainTime.Days * 24 : 0), remainTime.Minutes, remainTime.Seconds);
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
		_changeCount = 0;
		_targetLevel = _currentLevel + levelUpCount;

		// 이미 맥스라면
		if (expGaugeSlider.value >= 1.0f)
			return;

		expGaugeColorTween.DORestart();
		expGaugeEndPointImage.color = new Color(expGaugeEndPointImage.color.r, expGaugeEndPointImage.color.g, expGaugeEndPointImage.color.b, _defaultExpGaugeColor.a);
		expGaugeEndPointImage.gameObject.SetActive(true);

		// 최대레벨로 되는 연출상황일땐 1을 빼놔야 괜히 한바퀴 돌아서 게이지가 차지 않고 최대치에 닿았을때 한번에 끝나게 된다.
		if (targetPercent >= 1.0f && _targetLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"))
			_levelUpCount -= 1;
	}

	Color _defaultExpGaugeColor;
	const float LevelUpExpFillTime = 0.6f;
	float _fillRemainTime;
	float _fillSpeed;
	float _targetPercent;
	int _levelUpCount;
	int _changeCount;
	int _targetLevel;
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

				++_changeCount;
				levelText.text = UIString.instance.GetString("GameUI_Lv", _currentLevel + _changeCount);
			}

			if (_fillRemainTime <= 0.0f)
			{
				_fillRemainTime = 0.0f;
				expGaugeSlider.value = _targetPercent;
				levelText.text = UIString.instance.GetString("GameUI_Lv", _targetLevel);

				expGaugeColorTween.DOPause();
				bool maxReached = (_targetLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
				expGaugeImage.color = maxReached ? new Color(1.0f, 1.0f, 0.25f, 1.0f) : _defaultExpGaugeColor;
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
		if (listGrantInfo.Count + listTrpInfo.Count > 0)
		{
			_listGrantInfo = listGrantInfo;
			_listTrpInfo = listTrpInfo;
			// 캐릭터 연출이 다 끝나고 나서 실행할 액션을 _resultAction에 저장해두고 연출을 진행
			_resultAction = () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("AnalysisResultCanvas", () =>
				{
					// 여기서 꺼야 제일 자연스럽다. 결과창이 로딩되서 보여지는 동시에 Show모드에서 돌아온다.
					if (CharacterBoxShowCanvas.instance != null && CharacterBoxShowCanvas.instance.gameObject.activeSelf)
						CharacterBoxShowCanvas.instance.gameObject.SetActive(false);

					// CharacterBoxShowCanvas가 닫아지면서 InfoCameraMode가 해제될테고
					// Stack되어있던 CharacterBoxShowCanvas를 Pop시키면 ResearchCanvas가 복구될거다.
					// 그리고 직접 InfoCameraMode 해제한거 역시 복구해줘야한다.
					StackCanvas.Pop(CharacterBoxShowCanvas.instance.gameObject);
					ResearchCanvas.instance.SetInfoCameraMode(true);

					AnalysisResultCanvas.instance.RefreshInfo(showLevelUp, _currentLevel, true);
					action.Invoke();
				});
			};

			UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxShowCanvas", () =>
			{
				// 이미 ResearchCanvas에 의해 InfoCameraMode로 전환되어있는 상태라서 CharacterBoxShowCanvas가 생성되면 중복호출되게 된다.
				// 그렇다고 ResearchCanvas를 닫으면 DotMainMenu만 남기때문에 전역 라이트가 어두워진 상태가 된다.
				//ResearchCanvas.instance.gameObject.SetActive(false);
				// 그래서 차라리 StackCanvas사용해서 하나 더 Stack시키면서 ResearchCanvas를 닫는 동시에
				// 직접 InfoCameraMode를 해제한 후 CharacterBoxShowCanvas가 보여지게 한다.
				StackCanvas.Push(CharacterBoxShowCanvas.instance.gameObject);
				ResearchCanvas.instance.SetInfoCameraMode(false);

				// 여러개 있을거 대비해서 순차적으로 넣어야한다.
				_grant = listGrantInfo.Count > 0;
				_index = 0;
				CharacterBoxShowCanvas.instance.ShowCanvas(_grant ? listGrantInfo[0] : listTrpInfo[0].actorId, OnConfirmCharacterShow);
			});
		}
		else if (showLevelUp)
		{
			UIInstanceManager.instance.ShowCanvasAsync("AnalysisResultCanvas", () =>
			{
				AnalysisResultCanvas.instance.RefreshInfo(showLevelUp, _currentLevel, true);
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

	List<string> _listGrantInfo;
	List<DropManager.CharacterTrpRequest> _listTrpInfo;
	bool _grant;
	int _index;
	Action _resultAction;
	void OnConfirmCharacterShow()
	{
		++_index;
		if (_grant)
		{
			if (_index < _listGrantInfo.Count)
				CharacterBoxShowCanvas.instance.ShowCanvas(_listGrantInfo[_index], OnConfirmCharacterShow);
			else
			{
				_grant = false;
				_index = 0;
			}
		}
		if (_grant == false)
		{
			if (_index < _listTrpInfo.Count)
				CharacterBoxShowCanvas.instance.ShowCanvas(_listTrpInfo[_index].actorId, OnConfirmCharacterShow);
			else
			{
				_listGrantInfo = null;
				_listTrpInfo = null;

				if (_resultAction != null)
					_resultAction();
				_resultAction = null;
			}
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


		// 레벨업 결과만 보여주는거니 풀스크린 결과창을 띄운다
		UIInstanceManager.instance.ShowCanvasAsync("AnalysisResultCanvas", () =>
		{
			AnalysisResultCanvas.instance.RefreshInfo(true, _currentLevel, false);
			action.Invoke();
		});
	}
}