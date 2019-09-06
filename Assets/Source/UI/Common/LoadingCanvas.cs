using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingCanvas : MonoBehaviour
{
	public static LoadingCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(Resources.Load<GameObject>("UI/LoadingCanvas")).GetComponent<LoadingCanvas>();
			}
			return _instance;
		}
	}
	static LoadingCanvas _instance = null;

	public GameObject progressObject;
	public Image progressImage;
	public DOTweenAnimation objectFadeTweenAnimation;
	public DOTweenAnimation backgroundFadeTweenAnimation;

	float _enableTime;
	void OnEnable()
	{
		_enableTime = Time.realtimeSinceStartup;
	}

	void OnDisable()
	{
		float lifeTime = Time.realtimeSinceStartup - _enableTime;
		Debug.LogFormat("Loading Time : {0:0.###}", lifeTime);
	}

	#region Progress
	float _targetValue = 0.0f;
	float _fillSpeed;
	public void SetProgressBarPoint(float value, float fillSpeed = 0.3f, bool immediateFill = false)
	{
		if (_targetValue != 0.0f && progressImage.fillAmount < _targetValue)
			progressImage.fillAmount = _targetValue;
		_targetValue = value;
		if (immediateFill)
			progressImage.fillAmount = _targetValue;
		else
			_fillSpeed = fillSpeed;
	}

	void Update()
	{
		if (progressImage.fillAmount >= 1.0f)
			return;

		if (progressImage.fillAmount < _targetValue)
		{
			progressImage.fillAmount += _fillSpeed * Time.deltaTime;
			if (progressImage.fillAmount > 1.0f)
				progressImage.fillAmount = 1.0f;
		}
	}

	public bool onlyObjectFade { get; set; }

	public void FadeOut()
	{
		progressObject.SetActive(false);
		objectFadeTweenAnimation.DOPlay();
	}
	
	public void OnCompleteObjectFade()
	{
		if (onlyObjectFade)
		{
			if (TitleCanvas.instance != null)
				TitleCanvas.instance.ShowTitle();
			gameObject.SetActive(false);
		}
		else
			backgroundFadeTweenAnimation.DOPlay();
	}
	#endregion
}
