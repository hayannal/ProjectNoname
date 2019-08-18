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
	
	public void FadeOut(float duration)
	{
		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		fadeImage.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		fadeImage.DOFade(1.0f, duration);
	}

	public void FadeIn(float duration)
	{
		//fadeImage.color = Color.white;
		fadeImage.DOFade(0.0f, duration);
		_duration = duration;
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
