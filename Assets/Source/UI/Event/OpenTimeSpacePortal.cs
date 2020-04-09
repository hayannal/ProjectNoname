using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenTimeSpacePortal : MonoBehaviour
{
	public static OpenTimeSpacePortal instance;

	public GameObject touchEffectRootObject;
	public GameObject origPortalEffectRootObject;

	void Awake()
	{
		instance = this;
	}

	bool _firstDisable = true;
	void OnDisable()
	{
		// 처음 꺼진다는건 이벤트용 시공간 포탈로 통해 시공간으로 갔다가 돌아올때라는 얘기다.
		// 이때 이벤트용 이펙트는 아예 꺼버리고 기존 포탈 이펙트를 켜서
		// 평소와 동일한 이펙트가 나오게 한다.
		if (_firstDisable)
		{
			touchEffectRootObject.SetActive(false);
			origPortalEffectRootObject.SetActive(true);
			_firstDisable = false;
		}
	}

	public void OnTouch()
	{
		touchEffectRootObject.SetActive(true);
		StartCoroutine(DelayedOnComplete(3.2f));
	}

	IEnumerator DelayedOnComplete(float delay)
	{
		yield return new WaitForSeconds(delay);
		EventManager.instance.OnCompleteAnimation();
	}
}