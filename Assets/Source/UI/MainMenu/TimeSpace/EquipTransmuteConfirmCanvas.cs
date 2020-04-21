using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;
using CodeStage.AntiCheat.ObscuredTypes;
using ActorStatusDefine;

public class EquipTransmuteConfirmCanvas : MonoBehaviour
{
	public static EquipTransmuteConfirmCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;

	public Transform titleTextTransform;
	public EquipCanvasListItem equipListItem;
	public Text optionStatusText;
	public Text optionStatusValueText;
	public Image optionStatusFillImage;
	public Text optionMinText;
	public Text optionMaxText;

	public GameObject completeEffectPrefab;
	public RectTransform toastBackImageRectTransform;
	public CanvasGroup processCanvasGroup;

	public GameObject completeObject;
	public EquipCanvasListItem resultEquipListItem;
	public Text resultOptionStatusText;
	public Text resultOptionStatusValueText;
	public DOTweenAnimation resultOptionStatusTweenAnimation;

	public Slider resultGaugeSlider;
	public Image resultGaugeImage;
	public DOTweenAnimation resultGaugeColorTween;
	public Image resultGaugeEndPointImage;

	public Text addValueText;
	public DOTweenAnimation addValueTweenAnimation;
	public Text resultOptionMinText;
	public Text resultOptionMaxText;

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
		resultGaugeEndPointImage.gameObject.SetActive(false);

