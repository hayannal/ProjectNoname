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
	public static void ShowHitRimBlink(Transform rootTransform, Vector3 hitNormal, bool firstCaching = false)
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
		hitRimBlink.Blink(-hitNormal, firstCaching);
	}
	#endregion


	public void Blink(Vector3 rimDir, bool firstCaching = false)
	{
		if (!_caching)
			CachingMaterials();
		_currentTime = 0.0f;
		enabled = true;

		#region First Caching
		// 프로파일링 걸어보니 첫번째로 림노말을 셋팅하는 부분에서 Shader.CreateGPUProgram 이라는 항목으로 400ms 이상 렉이 발생했다.
		// 이상하게도 림노말 블링크는 키워드를 써서 하는것도 아니고 쉐이더 상수만 바꾸는건데도 이렇게 새로 컴파일 하는거였다.
		// 더 이상한건 씬에다가 림노말을 켠 상태의 게이트필라를 두고 빌드하더라도
		// 첫번째 타격시 렉이 발생하는건 동일했다.
		// 이게 프로그램으로 동적으로 바꾸는 것과 미리 씬에 올려두는거에서 차이가 있어서일까..
		// 아무튼 이런 이유로 어쩔 수 없이 스폰한 게이트필라에다가 미리 림노말 블링크를 적용하는게 최선이라 판단했고
		// 이걸 위해 firstCaching이라는 기능을 추가한 것이다.
		// 림노말 블링크에는 디폴트값을 기억하는 코드가 존재하지 않았기에 차라리 현재값을 얻어와서 다시 대입하는 형태로 짜게 되었다.
		// (이렇게 해도 쉐이더는 컴파일 하기때문에 매우 잘 캐싱된다.)
		if (firstCaching && _cachedMaterials.Count > 0)
		{
			for (int i = 0; i < _cachedMaterials.Count; ++i)
				_cachedMaterials[i].SetFloat(HIT_RIM_POWER, _cachedMaterials[i].GetFloat(HIT_RIM_POWER));
			_firstCachingUpdateCount = 2;
			return;
		}
		#endregion

		for (int i = 0; i < _cachedMaterials.Count; ++i)
		{
			if (_cachedMaterials[i] == null)
				continue;
			_cachedMaterials[i].SetVector(HIT_RIM_ADD_DIR, rimDir);
		}
	}

	// Update is called once per frame
	int _firstCachingUpdateCount = 0;
	void Update()
	{
		#region First Caching
		if (_firstCachingUpdateCount > 0)
		{
			_firstCachingUpdateCount -= 1;
			if (_firstCachingUpdateCount == 0)
				enabled = false;
			return;
		}
		#endregion

		_currentTime += Time.deltaTime;

		if (_cachedMaterials.Count > 0)
		{
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
