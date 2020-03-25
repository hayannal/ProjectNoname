using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterInfoDetailCanvas : MonoBehaviour
{
	public static CharacterInfoDetailCanvas instance;

	public Transform infoCameraTransform;
	public RectTransform backButtonRectTransform;
	public RectTransform backButtonHideRectTransform;
	public float noInputTime = 3.0f;

	Vector2 _defaultBackButtonPosition;
	void Awake()
	{
		instance = this;
		_defaultBackButtonPosition = backButtonRectTransform.anchoredPosition;
	}

	Vector3 _origPosition;
	Quaternion _origRotation;
	void OnEnable()
	{
		_origPosition = CustomFollowCamera.instance.cachedTransform.position;
		_origRotation = CustomFollowCamera.instance.cachedTransform.rotation;
		_targetPosition = infoCameraTransform.localPosition + CharacterListCanvas.instance.rootOffsetPosition;
		_targetRotation = infoCameraTransform.localRotation;
		_reservedHide = false;
		_lerpRemainTime = 3.0f;

		_noInputRemainTime = noInputTime;
		backButtonRectTransform.anchoredPosition = _defaultBackButtonPosition;

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	bool _reservedHide = false;
	public void OnClickBackButton()
	{
		if (_buttonHideState)
		{
			_buttonHideState = false;
			return;
		}

		if (_reservedHide)
			return;

		if (_reservedHide == false)
		{
			_reservedHide = true;
			_targetPosition = _origPosition;
			_targetRotation = _origRotation;
			_lerpRemainTime = 0.2f;
			return;
		}
	}

	Vector3 _targetPosition;
	Quaternion _targetRotation;
	float _lerpRemainTime = 0.0f;
	void Update()
	{
		UpdateNoInput();

		if (_lerpRemainTime > 0.0f)
		{
			CustomFollowCamera.instance.cachedTransform.position = Vector3.Lerp(CustomFollowCamera.instance.cachedTransform.position, _targetPosition, Time.deltaTime * (_reservedHide ? 12.0f : 6.0f));
			CustomFollowCamera.instance.cachedTransform.rotation = Quaternion.Slerp(CustomFollowCamera.instance.cachedTransform.rotation, _targetRotation, Time.deltaTime * (_reservedHide ? 12.0f : 6.0f));

			_lerpRemainTime -= Time.deltaTime;
			if (_lerpRemainTime <= 0.0f)
			{
				_lerpRemainTime = 0.0f;
				CustomFollowCamera.instance.cachedTransform.position = _targetPosition;
				CustomFollowCamera.instance.cachedTransform.rotation = _targetRotation;

				if (_reservedHide)
					gameObject.SetActive(false);
			}
		}
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
		CharacterListCanvas.instance.OnDragRect(baseEventData);
	}

	public void OnPointerDown(BaseEventData baseEventData)
	{
		_buttonHideState = false;
		_noInputRemainTime = noInputTime;
	}
}