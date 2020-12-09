using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EliteMonsterRim : MonoBehaviour
{
	static int s_baseRimPropertyID;

	public static void ShowRim(Transform rootTransform)
	{
		if (s_baseRimPropertyID == 0) s_baseRimPropertyID = Shader.PropertyToID("_UseBaseRim");

		EliteMonsterRim eliteMonsterRim = rootTransform.GetComponent<EliteMonsterRim>();
		if (eliteMonsterRim == null) eliteMonsterRim = rootTransform.gameObject.AddComponent<EliteMonsterRim>();
		eliteMonsterRim.Rim();
	}

	List<Material> _listCachedMaterial;
	public void Rim()
	{
		if (_listCachedMaterial == null)
		{
			_listCachedMaterial = new List<Material>();
			Renderer[] renderers = GetComponentsInChildren<Renderer>();
			for (int i = 0; i < renderers.Length; ++i)
			{
				for (int j = 0; j < renderers[i].materials.Length; ++j)
				{
					if (renderers[i].materials[j].HasProperty(s_baseRimPropertyID))
						_listCachedMaterial.Add(renderers[i].materials[j]);
				}
			}
		}

		if (_listCachedMaterial.Count == 0)
			return;

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].EnableKeyword("_BASERIM");
		}
	}

	void OnDisable()
	{
		if (_listCachedMaterial == null)
			return;
		if (_listCachedMaterial.Count == 0)
			return;

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].DisableKeyword("_BASERIM");
		}
	}
}