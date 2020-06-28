using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MEC;

public partial class SoundManager : MonoBehaviour {

	public static SoundManager instance
	{
		get
		{
			if (_instance == null)
			{
				GameObject newObject = Instantiate(Resources.Load<GameObject>("SoundManager"));
				_instance = newObject.GetComponent<SoundManager>();
			}
			return _instance;
		}
	}
	static SoundManager _instance = null;

	void Awake()
	{
		_instance = this;
		DontDestroyOnLoad(gameObject);

		_audioSourceForBGM = GetComponent<AudioSource>();
	}

	public AudioMixer audioMixer;
	public AudioMixerGroup bgmMixerGroup;
	public AudioMixerGroup seMixerGroup;	// 3d sound effect
	public AudioMixerGroup uiMixerGroup;	// 2d sound effect
	public AudioMixerSnapshot bgmFadeInSnapshot;
	public AudioMixerSnapshot bgmFadeOutSnapshot;

	#region volume
	public void SetBgmVolume(float normalizedValue)
	{
		// 데시벨은 선형적이지 않아서 0~1사이의 값으로 조절하려면 이런 연산을 거쳐야한다. 1일땐 0dB 0.5일땐 -6dB 0일땐 -80dB 이런식이다.
		float dB = Mathf.Log10(normalizedValue) * 20.0f;
		if (float.IsInfinity(dB))
			dB = -80.0f;
		audioMixer.SetFloat("_bgmVolume", dB);
		//audioMixer.SetFloat("_bgmVolume", (normalizedValue - 1.0f) * 80.0f);
	}

	public void SetSeVolume(float normalizedValue)
	{
		float dB = Mathf.Log10(normalizedValue) * 20.0f;
		if (float.IsInfinity(dB))
			dB = -80.0f;
		audioMixer.SetFloat("_seVolume", dB);
		//audioMixer.SetFloat("_seVolume", (normalizedValue - 1.0f) * 80.0f);
	}

	public void SetUiVolume(float normalizedValue)
	{
		float dB = Mathf.Log10(normalizedValue) * 20.0f;
		if (float.IsInfinity(dB))
			dB = -80.0f;
		audioMixer.SetFloat("_uiVolume", dB);
		//audioMixer.SetFloat("_uiVolume", (normalizedValue - 1.0f) * 80.0f);
	}
	#endregion


	#region BGM
	AudioSource _audioSourceForBGM;
	Coroutine _fadeCoroutine;
	public void PlayBgm(AudioClip clip, float volume, float fadeTime)
	{
		if (_audioSourceForBGM.isPlaying)
		{
			if (_fadeCoroutine != null)
				StopCoroutine(_fadeCoroutine);
			
			_fadeCoroutine = StartCoroutine(CoroutineFadeOutBGM(clip, volume, fadeTime));
		}
		else
		{
			_audioSourceForBGM.clip = clip;
			_audioSourceForBGM.volume = volume;
			_audioSourceForBGM.Play();
			FadeInBGM(fadeTime);
		}
	}

	public void StopBGM(float fadeTime)
	{
		if (!_audioSourceForBGM.isPlaying)
			return;

		if (_fadeCoroutine != null)
			StopCoroutine(_fadeCoroutine);

		_fadeCoroutine = StartCoroutine(CoroutineFadeOutBGM(null, 0.0f, fadeTime));
	}

	IEnumerator CoroutineFadeOutBGM(AudioClip clip, float volume, float fadeTime)
	{
		float fadeOutTime = (clip == null) ? fadeTime : fadeTime * 0.75f;
		FadeOutBGM(fadeOutTime);
		yield return new WaitForSeconds(fadeOutTime);
		_audioSourceForBGM.Stop();
		_fadeCoroutine = null;

		if (clip == null)
			yield break;
		
		_audioSourceForBGM.clip = clip;
		_audioSourceForBGM.volume = volume;
		_audioSourceForBGM.Play();
		FadeInBGM(fadeTime * 0.25f);
		yield break;
	}

	public void FadeOutBGM(float timeToReach)
	{
		bgmFadeOutSnapshot.TransitionTo(timeToReach);
	}

	public void FadeInBGM(float timeToReach)
	{
		bgmFadeInSnapshot.TransitionTo(timeToReach);
	}
	#endregion


	#region 3D Sound

	List<AudioSource> _cachedAudioSource = new List<AudioSource>();
	float _timeScale = 1.0f;

