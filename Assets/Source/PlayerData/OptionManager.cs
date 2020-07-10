using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class OptionManager : MonoBehaviour
{
	public static OptionManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("OptionManager")).AddComponent<OptionManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static OptionManager _instance = null;

	void Awake()
	{
		LoadSystemLanguage();
		LoadPlayerPrefs();
	}

	float _bgmVolume = 1.0f;
	float _systemVolume = 1.0f;
	string _language = "KOR";
	int _frame = 2;
	int _useDoubleTab = 1;
	int _lockIcon = 0;

	string OPTION_SYSTEM_LANGUAGE_KEY = "_option_system_language_key";
	string OPTION_BGM_VOLUME_KEY = "_option_bgm_volume_key";
	string OPTION_SYSTEM_VOLUME_KEY = "_option_system_volume_key";
	string OPTION_LANGUAGE_KEY = "_option_language_key";
	string OPTION_FRAME_KEY = "_option_frame_key";
	string OPTION_DOUBLE_TAB_KEY = "_option_double_tab_key";
	string OPTION_LOCK_ICON_KEY = "_option_lock_icon_key";

	void LoadSystemLanguage()
	{
		if (ObscuredPrefs.HasKey(OPTION_SYSTEM_LANGUAGE_KEY))
			return;

		LanguageTableData languageTableData = UIString.instance.FindLanguageTableDataBySystemLanguage((int)Application.systemLanguage);
		if (languageTableData != null)
			_language = languageTableData.id;
		else
			_language = "ENG";
		PlayerPrefs.SetString(OPTION_LANGUAGE_KEY, _language);
		ObscuredPrefs.SetInt(OPTION_SYSTEM_LANGUAGE_KEY, 1);
	}

	void LoadPlayerPrefs()
	{
		if (PlayerPrefs.HasKey(OPTION_BGM_VOLUME_KEY))
		{
			bgmVolume = PlayerPrefs.GetFloat(OPTION_BGM_VOLUME_KEY);
		}

		if (PlayerPrefs.HasKey(OPTION_SYSTEM_VOLUME_KEY))
		{
			systemVolume = PlayerPrefs.GetFloat(OPTION_SYSTEM_VOLUME_KEY);
		}

		if (PlayerPrefs.HasKey(OPTION_LANGUAGE_KEY))
		{
			language = PlayerPrefs.GetString(OPTION_LANGUAGE_KEY);
		}

		if (PlayerPrefs.HasKey(OPTION_FRAME_KEY))
		{
			frame = PlayerPrefs.GetInt(OPTION_FRAME_KEY);
		}
		else
		{
			frame = _frame;
		}

		if (PlayerPrefs.HasKey(OPTION_DOUBLE_TAB_KEY))
		{
			_useDoubleTab = PlayerPrefs.GetInt(OPTION_DOUBLE_TAB_KEY);
		}

		if (PlayerPrefs.HasKey(OPTION_LOCK_ICON_KEY))
		{
			_lockIcon = PlayerPrefs.GetInt(OPTION_LOCK_ICON_KEY);
		}
	}

	public void SavePlayerPrefs()
	{
		PlayerPrefs.SetFloat(OPTION_BGM_VOLUME_KEY, _bgmVolume);
		PlayerPrefs.SetFloat(OPTION_SYSTEM_VOLUME_KEY, _systemVolume);
		//PlayerPrefs.SetString(OPTION_LANGUAGE_KEY, _language);
		PlayerPrefs.SetInt(OPTION_FRAME_KEY, _frame);
		PlayerPrefs.SetInt(OPTION_DOUBLE_TAB_KEY, _useDoubleTab);
		PlayerPrefs.SetInt(OPTION_LOCK_ICON_KEY, _lockIcon);
	}

	public void SaveLanguagePlayerPref()
	{
		PlayerPrefs.SetString(OPTION_LANGUAGE_KEY, _language);
	}

	public float bgmVolume
	{
		get
		{
			return _bgmVolume;
		}
		set
		{
			_bgmVolume = value;
			SoundManager.instance.SetBgmVolume(_bgmVolume);
		}
	}

	public float systemVolume
	{
		get
		{
			return _systemVolume;
		}
		set
		{
			_systemVolume = value;
			SoundManager.instance.SetUiVolume(_systemVolume);
		}
	}

	public string language
	{
		get
		{
			return _language;
		}
		set
		{
			_language = value;

			// LocalizedText밖에 안되는거라면 손이 많이가게된다. 차라리 씬로드를 통해서만 언어변경을 한다.
			//if (UIString.instance.currentRegion != _language)
			//{
			//	UIString.instance.currentRegion = _language;
			//	UIString.instance.ReloadRegionFont();
			//	LocalizedText.OnChangeRegion();
			//}
		}
	}

	public int frame
	{
		get
		{
			return _frame;
		}
		set
		{
			_frame = value;
			_frame = Mathf.Clamp(_frame, 0, 3);
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = GetTargetFrameRate(_frame);
		}
	}

	int GetTargetFrameRate(int frame)
	{
		int targetFrameRate = 60;
		switch (frame)
		{
			case 0: targetFrameRate = 34; break;
			case 1: targetFrameRate = 45; break;
			case 2: targetFrameRate = 52; break;
			case 3: targetFrameRate = 60; break;
		}
		return targetFrameRate;
	}

	public int GetTargetFrameRateText(int frame)
	{
		int targetFrameRate = 60;
		switch (frame)
		{
			case 0: targetFrameRate = 30; break;
			case 1: targetFrameRate = 40; break;
			case 2: targetFrameRate = 50; break;
			case 3: targetFrameRate = 60; break;
		}
		return targetFrameRate;
	}

	public int useDoubleTab
	{
		get
		{
			return _useDoubleTab;
		}
		set
		{
			_useDoubleTab = value;
		}
	}

	public int lockIcon
	{
		get
		{
			return _lockIcon;
		}
		set
		{
			_lockIcon = value;
		}
	}
}
