using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AlarmObject : MonoBehaviour
{
	public DOTweenAnimation tweenAnimation;

	void OnDisable()
	{
		// 다른 창에 포함된채로 꺼질땐 무조건 없애두면 재활용 가능성이 더 높아진다.
		gameObject.SetActive(false);
	}

	public static AlarmObject Show(Transform parentTransform, bool useTweenAnimation = true)
	{
		AlarmObject alarmObject = UIInstanceManager.instance.GetCachedAlarmObject(parentTransform);
		alarmObject.tweenAnimation.enabled = useTweenAnimation;
		return alarmObject;
	}

	public static void Hide(AlarmObject alarmObject)
	{
		if (alarmObject == null)
			return;
		alarmObject.gameObject.SetActive(false);
	}

	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
}