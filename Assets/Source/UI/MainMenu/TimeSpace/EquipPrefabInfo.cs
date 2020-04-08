using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipPrefabInfo : MonoBehaviour
{
	public float pivotOffset;
	public float infoPivotAddOffset;




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
