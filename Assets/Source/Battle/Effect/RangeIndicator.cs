using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

[RequireComponent(typeof(LineRenderer))]
public class RangeIndicator : MonoBehaviour
{
	public static RangeIndicator instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.rangeIndicatorPrefab).GetComponent<RangeIndicator>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static RangeIndicator _instance = null;

	public LineRenderer lineRenderer;
	public int segments = 80;

	const float MAX_ALPHA = 0.6f;
	const float MIN_ALPHA = 0.3f;

	float _defaultHeight;
	void Awake()
	{
		_defaultHeight = cachedTransform.position.y;
	}

	public void ShowIndicator(float range, bool outRange, Transform parentTransform, bool useFadeOut)
	{
		if (cachedTransform.parent != parentTransform)
		{
			cachedTransform.parent = parentTransform;
			cachedTransform.localPosition = new Vector3(0.0f, _defaultHeight, 0.0f);
		}
		SetRadius(range);

		Color color = outRange ? Color.red : Color.white;
		lineRenderer.startColor = lineRenderer.endColor = new Color(color.r, color.g, color.b, lineRenderer.startColor.a);

		if (useFadeOut)
		{
			_fadeRemainTime = FADE_TIME;
			lineRenderer.startColor = lineRenderer.endColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, MAX_ALPHA);
		}
		else
		{
			_fadeRemainTime = 0.0f;

			// 조우 즉시 최대치로 올려둔채 pingpong을 시작한다. 이걸 하려면 sum값이 필요하다.
			if (Time.frameCount - _lastFrameCount > 1)
				_sumDeltaTimeForPingPong = 0.0f;

			_lastFrameCount = Time.frameCount;
		}
		if (gameObject.activeSelf == false)
			gameObject.SetActive(true);
	}

	float _lastRadius = 0.0f;
	void SetRadius(float radius)
	{
		if (_lastRadius == radius)
			return;

		lineRenderer.positionCount = segments + 1;
		lineRenderer.useWorldSpace = false;

		float x = 0.0f;
		float z = 0.0f;
		float deltaTheta = (float)(2.0 * Mathf.PI) / segments;
		float theta = 0.0f;

		for (int i = 0; i < segments + 1; ++i)
		{
			x = radius * Mathf.Cos(theta);
			z = radius * Mathf.Sin(theta);
			Vector3 pos = new Vector3(x, 0.0f, z);
			lineRenderer.SetPosition(i, pos);
			theta += deltaTheta;
		}
		_lastRadius = radius;
	}

	
	void Update()
	{
		UpdateFadeOut();
	}

	const float FADE_TIME = 1.5f;
	float _fadeRemainTime = 0.0f;
	void UpdateFadeOut()
	{
		if (_fadeRemainTime <= 0.0f)
			return;

		lineRenderer.startColor = lineRenderer.endColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, _fadeRemainTime / FADE_TIME * MAX_ALPHA);
		_fadeRemainTime -= Time.deltaTime;
		if (_fadeRemainTime < 0.0f)
			gameObject.SetActive(false);
	}

	void LateUpdate()
	{
		LateUpdateLoopAlpha();
	}

	const float PINGPONG_TIME = 2.0f;
	int _lastFrameCount = -1;
	float _sumDeltaTimeForPingPong = 0.0f;
	void LateUpdateLoopAlpha()
	{
		if (_fadeRemainTime > 0.0f)
			return;

		if (_lastFrameCount == Time.frameCount)
		{
			_sumDeltaTimeForPingPong += Time.deltaTime / PINGPONG_TIME;

			float result = Mathf.Cos(_sumDeltaTimeForPingPong * Mathf.PI * 2.0f);
			float ratio = (result + 1.0f) * 0.5f;
			float alpha = MIN_ALPHA + ratio * (MAX_ALPHA - MIN_ALPHA);

			// 하얀색이 유독 밝아서 조금 낮춘다.
			if (lineRenderer.startColor.g > 0.0f || lineRenderer.startColor.b > 0.0f) alpha -= 0.1f;

			lineRenderer.startColor = lineRenderer.endColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, alpha);
		}
		else
		{
			// 같지 않다면 이번 Update에서 호출이 안된거다. 이럴땐 fadeRemainTime 으로 전환해서 현재값에서부터 fade시킨다.
			if (Time.frameCount - _lastFrameCount > 1)
			{
				float alphaRatio = lineRenderer.startColor.a / MAX_ALPHA;
				_fadeRemainTime = FADE_TIME * alphaRatio;
			}
		}
	}




	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
