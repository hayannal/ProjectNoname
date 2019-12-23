using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlitchUpdater : MonoBehaviour
{
	public static int SCANLINE_JITTER;
	public static int HORIZONTAL_SHAKE;
	public static int COLOR_DRIFT;

	#region Public Properties
	// Scan line jitter
	[Range(0, 1)]
	public float scanLineJitter;
	// Horizontal shake
	[Range(0, 1)]
	public float horizontalShake;
	// Color drift
	[Range(0, 1)]
	public float colorDrift;
	#endregion

	Material cachedMaterial;

	void Start()
	{
		if (SCANLINE_JITTER == 0) SCANLINE_JITTER = Shader.PropertyToID("_ScanLineJitter");
		if (HORIZONTAL_SHAKE == 0) HORIZONTAL_SHAKE = Shader.PropertyToID("_HorizontalShake");
		if (COLOR_DRIFT == 0) COLOR_DRIFT = Shader.PropertyToID("_ColorDrift");
		CachingMaterial();
	}

	void OnDestroy()
	{
		ResetMaterials();
	}

	void OnEnable()
	{
		_changeRemainTime = Random.Range(delayRange.x, delayRange.y);
	}

	bool caching = false;
	public void CachingMaterial()
	{
		ResetMaterials();
		CanvasRenderer canvasRenderer = GetComponent<CanvasRenderer>();
		cachedMaterial = canvasRenderer.GetMaterial();
		if (cachedMaterial == null)
			return;
		caching = true;
	}

	public void ResetMaterials()
	{
		if (!caching)
			return;
		cachedMaterial = null;
		caching = false;
	}

	void Update()
	{
		UpdateLoop();
		UpdateParameter();
	}

	void UpdateParameter()
	{
		if (!caching)
		{
			CachingMaterial();
			return;
		}

		if (cachedMaterial == null)
			return;

		var sl_thresh = Mathf.Clamp01(1.0f - scanLineJitter * 1.2f);
		var sl_disp = 0.002f + Mathf.Pow(scanLineJitter, 3) * 0.05f;
		var hs = horizontalShake * 0.2f;
		var cd = new Vector2(colorDrift * 0.04f, Time.time * 606.11f);

		cachedMaterial.SetVector(SCANLINE_JITTER, new Vector2(sl_disp, sl_thresh));
		cachedMaterial.SetFloat(HORIZONTAL_SHAKE, hs);
		cachedMaterial.SetVector(COLOR_DRIFT, cd);
	}

	public bool applyLoop;
	public Vector2 delayRange = new Vector2(1.0f, 4.0f);
	public Vector2 changeTimeRange = new Vector2(0.1f, 0.5f);
	public float lerpPower = 20.0f;
	float _changeRemainTime;
	bool _targetApplied;
	[Range(0, 1)]
	public float targetScanLineJitter;
	[Range(0, 1)]
	public float targetColorDrift;
	void UpdateLoop()
	{
		if (applyLoop == false)
			return;
		_changeRemainTime -= Time.deltaTime;
		if (_changeRemainTime < 0.0f)
		{
			_targetApplied ^= true;
			if (_targetApplied)
				_changeRemainTime = Random.Range(changeTimeRange.x, changeTimeRange.y);
			else
				_changeRemainTime = Random.Range(delayRange.x, delayRange.y);
		}

		scanLineJitter = Mathf.Lerp(scanLineJitter, _targetApplied ? targetScanLineJitter : 0.0f, Time.deltaTime * lerpPower);
		colorDrift = Mathf.Lerp(colorDrift, _targetApplied ? targetColorDrift : 0.0f, Time.deltaTime * lerpPower);
	}
}