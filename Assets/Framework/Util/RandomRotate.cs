using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotate : MonoBehaviour
{
	public bool applyX;
	public bool applyY;
	public bool applyZ;

	Transform _transform;

	void Awake()
	{
		_transform = GetComponent<Transform>();
	}

	void OnEnable()
	{
		float x = applyX ? Random.Range(0.0f, 360.0f) : 0.0f;
		float y = applyY ? Random.Range(0.0f, 360.0f) : 0.0f;
		float z = applyZ ? Random.Range(0.0f, 360.0f) : 0.0f;
		_transform.Rotate(x, y, z);
	}
}