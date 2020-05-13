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
	Vector3 _defaultLocalScale;
	Button _button;
	Toggle _toggle;
	bool _applyLerp = false;
	bool _exitWhenPressed = false;
	Vector3 _cachedAdjustRectScale;

	void Start()
	{
		_transform = GetComponent<Transform>();
		_defaultLocalScale = _transform.localScale;
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
		_targetScale = _defaultLocalScale;
		_applyLerp = false;
		if (adjustRectTransform != null)
			_cachedAdjustRectScale = adjustRectTransform.localScale;
	}

	Vector3 _targetScale;
	float _lerpSpeed = 10.0f;
	void Update()
	{
		bool apply = (_applyLerp && _exitWhenPressed == false);
		if (apply)
			_transform.localScale = Vector3.Lerp(_transform.localScale, _targetScale, Time.unscaledDeltaTime * _lerpSpeed);
		if (adjustRectTransform != null && (apply || _clickAnimation))
			adjustRectTransform.localScale = new Vector3(
				_cachedAdjustRectScale.x * _defaultLocalScale.x / _transform.localScale.x,
				_cachedAdjustRectScale.y * _defaultLocalScale.y / _transform.localScale.y,
				_cachedAdjustRectScale.z * _defaultLocalScale.z / _transform.localScale.z);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (_button != null && _button.interactable == false)
			return;
		if (_toggle != null && _toggle.interactable == false)
			return;

		_applyLerp = true;
		_targetScale = _defaultLocalScale * downScale;
		_transform.DOComplete(false);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_applyLerp = false;
		_targetScale = _defaultLocalScale;

		if (eventData.dragging)
		{
			_transform.localScale = _defaultLocalScale;
			if (adjustRectTransform != null)
				adjustRectTransform.localScale = _cachedAdjustRectScale;
		}
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
			_transform.localScale = _defaultLocalScale;
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
		_transform.DOScale(_defaultLocalScale * clickScale, clickAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScale).SetUpdate(true);

		if (lockCanvasInputInAnimation)
		{
			// 처음에는 이렇게 graphicRaycaster를 끄는거로 구현했었는데
			//if (_graphicRaycaster == null)
			//	_graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
			//if (_graphicRaycaster != null)
			//	_graphicRaycaster.enabled = false;
			// 이렇게 하니까 뒤에 있는게 눌려져서 원하지 않는 결과가 나왔다.
			// 그래서 EventSystem을 막기로 해본다.
			DragThresholdController.instance.EnableEventSystem(false);
		}
	}
	
	void OnCompleteScale()
	{
		_transform.DOScale(_defaultLocalScale, clickAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScaleEnd).SetUpdate(true);
	}

	void OnCompleteScaleEnd()
	{
		_clickAnimation = false;
		if (onCompleteAnimation != null)
			onCompleteAnimation.Invoke();
		if (lockCanvasInputInAnimation)// && _graphicRaycaster != null)
		{
			//_graphicRaycaster.enabled = true;
			DragThresholdController.instance.EnableEventSystem(true);
		}
	}
}

