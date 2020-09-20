using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LobbyCanvas : MonoBehaviour
{
	public static LobbyCanvas instance;

	public Button dotMainMenuButton;
	public GameObject rightTopRootObject;
	public Button lobbyOptionButton;
	public Button timeSpaceHomeButton;
	public Button battlePauseButton;
	public Text levelText;
	public Slider expGaugeSlider;
	public RectTransform expGaugeRectTransform;
	public Image expGaugeImage;
	public DOTweenAnimation expGaugeColorTween;
	public Image expGaugeEndPointImage;
	public RectTransform alarmRootTransform;

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

		// 이게 켜있으면 TitleCanvas보이지 않는 씬 재로드일때에도 재진입 로직을 체크한다.
		if (ClientSaveData.instance.checkClientSaveDataOnEnterLobby)
			CheckClientSaveData();
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
			// 도전모드면 클리어 패킷을 보내버림. 로그인 하자마자 보내는거라 막을 방법이 없을거로 판단.
			if (needCancelChallengeMode)
				PlayFabApiManager.instance.RequestCancelChallenge(null, false);
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_ReenterAfterDying"), () =>
			{
				ClientSaveData.instance.OnEndGame();
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

			if (needCancelChallengeMode)
			{
				// 도전모드의 재진입을 취소하는거라면 여러가지 할일이 있다.
				// 먼저 서버 동기화
				PlayFabApiManager.instance.RequestCancelChallenge(() =>
				{
					ClientSaveData.instance.OnEndGame();

					// 이제 게이트 필라를 카오스 모드꺼로 바꿔주고
					GatePillar.instance.gameObject.SetActive(false);
					BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
					//BattleInstanceManager.instance.GetCachedObject(challengeGatePillarSpawnEffectPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

					// 가장 중요한 맵 재구축. 씬 이동 없이 해야한다. 이름은 ChangeChallengeMode지만 전환용으로 쓸수도 있다.
					StageManager.instance.ChangeChallengeMode();
				}, true);
			}
			else
				ClientSaveData.instance.OnEndGame();
		}, true);

		// 여기서 복구할때 필요한 레벨팩 이펙트들을 미리 로딩해놓는다.
		// 다른 이펙트는 사실상 복구 직후에 필요하지 않아서 그때 해도 충분한데 레벨팩만 문제라 미리 해두는거다.
		string jsonCachedLevelPackData = ClientSaveData.instance.GetCachedLevelPackData();
		if (string.IsNullOrEmpty(jsonCachedLevelPackData) == false)
			LevelPackDataManager.instance.PreloadInProgressLevelPackData(jsonCachedLevelPackData);
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

		if (DotMainMenuCanvas.instance != null)
		{
			DotMainMenuCanvas.instance.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
			DotMainMenuCanvas.instance.ToggleShow();
			return;
		}

		if (NodeWarPortal.instance != null && NodeWarPortal.instance.enteredPortal)
			return;

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
	public void RefreshAlarmObject()
	{
		_isTutorialPlusAlarmCharacter = DotMainMenuCanvas.IsTutorialPlusAlarmCharacter();
		_isAlarmCashShop = DotMainMenuCanvas.IsAlarmCashShop();
		_isAlarmCharacter = DotMainMenuCanvas.IsAlarmCharacter();
		_isAlarmResearch = DotMainMenuCanvas.IsAlarmResearch();
		_isAlarmMail = DotMainMenuCanvas.IsAlarmMail();

		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail) showAlarm = true;
		if (ContentsManager.IsTutorialChapter()) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
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
		}
		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail) showAlarm = true;
		if (ContentsManager.IsTutorialChapter()) showAlarm = false;

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
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail) showAlarm = true;
		if (ContentsManager.IsTutorialChapter()) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
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
		if (battlePauseButton.gameObject.activeSelf == false)
			return;
		if (battlePauseButton.interactable == false)
			return;

		if (pauseStatus)
			PauseCanvas.instance.gameObject.SetActive(true);
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
				}, false);
			}
			_paused = false;
		}
	}
}
