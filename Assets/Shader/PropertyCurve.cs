using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PropertyCurve : MonoBehaviour {

	public string propertyName;
	public AnimationCurve curve;
	public bool useSharedMaterials;

	int propertyID;
	float startTime;
	List<Material> cachedMaterials = new List<Material>();
	List<float> cachedValue = new List<float>();

	void Start()
	{
		propertyID = Shader.PropertyToID(propertyName);
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
				if (materials[j].HasProperty(propertyID))
				{
					cachedMaterials.Add(materials[j]);
					if (useSharedMaterials) cachedValue.Add(materials[j].GetFloat(propertyID));
				}
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
				cachedMaterials[i].SetFloat(propertyID, cachedValue[i]);
			}
		}
		cachedMaterials.Clear();
		caching = false;
	}

	void Update()
	{
		float fTime = Time.time - startTime;
		float result = curve.Evaluate(fTime);

		for (int i = 0; i < cachedMaterials.Count; ++i)
		{
			if (cachedMaterials[i] == null)
				continue;
			cachedMaterials[i].SetFloat(propertyID, result);
		}
	}
}
