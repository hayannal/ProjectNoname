using UnityEngine;
using System.Collections;

public class DestroyObject : MonoBehaviour {

	public float delayTime;

	void Start()
	{
		Destroy(gameObject, delayTime);
	}
}
