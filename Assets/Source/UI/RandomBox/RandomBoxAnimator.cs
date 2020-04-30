using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RandomBoxAnimator : MonoBehaviour
{
	public DOTweenAnimation punchScaleTweenAnimation;
	public Animator openAnimator;
	public Transform boxTransform;
	public Transform topTransform;

	void OnDisable()
	{
		openAnimator.enabled = false;
		boxTransform.localScale = Vector3.one;
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