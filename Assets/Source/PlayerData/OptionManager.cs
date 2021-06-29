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

	float _bgmVolume = 0.75f;
	float _systemVolume = 0.75f;
	string _language = "KOR";
#if UNITY_IOS
	int _frame = 6;
#else
	int _frame = 5;
#endif
	int _useDoubleTab = 1;
	int _lockIcon = 0;
	int _energyAlarm = 0;
	int _darkMode = 0;

	string OPTION_SYSTEM_LANGUAGE_KEY = "_option_system_language_key";
	string OPTION_BGM_VOLUME_KEY = "_option_bgm_volume_key";
	string OPTION_SYSTEM_VOLUME_KEY = "_option_system_volume_key";
	string OPTION_LANGUAGE_KEY = "_option_language_key";
	string OPTION_FRAME_KEY = "_option_frame_key";
	string OPTION_DOUBLE_TAB_KEY = "_option_double_tab_key";
	string OPTION_LOCK_ICON_KEY = "_option_lock_icon_key";
	string OPTION_ENERGY_ALARM = "_option_energy_alarm_key";
	string OPTION_DARK_MODE_KEY = "_option_dark_mode_key";

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

		if (PlayerPrefs.HasKey(OPTION_ENERGY_ALARM))
		{
			_energyAlarm = PlayerPrefs.GetInt(OPTION_ENERGY_ALARM);
		}

		if (PlayerPrefs.HasKey(OPTION_DARK_MODE_KEY))
		{
			_darkMode = PlayerPrefs.GetInt(OPTION_DARK_MODE_KEY);
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
		PlayerPrefs.SetInt(OPTION_ENERGY_ALARM, _energyAlarm);
		PlayerPrefs.SetInt(OPTION_DARK_MODE_KEY, _darkMode);
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
#if UNITY_IOS
			_frame = Mathf.Clamp(_frame, 5, 6);
#else
			_frame = Mathf.Clamp(_frame, 0, 6);
#endif

			// 30프레임일때 vSyncCount값을 2로 설정해서 처리하는 방법도 있지만 이걸 한다고 30프레임의 stuttering이 없어지지 않는다. 그래서 그냥 0으로 간다.
			QualitySettings.vSyncCount = 0;
			// targetFrameRate를 -1로 설정하면 모바일 디바이스의 기본값인 30프레임으로 나오지만 이런다고 부드러워지지 않는다.
			Application.targetFrameRate = GetTargetFrameRate(_frame);

			// deltaTime을 고정할 수 있는 꼼수이지만 사용하지 않는다. 이건 프레임에 따른 차이를 그냥 두는 옛날 고정 프레임 방식이다.
			//Time.captureFramerate = Screen.currentResolution.refreshRate;
		}
	}

	int GetTargetFrameRate(int frame)
	{
		int targetFrameRate = 60;
		switch (frame)
		{
			// 카메라 포지션 lerp에 Time.smoothDeltaTime를 사용하는게 가장 효과적이라서 굳이 50프레임일때 52프레임으로 늘릴 이유는 없어보인다.
			// 숫자 적힌대로 설정하기로 한다.
#if UNITY_IOS
			case 6: targetFrameRate = 60; break;
			default: targetFrameRate = 30; break;
#else
			case 0: targetFrameRate = 30; break;
			case 1: targetFrameRate = 35; break;
			case 2: targetFrameRate = 40; break;
			case 3: targetFrameRate = 45; break;
			case 4: targetFrameRate = 50; break;
			case 5: targetFrameRate = 55; break;
			case 6: targetFrameRate = 60; break;
#endif
		}
		return targetFrameRate;
	}

	public int GetTargetFrameRateText(int frame)
	{
		int targetFrameRate = 60;
		switch (frame)
		{
#if UNITY_IOS
			case 6: targetFrameRate = 60; break;
			default: targetFrameRate = 30; break;
#else
			case 0: targetFrameRate = 30; break;
			case 1: targetFrameRate = 35; break;
			case 2: targetFrameRate = 40; break;
			case 3: targetFrameRate = 45; break;
			case 4: targetFrameRate = 50; break;
			case 5: targetFrameRate = 55; break;
			case 6: targetFrameRate = 60; break;
#endif
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

	public int energyAlarm
	{
		get
		{
			return _energyAlarm;
		}
		set
		{
			_energyAlarm = value;
		}
	}

	public int darkMode
	{
		get
		{
			return _darkMode;
		}
		set
		{
			_darkMode = value;
		}
	}
}
