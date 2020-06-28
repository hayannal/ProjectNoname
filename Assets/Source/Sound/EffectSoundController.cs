using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class EffectSoundController : MonoBehaviour
{
	public bool multipleComponent;
	public bool checkDuplicate;

	AudioSource _audioSource;
	float _defaultAudioVolume;
	AudioSource[] _audioSourceList;
	float[] _defaultAudioVolumeList;

	[Range(0.5f, 1.5f)]
	public float pitchRandomMultiplier = 1f;
	float _defaultPitch;
	float[] _defaultPitchList;

	public float startDelay;

	void OnEnable()
	{
		if (_audioSource != null || _audioSourceList != null)
		{
			AdjustVolume();
			CheckStartDelay();
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
				_defaultPitchList = new float[_audioSourceList.Length];
				for (int i = 0; i < _defaultAudioVolumeList.Length; ++i)
				{
					_audioSourceList[i].outputAudioMixerGroup = SoundManager.instance.uiMixerGroup;
					_audioSourceList[i].spatialBlend = 0.0f;
					_audioSourceList[i].reverbZoneMix = 0.0f;
					_audioSourceList[i].dopplerLevel = 0.0f;
					_defaultAudioVolumeList[i] = _audioSourceList[i].volume;
					_defaultPitchList[i] = _audioSourceList[i].pitch;
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
				_defaultPitch = _audioSource.pitch;
				AdjustVolume();
			}
		}

		CheckStartDelay();
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

		float pitchMultiplier = 1.0f;
		if (pitchRandomMultiplier != 1.0f)
		{
			if (Random.value < .5)
				pitchMultiplier *= Random.Range(1.0f / pitchRandomMultiplier, 1.0f);
			else
				pitchMultiplier *= Random.Range(1.0f, pitchRandomMultiplier);
		}

		if (_audioSource != null)
		{
			_audioSource.volume = _defaultAudioVolume * OptionManager.instance.systemVolume * duplicateVolumeRatio;
			if (pitchMultiplier != 1.0f)
				_audioSource.pitch = _defaultPitch * pitchMultiplier;
		}
		else if (_audioSourceList != null)
		{
			for (int i = 0; i < _defaultAudioVolumeList.Length; ++i)
				_audioSourceList[i].volume = _defaultAudioVolumeList[i] * OptionManager.instance.systemVolume * duplicateVolumeRatio;
			if (pitchMultiplier != 1.0f)
			{
				for (int i = 0; i < _defaultAudioVolumeList.Length; ++i)
					_audioSourceList[i].pitch = _defaultPitchList[i] * pitchMultiplier;
			}
		}

		if (checkDuplicate && !string.IsNullOrEmpty(soundName))
			SoundManager.instance.RegisterCharacterSound(soundName);
	}

	void CheckStartDelay()
	{
		if (startDelay > 0.0f)
		{
			if (_audioSource != null)
			{
				_audioSource.enabled = false;
			}
			else if (_audioSourceList != null)
			{
				for (int i = 0; i < _defaultAudioVolumeList.Length; ++i)
					_audioSourceList[i].enabled = false;
			}
			Timing.RunCoroutine(DelayedPlaySound(startDelay));
		}
	}

	IEnumerator<float> DelayedPlaySound(float delayTime)
	{
		yield return Timing.WaitForSeconds(delayTime);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		if (_audioSource != null)
		{
			_audioSource.enabled = true;
		}
		else if (_audioSourceList != null)
		{
			for (int i = 0; i < _audioSourceList.Length; ++i)
				_audioSourceList[i].enabled = true;
		}
	}
}