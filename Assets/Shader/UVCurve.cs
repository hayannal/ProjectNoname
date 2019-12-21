using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UVCurve : MonoBehaviour {

	public string propertyName;
	public AnimationCurve curveU;
	public AnimationCurve curveV;
	public bool useSharedMaterials;

	float startTime;
	List<Material> cachedMaterials = new List<Material>();
	List<Vector2> cachedUV = new List<Vector2>();

	void Start()
	{
		CachingMaterials();
		startTime = Time.time;
	}

	void OnDestroy()
	{
		ResetMaterials();
	}

	bool caching = false;
	public void CachingMaterials()
	{
		ResetMaterials();
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < renderers.Length; ++i)
		{
			Material[] materials = null;
			if (useSharedMaterials) materials = renderers[i].sharedMaterials;
			else materials = renderers[i].materials;

			for (int j = 0; j < materials.Length; ++j)
			{
				cachedMaterials.Add(materials[j]);
				if (useSharedMaterials) cachedUV.Add(materials[j].GetTextureOffset(propertyName));
			}
		}
		caching = true;
	}

	public void ResetMaterials()
	{
		if (!caching)
			return;
		if (useSharedMaterials)
		{
			for (int i = 0; i < cachedMaterials.Count; ++i)
			{
				if (cachedMaterials[i] == null)
					continue;
				cachedMaterials[i].SetTextureOffset(propertyName, cachedUV[i]);
			}
		}
		cachedMaterials.Clear();
		caching = false;
	}

	Vector2 resultVector = Vector2.zero;
	void Update()
	{
		float fTime = Time.time - startTime;
		float resultU = curveU.Evaluate(fTime);
		float resultV = curveV.Evaluate(fTime);
		resultVector.x = resultU;
		resultVector.y = resultV;

		for (int i = 0; i < cachedMaterials.Count; ++i)
		{
			if (cachedMaterials[i] == null)
				continue;
			cachedMaterials[i].SetTextureOffset(propertyName, resultVector);
		}
	}
}
