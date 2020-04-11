using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DetailShowCanvasBase : MonoBehaviour
{
	public Transform infoCameraTransform;

	Vector3 _origPosition;
	Quaternion _origRotation;
	protected void CenterOn()
	{
		_origPosition = CustomFollowCamera.instance.cachedTransform.position;
		_origRotation = CustomFollowCamera.instance.cachedTransform.rotation;
		Vector3 basePosition = Vector3.zero;
		if (EquipListCanvas.instance != null && EquipListCanvas.instance.gameObject.activeSelf) basePosition = EquipListCanvas.instance.rootOffsetPosition;
		else if (CharacterListCanvas.instance != null && StackCanvas.IsInStack(CharacterListCanvas.instance.gameObject)) basePosition = CharacterListCanvas.instance.rootOffsetPosition;
		_targetPosition = infoCameraTransform.localPosition + basePosition;
		_targetRotation = infoCameraTransform.localRotation;
		_reservedHide = false;
		_lerpRemainTime = 3.0f;
	}

	bool _reservedHide = false;
	protected void Hide(float overrideLerpTime = 0.0f)
	{
		if (_reservedHide)
			return;

		_reservedHide = true;
		_targetPosition = _origPosition;
		_targetRotation = _origRotation;
		_lerpRemainTime = 0.2f;
		if (overrideLerpTime != 0.0f) _lerpRemainTime = overrideLerpTime;
		return;
	}

	Vector3 _targetPosition;
	Quaternion _targetRotation;
	float _lerpRemainTime = 0.0f;
	protected void UpdateLerp()
	{
		if (_lerpRemainTime <= 0.0f)
			return;

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