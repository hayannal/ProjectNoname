using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FadeCanvas : MonoBehaviour
{
	public static FadeCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(StageManager.instance.fadeCanvasPrefab).GetComponent<FadeCanvas>();			
			}
			return _instance;
		}
	}
	static FadeCanvas _instance = null;

	public Image fadeImage;
	
	public void FadeOut(float duration, float endValue = 1.0f, bool blackFadeColor = false)
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		fadeImage.color = new Color(blackFadeColor ? 0.0f : 1.0f, blackFadeColor ? 0.0f : 1.0f, blackFadeColor ? 0.0f : 1.0f, 0.0f);
		fadeImage.DOFade(endValue, duration);
		_duration = 0.0f;
	}

	public void FadeIn(float duration)
	{
		//fadeImage.color = Color.white;
		fadeImage.DOFade(0.0f, duration);
		_duration = duration;
	}

	// FadeOut 없이 In만 단독으로 실행할때 사용하는 함수
	public void FadeInOnly(float duration, float startValue = 1.0f, bool blackFadeColor = false)
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		fadeImage.color = new Color(blackFadeColor ? 0.0f : 1.0f, blackFadeColor ? 0.0f : 1.0f, blackFadeColor ? 0.0f : 1.0f, startValue);
		FadeIn(duration);
	}

	float _duration;
	void Update()
	{
		if (_duration > 0.0f)
		{
			_duration -= Time.deltaTime;
			if (_duration <= 0.0f)
			{
				_duration = 0.0f;
				gameObject.SetActive(false);
			}
		}
	}
}
