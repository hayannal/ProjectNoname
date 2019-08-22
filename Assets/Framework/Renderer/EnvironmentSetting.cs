using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class EnvironmentSetting : MonoBehaviour
{
	[ColorUsageAttribute(false, true)]
	public Color ambientColor = Color.grey;
	public float bloomThreshold = 1.3f;

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
#endif
		_started = true;
	}

	void SetEnvironment()
	{
		SetSun();
		SetAmbientColor();
		SetBloomThreshold();
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
			RFX4_MobileBloom bloom = CustomRenderer.instance.GetComponent<RFX4_MobileBloom>();
			bloom.bloomThreshold = bloomThreshold;
		}
	}

#if UNITY_EDITOR
	Color _prevAmbientColor;
	float _prevBloomThreshold;
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
	}
#endif
}
