using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitRimBlink : MonoBehaviour {

	public static int HIT_RIM_ADD_DIR;
	public static int HIT_COLOR_INTENSITY;
	public static int HIT_RIM_POWER;
	//public static int HIT_RIM_UV_NORMAL;
	static AnimationCurveAsset s_HitRimCurveAsset;
	static AnimationCurveAsset s_HitColorCurveAsset;

	List<Material> _cachedMaterials = new List<Material>();
	float _currentTime;

	#region staticFunction
	public static void ShowHitRimBlink(Transform rootTransform, Vector3 hitNormal)
	{
		if (HIT_RIM_ADD_DIR == 0) HIT_RIM_ADD_DIR = Shader.PropertyToID("_RimDirAdjust");
		if (HIT_COLOR_INTENSITY == 0) HIT_COLOR_INTENSITY = Shader.PropertyToID("_Color");
		if (HIT_RIM_POWER == 0) HIT_RIM_POWER = Shader.PropertyToID("_RimPower");
		//if (HIT_RIM_UV_NORMAL == 0) HIT_RIM_UV_NORMAL = Shader.PropertyToID("_RimNormalTex");
		//if (s_HitCurveAsset == null) s_HitCurveAsset = AssetBundleManager.LoadAsset<AnimationCurveAsset>("animationcurve.unity3d", "HitBlink");
		if (s_HitRimCurveAsset == null) s_HitRimCurveAsset = Resources.Load<AnimationCurveAsset>("Animationcurve/HitRimBlink");
		if (s_HitColorCurveAsset == null) s_HitColorCurveAsset = Resources.Load<AnimationCurveAsset>("Animationcurve/HitRimBlinkColor");

		HitRimBlink hitRimBlink = rootTransform.GetComponent<HitRimBlink>();
		if (hitRimBlink == null) hitRimBlink = rootTransform.gameObject.AddComponent<HitRimBlink>();
		hitRimBlink.Blink(-hitNormal);
	}
	#endregion


	public void Blink(Vector3 rimDir)
	{
		if (!_caching)
			CachingMaterials();
		_currentTime = 0.0f;
		enabled = true;

		for (int i = 0; i < _cachedMaterials.Count; ++i)
		{
			if (_cachedMaterials[i] == null)
				continue;
			_cachedMaterials[i].SetVector(HIT_RIM_ADD_DIR, rimDir);
		}
	}

	// Update is called once per frame
	void Update()
	{
		_currentTime += Time.deltaTime;
		float resultRim = s_HitRimCurveAsset.curve.Evaluate(_currentTime);
		float resultColor = s_HitColorCurveAsset.curve.Evaluate(_currentTime);

		for (int i = 0; i < _cachedMaterials.Count; ++i)
		{
			if (_cachedMaterials[i] == null)
				continue;
			_cachedMaterials[i].SetFloat(HIT_RIM_POWER, resultRim);
			_cachedMaterials[i].SetFloat(HIT_COLOR_INTENSITY, resultColor);
			//_cachedMaterials[i].SetTextureOffset(HIT_RIM_UV_NORMAL, new Vector2(Random.value, Random.value));
		}

		if (_currentTime > s_HitRimCurveAsset.curve.keys[s_HitRimCurveAsset.curve.length-1].time)
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
				if (renderers[i].materials[j].HasProperty(HIT_RIM_POWER))
					_cachedMaterials.Add(renderers[i].materials[j]);
			}
		}
		_caching = true;
	}
}
