using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using MEC;

public class ButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	public float downScale = 0.9f;
	public float clickScale = 1.1f;
	public float clickAnimationDuration = 0.2f;

	Transform _transform;
	Button _button;
	Toggle _toggle;
	bool _applyLerp = false;
	bool _exitWhenPressed = false;

	void Start()
	{
		_transform = GetComponent<Transform>();
		_button = GetComponent<Button>();
		if (_button != null)
		{
			_button.onClick.AddListener(OnClick);
		}
		_toggle = GetComponent<Toggle>();
		if (_toggle != null)
		{
			_toggle.onValueChanged.AddListener(OnToggle);
		}
		_targetScale = 1.0f;
		_applyLerp = false;
	}

	float _targetScale;
	float _lerpSpeed = 10.0f;
	void Update()
	{
		if (_applyLerp == false)
			return;
		if (_exitWhenPressed)
			return;

		_transform.localScale = Vector3.Lerp(_transform.localScale, new Vector3(_targetScale, _targetScale, 1.0f), Time.deltaTime * _lerpSpeed);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_applyLerp = true;
		_targetScale = downScale;
		_transform.DOComplete(false);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_applyLerp = false;
		_targetScale = 1.0f;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (_exitWhenPressed)
			_exitWhenPressed = false;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (_applyLerp)
		{
			_exitWhenPressed = true;
			_transform.localScale = Vector3.one;
		}
	}

	void OnClick()
	{
		PlayAnimation();
	}

	void OnToggle(bool toggle)
	{
		if (toggle)
			PlayAnimation();
	}

	void PlayAnimation()
	{
		if (_button != null && _button.interactable == false)
			return;

		_transform.DOScale(clickScale, clickAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScale);
	}
	
	void OnCompleteScale()
	{
		_transform.DOScale(1.0f, clickAnimationDuration * 0.5f).SetEase(Ease.OutQuad);
	}
}

