using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EnvironmentSetting : MonoBehaviour
{
	[ColorUsageAttribute(false, true)]
	public Color ambientColor = Color.grey;
	public float bloomThreshold = 1.3f;
	public float dirtIntensity = 2.0f;

	// for disable caching
	void OnEnable()
	{
		if (!_started)
			return;

		SetEnvironment();
	}

	Light _directionalLight;
	float _defaultDirectionalLightIntensity;
	public float defaultDirectionalLightIntensity { get { return _defaultDirectionalLightIntensity; } }
	void Awake()
	{
		_directionalLight = GetComponent<Light>();
		_defaultDirectionalLightIntensity = _directionalLight.intensity;
	}

	bool _started;
	void Start()
	{
		SetEnvironment();

#if UNITY_EDITOR
		_prevAmbientColor = ambientColor;
		_prevBloomThreshold = bloomThreshold;
		_prevDirtIntensity = dirtIntensity;
#endif
		_started = true;
	}

	public void SetDefaultLightIntensity(float intensity)
	{
		_defaultDirectionalLightIntensity = intensity;
	}

	void SetEnvironment()
	{
		SetSun();
		SetAmbientColor();
		SetBloomThreshold();
		SetDirtIntensity();
	}

	void SetSun()
	{
		RenderSettings.sun = _directionalLight;
	}

	void SetAmbientColor()
	{
		RenderSettings.ambientLight = ambientColor;
	}

	void SetBloomThreshold()
	{
		if (CustomRenderer.instance != null)
		{	
			CustomRenderer.instance.bloom.bloomThreshold = bloomThreshold;
		}
	}

	void SetDirtIntensity()
	{
		if (CustomRenderer.instance != null)
		{
			CustomRenderer.instance.bloom.DirtIntensity = dirtIntensity;
			CustomRenderer.instance.bloom.SaveDefaultDirtIntensity();
		}
	}

#if UNITY_EDITOR
	Color _prevAmbientColor;
	float _prevBloomThreshold;
	float _prevDirtIntensity;
#endif
	void Update()
	{
#if UNITY_EDITOR
		if (Application.isPlaying)
#endif
			UpdateApplyGlobalLightIntensity();

#if UNITY_EDITOR
		if (_prevAmbientColor != ambientColor)
		{
			SetAmbientColor();
			_prevAmbientColor = ambientColor;
		}

		if (_prevBloomThreshold != bloomThreshold)
		{
			SetBloomThreshold();
			_prevBloomThreshold = bloomThreshold;
		}

		if (_prevDirtIntensity != dirtIntensity)
		{
			SetDirtIntensity();
			_prevDirtIntensity = dirtIntensity;
		}
#endif
	}

	float _lastGlobalLightIntensityRatio = 1.0f;
	void UpdateApplyGlobalLightIntensity()
	{
		// EnvironmentSetting 이 여러개 만들어 질 수는 있어도 동시에 두개이상이 켜있을순 없다.
		// 그러니 스태틱 함수를 호출해도 1회만 될거라 괜찮다.
		UpdateGlobalLightIntensityRatio();

		if (_lastGlobalLightIntensityRatio != s_currentGlobalLightIntensityRatio)
		{
			_lastGlobalLightIntensityRatio = s_currentGlobalLightIntensityRatio;
			_directionalLight.intensity = _defaultDirectionalLightIntensity * _lastGlobalLightIntensityRatio;
			RenderSettings.ambientLight = ambientColor * _lastGlobalLightIntensityRatio;			
		}
	}


	#region Global Effect
	static int s_globalRefCount = 0;
	static float s_currentGlobalLightIntensityRatio = 1.0f;
	static float s_targetGlobalLightIntensityRatio = 1.0f;
	static float s_resetTime = 0.0f;
	public static void SetGlobalLightIntensityRatio(float intensityRatio, float resetTimer)
	{
		++s_globalRefCount;
		s_targetGlobalLightIntensityRatio = intensityRatio;

		if (resetTimer > 0.0f && s_resetTime < Time.time + resetTimer)
			s_resetTime = Time.time + resetTimer;
	}

	public static void ResetGlobalLightIntensityRatio()
	{
		if (s_globalRefCount <= 0)
		{
			Debug.Log("RefCount is invalid. Global Light Setting Ref Count is zero.");
			return;
		}
		--s_globalRefCount;
		if (s_globalRefCount > 0)
			return;

		// Reset
		s_targetGlobalLightIntensityRatio = 1.0f;
		s_resetTime = 0.0f;
	}

	static void UpdateGlobalLightIntensityRatio()
	{
		if (s_globalRefCount > 0 && s_resetTime != 0.0f && Time.time > s_resetTime)
		{
			// 강제 리셋이 될땐 레퍼런스 카운터도 완전히 초기화 시켜야한다.
			s_globalRefCount = 0;
			s_targetGlobalLightIntensityRatio = 1.0f;
			s_resetTime = 0.0f;
		}

		if (s_currentGlobalLightIntensityRatio == s_targetGlobalLightIntensityRatio)
			return;

		s_currentGlobalLightIntensityRatio = Mathf.Lerp(s_currentGlobalLightIntensityRatio, s_targetGlobalLightIntensityRatio, Time.deltaTime * 3.0f);
		if (Mathf.Abs(s_targetGlobalLightIntensityRatio - s_currentGlobalLightIntensityRatio) < 0.01f)
			s_currentGlobalLightIntensityRatio = s_targetGlobalLightIntensityRatio;
	}
	#endregion
}
