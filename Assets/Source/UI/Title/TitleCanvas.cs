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

	public void OnCompleteBlackScreenFade()
	{
		// Mask를 클릭해서 스킵한거라면 이벤트를 날리지 않는다.
		if (maskObject.activeSelf == false)
			return;

		maskObject.SetActive(false);

		// 이 타이밍이 타이틀 나올때 어두운 백그라운드가 사라지는 타이밍이다.
		// TitleImage는 이 타이밍에 맞춰서 하얀색으로 바뀌어져있을거다.
		// 이때 EventManager에게 Lobby화면이 시작됨을 알린다.
		EventManager.instance.OnLobby();
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

		// 타이틀 나올때 스킵하면 이쪽을 통해서 OnLobby 호출
		EventManager.instance.OnLobby();
	}

	IEnumerator<float> ShowLogoObject(float delayTime)
	{
		yield return Timing.WaitForSeconds(1.0f);
		logoObject.SetActive(true);
	}
}
