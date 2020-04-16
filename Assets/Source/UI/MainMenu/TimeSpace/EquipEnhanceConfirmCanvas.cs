using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;

public class EquipEnhanceConfirmCanvas : MonoBehaviour
{
	public static EquipEnhanceConfirmCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;

	public Transform titleTextTransform;
	public EquipCanvasListItem equipListItem;
	public Text mainStatusText;
	public Text increaseAtkText;
	public Text materialCountText;

	public GameObject effectPrefab;
	public RectTransform toastBackImageRectTransform;
	public CanvasGroup processCanvasGroup;

	public GameObject successObject;
	public Text successCountText;
	public GameObject failureObject;
	public EquipCanvasListItem resultEquipListItem;
	public Text resultMainStatusText;
	public Text addAtkText;

	public Button priceButton;
	public Text priceText;
	public Text exitText;
	public Graphic processGraphicElement;

	Color _defaultBackgroundColor;
	void Awake()
	{
		instance = this;
		_defaultBackgroundColor = backgroundImage.color;
	}

	void OnEnable()
	{
		canvasGroup.alpha = 1.0f;
		canvasGroup.gameObject.SetActive(true);
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		backgroundImage.color = _defaultBackgroundColor;
		toastBackImageRectTransform.gameObject.SetActive(false);
		processCanvasGroup.gameObject.SetActive(false);
		processCanvasGroup.alpha = 0.0f;
		
		priceButton.gameObject.SetActive(true);
		exitText.gameObject.SetActive(false);

		//nextPowerLevelText.color = Color.white;
		//arrowImage.color = Color.white;
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
		UpdateAtkText();
	}

	EquipData _equipData;
	int _price;
	float _currentAtk;	
	float _addAtk;
	public void ShowCanvas(bool show, EquipData equipData, string displayAttack, int materialCount, int price)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		_equipData = equipData;
		_price = price;
		equipListItem.Initialize(equipData, null);
		mainStatusText.text = displayAttack;
		materialCountText.text = materialCount.ToString();

		resultEquipListItem.Initialize(equipData, null);
		resultMainStatusText.text = displayAttack;

		priceText.text = price.ToString("N0");
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("EquipUI_EquipmentEnhanceMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickOkButton()
	{
	}

	IEnumerator<float> EnhanceProcess()
	{
		bool enhanceResult = false;

		// 인풋 차단
		backKeyButton.interactable = false;
		processGraphicElement.raycastTarget = true;

		// 배경 페이드
		DOTween.To(() => backgroundImage.color, x => backgroundImage.color = x, Color.clear, 0.3f).SetEase(Ease.Linear);
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		// 나머지 창들도 다 닫고
		StackCanvas.Push(gameObject);
		yield return Timing.WaitForSeconds(0.2f);
		canvasGroup.gameObject.SetActive(false);

		// 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, EquipListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.5f);

		// 새로운 Toast Back Image
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.2f);

		successObject.SetActive(enhanceResult);
		successCountText.text = "4";
		failureObject.SetActive(!enhanceResult);
		addAtkText.text = "+20";
		processCanvasGroup.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		// exit
		exitText.SetLocalizedText(UIString.instance.GetString("GameUI_TouchToExit"));
		//"EquipUI_ReturnForMax"
		exitText.gameObject.SetActive(true);
		//yield return Timing.WaitForSeconds(0.2f);

		// 인풋 복구
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		_processed = true;

		//EquipListCanvas.instance.RefreshGrid(false);
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
			DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 0.0f, 0.1f).OnComplete(() => gameObject.SetActive(false));
		toastBackImageRectTransform.gameObject.SetActive(false);
		exitText.gameObject.SetActive(false);
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
			resultMainStatusText.text = (_currentAtk + (_addAtk - _lastAtk)).ToString("N0");
			if (_lastAtk > 0)
				addAtkText.text = string.Format("+{0:N0}", _lastAtk);
			else
				addAtkText.text = "";
		}
	}
}