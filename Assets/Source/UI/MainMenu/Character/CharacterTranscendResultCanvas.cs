using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CharacterTranscendResultCanvas : DetailShowCanvasBase
{
	public static CharacterTranscendResultCanvas instance = null;

	public GameObject effectPrefab;
	public RectTransform toastBackImageRectTransform;

	public GameObject imageGroupObject;
	public GameObject[] fillImageObjectList;
	public GameObject[] tweenAnimationObjectList;
	public GameObject messageTextObject;
	public GameObject exitObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		// 프로세스 단에서 직접 제어하기 위해 호출하지 않는다.
		//CenterOn();

		toastBackImageRectTransform.gameObject.SetActive(false);
		imageGroupObject.SetActive(false);
		messageTextObject.SetActive(false);
		exitObject.SetActive(false);

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	void Update()
	{
		UpdateLerp();
	}

	public void ShowTranscendResult(int resultTranscendLevel)
	{
		Timing.RunCoroutine(TranscendProcess(resultTranscendLevel));
	}

	IEnumerator<float> TranscendProcess(int resultTranscendLevel)
	{
		_processed = true;

		// 0.15초 초기화 대기 후 카메라부터 센터로 옮긴다.
		yield return Timing.WaitForSeconds(0.15f);
		CenterOn();
		yield return Timing.WaitForSeconds(0.2f);

		// 인풋 막은 상태에서 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, CharacterListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(3.0f);

		// 사전 이펙트 끝나갈때쯤 화이트 페이드
		FadeCanvas.instance.FadeOut(0.3f, 0.85f);
		yield return Timing.WaitForSeconds(0.3f);

		FadeCanvas.instance.FadeIn(2.0f);
		yield return Timing.WaitForSeconds(1.5f);

		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.4f);

		int imageIndex = resultTranscendLevel - 1;
		for (int i = 0; i < fillImageObjectList.Length; ++i)
			fillImageObjectList[i].SetActive(i < imageIndex);
		for (int i = 0; i < tweenAnimationObjectList.Length; ++i)
			tweenAnimationObjectList[i].SetActive(false);
		imageGroupObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		for (int i = 0; i < tweenAnimationObjectList.Length; ++i)
			tweenAnimationObjectList[i].SetActive(i == imageIndex);
		yield return Timing.WaitForSeconds(0.5f);

		messageTextObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.5f);

		exitObject.SetActive(true);

		// CharacterInfoTranscendCanvas는 어차피 새로 열릴테니 알아서 갱신될거고
		//RefreshInfo();

		// 하단 탭메뉴부터 알람까지 전부 리프레쉬 해주면 된다.
		CharacterInfoCanvas.instance.RefreshOpenMenuSlot(resultTranscendLevel);

		CharacterListCanvas.instance.RefreshGrid(false);
		CharacterInfoCanvas.instance.RefreshAlarmObjectList();
		CharacterListCanvas.instance.RefreshAlarmList();
		DotMainMenuCanvas.instance.RefreshCharacterAlarmObject();

		_processed = false;
	}

	bool _processed = false;
	public void OnClickBackButton()
	{
		if (_processed)
			return;

		toastBackImageRectTransform.gameObject.SetActive(false);
		imageGroupObject.SetActive(false);
		messageTextObject.SetActive(false);
		exitObject.SetActive(false);
		Hide();
	}
}