		priceButton.gameObject.SetActive(true);
		exitText.gameObject.SetActive(false);

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
		UpdateValueText();
		UpdateResultGauge();
	}

	EquipData _equipData;
	int _optionIndex;
	ObscuredInt _price;
	public void ShowCanvas(bool show, EquipData equipData, int optionIndex, string optionName, string displayString, string displayMin, string displayMax, int price)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		_equipData = equipData;
		_optionIndex = optionIndex;
		_price = price;
		equipListItem.Initialize(equipData, null);

		optionStatusText.SetLocalizedText(optionName);
		optionStatusValueText.text = displayString;
		EquipData.RandomOptionInfo info = equipData.GetOption(optionIndex);
		optionStatusFillImage.fillAmount = info.GetRandomStatusRatio();
		optionMinText.text = displayMin;
		optionMaxText.text = displayMax;

		// 여기서는 물음표라 더이상 할게 없다.
		resultEquipListItem.Initialize(equipData, null);
		priceText.text = price.ToString("N0");
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("EquipUI_OptionTransmuteMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickOkButton()
	{
		// 장비 관련해서는 클라가 처리해야한다.
		eActorStatus eType = eActorStatus.ExAmount;
		float value = 0.0f;
		RandomOption.GenerateRandomOption(_equipData.cachedEquipTableData.optionType, _equipData.cachedEquipTableData.innerGrade, ref eType, ref value, _equipData.GetOption(_optionIndex).statusType);

		priceButton.gameObject.SetActive(false);

		string optionString = string.Format("{0}:{1}", eType.ToString(), value.ToString());
		PlayFabApiManager.instance.RequestTransmute(_equipData, _optionIndex, optionString.ToString(), EquipInfoGrowthCanvas.instance.selectedEquipData, _price, () =>
		{
			EquipInfoGrowthCanvas.instance.currencySmallInfo.RefreshInfo();
			Timing.RunCoroutine(TransmuteProcess(EquipInfoGrowthCanvas.instance.selectedEquipData, _price));
		});
	}

	float _currentValue;
	float _addValue;
	bool _usePercent = false;
	IEnumerator<float> TransmuteProcess(EquipData materialEquipData, int price)
	{
		// 인풋 차단
		backKeyButton.interactable = false;
		processGraphicElement.raycastTarget = true;

		// 배경 페이드
		DOTween.To(() => backgroundImage.color, x => backgroundImage.color = x, Color.clear, 0.3f).SetEase(Ease.Linear);
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		// 제단 이펙트 작게
		EquipInfoGround.instance.ScaleDownGradeParticle(true);
		// 나머지 창들도 다 닫고
		StackCanvas.Push(gameObject);
		yield return Timing.WaitForSeconds(0.2f);
		canvasGroup.gameObject.SetActive(false);

		// 이펙트
		BattleInstanceManager.instance.GetCachedObject(completeEffectPrefab, EquipListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(2.0f);

		// 새로운 Toast Back Image
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.2f);

		// 새 옵션으로 바뀌었을테니 갱신해줘야한다. 디스플레이용 정보를 구하기 위해 강제로 Refresh를 호출한다.
		EquipOptionCanvas.instance.equipStatusInfo.RefreshStatus();
		EquipOptionCanvas.instance.RefreshOption();
		EquipData.RandomOptionInfo info = _equipData.GetOption(_optionIndex);
		switch (info.statusType)
		{
			case eActorStatus.MaxHp:
			case eActorStatus.Attack:
				_usePercent = false;
				break;
			default:
				_usePercent = true;
				break;
		}
		resultOptionStatusText.gameObject.SetActive(false);
		resultOptionStatusValueText.text = "";
		resultGaugeSlider.value = 0.0f;
		resultGaugeImage.color = EquipListStatusInfo.GetGaugeColor(false);
		resultOptionMinText.text = "";
		resultOptionMaxText.text = "";

		completeObject.SetActive(false);
		addValueText.text = "";
		processCanvasGroup.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		completeObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		resultOptionStatusText.SetLocalizedText(EquipOptionCanvas.instance.equipStatusInfo.optionStatusTextList[_optionIndex].text);
		resultOptionStatusText.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		resultOptionStatusValueText.text = EquipOptionCanvas.instance.optionMinTextList[_optionIndex].text;
		resultGaugeSlider.value = 0.0f;
		resultGaugeImage.color = EquipListStatusInfo.GetGaugeColor(false);
		resultOptionMinText.text = EquipOptionCanvas.instance.optionMinTextList[_optionIndex].text;
		resultOptionMaxText.text = EquipOptionCanvas.instance.optionMaxTextList[_optionIndex].text;

		float tweenDelay = 0.3f;
		string targetText = EquipOptionCanvas.instance.equipStatusInfo.optionStatusValueTextList[_optionIndex].text;
		string currentText = resultOptionStatusValueText.text;

		float currentValue = 0;
		float targetValue = 0;
		float.TryParse(currentText.Replace(",", "").Replace("%", ""), out currentValue);
		float.TryParse(targetText.Replace(",", "").Replace("%", ""), out targetValue);
		_addValue = targetValue - currentValue;
		_currentValue = currentValue;
		if (_usePercent)
			addValueText.text = string.Format("+{0:0.##}%", _addValue);
		else
			addValueText.text = string.Format("+{0:N0}", _addValue);
		addValueTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// 게이지 애니메이션
		float diff = info.GetRandomStatusRatio() - resultGaugeSlider.value;
		_fillSpeed = diff / valueChangeTime;
		_fillRemainTime = valueChangeTime;
		resultGaugeColorTween.DORestart();
		resultGaugeEndPointImage.color = new Color(resultGaugeEndPointImage.color.r, resultGaugeEndPointImage.color.g, resultGaugeEndPointImage.color.b, resultGaugeImage.color.a);
		resultGaugeEndPointImage.gameObject.SetActive(true);

		_valueChangeSpeed = -_addValue / valueChangeTime;
		_floatCurrentValue = _addValue;
		_lastValue = -1;
		_updateValueText = true;
		yield return Timing.WaitForSeconds(valueChangeTime);
		resultOptionStatusTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		exitText.gameObject.SetActive(true);

		// 인풋 복구
		backKeyButton.interactable = true;
		processGraphicElement.raycastTarget = false;
		_processed = true;

		// 이펙트 복구
		EquipInfoGround.instance.ScaleDownGradeParticle(false);

		EquipListCanvas.instance.RefreshGrid(true, false);
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

	const float valueChangeTime = 0.4f;
	float _valueChangeSpeed;
	float _floatCurrentValue;
	int _lastValue;
	bool _updateValueText;
	void UpdateValueText()
	{
		if (_updateValueText == false)
			return;

		_floatCurrentValue += _valueChangeSpeed * Time.deltaTime;

		if (_usePercent)
		{
			if (_floatCurrentValue <= 0.0f)
			{
				_floatCurrentValue = 0.0f;
				_updateValueText = false;
			}
			resultOptionStatusValueText.text = string.Format("{0:0.##}%", (_currentValue + (_addValue - _floatCurrentValue)));
			if (_updateValueText)
				addValueText.text = string.Format("+{0:0.##}%", _floatCurrentValue);
			else
				addValueText.text = "";
		}
		else
		{
			int currentAtkInt = (int)_floatCurrentValue;
			if (currentAtkInt <= 0)
			{
				currentAtkInt = 0;
				_updateValueText = false;
			}
			if (currentAtkInt != _lastValue)
			{
				_lastValue = currentAtkInt;
				resultOptionStatusValueText.text = (_currentValue + (_addValue - _lastValue)).ToString("N0");
				if (_lastValue > 0)
					addValueText.text = string.Format("+{0:N0}", _lastValue);
				else
					addValueText.text = "";
			}
		}
	}

	float _fillRemainTime;
	float _fillSpeed;
	void UpdateResultGauge()
	{
		if (_fillRemainTime <= 0.0f)
			return;

		_fillRemainTime -= Time.deltaTime;
		resultGaugeSlider.value += _fillSpeed * Time.deltaTime;

		if (_fillRemainTime <= 0.0f)
		{
			_fillRemainTime = 0.0f;

			resultGaugeColorTween.DOPause();
			resultGaugeImage.color = EquipListStatusInfo.GetGaugeColor(_equipData.GetOption(_optionIndex).GetRandomStatusRatio() == 1.0f);
			resultGaugeEndPointImage.DOFade(0.0f, 0.25f).SetEase(Ease.OutQuad).onComplete = () => { resultGaugeEndPointImage.gameObject.SetActive(false); };
		}
	}
}