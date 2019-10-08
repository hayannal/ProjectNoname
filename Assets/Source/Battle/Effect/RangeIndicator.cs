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
	public float radius = 4.0f;

	float _defaultHeight;
	void Awake()
	{
		_defaultHeight = cachedTransform.position.y;
	}

	float _lastRadius = 0.0f;
	public void Initialize(float overrideRange = 0.0f)
	{
		cachedTransform.parent = BattleInstanceManager.instance.playerActor.cachedTransform;
		cachedTransform.localPosition = new Vector3(0.0f, _defaultHeight, 0.0f);

		if (_fadeRemainTime > 0.0f)
			return;
		_fadeRemainTime = FADE_TIME;
		lineRenderer.startColor = lineRenderer.endColor = Color.white;
		gameObject.SetActive(true);

		if (overrideRange != 0.0f)
			radius = overrideRange;
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

	const float FADE_TIME = 1.0f;
	float _fadeRemainTime = 0.0f;
	void Update()
	{
		if (_fadeRemainTime <= 0.0f)
			return;

		lineRenderer.startColor = lineRenderer.endColor = new Color(1.0f, 1.0f, 1.0f, _fadeRemainTime / FADE_TIME);
		_fadeRemainTime -= Time.deltaTime;
		if (_fadeRemainTime < 0.0f)
			gameObject.SetActive(false);
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
