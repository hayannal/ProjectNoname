using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipInfoDetailShowCanvas : DetailShowCanvasBase
{
	public static EquipInfoDetailShowCanvas instance;

	public RectTransform backButtonRectTransform;
	public RectTransform backButtonHideRectTransform;
	public float noInputTime = 3.0f;

	Vector2 _defaultBackButtonPosition;
	void Awake()
	{
		instance = this;
		_defaultBackButtonPosition = backButtonRectTransform.anchoredPosition;
	}

	void OnEnable()
	{
		CenterOn();

		_noInputRemainTime = noInputTime;
		backButtonRectTransform.anchoredPosition = _defaultBackButtonPosition;

		EquipInfoGround.instance.EnableRotationTweenAnimation(false);

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		EquipInfoGround.instance.EnableRotationTweenAnimation(true);

		// 여기서 처리하면 뒤로 카메라가 빠지고나서 복구되는건데..
		// 원래 장착된 거로 복구되고나서 카메라가 뒤로 빠지게 하려면 아래 Hide()호출하는 곳에서 복구하면 된다.
		if (EquipInfoGround.instance.diffMode)
			EquipInfoGround.instance.RestoreDiffMode();

		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		if (_buttonHideState)
		{
			_buttonHideState = false;
			return;
		}

		Hide();

		//if (EquipInfoGround.instance.diffMode)
		//	EquipInfoGround.instance.RestoreDiffMode();
	}

	void Update()
	{
		UpdateNoInput();
		UpdateLerp();
	}

	float _noInputRemainTime = 0.0f;
	bool _buttonHideState = false;
	void UpdateNoInput()
	{
		if (_noInputRemainTime > 0.0f)
		{
			_noInputRemainTime -= Time.deltaTime;
			if (_noInputRemainTime <= 0.0f)
			{
				_buttonHideState = true;
				_noInputRemainTime = 0.0f;
			}
		}

		backButtonRectTransform.anchoredPosition = Vector3.Lerp(backButtonRectTransform.anchoredPosition, _buttonHideState ? backButtonHideRectTransform.anchoredPosition : _defaultBackButtonPosition, Time.deltaTime * 5.0f);
	}


	public void OnDragRect(BaseEventData baseEventData)
	{
		_buttonHideState = false;
		_noInputRemainTime = noInputTime;

		EquipInfoGround.instance.OnDragRect(baseEventData);
	}

	public void OnPointerDown(BaseEventData baseEventData)
	{
		_buttonHideState = false;
		_noInputRemainTime = noInputTime;
	}
}