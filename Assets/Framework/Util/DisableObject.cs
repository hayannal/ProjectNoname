#define USE_MEC

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if USE_MEC
using MEC;
#endif

public class DisableObject : MonoBehaviour {

	public float delayTime;

	bool started = false;
	void Start()
	{
		started = true;
#if USE_MEC
		Timing.RunCoroutine(Disable());
#else
		StartCoroutine(Disable(delayTime));
#endif
	}

	void OnEnable()
	{
		if (!started)
			return;
#if USE_MEC
		Timing.RunCoroutine(Disable());
#else
		StartCoroutine(Disable(delayTime));
#endif
	}

#if USE_MEC
	IEnumerator<float> Disable()
#else
	IEnumerator Disable(float delayTime)
#endif
	{
#if USE_MEC
		yield return Timing.WaitForSeconds(delayTime);
#else
		yield return new WaitForSeconds(delayTime);
#endif

		gameObject.SetActive(false);
	}
}