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