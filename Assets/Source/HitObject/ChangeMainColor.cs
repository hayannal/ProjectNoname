using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMainColor : MonoBehaviour
{
	List<Material> _cachedMaterials = new List<Material>();
	List<Color> _cachedColors = new List<Color>();

	#region staticFunction
	public static int MAIN_COLOR;
	public static void ChangeColor(Transform rootTransform, Color mainColor)
	{
		if (MAIN_COLOR == 0) MAIN_COLOR = Shader.PropertyToID("_Color");

		ChangeMainColor changeMainColor = rootTransform.GetComponent<ChangeMainColor>();
		if (changeMainColor == null) changeMainColor = rootTransform.gameObject.AddComponent<ChangeMainColor>();
		changeMainColor.ChangeColor(mainColor);
	}

	public static void ResetColor(Transform rootTransform)
	{
		if (MAIN_COLOR == 0) MAIN_COLOR = Shader.PropertyToID("_Color");

		ChangeMainColor changeMainColor = rootTransform.GetComponent<ChangeMainColor>();
		if (changeMainColor == null) changeMainColor = rootTransform.gameObject.AddComponent<ChangeMainColor>();
		changeMainColor.ResetColor();
	}
	#endregion


	bool _applyTargetMainColor;
	Color _targetMainColor = Color.black;
	float _remainTime;
	public void ChangeColor(Color mainColor)
	{
		if (!_caching)
			CachingMaterials();
		_targetMainColor = mainColor;
		_applyTargetMainColor = true;
		_remainTime = 0.5f;
		enabled = true;
	}

	public void ResetColor()
	{
		if (!_caching)
			return;

		_applyTargetMainColor = false;
		_remainTime = 0.5f;
		enabled = true;
	}

	// Update is called once per frame
	void Update()
	{
		if (_cachedMaterials.Count > 0)
		{
			for (int i = 0; i < _cachedMaterials.Count; ++i)
			{
				if (_cachedMaterials[i] == null)
					continue;

				Color current = _cachedMaterials[i].GetColor(MAIN_COLOR);
				Color lerpColor = Color.Lerp(current, _applyTargetMainColor ? _targetMainColor : _cachedColors[i], Time.deltaTime * 5.0f);
				_cachedMaterials[i].SetColor(MAIN_COLOR, lerpColor);
			}
		}

		_remainTime -= Time.deltaTime;
		if (_remainTime <= 0.0f)
		{
			_remainTime = 0.0f;
			enabled = false;
		}
	}

	bool _caching;
	public void CachingMaterials()
	{
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < renderers.Length; ++i)
		{
			for (int j = 0; j < renderers[i].materials.Length; ++j)
			{
				if (renderers[i].materials[j].HasProperty(MAIN_COLOR) == false)
					continue;
				_cachedMaterials.Add(renderers[i].materials[j]);
				Color defaultMainColor = renderers[i].materials[j].GetColor(MAIN_COLOR);
				_cachedColors.Add(defaultMainColor);				
			}
		}
		_caching = true;
	}
}