using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;

public class CharacterLimitBreakCanvas : DetailShowCanvasBase
{
	public static CharacterLimitBreakCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;

	public GameObject effectPrefab;
	public RectTransform toastBackImageRectTransform;
	public RectTransform processTargetRectTransform;

	public CanvasGroup processCanvasGroup;
	public RectTransform processRootRectTransform;
	public Text prevMaxPowerLevelText;
	public Image arrowImage;
	public Text nextMaxPowerLevelText;
	public RectTransform nextMaxPowerLevelRectTransform;
	public DOTweenAnimation nextMaxPowerLevelTweenAnimation;
	public Button priceButton;
	public Text priceText;
	public GameObject exitObject;
	public Graphic processGraphicElement;

	Color _defaultBackgroundColor;
	Vector2 _defaultTextAnchoredPosition;
	void Awake()
	{
		instance = this;
		_defaultTextAnchoredPosition = nextMaxPowerLevelRectTransform.anchoredPosition;
		_defaultBackgroundColor = backgroundImage.color;
	}

	void OnEnable()
	{
		canvasGroup.alpha = 1.0f;
		canvasGroup.gameObject.SetActive(true);
		nextMaxPowerLevelRectTransform.anchoredPosition = _defaultTextAnchoredPosition;
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		backgroundImage.color = _defaultBackgroundColor;
		toastBackImageRectTransform.gameObject.SetActive(false);
		processCanvasGroup.alpha = 1.0f;
		processRootRectTransform.anchoredPosition = Vector2.zero;
		priceButton.gameObject.SetActive(true);
		exitObject.SetActive(false);

		prevMaxPowerLevelText.color = Color.white;
		arrowImage.color = Color.white;
		_processed = false;
	}

	void OnDisable()
	{
		if (_processed)
		{
			StackCanvas.Pop(gameObject);
			_processed = false;
		}
	}

	void Update()
	{
		UpdateLerp();
	}

	CharacterData _characterData;
	int _price;
	public void ShowCanvas(bool show, CharacterData characterData, int price)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		_characterData = characterData;
		_price = price;
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(characterData.actorId);
		if (playerActor == null)
			return;

		prevMaxPowerLevelText.text = characterData.maxPowerLevelOfCurrentLimitBreak.ToString();
		nextMaxPowerLevelText.text = (characterData.maxPowerLevelOfCurrentLimitBreak + 2).ToString();
		priceText.text = price.ToString("N0");
	}

	public void OnClickOkButton()
	{
		priceButton.gameObject.SetActive(false);
		PlayFabApiManager.instance.RequestCharacterLimitBreak(_characterData, _price, () =>
		{
			CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
			CharacterInfoCanvas.instance.RefreshOpenMenuSlot(_characterData.limitBreakLevel);
			Timing.RunCoroutine(LimitBreakProcess());
		});
	}

	IEnumerator<float> LimitBreakProcess()
	{
		// 인풋 차단
		backKeyButton.interactable = false;
		processGraphicElement.raycastTarget = true;

		// 배경 페이드
		DOTween.To(() => backgroundImage.color, x => backgroundImage.color = x, Color.clear, 0.3f).SetEase(Ease.Linear);
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		// 나머지 창들도 다 닫고 캐릭터 중앙으로
		StackCanvas.Push(gameObject);
		CenterOn();
		yield return Timing.WaitForSeconds(0.4f);
		canvasGroup.gameObject.SetActive(false);

		// 캐릭터 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, CharacterListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.2f);

		// 새로운 Toast Back Image
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		processRootRectTransform.position = processTargetRectTransform.position;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		// nextPowerLevel 알파 제거
		DOTween.To(() => prevMaxPowerLevelText.color, x => prevMaxPowerLevelText.color = x, new Color(1.0f, 1.0f, 1.0f, 0.0f), 0.2f);
		DOTween.To(() => arrowImage.color, x => arrowImage.color = x, new Color(1.0f, 1.0f, 1.0f, 0.0f), 0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		// powerlevel 중앙 정렬
		nextMaxPowerLevelRectTransform.DOAnchorPos(new Vector2(0.0f, nextMaxPowerLevelRectTransform.anchoredPosition.y), 0.4f).SetEase(Ease.OutQuad);
		yield return Timing.WaitForSeconds(0.4f);

		float tweenDelay = 0.3f;
		// powerlevel value scale
		nextMaxPowerLevelTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// exit
		exitObject.SetActive(true);
		//yield return Timing.WaitForSeconds(0.2f);

		// 인풋 복구
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		_processed = true;
	}

	bool _processed = false;
	public void OnClickBackButton()
	{
		if (_processed == false)
		{
			gameObject.SetActive(false);
			return;
		}

		if (processCanvasGroup.alpha >= 1.0f)
			DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 0.0f, 0.1f);
		toastBackImageRectTransform.gameObject.SetActive(false);
		exitObject.SetActive(false);
		Hide();
	}
}