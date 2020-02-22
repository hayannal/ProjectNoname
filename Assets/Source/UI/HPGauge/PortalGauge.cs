using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortalGauge : MonoBehaviour
{
	public Slider ratioSlider;

	public void InitializeGauge(Vector3 targetPosition)
	{
		ratioSlider.value = 0.0f;

		Vector3 desiredPosition = targetPosition;
		if (desiredPosition.y < 0.0f)
			desiredPosition.y = 0.0f;
		desiredPosition.y += 1.7f;
		cachedTransform.position = desiredPosition;

		//float rotateY = cachedTransform.position.x * 2.0f;
		//cachedTransform.rotation = Quaternion.Euler(0.0f, rotateY, 0.0f);
	}

	public void OnChanged(float value)
	{
		ratioSlider.value = value;
	}


	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}