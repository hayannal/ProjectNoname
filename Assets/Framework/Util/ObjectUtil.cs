using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ObjectUtil : MonoBehaviour
{
	public static void ChangeLayer(GameObject obj, int layer)
	{
		obj.layer = layer;
		for (int i = 0; i < obj.transform.childCount; ++i)
			ChangeLayer(obj.transform.GetChild(i).gameObject, layer);
	}

#if UNITY_EDITOR
	static public void ReloadShader(GameObject obj)
	{
		Renderer[] mrs = obj.GetComponentsInChildren<Renderer>();
		if (mrs != null)
		{
			for (int i = 0; i < mrs.Length; ++i)
			{
				for (int j = 0; j < mrs[i].sharedMaterials.Length; ++j)
				{
					if (mrs[i].sharedMaterials[j] == null)
						continue;

					mrs[i].sharedMaterials[j].shader = Shader.Find(mrs[i].sharedMaterials[j].shader.name);
				}
			}
		}

		Graphic[] graphics = obj.GetComponentsInChildren<Graphic>(true);
		if (graphics != null)
		{
			for (int i = 0; i < graphics.Length; ++i)
			{
				if (graphics[i].material == null)
					continue;
				graphics[i].material.shader = Shader.Find(graphics[i].material.shader.name);
			}
		}

		ParticleSystemRenderer[] particleSystemRenderers = obj.GetComponentsInChildren<ParticleSystemRenderer>(false);
		if (particleSystemRenderers != null)
		{
			for (int i = 0; i < particleSystemRenderers.Length; ++i)
			{
				for (int j = 0; j < particleSystemRenderers[i].sharedMaterials.Length; ++j)
				{
					if (particleSystemRenderers[i].sharedMaterials[j] == null)
						continue;

					particleSystemRenderers[i].sharedMaterials[j].shader = Shader.Find(particleSystemRenderers[i].sharedMaterials[j].shader.name);
				}
			}
		}
	}
#endif


	public static T CopyComponent<T>(T original, GameObject targetObject) where T : Component
	{
		Type type = original.GetType();
		Component comp = targetObject.AddComponent(type);
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
		PropertyInfo[] pinfos = type.GetProperties(flags);
		foreach (var pinfo in pinfos)
		{
			if (pinfo.CanWrite)
			{
				try
				{
					pinfo.SetValue(comp, pinfo.GetValue(original, null), null);
				}
				catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
			}
		}
		FieldInfo[] finfos = type.GetFields(flags);
		foreach (var finfo in finfos)
		{
			finfo.SetValue(comp, finfo.GetValue(original));
		}
		return comp as T;
	}
}
