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
	void Awake()
	{
		_directionalLight = GetComponent<Light>();
	}

	bool _started;
	void Start()
	{
		_defaultDirectionalLightIntensity = _directionalLight.intensity;
		SetEnvironment();

#if UNITY_EDITOR
		_prevAmbientColor = ambientColor;
		_prevBloomThreshold = bloomThreshold;
		_prevDirtIntensity = dirtIntensity;
#endif
		_started = true;
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
	}
#endif

	float _lastGlobalLightIntensityRatio = 1.0f;
	void UpdateApplyGlobalLightIntensity()
	{
		// EnvironmentSetting 이 여러개 만들어 질 수는 있어도 동시에 두개이상이 켜있을순 없다.
		// 그러니 스태틱 함수를 호출해도 1회만 될거라 괜찮다.
		UpdateLerpGlobalLightIntensityRatio();

		if (_lastGlobalLightIntensityRatio != s_currentGlobalLightIntensityRatio)
		{
			_lastGlobalLightIntensityRatio = s_currentGlobalLightIntensityRatio;
			_directionalLight.intensity = _defaultDirectionalLightIntensity * _lastGlobalLightIntensityRatio;
			RenderSettings.ambientLight = ambientColor * _lastGlobalLightIntensityRatio;			
		}
	}


	#region Global Effect
	public static int s_globalRefCount = 0;
	public static float s_currentGlobalLightIntensityRatio = 1.0f;
	public static float s_targetGlobalLightIntensityRatio = 1.0f;
	public static void SetGlobalLightIntensityRatio(float intensityRatio)
	{
		++s_globalRefCount;
		s_targetGlobalLightIntensityRatio = intensityRatio;
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
	}

	public static void UpdateLerpGlobalLightIntensityRatio()
	{
		if (s_currentGlobalLightIntensityRatio == s_targetGlobalLightIntensityRatio)
			return;

		s_currentGlobalLightIntensityRatio = Mathf.Lerp(s_currentGlobalLightIntensityRatio, s_targetGlobalLightIntensityRatio, Time.deltaTime * 3.0f);
		if (Mathf.Abs(s_targetGlobalLightIntensityRatio - s_currentGlobalLightIntensityRatio) < 0.01f)
			s_currentGlobalLightIntensityRatio = s_targetGlobalLightIntensityRatio;
	}
	#endregion
}
