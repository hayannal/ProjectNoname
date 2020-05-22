using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AlarmObject : MonoBehaviour
{
	public DOTweenAnimation tweenAnimation;

	public bool ignoreAutoDisable { get; private set; }
	void OnDisable()
	{
		// CharacterListCanvas에는 Canvas를 스택해놨다가 돌아올때 Grid를 그대로 유지하는 기능이 있기때문에 매번 RefreshGrid가 호출되지 않는다.
		// 그렇기때문에 이렇게 OnDisable에서 꺼버리면 복구를 할 방법이 없게 된다.
		// 그래서 플래그 하나 둬서
		// CharacterListCanvas에서는 예외처리 적용하기로 한다.
		if (ignoreAutoDisable)
			return;

		// 다른 창에 포함된채로 꺼질땐 무조건 없애두면 재활용 가능성이 더 높아진다.
		gameObject.SetActive(false);
	}

	public static void Show(Transform parentTransform, bool useTweenAnimation = true, bool ignoreAutoDisable = false)
	{
		if (parentTransform.childCount > 0 && parentTransform.GetChild(0).gameObject.activeSelf)
			return;

		AlarmObject alarmObject = UIInstanceManager.instance.GetCachedAlarmObject(parentTransform);
		if (useTweenAnimation)
			alarmObject.tweenAnimation.DORestart();
		else
			alarmObject.tweenAnimation.DOPause();
		alarmObject.ignoreAutoDisable = ignoreAutoDisable;
	}

	public static void Hide(Transform parentTransform)
	{
		if (parentTransform.childCount == 0)
			return;
		Transform childTransform = parentTransform.GetChild(0);
		if (childTransform.gameObject.activeSelf == false)
			return;
		childTransform.gameObject.SetActive(false);
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