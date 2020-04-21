using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;
using ActorStatusDefine;

public class EquipAmplifyRandomConfirmCanvas : MonoBehaviour
{
	public static EquipAmplifyRandomConfirmCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;

	public Transform titleTextTransform;
	public EquipCanvasListItem equipListItem;
	public Text optionStatusText;
	public Text optionStatusValueText;
	public Image optionStatusFillImage;
	public Image expectOptionStatusFillImage;
	public Text optionMinText;
	public Text optionMaxText;
	public Text materialCountText;

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
	int _price;
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

		int materialCount = EquipInfoGrowthCanvas.instance.listMultiSelectEquipData.Count;
		materialCountText.text = materialCount.ToString();

		// 여기서 얼마나 오를지 기대수치를 게이지로 표시해야한다.
		float value = info.value;
		float displayValue = 0.0f;
		float displayMinValue = 0.0f;
		float displayMaxValue = 0.0f;
		switch (info.statusType)
		{
			case eActorStatus.MaxHp:
				for (int i = 0; i < materialCount; ++i)
					value *= info.cachedOptionTableData.amplifyMax;
				displayValue = ActorStatus.GetDisplayMaxHp(value);
				displayMinValue = ActorStatus.GetDisplayMaxHp(info.cachedOptionTableData.min);
				displayMaxValue = ActorStatus.GetDisplayMaxHp(info.cachedOptionTableData.max);
				_usePercent = false;
				break;
			case eActorStatus.Attack:
				for (int i = 0; i < materialCount; ++i)
					value *= info.cachedOptionTableData.amplifyMax;
				displayValue = ActorStatus.GetDisplayAttack(value);
				displayMinValue = ActorStatus.GetDisplayAttack(info.cachedOptionTableData.min);
				displayMaxValue = ActorStatus.GetDisplayAttack(info.cachedOptionTableData.max);
				_usePercent = false;
				break;
			default:
				for (int i = 0; i < materialCount; ++i)
					value += info.cachedOptionTableData.amplifyMax;
				// 사실은 % 옵션의 display는 100.0f를 곱하는건데 어차피 게이지 표시용으로 계산하는거라 안하고 그냥 넘긴다.
				displayValue = value;
				displayMinValue = info.cachedOptionTableData.min;
				displayMaxValue = info.cachedOptionTableData.max;
				_usePercent = true;
				break;
		}
		float expectRatio = ((displayValue - displayMinValue) / (displayMaxValue - displayMinValue));
		if (expectRatio > 1.0f) expectRatio = 1.0f;
		expectOptionStatusFillImage.fillAmount = expectRatio;

		resultEquipListItem.Initialize(equipData, null);
		resultOptionStatusText.SetLocalizedText(optionName);
		resultOptionStatusValueText.text = displayString;
		resultGaugeSlider.value = equipData.GetOption(optionIndex).GetRandomStatusRatio();
		resultGaugeImage.color = EquipListStatusInfo.GetGaugeColor(false);
		resultOptionMinText.text = displayMin;
		resultOptionMaxText.text = displayMax;

		priceText.text = price.ToString("N0");
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("EquipUI_OptionAmplifyMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickOkButton()
	{
		// 장비 관련해서는 클라가 처리해야한다.
		bool maxReached = false;
		int materialIndex = -1;
		int sumPrice = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		EquipData.RandomOptionInfo info = _equipData.GetOption(_optionIndex);
		List<EquipData> listMultiSelectEquipData = EquipInfoGrowthCanvas.instance.listMultiSelectEquipData;
		float value = info.value;
		for (int i = 0; i < listMultiSelectEquipData.Count; ++i)
		{
			float result = RandomOption.GetRandomRange(info.cachedOptionTableData.amplifyMin, info.cachedOptionTableData.amplifyMax, (RandomOption.eRandomCalculateType)info.cachedOptionTableData.amplifyRandType,
				info.cachedOptionTableData.amplifyF1, (info.cachedOptionTableData.amplifyLeftRight == 1) ? RandomFromDistribution.Direction_e.Left : RandomFromDistribution.Direction_e.Right);

			switch (info.statusType)
			{
				case eActorStatus.MaxHp:
				case eActorStatus.Attack:
					value *= result;
					break;
				default:
					value += result;
					break;
			}
			sumPrice += innerGradeTableData.amplifyRandomGold;
			materialIndex = i;

			if (value >= info.cachedOptionTableData.max)
			{
				value = info.cachedOptionTableData.max;
				maxReached = true;
				break;
			}
		}

		// 일부만 소모된건지 체크 후 리스트 재설정
		bool returnForMax = false;
		if (maxReached && materialIndex < (listMultiSelectEquipData.Count - 1))
		{
			for (int i = listMultiSelectEquipData.Count - 1; i >= 0; --i)
			{
				if (i > materialIndex)
				{
					listMultiSelectEquipData.RemoveAt(i);
					returnForMax = true;
				}
			}
		}

		if (maxReached == false)
		{
			// 소수점 정리
			value = RandomOption.GetTruncate(value);
		}

		priceButton.gameObject.SetActive(false);

		string optionString = string.Format("{0}:{1}", info.statusType.ToString(), value.ToString());
		PlayFabApiManager.instance.RequestAmplifyRandom(_equipData, _optionIndex, optionString.ToString(), listMultiSelectEquipData, sumPrice, () =>
		{
			EquipInfoGrowthCanvas.instance.currencySmallInfo.RefreshInfo();
			Timing.RunCoroutine(AmplifyRandomProcess(listMultiSelectEquipData, sumPrice, returnForMax));
		});
	}

	float _currentValue;
	float _addValue;
	bool _usePercent = false;
	IEnumerator<float> AmplifyRandomProcess(List<EquipData> listMultiSelectEquipData, int sumPrice, bool returnForMax)
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

		completeObject.SetActive(false);
		addValueText.text = "";
		processCanvasGroup.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		completeObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		float tweenDelay = 0.3f;

		// 디스플레이용 공격력을 구하기 위해 강제로 Refresh를 호출한다.
		EquipOptionCanvas.instance.equipStatusInfo.RefreshStatus();
		string targetText = EquipOptionCanvas.instance.equipStatusInfo.optionStatusValueTextList[_optionIndex].text;
		string currentText = optionStatusValueText.text;
		
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
		float diff = _equipData.GetOption(_optionIndex).GetRandomStatusRatio() - resultGaugeSlider.value;
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

		string text = UIString.instance.GetString("GameUI_TouchToExit");
		if (returnForMax)
			text = string.Format("{0}\n\n<size=16>{1}</size>", text, UIString.instance.GetString("EquipUI_ReturnForMaxAmplify"));
		exitText.SetLocalizedText(text);
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