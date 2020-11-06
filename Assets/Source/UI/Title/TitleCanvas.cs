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

		// 게임 설치 후 새 계정에서 튜토시작해서 죽지 않고 클리어 후 로비에 오면 타이틀이 뜨는데
		// 이 타이틀이 뜨고나서 클릭이든 이동에 의해 사라질때 1챕터 진입 클라 이벤트가 떠야하므로 예외처리 해둔다.
		// 1.5초동안 사라지게 되어있었으니 0.7초 쉬고 처리하면 될거같다.
		if (EventManager.instance.IsStandbyClientEvent(EventManager.eClientEvent.NewChapter))
			Timing.RunCoroutine(ShowNewChapterEventAfterTitle(0.7f));
	}

	IEnumerator<float> ShowNewChapterEventAfterTitle(float delayTime)
	{
		yield return Timing.WaitForSeconds(delayTime);

		// avoid gc
		if (this == null)
			yield break;

		EventManager.instance.OnCompleteLobbyEvent();
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
		// 여기서 직접 하려다보니 타이틀이 없을때 처리하기가 애매해서 LoadingCanvas쪽으로 모든걸 처리하고 여기서는 호출하는 형태로만 한다.
		LoadingCanvas.instance.OnEnterLobby();
	}
}
