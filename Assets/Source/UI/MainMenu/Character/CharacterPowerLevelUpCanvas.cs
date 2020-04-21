using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;

public class CharacterPowerLevelUpCanvas : DetailShowCanvasBase
{
	public static CharacterPowerLevelUpCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;
	public Transform titleTextTransform;

	public GameObject effectPrefab;
	public RectTransform toastBackImageRectTransform;
	public RectTransform processTargetRectTransform;

	public CanvasGroup processCanvasGroup;
	public RectTransform processRootRectTransform;
	public RectTransform textRootRectTransform;
	public Text currentPowerLevelTextForRect;	// 투명 글씨인데 영역 맞추기 위한 루트로서 사용하는 텍스트
	public Text currentPowerLevelText;
	public DOTweenAnimation currentPowerLevelTweenAnimation;
	public Text nextPowerLevelText;
	public Image arrowImage;
	public Text currentHpText;
	public DOTweenAnimation currentMaxHpTweenAnimation;
	public Text addHpText;
	public Text currentAtkText;
	public DOTweenAnimation currentAtkTweenAnimation;
	public Text addAtkText;
	public Button priceButton;
	public Text priceText;
	public GameObject exitObject;
	public Graphic processGraphicElement;

	Color _defaultBackgroundColor;
	Vector2 _defaultTextAnchoredPosition;
	void Awake()
	{
		instance = this;
		_defaultTextAnchoredPosition = textRootRectTransform.anchoredPosition;
		_defaultBackgroundColor = backgroundImage.color;
	}

	void OnEnable()
	{
		canvasGroup.alpha = 1.0f;
		canvasGroup.gameObject.SetActive(true);
		textRootRectTransform.anchoredPosition = _defaultTextAnchoredPosition;
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		backgroundImage.color = _defaultBackgroundColor;
		toastBackImageRectTransform.gameObject.SetActive(false);
		processCanvasGroup.alpha = 1.0f;
		processRootRectTransform.anchoredPosition = Vector2.zero;
		priceButton.gameObject.SetActive(true);
		exitObject.SetActive(false);

		nextPowerLevelText.color = Color.white;
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
		UpdateHpText();
		UpdateAtkText();
		UpdateLerp();
	}

	CharacterData _characterData;
	int _price;
	float _currentMaxHp;
	float _currentAtk;
	float _addMaxHp;
	float _addAtk;
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

		currentPowerLevelText.text = currentPowerLevelTextForRect.text = characterData.powerLevel.ToString();
		nextPowerLevelText.text = (characterData.powerLevel + 1).ToString();
		_currentMaxHp = playerActor.actorStatus.GetDisplayMaxHp();
		_currentAtk = playerActor.actorStatus.GetDisplayAttack();
		currentHpText.text = _currentMaxHp.ToString("N0");
		currentAtkText.text = _currentAtk.ToString("N0");
		float nextAtk = 0;
		float nextMaxHp = 0;
		playerActor.actorStatus.GetNextPowerLevelDisplayValue(ref nextAtk, ref nextMaxHp);
		_addMaxHp = nextMaxHp - _currentMaxHp;
		_addAtk = nextAtk - _currentAtk;
		addHpText.text = string.Format("+{0:N0}", _addMaxHp);
		addAtkText.text = string.Format("+{0:N0}", _addAtk);
		priceText.text = price.ToString("N0");
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GameUI_PowerLevelUpMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickOkButton()
	{
		priceButton.gameObject.SetActive(false);
		PlayFabApiManager.instance.RequestCharacterPowerLevelUp(_characterData, _price, () =>
		{
			CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
			Timing.RunCoroutine(PowerLevelUpProcess());
		});
	}

	IEnumerator<float> PowerLevelUpProcess()
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
		yield return Timing.WaitForSeconds(0.2f);
		canvasGroup.gameObject.SetActive(false);

		// 캐릭터 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, CharacterListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.5f);

		// 새로운 Toast Back Image
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		processRootRectTransform.position = processTargetRectTransform.position;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		// nextPowerLevel 알파 제거
		DOTween.To(() => nextPowerLevelText.color, x => nextPowerLevelText.color = x, new Color(1.0f, 1.0f, 1.0f, 0.0f), 0.2f);
		DOTween.To(() => arrowImage.color, x => arrowImage.color = x, new Color(1.0f, 1.0f, 1.0f, 0.0f), 0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		// powerlevel 중앙 정렬
		textRootRectTransform.DOAnchorPos(new Vector2(105.0f, textRootRectTransform.anchoredPosition.y), 0.4f).SetEase(Ease.OutQuad);
		yield return Timing.WaitForSeconds(0.4f);

		float tweenDelay = 0.3f;
		// powerlevel value scale
		currentPowerLevelText.text = currentPowerLevelTextForRect.text = nextPowerLevelText.text;
		currentPowerLevelTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// hp
		_hpChangeSpeed = -_addMaxHp / hpChangeTime;
		_floatCurrentHp = _addMaxHp;
		_lastHp = -1;
		_updateHpText = true;
		yield return Timing.WaitForSeconds(hpChangeTime);
		currentMaxHpTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// atk
		_atkChangeSpeed = -_addAtk / atkChangeTime;
		_floatCurrentAtk = _addAtk;
		_lastAtk = -1;
		_updateAtkText = true;
		yield return Timing.WaitForSeconds(atkChangeTime);
		currentAtkTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// exit
		exitObject.SetActive(true);
		//yield return Timing.WaitForSeconds(0.2f);

		// 인풋 복구
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		_processed = true;

		CharacterListCanvas.instance.RefreshGrid(false);

		// StackCanvas로 Push Pop하면서 어차피 자동으로 갱신될거다.
		//CharacterInfoGrowthCanvas.instance.RefreshStatus();
		//CharacterInfoGrowthCanvas.instance.RefreshRequired();
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

	const float hpChangeTime = 0.4f;
	float _hpChangeSpeed;
	float _floatCurrentHp;
	int _lastHp;
	bool _updateHpText;
	void UpdateHpText()
	{
		if (_updateHpText == false)
			return;

		_floatCurrentHp += _hpChangeSpeed * Time.deltaTime;
		int currentHpInt = (int)_floatCurrentHp;
		if (currentHpInt <= 0)
		{
			currentHpInt = 0;
			_updateHpText = false;
		}
		if (currentHpInt != _lastHp)
		{
			_lastHp = currentHpInt;
			currentHpText.text = (_currentMaxHp + (_addMaxHp - _lastHp)).ToString("N0");
			if (_lastHp > 0)
				addHpText.text = string.Format("+{0:N0}", _lastHp);
			else
				addHpText.text = "";
		}
	}

	const float atkChangeTime = 0.4f;
	float _atkChangeSpeed;
	float _floatCurrentAtk;
	int _lastAtk;
	bool _updateAtkText;
	void UpdateAtkText()
	{
		if (_updateAtkText == false)
			return;

		_floatCurrentAtk += _atkChangeSpeed * Time.deltaTime;
		int currentAtkInt = (int)_floatCurrentAtk;
		if (currentAtkInt <= 0)
		{
			currentAtkInt = 0;
			_updateAtkText = false;
		}
		if (currentAtkInt != _lastAtk)
		{
			_lastAtk = currentAtkInt;
			currentAtkText.text = (_currentAtk + (_addAtk - _lastAtk)).ToString("N0");
			if (_lastAtk > 0)
				addAtkText.text = string.Format("+{0:N0}", _lastAtk);
			else
				addAtkText.text = "";
		}
	}
}