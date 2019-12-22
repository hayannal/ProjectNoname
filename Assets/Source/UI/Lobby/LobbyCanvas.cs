using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LobbyCanvas : MonoBehaviour
{
	public static LobbyCanvas instance;

	public Button dotMainMenuButton;
	public Button lobbyOptionButton;
	public Button battlePauseButton;
	public Text levelText;
	public Slider expGaugeSlider;
	public RectTransform expGaugeRectTransform;
	public Image expGaugeImage;
	public DOTweenAnimation expGaugeColorTween;
	public Image expGaugeEndPointImage;

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
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null)
			TitleCanvas.instance.FadeTitle();

		if (PlayerData.instance.tutorialChapter)
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
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null)
			TitleCanvas.instance.FadeTitle();

		//UIInstanceManager.instance.ShowCanvasAsync("OptionCanvas", () =>
		//{
		//});
	}

	public void OnClickBattlePauseButton()
	{
		//UIInstanceManager.instance.ShowCanvasAsync("OptionCanvas", () =>
		//{
		//});
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
			FullscreenYesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
				SceneManager.LoadScene(0);
			});
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
		lobbyOptionButton.gameObject.SetActive(!enter);
	}

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
}
