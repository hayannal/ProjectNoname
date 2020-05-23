using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AlarmObject : MonoBehaviour
{
	public Image alarmImage;
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

	public static void Show(Transform parentTransform, bool useTweenAnimation = true, bool ignoreAutoDisable = false, bool usePlusSprite = false)
	{
		// 사실 이 방식이 안좋을 수 있는게
		// 인자가 다른 알람이 켜있는 상태에서 또 다른 인자의 Show 호출이 오면 적용하지 못한채 return되버린다.
		// 하지만 보통의 상황에선 항상 하이드 시켰다가 나올테니 상관없을거고
		// 직접 관리하는 CharacterListCanvas역시 Refresh할때 하이드 시켰다가 조건에 맞게 켤테니 그냥 두기로 한다.
		if (parentTransform.childCount > 0 && parentTransform.GetChild(0).gameObject.activeSelf)
			return;

		AlarmObject alarmObject = UIInstanceManager.instance.GetCachedAlarmObject(parentTransform);
		if (useTweenAnimation)
			alarmObject.tweenAnimation.DORestart();
		else
			alarmObject.tweenAnimation.DOPause();
		alarmObject.ignoreAutoDisable = ignoreAutoDisable;
		alarmObject.alarmImage.sprite = usePlusSprite ? CommonCanvasGroup.instance.alarmObjectSpriteList[1] : CommonCanvasGroup.instance.alarmObjectSpriteList[0];

		// Hide 함수 보면 알겠지만 0번 자리에 있는걸 지운다. 동시에 하나의 자식만 활성화 된다는걸 전제로 하는건데
		// 하필 이미 비활성화 된 자식이 존재하는데 새로운걸 가져온다면 인덱스가 1로 밀려나게 된다.
		// 이때 Hide가 안되는 문제가 발생해서 이렇게 처리해둔다.
		// 이런 과정들은 결국.. 오브젝트를 캐싱하지 않은채 Show/Hide를 처리하기 위함이다.
		alarmObject.cachedRectTransform.SetAsFirstSibling();
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