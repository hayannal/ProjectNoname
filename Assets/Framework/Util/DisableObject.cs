using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MEC;

public class DisableObject : MonoBehaviour {

	public float delayTime;

	bool started = false;
	void Start()
	{
		started = true;
		Timing.RunCoroutine(Disable());
	}

	void OnEnable()
	{
		if (!started)
			return;
		Timing.RunCoroutine(Disable());
	}

	IEnumerator<float> Disable()
	{
		yield return Timing.WaitForSeconds(delayTime);
		gameObject.SetActive(false);
	}
}
