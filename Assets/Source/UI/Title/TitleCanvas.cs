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
		OnFadeOut();
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

		// 타이틀 나올때 스킵하면 이쪽을 통해서 OnFadeOut 호출
		OnFadeOut();
	}

	IEnumerator<float> ShowLogoObject(float delayTime)
	{
		yield return Timing.WaitForSeconds(1.0f);
		logoObject.SetActive(true);
	}

	void OnFadeOut()
	{
		// SaveData 처리를 여기서 직접 하려다보니 타이틀이 없을때 처리하기가 애매해서 LobbyCanvas쪽으로 빼기로 한다.
		// Event를 진행할게 있다면 튕겨서 재접한 상황은 아니라 정상적인 종료나 패배일거다. 처음 켤때만 호출되는 곳이니 서버 이벤트만 있는지 검사하면 된다.
		if (EventManager.instance.IsStandbyServerEvent())
			EventManager.instance.OnLobby();
		else
			LobbyCanvas.instance.CheckClientSaveData();
	}
}
