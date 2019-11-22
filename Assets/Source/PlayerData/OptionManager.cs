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

	string OPTION_SYSTEM_LANGUAGE_KEY = "_option_system_language_key";
	string OPTION_BGM_VOLUME_KEY = "_option_bgm_volume_key";
	string OPTION_SYSTEM_VOLUME_KEY = "_option_system_volume_key";
	string OPTION_LANGUAGE_KEY = "_option_language_key";
	string OPTION_FRAME_KEY = "_option_frame_key";

	void LoadSystemLanguage()
	{
		if (ObscuredPrefs.HasKey(OPTION_SYSTEM_LANGUAGE_KEY))
			return;

		switch (Application.systemLanguage)
		{
			case SystemLanguage.Korean: _language = "KOR"; break;
			default: _language = "ENG"; break;
		}
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
	}

	public void SavePlayerPrefs()
	{
		PlayerPrefs.SetFloat(OPTION_BGM_VOLUME_KEY, _bgmVolume);
		PlayerPrefs.SetFloat(OPTION_SYSTEM_VOLUME_KEY, _systemVolume);
		PlayerPrefs.SetString(OPTION_LANGUAGE_KEY, _language);
		PlayerPrefs.SetInt(OPTION_FRAME_KEY, _frame);
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
			//SoundManager.instance.volumeGlobalBgm = _bgmVolume;
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
			//SoundManager.instance.volumeGlobalSystem = _systemVolume;
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
			if (UIString.instance.currentRegion != _language)
			{
				UIString.instance.currentRegion = _language;
				UIString.instance.ReloadRegionFont();
				LocalizedText.OnChangeRegion();
			}
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
			_frame = Mathf.Clamp(_frame, 0, 4);

			//QualitySettings.vSyncCount = 0;
			/*
			// 개발중엔 프레임 봐야해서 막아둔다.
			switch (_frame)
			{
				case 0:
					Application.targetFrameRate = 30;
					break;
				case 1:
					Application.targetFrameRate = 40;
					break;
				case 2:
					Application.targetFrameRate = 45;
					break;
				case 3:
					Application.targetFrameRate = 50;
					break;
				case 4:
					Application.targetFrameRate = 60;
					break;
			}
			*/
		}
	}

	// 클라 켜지는 동안 유지해야해서 이쪽에 둔다.
	public int suggestedChapter { get; set; }
}
