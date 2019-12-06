using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyCanvas : MonoBehaviour
{
	public static LobbyCanvas instance;

	public Button dotMainMenuButton;
	public Button lobbyOptionButton;
	public Button battlePauseButton;
	public Text levelText;
	public Image expGaugeImage;

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		UpdateExpGauge();
	}

	void Start()
	{
		battlePauseButton.gameObject.SetActive(false);
		levelText.gameObject.SetActive(false);
		expGaugeImage.gameObject.SetActive(false);
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
		expGaugeImage.gameObject.SetActive(true);
		expGaugeImage.fillAmount = 0.0f;
		RefreshLevelText(1);

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.gameObject.SetActive(false);
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
		totalDiff += (targetPercent - expGaugeImage.fillAmount);
		_fillSpeed = totalDiff / LevelUpExpFillTime;
		_fillRemainTime = LevelUpExpFillTime;
	}

	const float LevelUpExpFillTime = 0.5f;
	float _fillRemainTime;
	float _fillSpeed;
	float _targetPercent;
	int _levelUpCount;
	void UpdateExpGauge()
	{
		if (_fillRemainTime > 0.0f)
		{
			_fillRemainTime -= Time.deltaTime;
			expGaugeImage.fillAmount += _fillSpeed * Time.deltaTime;
			if (expGaugeImage.fillAmount >= 1.0f && _levelUpCount > 0)
			{
				expGaugeImage.fillAmount -= 1.0f;
				_levelUpCount -= 1;
			}

			if (_fillRemainTime <= 0.0f)
			{
				_fillRemainTime = 0.0f;
				expGaugeImage.fillAmount = _targetPercent;
			}
		}
	}
	#endregion
}
