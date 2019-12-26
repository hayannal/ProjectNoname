using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TransformScale : MonoBehaviour
{
	public float downScale = 0.9f;
	public float startScale = 1.1f;
	public float startAnimationDuration = 0.2f;
	public bool ignoreTimeScale;

	Transform _transform;
	void Awake()
	{
		_transform = transform;
	}

	void OnEnable()
	{
		PlayStartAnimation();
	}

	void PlayStartAnimation()
	{
		_transform.localScale = new Vector3(downScale, downScale, 1.0f);
		if (ignoreTimeScale)
			_transform.DOScale(startScale, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScale).SetUpdate(true);
		else
			_transform.DOScale(startScale, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad).OnComplete(OnCompleteScale);
	}

	void OnCompleteScale()
	{
		if (ignoreTimeScale)
			_transform.DOScale(1.0f, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad).SetUpdate(true);
		else
			_transform.DOScale(1.0f, startAnimationDuration * 0.5f).SetEase(Ease.OutQuad);
	}
}