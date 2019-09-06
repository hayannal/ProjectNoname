using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class TitleCanvas : MonoBehaviour
{
	public static TitleCanvas instance = null;

	public GameObject maskObject;
	public DOTweenAnimation unmaskMoveTweenAnimation;
	public DOTweenAnimation titleTweenAnimation;
	public Image titleImge;
	public GameObject logoObject;

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		if (titleImge.gameObject.activeSelf == false && logoObject.activeSelf == false)
		{
			if (MainSceneBuilder.instance != null)
				MainSceneBuilder.instance.OnFinishTitleCanvas();
			gameObject.SetActive(false);
		}
	}

	public void ShowTitle()
	{
		unmaskMoveTweenAnimation.DOPlayForward();
	}

	bool _fade = false;
	public void FadeTitle()
	{
		if (_fade)
			return;

		titleTweenAnimation.DOPlayById("2");
		_fade = true;
	}

	public void OnClickMaskButton()
	{
		maskObject.SetActive(false);
		titleImge.color = Color.white;
		Timing.RunCoroutine(ShowLogoObject(1.0f));
	}

	IEnumerator<float> ShowLogoObject(float delayTime)
	{
		yield return Timing.WaitForSeconds(1.0f);
		logoObject.SetActive(true);
	}
}
