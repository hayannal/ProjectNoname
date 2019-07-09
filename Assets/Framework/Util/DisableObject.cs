using UnityEngine;
using System.Collections;

public class DisableObject : MonoBehaviour {

	public float delayTime;

	bool started = false;
	void Start()
	{
		started = true;
		StartCoroutine(Disable(delayTime));
	}

	void OnEnable()
	{
		if (!started)
			return;
		StartCoroutine(Disable(delayTime));
	}

	IEnumerator Disable(float delayTime)
	{
		yield return new WaitForSeconds(delayTime);
		gameObject.SetActive(false);
	}
}
