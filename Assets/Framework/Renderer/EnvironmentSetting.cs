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
	void Awake()
	{
		_directionalLight = GetComponent<Light>();
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
	void Update()
	{
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
}
