using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Michsky.UI.Hexart;

public class SettingCanvas : MonoBehaviour
{
	public static SettingCanvas instance = null;

	public Slider systemVolumeSlider;
	public Text systemVolumeText;
	public Slider bgmVolumeSlider;
	public Text bgmVolumeText;
	public SwitchAnim doubleTabSwitch;
	public Text doubleTabOnOffText;
	public SwitchAnim lockIconSwitch;
	public Text lockIconOnOffText;

	public Button languageButton;
	public Text languageText;
	public Slider frameRateSlider;
	public Text frameRateText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		if (LobbyCanvas.instance != null)
			LobbyCanvas.instance.lobbyOptionButton.gameObject.SetActive(false);

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		LoadOption();
	}

	void OnDisable()
	{
		if (LobbyCanvas.instance != null)
			LobbyCanvas.instance.lobbyOptionButton.gameObject.SetActive(true);
	}

	public void OnClickHomeButton()
	{
		SaveOption();
		gameObject.SetActive(false);
	}


	#region GameOption
	void LoadOption()
	{
		systemVolumeSlider.value = OptionManager.instance.systemVolume;
		bgmVolumeSlider.value = OptionManager.instance.bgmVolume;
		doubleTabSwitch.isOn = (OptionManager.instance.useDoubleTab == 1);
		lockIconSwitch.isOn = (OptionManager.instance.lockIcon == 1);

		LoadLanguage();
		frameRateSlider.value = OptionManager.instance.frame;
	}

	public void SaveOption()
	{
		OptionManager.instance.SavePlayerPrefs();
	}

	public void OnValueChangedSystem(float value)
	{
		OptionManager.instance.systemVolume = value;
		systemVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}

	public void OnValueChangedBgm(float value)
	{
		OptionManager.instance.bgmVolume = value;
		bgmVolumeText.text = Mathf.RoundToInt(value * 100.0f).ToString();
	}

	public void OnSwitchOnDoubleTab()
	{
		OptionManager.instance.useDoubleTab = 1;
		doubleTabOnOffText.text = "ON";
		doubleTabOnOffText.color = Color.white;
	}

	public void OnSwitchOffDoubleTab()
	{
		OptionManager.instance.useDoubleTab = 0;
		doubleTabOnOffText.text = "OFF";
		doubleTabOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
	}

	public void OnSwitchOnLockIcon()
	{
		OptionManager.instance.lockIcon = 1;
		lockIconOnOffText.text = "ON";
		lockIconOnOffText.color = Color.white;
	}

	public void OnSwitchOffLockIcon()
	{
		OptionManager.instance.lockIcon = 0;
		lockIconOnOffText.text = "OFF";
		lockIconOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
	}
	#endregion

	#region Language
	void LoadLanguage()
	{
		languageText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_Language_{0}", OptionManager.instance.language)));
	}

	public void OnClickLanguageButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SelectLanguageCanvas", null);
	}
	#endregion

	#region Frame Rate
	public void OnValueChangedFrameRate(float value)
	{
		int intValue = Mathf.RoundToInt(value);
		OptionManager.instance.frame = intValue;
		frameRateText.text = OptionManager.instance.GetTargetFrameRateText(intValue).ToString();
	}
	#endregion

	#region System
	#endregion
}