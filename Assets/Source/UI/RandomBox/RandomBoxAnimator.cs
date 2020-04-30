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
	public Renderer[] meshRendererList;

	Vector3 _defaultBoxScale;
	void Awake()
	{
		_defaultBoxScale = boxTransform.localScale;
	}

	void OnDisable()
	{
		openAnimator.enabled = false;
		boxTransform.localScale = _defaultBoxScale;
		topTransform.localRotation = Quaternion.identity;

		for (int i = 0; i < meshRendererList.Length; ++i)
			meshRendererList[i].enabled = true;
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