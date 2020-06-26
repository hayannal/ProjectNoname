using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectSoundController : MonoBehaviour
{
	public bool multipleComponent;
	public bool checkDuplicate;

	AudioSource _audioSource;
	float _defaultAudioVolume;
	AudioSource[] _audioSourceList;
	float[] _defaultAudioVolumeList;

	void OnEnable()
	{
		if (_audioSource != null || _audioSourceList != null)
		{
			AdjustVolume();
		}
	}

	void Start()
	{
		if (multipleComponent)
		{
			_audioSourceList = GetComponents<AudioSource>();
			if (_audioSourceList != null)
			{
				_defaultAudioVolumeList = new float[_audioSourceList.Length];
				for (int i = 0; i < _defaultAudioVolumeList.Length; ++i)
				{
					_audioSourceList[i].outputAudioMixerGroup = SoundManager.instance.uiMixerGroup;
					_audioSourceList[i].spatialBlend = 0.0f;
					_audioSourceList[i].reverbZoneMix = 0.0f;
					_audioSourceList[i].dopplerLevel = 0.0f;
					_defaultAudioVolumeList[i] = _audioSourceList[i].volume;
				}
				AdjustVolume();
			}
		}
		else
		{
			_audioSource = GetComponent<AudioSource>();
			if (_audioSource != null)
			{
				// 이번 프로젝트에서 이펙트 사운드는 대부분 다 2D 사운드로 할거기때문에 UI MixerGroup을 쓰면 된다.
				_audioSource.outputAudioMixerGroup = SoundManager.instance.uiMixerGroup;
				_audioSource.spatialBlend = 0.0f;
				_audioSource.reverbZoneMix = 0.0f;
				_audioSource.dopplerLevel = 0.0f;
				_defaultAudioVolume = _audioSource.volume;
				AdjustVolume();
			}
		}
	}


	void AdjustVolume()
	{
		float duplicateVolumeRatio = 1.0f;
		string soundName = "";
		if (checkDuplicate)
		{
			if (_audioSource != null && _audioSource.clip != null)
				soundName = _audioSource.clip.name;
			if (_audioSourceList != null && _audioSourceList.Length > 0 && _audioSourceList[0].clip != null)
				soundName = _audioSourceList[0].clip.name;
			if (!string.IsNullOrEmpty(soundName))
			{
				if (SoundManager.instance.CheckSameFrameSound(soundName))
					duplicateVolumeRatio = 0.0f;
			}
		}

		if (_audioSource != null)
		{
			_audioSource.volume = _defaultAudioVolume * OptionManager.instance.systemVolume * duplicateVolumeRatio;
		}
		else if (_audioSourceList != null)
		{
			for (int i = 0; i < _defaultAudioVolumeList.Length; ++i)
				_audioSourceList[i].volume = _defaultAudioVolumeList[i] * OptionManager.instance.systemVolume * duplicateVolumeRatio;
		}

		if (checkDuplicate && !string.IsNullOrEmpty(soundName))
			SoundManager.instance.RegisterCharacterSound(soundName);
	}
}