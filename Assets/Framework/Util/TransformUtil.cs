using UnityEngine;
using System.Collections;

public class TransformUtil : MonoBehaviour {
	
	static public Transform FindChildByName(Transform t, string name)
	{
		if (t.name == name)
			return t;

		Transform child;
		for (int i = (t.childCount - 1); i >= 0; i--)
		{
			if ((child = FindChildByName(t.GetChild(i), name)) != null)
				return child;
		}
		return null;
	}

	static public void BringToFront(Transform t)
	{
		t.SetAsLastSibling();
	}
}
