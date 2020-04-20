using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;

public class EquipAmplifyMainConfirmCanvas : MonoBehaviour
{
	public static EquipAmplifyMainConfirmCanvas instance = null;

	public CanvasGroup canvasGroup;
	public Button backKeyButton;
	public Image backgroundImage;

	public Transform titleTextTransform;
	public EquipCanvasListItem equipListItem;
	public Text mainStatusText;
	public Image mainStatusFillImage;
	public Image expectMainStatusFillImage;
	public Text mainMinText;
	public Text mainMaxText;
	public Text materialCountText;

	public GameObject completeEffectPrefab;
	public RectTransform toastBackImageRectTransform;
	public CanvasGroup processCanvasGroup;

	public GameObject completeObject;
	public EquipCanvasListItem resultEquipListItem;
	public Text resultMainStatusText;
	public DOTweenAnimation resultMainStatusTweenAnimation;

	public Slider resultGaugeSlider;
	public Image resultGaugeImage;
	public DOTweenAnimation resultGaugeColorTween;
	public Image resultGaugeEndPointImage;

	public Text addAtkText;
	public DOTweenAnimation addAtkTweenAnimation;
	public Text resultMainMinText;
	public Text resultMainMaxText;

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
		UpdateAtkText();
		UpdateResultGauge();
	}

	EquipData _equipData;
	int _price;
	public void ShowCanvas(bool show, EquipData equipData, string displayAttack, string displayAttackMin, string displayAttackMax, int price)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		_equipData = equipData;
		_price = price;
		equipListItem.Initialize(equipData, null);
		mainStatusText.text = displayAttack;
		mainStatusFillImage.fillAmount = equipData.GetMainStatusRatio();
		mainMinText.text = displayAttackMin;
		mainMaxText.text = displayAttackMax;

		int materialCount = EquipInfoGrowthCanvas.instance.listMultiSelectEquipData.Count;
		materialCountText.text = materialCount.ToString();

		// 여기서 얼마나 오를지 기대수치를 게이지로 표시해야한다.
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(equipData.cachedEquipTableData.innerGrade);
		float value = equipData.mainStatusValue;
		for (int i = 0; i < materialCount; ++i)
			value *= innerGradeTableData.amplifyMainMax;
		float displayValue = ActorStatus.GetDisplayAttack(value);
		float displayMinValue = ActorStatus.GetDisplayAttack(equipData.GetMainStatusValueMin());
		float displayMaxValue = ActorStatus.GetDisplayAttack(equipData.GetMainStatusValueMax());
		float expectRatio = ((displayValue - displayMinValue) / (displayMaxValue - displayMinValue));
		if (expectRatio > 1.0f) expectRatio = 1.0f;
		expectMainStatusFillImage.fillAmount = expectRatio;

		resultEquipListItem.Initialize(equipData, null);
		resultMainStatusText.text = displayAttack;
		resultGaugeSlider.value = equipData.GetMainStatusRatio();
		resultGaugeImage.color = EquipListStatusInfo.GetGaugeColor(false);
		resultMainMinText.text = displayAttackMin;
		resultMainMaxText.text = displayAttackMax;

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
		List<EquipData> listMultiSelectEquipData = EquipInfoGrowthCanvas.instance.listMultiSelectEquipData;
		float value = _equipData.mainOption;
		for (int i = 0; i < listMultiSelectEquipData.Count; ++i)
		{
			float result = RandomOption.GetRandomRange(innerGradeTableData.amplifyMainMin, innerGradeTableData.amplifyMainMax, (RandomOption.eRandomCalculateType)innerGradeTableData.amplifyMainRandType,
				innerGradeTableData.amplifyMainF1, (innerGradeTableData.amplifyMainLeftRight == 1) ? RandomFromDistribution.Direction_e.Left : RandomFromDistribution.Direction_e.Right);
			value *= result;
			sumPrice += innerGradeTableData.amplifyMainGold;
			materialIndex = i;

			if (value >= _equipData.cachedEquipTableData.max)
			{
				value = _equipData.cachedEquipTableData.max;
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

		// 옵션변경쪽은 선이펙트 없이 후이펙트로만 간다.
		PlayFabApiManager.instance.RequestAmplifyMain(_equipData, value.ToString(), listMultiSelectEquipData, sumPrice, () =>
		{
			EquipInfoGrowthCanvas.instance.currencySmallInfo.RefreshInfo();
			Timing.RunCoroutine(AmplifyMainProcess(listMultiSelectEquipData, sumPrice, returnForMax));
		});
	}

	float _currentAtk;
	float _addAtk;
	IEnumerator<float> AmplifyMainProcess(List<EquipData> listMultiSelectEquipData, int sumPrice, bool returnForMax)
	{
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
		BattleInstanceManager.instance.GetCachedObject(completeEffectPrefab, EquipListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(2.0f);

		// 새로운 Toast Back Image
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.2f);

		completeObject.SetActive(false);
		addAtkText.text = "";
		processCanvasGroup.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		DOTween.To(() => processCanvasGroup.alpha, x => processCanvasGroup.alpha = x, 1.0f, 0.1f);
		yield return Timing.WaitForSeconds(0.3f);

		completeObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		float tweenDelay = 0.3f;		

		// 디스플레이용 공격력을 구하기 위해 강제로 Refresh를 호출한다.
		EquipOptionCanvas.instance.equipStatusInfo.RefreshStatus();
		string targetText = EquipOptionCanvas.instance.equipStatusInfo.mainStatusText.text;
		string currentText = mainStatusText.text;
		int currentValue = 0;
		int targetValue = 0;
		int.TryParse(currentText.Replace(",", ""), out currentValue);
		int.TryParse(targetText.Replace(",", ""), out targetValue);
		_addAtk = targetValue - currentValue;
		_currentAtk = currentValue;
		addAtkText.text = string.Format("+{0:N0}", _addAtk);
		addAtkTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		// 게이지 애니메이션
		float diff = _equipData.GetMainStatusRatio() - resultGaugeSlider.value;
		_fillSpeed = diff / atkChangeTime;
		_fillRemainTime = atkChangeTime;
		resultGaugeColorTween.DORestart();
		resultGaugeEndPointImage.color = new Color(resultGaugeEndPointImage.color.r, resultGaugeEndPointImage.color.g, resultGaugeEndPointImage.color.b, resultGaugeImage.color.a);
		resultGaugeEndPointImage.gameObject.SetActive(true);

		_atkChangeSpeed = -_addAtk / atkChangeTime;
		_floatCurrentAtk = _addAtk;
		_lastAtk = -1;
		_updateAtkText = true;
		yield return Timing.WaitForSeconds(atkChangeTime);
		resultMainStatusTweenAnimation.DORestart();
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
			resultGaugeImage.color = EquipListStatusInfo.GetGaugeColor(_equipData.GetMainStatusRatio() == 1.0f);
			resultGaugeEndPointImage.DOFade(0.0f, 0.25f).SetEase(Ease.OutQuad).onComplete = () => { resultGaugeEndPointImage.gameObject.SetActive(false); };
		}
	}
}