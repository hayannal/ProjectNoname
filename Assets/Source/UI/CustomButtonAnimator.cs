using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
	public string pressedStateName = "Pressed";
	public string pressed2NormalStateName = "Pressed to Normal";

	Animator buttonAnimator;
	int pressedStateValue;
	int pressed2NormalStateValue;

	void Awake()
	{
		buttonAnimator = GetComponent<Animator>();
		pressedStateValue = BattleInstanceManager.instance.GetActionNameHash(pressedStateName);
		pressed2NormalStateValue = BattleInstanceManager.instance.GetActionNameHash(pressed2NormalStateName);
	}

	bool _pressed = false;
	public void OnPointerDown(PointerEventData eventData)
	{
		buttonAnimator.CrossFade(pressedStateValue, 0.05f);
		_pressed = true;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		buttonAnimator.CrossFade(pressed2NormalStateValue, 0.05f);
		_pressed = false;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (_pressed)
			buttonAnimator.CrossFade(pressed2NormalStateValue, 0.05f);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (_pressed)
			buttonAnimator.CrossFade(pressedStateValue, 0.05f);
	}
}
