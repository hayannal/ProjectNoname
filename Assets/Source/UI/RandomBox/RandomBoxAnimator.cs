using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RandomBoxAnimator : MonoBehaviour
{
	public GameObject touchCanvasObject;
	public DisableObject disableObjectComponent;
	public DOTweenAnimation punchScaleTweenAnimation;
	public Animator openAnimator;
	public Transform boxTransform;
	public Transform topTransform;

	Vector3 _defaultBoxScale;
	void Awake()
	{
		_defaultBoxScale = boxTransform.localScale;
	}

	void OnDisable()
	{
		disableObjectComponent.enabled = false;
		openAnimator.enabled = false;
		boxTransform.localScale = _defaultBoxScale;
		topTransform.localRotation = Quaternion.identity;
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