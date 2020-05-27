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

		UIInstanceManager.instance.ShowCanvasAsync("DotMainMenuCanvas", () => {

			if (this == null) return;
			if (gameObject == null) return;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false) return;

			DotMainMenuCanvas.instance.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
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
	bool _isAlarmCashShop = false;
	bool _isAlarmCharacter = false;
	bool _isAlarmResearch = false;
	bool _isAlarmMail = false;
	public void RefreshAlarmObject()
	{
		_isAlarmCashShop = DotMainMenuCanvas.IsAlarmCashShop();
		_isAlarmCharacter = DotMainMenuCanvas.IsAlarmCharacter();
		_isAlarmResearch = DotMainMenuCanvas.IsAlarmResearch();
		_isAlarmMail = DotMainMenuCanvas.IsAlarmMail();

		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail) showAlarm = true;
		if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
		else
			AlarmObject.Hide(alarmRootTransform);
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
		if (showAlarm)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);
	}
	#endregion





	void OnApplicationPause(bool pauseStatus)
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
}
