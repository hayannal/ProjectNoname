using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UVCurveUI : MonoBehaviour {

	public string propertyName;
	public AnimationCurve curveU;
	public AnimationCurve curveV;

	float startTime;
	Material cachedMaterial;
	Vector2 cachedUV;

	void Start()
	{
		CachingMaterial();
		startTime = Time.time;
	}

	void OnDisable()
	{
		ResetMaterial();
	}

	bool caching = false;
	public void CachingMaterial()
	{
		ResetMaterial();
		CanvasRenderer canvasRenderer = GetComponent<CanvasRenderer>();
		cachedMaterial = canvasRenderer.GetMaterial();
		if (cachedMaterial == null)
			return;
		cachedUV = cachedMaterial.GetTextureOffset(propertyName);
		caching = true;
	}

	public void ResetMaterial()
	{
		if (!caching)
			return;
		cachedMaterial.SetTextureOffset(propertyName, cachedUV);
		cachedMaterial = null;
		caching = false;
	}

	Vector2 resultVector = Vector2.zero;
	void Update()
	{
		if (!caching)
		{
			CachingMaterial();
			return;
		}
		
		float fTime = Time.time - startTime;
		float resultU = curveU.Evaluate(fTime);
		float resultV = curveV.Evaluate(fTime);
		resultVector.x = resultU;
		resultVector.y = resultV;

		if (cachedMaterial != null)
			cachedMaterial.SetTextureOffset(propertyName, resultVector);
	}
}
