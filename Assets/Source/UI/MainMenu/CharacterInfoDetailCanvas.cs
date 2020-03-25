using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CharacterInfoDetailCanvas : MonoBehaviour
{
	public static CharacterInfoDetailCanvas instance;

	public Transform infoCameraTransform;

	void Awake()
	{
		instance = this;
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

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	bool _reservedHide = false;
	public void OnClickBackButton()
	{
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


	public void OnDragRect(BaseEventData baseEventData)
	{
		CharacterListCanvas.instance.OnDragRect(baseEventData);
	}
}