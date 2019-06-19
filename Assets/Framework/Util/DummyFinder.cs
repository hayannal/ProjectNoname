using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DummyFinder : MonoBehaviour
{
	Dictionary<string, Transform> m_dicDummyTransform = new Dictionary<string, Transform>();

	public Transform FindTransform(string name)
	{
		if (m_dicDummyTransform.ContainsKey(name))
			return m_dicDummyTransform[name];

		Transform dummyTransform = TransformUtil.FindChildByName(transform, name);
		if (dummyTransform == null) dummyTransform = transform;
		m_dicDummyTransform.Add(name, dummyTransform);
		return dummyTransform;
	}
}
