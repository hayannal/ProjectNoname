using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectUtil : MonoBehaviour
{
	public static void ChangeLayer(GameObject obj, int layer)
	{
		obj.layer = layer;
		for (int i = 0; i < obj.transform.childCount; ++i)
			ChangeLayer(obj.transform.GetChild(i).gameObject, layer);
	}
}
