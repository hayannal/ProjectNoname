using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EmissiveUpdater : MonoBehaviour {

	public static int EMISSIVE_COLOR;

	public Color emissiveColor = Color.white;
	public float emissiveSpeed = 2.0f;
	public float emissiveBase = 0.6f;
	public float emissiveRange = 0.4f;
	public bool useSharedMaterials;

	float startTime;
	List<Material> cachedMaterials = new List<Material>();
	List<Color> cachedEmissiveStartColors = new List<Color>();

	void Start()
	{
		if (EMISSIVE_COLOR == 0) EMISSIVE_COLOR = Shader.PropertyToID("_EmissiveColor");
		CachingMaterials();
		startTime = Time.time;
	}

	void OnDisable()
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
			EmissiveUpdater childEmissiveUpdater = renderers[i].GetComponent<EmissiveUpdater>();
			if (childEmissiveUpdater != null && childEmissiveUpdater != this)
				continue;
			
			Material[] materials = null;
			if (useSharedMaterials) materials = renderers[i].sharedMaterials;
			else materials = renderers[i].materials;

			for (int j = 0; j < materials.Length; ++j)
			{
				if (materials[j].HasProperty(EMISSIVE_COLOR))
				{
					cachedMaterials.Add(materials[j]);
					if (useSharedMaterials) cachedEmissiveStartColors.Add(materials[j].GetColor(EMISSIVE_COLOR));
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
				cachedMaterials[i].SetColor(EMISSIVE_COLOR, cachedEmissiveStartColors[i]);
			}
		}
		cachedMaterials.Clear();
		caching = false;
	}

	void Update()
	{
		float fTime = Time.time - startTime;

		Color resultColor = Color.white;
		float value = Mathf.PingPong(fTime / emissiveSpeed, 1.0f) * emissiveRange + emissiveBase;
		resultColor = emissiveColor * value;

		for (int i = 0; i < cachedMaterials.Count; ++i)
		{
			if (cachedMaterials[i] == null)
				continue;
			cachedMaterials[i].SetColor(EMISSIVE_COLOR, resultColor);
		}
	}
}
