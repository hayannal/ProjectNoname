using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBlink : MonoBehaviour {

	public static int HIT_COLOR_INTENSITY;
	static AnimationCurveAsset s_HitCurveAsset;

	List<Material> cachedMaterials = new List<Material>();
	float _currentTime;

	#region staticFunction
	public static void ShowHitBlink(Transform rootTransform)
	{
		if (HIT_COLOR_INTENSITY == 0) HIT_COLOR_INTENSITY = Shader.PropertyToID("_ColorIntensity");
		//if (s_HitCurveAsset == null) s_HitCurveAsset = AssetBundleManager.LoadAsset<AnimationCurveAsset>("animationcurve.unity3d", "HitBlink");
		if (s_HitCurveAsset == null) s_HitCurveAsset = Resources.Load<AnimationCurveAsset>("Animationcurve/HitBlink");

		HitBlink hitBlink = rootTransform.GetComponent<HitBlink>();
		if (hitBlink == null) hitBlink = rootTransform.gameObject.AddComponent<HitBlink>();
		hitBlink.Blink();
	}
	#endregion


	public void Blink()
	{
		if (!_caching)
			CachingMaterials();
		_currentTime = 0.0f;
		enabled = true;
	}

	// Update is called once per frame
	void Update()
	{
		_currentTime += Time.deltaTime;
		float result = s_HitCurveAsset.curve.Evaluate(_currentTime);

		for (int i = 0; i < cachedMaterials.Count; ++i)
		{
			if (cachedMaterials[i] == null)
				continue;
			cachedMaterials[i].SetFloat(HIT_COLOR_INTENSITY, result);
		}

		if (_currentTime > s_HitCurveAsset.curve.keys[s_HitCurveAsset.curve.length-1].time)
			enabled = false;
	}

	bool _caching;
	public void CachingMaterials()
	{
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < renderers.Length; ++i)
		{
			for (int j = 0; j < renderers[i].materials.Length; ++j)
			{
				if (renderers[i].materials[j].HasProperty(HIT_COLOR_INTENSITY))
					cachedMaterials.Add(renderers[i].materials[j]);
			}
		}
		_caching = true;
	}
}