	// for 3D Sound
	public AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position, float volume, bool loop = false)
	{
		AudioSource cachedAudioSource = GetCachedAudioSource();
		cachedAudioSource.transform.position = position;
		cachedAudioSource.gameObject.SetActive(true);
		cachedAudioSource.clip = clip;
		cachedAudioSource.pitch = _timeScale;
		cachedAudioSource.volume = volume;
		cachedAudioSource.loop = loop;
		cachedAudioSource.Play();

		if (loop == false)
			Timing.RunCoroutine(CheckAudioStop(cachedAudioSource));

		return cachedAudioSource;
	}

	AudioSource GetCachedAudioSource()
	{
		for (int i = 0; i < _cachedAudioSource.Count; ++i)
		{
			if (!_cachedAudioSource[i].gameObject.activeSelf)
				return _cachedAudioSource[i];
		}

		AudioSource newAudioSource = CreateAudioSourceInstance();
		_cachedAudioSource.Add(newAudioSource);
		return newAudioSource;
	}

	AudioSource CreateAudioSourceInstance()
	{
		GameObject obj = new GameObject();
		AudioSource cachedAudioSource = obj.AddComponent<AudioSource>();
		cachedAudioSource.outputAudioMixerGroup = seMixerGroup;
		cachedAudioSource.transform.parent = transform;
		cachedAudioSource.gameObject.name = "cachedAudioSource";
		cachedAudioSource.gameObject.SetActive(false);
		return cachedAudioSource;
	}

	IEnumerator<float> CheckAudioStop(AudioSource audioSource)
	{
		// BGM은 변경 횟수가 적어도 효과음은 엄청나게 많이 호출되니 MEC를 사용한다.
		yield return Timing.WaitForSeconds(audioSource.clip.length);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		audioSource.Stop();
		audioSource.clip = null;
		audioSource.gameObject.SetActive(false);
		yield break;
	}

	public void TimeScale(float timeScale)
	{
		for (int i = 0; i < _cachedAudioSource.Count; ++i)
		{
			if (!_cachedAudioSource[i].gameObject.activeSelf)
				continue;
			_cachedAudioSource[i].pitch = timeScale;
		}
		_timeScale = timeScale;
	}

	#endregion

	#region 2D Sound

	List<AudioSource> _cachedAudioSourceFor2D = new List<AudioSource>();

	public AudioSource PlaySFX(AudioClip clip, float volume, float pitch, bool loop = false)
	{
		AudioSource cachedAudioSource = GetCachedAudioSourceFor2D();
		cachedAudioSource.gameObject.SetActive(true);
		cachedAudioSource.clip = clip;
		cachedAudioSource.pitch = pitch;
		cachedAudioSource.volume = volume;
		cachedAudioSource.loop = loop;
		cachedAudioSource.Play();

		if (loop == false)
			Timing.RunCoroutine(CheckAudioStop(cachedAudioSource));

		return cachedAudioSource;
	}

	AudioSource GetCachedAudioSourceFor2D()
	{
		for (int i = 0; i < _cachedAudioSourceFor2D.Count; ++i)
		{
			if (!_cachedAudioSourceFor2D[i].gameObject.activeSelf)
				return _cachedAudioSourceFor2D[i];
		}

		AudioSource newAudioSource = CreateAudioSourceInstanceFor2D();
		_cachedAudioSourceFor2D.Add(newAudioSource);
		return newAudioSource;
	}

	AudioSource CreateAudioSourceInstanceFor2D()
	{
		GameObject obj = new GameObject();
		AudioSource cachedAudioSource = obj.AddComponent<AudioSource>();
		cachedAudioSource.outputAudioMixerGroup = uiMixerGroup;
		cachedAudioSource.transform.parent = transform;
		cachedAudioSource.gameObject.name = "cachedAudioSource";
		cachedAudioSource.gameObject.SetActive(false);
		cachedAudioSource.spatialBlend = 0.0f;
		cachedAudioSource.reverbZoneMix = 0.0f;
		cachedAudioSource.dopplerLevel = 0.0f;
		return cachedAudioSource;
	}
	#endregion




	#region Check SameFrame
	const float CheckDelay = 0.15f;
	Dictionary<string, float> _cachedLastTimeList = new Dictionary<string, float>();
	public bool CheckSameFrameSound(string name)
	{
		float currentTime = Time.time;
		if (_cachedLastTimeList.ContainsKey(name))
		{
			if (currentTime < _cachedLastTimeList[name] + CheckDelay)
				return true;
		}
		return false;
	}

	public void RegisterCharacterSound(string name)
	{
		float currentTime = Time.time;
		//Debug.Log("Register Sound Time " + currentTime.ToString());
		if (_cachedLastTimeList.ContainsKey(name))
		{
			_cachedLastTimeList[name] = currentTime;
		}
		else
		{
			_cachedLastTimeList.Add(name, currentTime);
		}
	}
	#endregion
}
