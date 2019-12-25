using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;
using MEC;

public class ButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	public float downScale = 0.9f;
	public float clickScale = 1.1f;
	public float clickAnimationDuration = 0.2f;

	// 버튼 영역 조정용 transform은 scale이 적용되지 않게 처리.
	public Transform adjustRectTransform;

	[Space]
	// 스케일 애니 후 이벤트
	public UnityEvent onCompleteAnimation;
	public bool lockCanvasInputInAnimation;

	Transform _transform;
	Button _button;
	Toggle _toggle;
	bool _applyLerp = false;
	bool _exitWhenPressed = false;
	Vector3 _cachedAdjustRectScale;

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
		if (adjustRectTransform != null)
			_cachedAdjustRectScale = adjustRectTransform.localScale;
	}

	float _targetScale;
	float _lerpSpeed = 10.0f;
	void Update()
	{
		bool apply = (_applyLerp && _exitWhenPressed == false);
		if (apply)
			_transform.localScale = Vector3.Lerp(_transform.localScale, new Vector3(_targetScale, _targetScale, 1.0f), Time.unscaledDeltaTime * _lerpSpeed);
		if (adjustRectTransform != null && (apply || _clickAnimation))
			adjustRectTransform.localScale = _cachedAdjustRectScale / _transform.localScale.x;
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
			if (adjustRectTransform != null)
				adjustRectTransform.localScale = _cachedAdjustRectScale;
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

	bool _clickAnimation = false;
	GraphicRaycaster _graphicRaycaster;
	void PlayAnimation()
	{
		if (_button != null && _button.interactable == false)
			return;

		_clickAnimation = true;
		_transform.DOScale(clickScale, clickAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScale).SetUpdate(true);

		if (lockCanvasInputInAnimation)
		{
			if (_graphicRaycaster == null)
				_graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
			if (_graphicRaycaster != null)
				_graphicRaycaster.enabled = false;
		}
	}
	
	void OnCompleteScale()
	{
		_transform.DOScale(1.0f, clickAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScaleEnd).SetUpdate(true);
	}

	void OnCompleteScaleEnd()
	{
		_clickAnimation = false;
		if (onCompleteAnimation != null)
			onCompleteAnimation.Invoke();
		if (lockCanvasInputInAnimation && _graphicRaycaster != null)
			_graphicRaycaster.enabled = true;
	}
}

