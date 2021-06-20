using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class LobbyCanvas : MonoBehaviour
{
	public static LobbyCanvas instance;

	public Button dotMainMenuButton;
	public GameObject rightTopRootObject;
	public Button lobbyOptionButton;
	public Button timeSpaceHomeButton;
	public CanvasGroup questInfoCanvasGroup;
	public Button battlePauseButton;
	public Text levelText;
	public Slider expGaugeSlider;
	public RectTransform expGaugeRectTransform;
	public Image expGaugeImage;
	public DOTweenAnimation expGaugeColorTween;
	public Image expGaugeEndPointImage;
	public RectTransform alarmRootTransform;
	public GameObject fastClearSmallToastObject;
	public Text fastClearText;
	public DOTweenAnimation fastClearTweenAnimation;
	public GameObject noHitClearSmallToastObject;
	public Text noHitClearText;
	public DOTweenAnimation noHitClearTweenAnimation;

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		UpdateExpGauge();
		UpdateExpGaugeHeight();
	}

	void Start()
	{
		battlePauseButton.gameObject.SetActive(false);
		levelText.gameObject.SetActive(false);
		expGaugeSlider.gameObject.SetActive(false);
		expGaugeEndPointImage.gameObject.SetActive(false);
		_defaultExpGaugeHeight = expGaugeRectTransform.sizeDelta.y;
		_defaultExpGaugeColor = expGaugeImage.color;

		fastClearText.text = UIString.instance.GetString("GameUI_FastClearPoint");
		noHitClearText.text = UIString.instance.GetString("GameUI_NoHitClearPoint");
	}

	// 원래 이 함수는 앱 실행 후 1회만 TitleCanvas 없어지는 시점에 호출되지만 네트워크 오류로 인한 재시작시 호출될때도 있다.
	// 그래서 원래 TitleCanvas에 있던 코드인데 이쪽으로 옮겨둔거다.
	public void CheckClientSaveData()
	{
		if (ClientSaveData.instance.IsCachedInProgressGame() == false)
			return;

		// 도전모드를 취소할때는 조금 다르게 처리해야한다.
		// 하나 예외상황이 있는데 카오스모드 오픈 이벤트를 보지 않은 유저들일 경우다.
		// 아직 카오스모드를 알지도 못하는데 취소하면 도전모드 기회가 날아간다고 적혀있으면 이상하니 결국 둘중에 하나인데
		//
		// 전자는 카오스 열리기 전처럼 그냥 도전모드를 유지해주는거다.
		// 이거의 단점은 강종하면 계속해서 도전모드인채로 유지할 수 있다는건데 한번의 패배도 없이 계속해서 챕터를 깨는건 핵과금밖에 없으니 방안중에 하나다.
		//
		// 후자는 취소했을때 카오스 오픈 이벤트를 발생시켜주는거다.
		// 이거의 단점은 강제 이벤트 처리 패킷을 별도로 만들어서 처리해야한다는거와 심지어 이 상황에서 네트워크 끊겼을때의 복구 처리까지 해야한다는거다.
		// 개발량이 적지도 않으면서 전자를 했을때의 단점이 크지 않기 때문에 전자 방식으로 가기로 한다.
		bool needCancelChallengeMode = (PlayerData.instance.currentChallengeMode && EventManager.instance.IsCompleteServerEvent(EventManager.eServerEvent.chaos));

		// 죽은 상태의 저장 데이터인지 확인한다.
		if (ClientSaveData.instance.GetCachedHpRatio() == 0.0f)
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ReenterAfterDying"), () =>
			{
				CancelClientSaveData(needCancelChallengeMode);
			});
			return;
		}

		string message = UIString.instance.GetString("GameUI_Reenter");
		if (needCancelChallengeMode)
			message = string.Format("{0}\n{1}", message, UIString.instance.GetString("GameUI_ReenterChallenge"));

		// 아무 이벤트도 실행할게 없는데 제대로 완료처리 되지 않은 게임이 있다면 복구를 물어본다.
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), message, () =>
		{
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
				TitleCanvas.instance.FadeTitle();

			ClientSaveData.instance.MoveToInProgressGame();
		}, () =>
		{
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
				TitleCanvas.instance.FadeTitle();

			CancelClientSaveData(needCancelChallengeMode);
		}, true);

		// 여기서 복구할때 필요한 레벨팩 이펙트들을 미리 로딩해놓는다.
		// 다른 이펙트는 사실상 복구 직후에 필요하지 않아서 그때 해도 충분한데 레벨팩만 문제라 미리 해두는거다.
		string jsonCachedLevelPackData = ClientSaveData.instance.GetCachedLevelPackData();
		if (string.IsNullOrEmpty(jsonCachedLevelPackData) == false)
			LevelPackDataManager.instance.PreloadInProgressLevelPackData(jsonCachedLevelPackData);
	}

	void CancelClientSaveData(bool needCancelChallengeMode)
	{
		if (!needCancelChallengeMode)
		{
			// 평소라면 패킷 보낼거 없이 ClientSaveData만 비우면 된다.
			ClientSaveData.instance.OnEndGame();
			// 이건 로그인 직후니 안해도 되지 않나..
			//QuestData.instance.OnEndGame(true);
			return;
		}

		// 도전모드의 재진입을 취소하는거라면 여러가지 할일이 있다.
		// 먼저 서버 동기화
		PlayFabApiManager.instance.RequestCancelChallenge(() =>
		{
			// 이제 게이트 필라를 카오스 모드꺼로 바꿔주고
			GatePillar.instance.gameObject.SetActive(false);
			BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
			//BattleInstanceManager.instance.GetCachedObject(challengeGatePillarSpawnEffectPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

			// 가장 중요한 맵 재구축. 씬 이동 없이 해야한다. 이름은 ChangeChallengeMode지만 전환용으로 쓸수도 있다.
			StageManager.instance.ChangeChallengeMode();
		});
	}

	public void OnClickDotButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

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

		if (NodeWarPortal.instance != null && NodeWarPortal.instance.enteredPortal)
			return;

		if (DotMainMenuCanvas.instance != null)
		{
			DotMainMenuCanvas.instance.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
			DotMainMenuCanvas.instance.ToggleShow();
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("DotMainMenuCanvas", () => {

			if (this == null) return;
			if (gameObject == null) return;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false) return;

			DotMainMenuCanvas.instance.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
			SoundManager.instance.PlaySFX("7DotOpen");
		});
	}

	public void OnClickLobbyOptionButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		UIInstanceManager.instance.ShowCanvasAsync("SettingCanvas", null);
	}

	public void OnClickTimeSpaceHomeButton()
	{
		TimeSpacePortal.instance.HomeProcessByCanvas();
	}

	public void OnClickBattlePauseButton()
	{
		PauseCanvas.instance.gameObject.SetActive(true);
		PauseCanvas.instance.ShowBattlePauseSimpleMenu(false);
	}

	public void OnClickBackButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null)
			TitleCanvas.instance.FadeTitle();

		if (LoadingCanvas.instance != null && LoadingCanvas.instance.gameObject.activeSelf)
			return;

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
		{
			FullscreenYesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_ExitGame"), UIString.instance.GetString("GameUI_ExitGameDescription"), () => {
				Application.Quit();
			});
		}
		else
		{
			if (battlePauseButton.gameObject.activeSelf && battlePauseButton.interactable)
				OnClickBattlePauseButton();
		}
	}


	public void OnExitLobby()
	{
		dotMainMenuButton.gameObject.SetActive(false);
		lobbyOptionButton.gameObject.SetActive(false);
		battlePauseButton.gameObject.SetActive(true);
		expGaugeSlider.value = 0.0f;
		expGaugeSlider.gameObject.SetActive(true);
		expGaugeEndPointImage.gameObject.SetActive(false);
		RefreshLevelText(1);

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.gameObject.SetActive(false);
	}

	public void OnEnterMainMenu(bool enter)
	{
		dotMainMenuButton.gameObject.SetActive(!enter);
		rightTopRootObject.SetActive(!enter);
	}

	public void OnEnterTimeSpace(bool enter)
	{
		lobbyOptionButton.gameObject.SetActive(!enter);
		timeSpaceHomeButton.gameObject.SetActive(enter);

		if (enter)
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EnterTimeSpace);
	}

	#region Home
	public static void Home()
	{
		StackCanvas.Home();

		// 하필 홈을 눌렀는데 서브 로비인 시공간에 가있었다면 예외처리.
		// StackCanvas 안에 하려다가 프레임워크기도 하고
		// ChapterCanvas 같은 경우엔 또 할필요가 없기 때문에 선별해서 넣기로 한다.
		if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
			TimeSpacePortal.instance.HomeProcessByCanvas();
	}
	#endregion



	#region Exp Percent Gauge
	public void RefreshLevelText(int level)
	{
		int maxStageLevel = StageManager.instance.GetMaxStageLevel();
		if (level == maxStageLevel)
			levelText.text = UIString.instance.GetString("GameUI_Lv", "Max");
		else
			levelText.text = UIString.instance.GetString("GameUI_Lv", level);
	}

	public void RefreshExpPercent(float targetPercent, int levelUpCount)
	{
		if (levelText.gameObject.activeSelf == false)
			levelText.gameObject.SetActive(true);

		_targetPercent = targetPercent;
		_levelUpCount = levelUpCount;

		float totalDiff = levelUpCount;
		totalDiff += (targetPercent - expGaugeSlider.value);
		_fillSpeed = totalDiff / LevelUpExpFillTime;
		_fillRemainTime = LevelUpExpFillTime;
		_currentExpGaugeHeight = ExpGaugeFillHeight;

		expGaugeRectTransform.sizeDelta = new Vector2(expGaugeRectTransform.sizeDelta.x, ExpGaugeFillHeight);
		expGaugeColorTween.DORestart();
		expGaugeEndPointImage.color = new Color(expGaugeEndPointImage.color.r, expGaugeEndPointImage.color.g, expGaugeEndPointImage.color.b, _defaultExpGaugeColor.a);
		expGaugeEndPointImage.gameObject.SetActive(true);
	}

	float _defaultExpGaugeHeight;
	Color _defaultExpGaugeColor;
	const float ExpGaugeFillHeight = 10.0f;
	const float LevelUpExpFillTime = 0.75f;
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
				expGaugeEndPointImage.DOFade(0.0f, HeightLerpDelayTime).SetEase(Ease.OutQuad);
				_lerpHeightDelayRemainTime = HeightLerpDelayTime;
				_lerpHeightRemainTime = 0.0f;
			}
		}
	}

	const float HeightLerpDelayTime = 1.25f;
	const float HeightLerpTime = 0.4f;
	float _lerpHeightDelayRemainTime;
	float _lerpHeightRemainTime;
	float _currentExpGaugeHeight;
	void UpdateExpGaugeHeight()
	{
		if (_lerpHeightDelayRemainTime > 0.0f)
		{
			_lerpHeightDelayRemainTime -= Time.deltaTime;
			if (_lerpHeightDelayRemainTime <= 0.0f)
			{
				_lerpHeightDelayRemainTime = 0.0f;
				_lerpHeightRemainTime = HeightLerpTime;
				expGaugeEndPointImage.gameObject.SetActive(false);
				//DOTween.To(() => _currentExpGaugeHeight, x => _currentExpGaugeHeight = x, _defaultExpGaugeHeight, HeightLerpTime).SetEase(Ease.OutQuad);
			}
		}

		if (_lerpHeightRemainTime > 0.0f)
		{
			_lerpHeightRemainTime -= Time.deltaTime;
			_currentExpGaugeHeight = Mathf.Lerp(_currentExpGaugeHeight, _defaultExpGaugeHeight, Time.deltaTime * 5.0f);
			expGaugeRectTransform.sizeDelta = new Vector2(expGaugeRectTransform.sizeDelta.x, _currentExpGaugeHeight);

			if (_lerpHeightRemainTime <= 0.0f)
			{
				_lerpHeightRemainTime = 0.0f;
				_currentExpGaugeHeight = _defaultExpGaugeHeight;
				expGaugeRectTransform.sizeDelta = new Vector2(expGaugeRectTransform.sizeDelta.x, _defaultExpGaugeHeight);
			}
		}
	}
	#endregion

	#region InProgressGame
	public void SetLevelExpForInProgressGame(int level, float percent)
	{
		RefreshLevelText(level);
		levelText.gameObject.SetActive(true);
		expGaugeSlider.value = _targetPercent = percent;
	}
	#endregion



	#region AlarmObject
	bool _isTutorialPlusAlarmCharacter = false;
	bool _isAlarmCashShop = false;
	bool _isAlarmCharacter = false;
	bool _isAlarmResearch = false;
	bool _isAlarmMail = false;
	bool _isAlarmBalance = false;
	public void RefreshAlarmObject()
	{
		_isTutorialPlusAlarmCharacter = DotMainMenuCanvas.IsTutorialPlusAlarmCharacter();
		_isAlarmCashShop = DotMainMenuCanvas.IsAlarmCashShop();
		_isAlarmCharacter = DotMainMenuCanvas.IsAlarmCharacter();
		_isAlarmResearch = DotMainMenuCanvas.IsAlarmResearch();
		_isAlarmMail = DotMainMenuCanvas.IsAlarmMail();
		_isAlarmBalance = DotMainMenuCanvas.IsAlarmBalance();

		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail || _isAlarmBalance) showAlarm = true;
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);

		GuideQuestInfo.instance.RefreshAlarmObject();

		// GuideQuest와 달리 데이터가 유효한지 보고 호출해야한다.
		if (QuestData.instance.CheckValidQuestList(false) == true)
			SubQuestInfo.instance.RefreshAlarmObject();
	}

	public void RefreshAlarmObject(DotMainMenuCanvas.eButtonType changedType, bool changedValue)
	{
		// 위 함수의 경우 모든 조건을 다 검사해야하다보니 불필요한 연산을 할때가 많다.
		// 처음 로비에 입장할때야 다 검사하는게 맞는데 그 이후부터는 변경되는 것만 반영하면 되기 때문.
		switch (changedType)
		{
			case DotMainMenuCanvas.eButtonType.Shop: _isAlarmCashShop = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Character: _isAlarmCharacter = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Research: _isAlarmResearch = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Mail: _isAlarmMail = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Balance: _isAlarmBalance = changedValue; break;
		}
		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail || _isAlarmBalance) showAlarm = true;
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
	}

	public void RefreshTutorialPlusAlarmObject()
	{
		_isTutorialPlusAlarmCharacter = DotMainMenuCanvas.IsTutorialPlusAlarmCharacter();

		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail || _isAlarmBalance) showAlarm = true;
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
	}
	#endregion

	#region Clear Point Info
	public void ShowClearPointInfo(bool fastClear, bool noHitClear)
	{
		// 둘다 클리어일때는 위에 있는 fastClear 먼저 보여주고 약간의 딜레이 후 noHitClear를 보여준다.
		if (fastClear && noHitClear)
		{
			Timing.RunCoroutine(DelayedShowClearPointInfo());
		}
		else if (fastClear || noHitClear)
		{
			fastClearSmallToastObject.SetActive(fastClear);
			noHitClearSmallToastObject.SetActive(noHitClear);
		}
	}

	IEnumerator<float> DelayedShowClearPointInfo()
	{
		fastClearSmallToastObject.SetActive(true);

		yield return Timing.WaitForSeconds(0.2f);

		// avoid gc
		if (this == null)
			yield break;

		noHitClearSmallToastObject.SetActive(true);
	}

	public void OnCompleteFastClearTweenAnimation()
	{
		Timing.RunCoroutine(DelayedBackwardFastClearTweenAnimation());
	}

	IEnumerator<float> DelayedBackwardFastClearTweenAnimation()
	{
		yield return Timing.WaitForSeconds(2.0f);

		// avoid gc
		if (this == null)
			yield break;

		fastClearTweenAnimation.DOPlayBackwards();

		yield return Timing.WaitForSeconds(1.0f);

		// avoid gc
		if (this == null)
			yield break;

		fastClearSmallToastObject.SetActive(false);
	}

	public void OnCompleteNoHitClearTweenAnimation()
	{
		Timing.RunCoroutine(DelayedBackwardNoHitClearTweenAnimation());
	}

	IEnumerator<float> DelayedBackwardNoHitClearTweenAnimation()
	{
		yield return Timing.WaitForSeconds(2.0f);

		// avoid gc
		if (this == null)
			yield break;

		noHitClearTweenAnimation.DOPlayBackwards();

		yield return Timing.WaitForSeconds(1.0f);

		// avoid gc
		if (this == null)
			yield break;

		noHitClearSmallToastObject.SetActive(false);
	}
	#endregion

	#region QuestInfo Gruop
	public void FadeOutQuestInfoGroup(float alpha, float duration, bool onlyFade, bool disableOnComplete)
	{
		DOTween.To(() => questInfoCanvasGroup.alpha, x => questInfoCanvasGroup.alpha = x, alpha, duration).SetEase(Ease.Linear).OnComplete(() =>
		{
			if (onlyFade)
				return;

			// Fade가 끝나고나서 상황에 맞게 초기화 해준다.
			if (disableOnComplete)
			{
				GuideQuestInfo.instance.gameObject.SetActive(false);
				SubQuestInfo.instance.gameObject.SetActive(false);
			}
			else
			{
				GuideQuestInfo.instance.CloseInfo();
				SubQuestInfo.instance.CloseInfo();
			}
		});
	}

	public void FadeInQuestInfoGroup(float alpha, float duration, bool onlyFade, bool stage)	//, bool bossWar)
	{
		if (onlyFade == false)
		{
			// FadeIn이다. 보여지기 전에 상황에 맞게 초기화 해준다.
			GuideQuestInfo.instance.RefreshCondition(stage);
			SubQuestInfo.instance.RefreshCondition(stage);
		}

		DOTween.To(() => questInfoCanvasGroup.alpha, x => questInfoCanvasGroup.alpha = x, alpha, duration).SetEase(Ease.Linear);
	}
	#endregion





	void OnApplicationPause(bool pauseStatus)
	{
		OnApplicationPauseCanvas(pauseStatus);
		OnApplicationPauseNetwork(pauseStatus);
	}

	void OnApplicationPauseCanvas(bool pauseStatus)
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
			return;
		if (SwapCanvas.instance != null && SwapCanvas.instance.gameObject.activeSelf)
			return;
		if (ReturnScrollConfirmCanvas.instance != null && ReturnScrollConfirmCanvas.instance.gameObject.activeSelf)
			return;
		if (battlePauseButton.gameObject.activeSelf == false)
			return;
		if (battlePauseButton.interactable == false)
			return;
		if (FullscreenYesNoCanvas.IsShow())
			return;

		if (pauseStatus)
		{
			PauseCanvas.instance.gameObject.SetActive(true);
			PauseCanvas.instance.ShowBattlePauseSimpleMenu(true);
		}
	}

	System.DateTime _pausedDateTime;
	bool _paused;
	void OnApplicationPauseNetwork(bool pauseStatus)
	{
		if (pauseStatus)
		{
			_paused = true;
			_pausedDateTime = System.DateTime.Now;
		}
		else
		{
			if (_paused == false)
				return;

			System.TimeSpan timeSpan = System.DateTime.Now - _pausedDateTime;
			//Debug.LogFormat("Delta Time : {0}", timeSpan.TotalSeconds);
			if (timeSpan.TotalMinutes > 10.0)
			{
				// 패킷을 보내서 유효한지 확인한다.
				PlayFabApiManager.instance.RequestNetworkOnce(() =>
				{
					// 성공시엔 아무것도 하지 않는다.
				}, () =>
				{
					// 실패시에는 로비냐 아니냐에 따라 나눌까 하다가 
					// 어차피 둘다 팝업 띄우고 재시작 해야해서 내부 ErrorHandler에서 처리하기로 한다.
					//if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
					//{
					//}

					// 원래는 여기에서만 
					// ClientSaveData.instance.checkClientSaveDataOnEnterLobby 플래그라던가
					// PlayerData.instance.checkRestartScene 플래그를 만들어서 관리하려고 했었는데
					// 사실 10분 지난거 체크하는거 말고도 와이파이 바뀌거나 네트워크 오류로 인해서
					// 언제든지 씬 리셋이 되는 상황이 발생할 수 있기 때문에
					// PlayerData.instance.ResetData가 호출하면서 재로그인할때 각종 진입처리를 다시 하는게 맞았다.
					//
					// 진입처리에는 서버 이벤트도 있고 ClientSaveData도 있고 나중에는 이용약관 확인창까지 포함되는 바람에
					// 이러다보니 어차피 플래그는 ResetData에서 거는게 맞으며
					// 여기서는 그냥 CommonErrorHandler로 알아서 처리되고 넘어가면 끝인거로 처리하기로 한다.

				}, false);
			}
			_paused = false;
		}
	}
}
