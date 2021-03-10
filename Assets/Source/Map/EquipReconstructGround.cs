using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class EquipReconstructGround : MonoBehaviour
{
	public static EquipReconstructGround instance;

	public Canvas worldCanvas;
	public DOTweenAnimation gaugeImageTweenAnimation;
	public Text baseValueText;
	public Text addValueText;
	public Image gaugeImage;
	public Animator crateAnimator;
	public GameObject greatSuccessTextObject;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	bool _started = false;
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_started = true;
	}

	void OnEnable()
	{
		_currentValue = 0.0f;
	}

	bool _reserveGaugeMoveTweenAnimation;
	void Update()
	{
		UpdateValueText();
		UpdateBaseValueText();

		if (_reserveGaugeMoveTweenAnimation)
		{
			gaugeImageTweenAnimation.DORestart();
			_reserveGaugeMoveTweenAnimation = false;
		}
	}

	TweenerCore<float, float, FloatOptions> _tweenReferenceForGauge;
	public void SetBaseValue(float ratio)
	{
		_currentBaseValue = ratio;
		baseValueText.text = string.Format("{0:0.###}%", ratio * 100.0f);

		if (_tweenReferenceForGauge != null)
			_tweenReferenceForGauge.Kill();

		gaugeImage.fillAmount = 0.0f;
		_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, GetAdjustFillAmount(ratio), 0.5f).SetEase(Ease.OutQuad).SetDelay(0.3f);
		if (_started)
			gaugeImageTweenAnimation.DORestart();
		else
			_reserveGaugeMoveTweenAnimation = true;
	}

	float GetAdjustFillAmount(float ratio)
	{
		// 그림을 반쪽으로 내서 사용하기 때문에 0 ~ 1.0 을 0 ~ 0.5로 바꿔서 쓰면 된다.
		return ratio * 0.5f;
	}

	public void SetDeconstructResult(bool greatSuccess, float baseRatio)
	{
		if (_tweenReferenceForGauge != null)
			_tweenReferenceForGauge.Kill();
		_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, GetAdjustFillAmount(baseRatio), valueChangeTime).SetEase(Ease.OutQuad);

		SetTargetBaseValue(baseRatio);

		if (greatSuccess)
		{
			greatSuccessTextObject.SetActive(false);
			greatSuccessTextObject.SetActive(true);
		}
	}

	public void ClearTargetValue()
	{
		_targetValue = 0.0f;
		addValueText.text = "";
	}

	float _targetValue;
	public void SetTargetValue(float value)
	{
		_targetValue = value;

		_valueChangeRemainTime = valueChangeTime;
		_valueChangeSpeed = (_targetValue - _currentValue) / _valueChangeRemainTime;
		_plusOrMinus = (_valueChangeSpeed > 0.0f) ? true : false;
		//_currentGold = 0.0f;
		_updateValueText = true;
	}

	const float valueChangeTime = 0.3f;
	float _valueChangeRemainTime;
	float _valueChangeSpeed;
	bool _plusOrMinus;
	float _currentValue;
	bool _updateValueText;
	void UpdateValueText()
	{
		if (_updateValueText == false)
			return;

		_currentValue += _valueChangeSpeed * Time.deltaTime;
		if (_plusOrMinus)
		{
			if (_currentValue >= _targetValue)
			{
				_currentValue = _targetValue;
				_updateValueText = false;
			}
		}
		else
		{

			if (_currentValue <= _targetValue)
			{
				_currentValue = _targetValue;
				_updateValueText = false;
			}
		}

		//resultOptionStatusValueText.text = string.Format("{0:0.##}%", (_currentValue + (_addValue - _floatCurrentValue)));
		if (_currentValue == 0.0f)
			addValueText.text = "";
		else
			addValueText.text = string.Format("+{0:0.##}%", _currentValue * 100.0f);
	}



	#region BaseValue Animation
	// 분해 연출 후에 베이스 수치도 주르륵 올라가는 연출이 필요해졌다. 복사해서 쓴다.
	float _targetBaseValue;
	public void SetTargetBaseValue(float value)
	{
		_targetBaseValue = value;

		_baseValueChangeSpeed = (_targetBaseValue - _currentBaseValue) / valueChangeTime;
		_plusOrMinusForBase = (_baseValueChangeSpeed > 0.0f) ? true : false;
		_updateBaseValueText = true;
	}

	float _baseValueChangeSpeed;
	bool _plusOrMinusForBase;
	float _currentBaseValue;
	bool _updateBaseValueText;
	void UpdateBaseValueText()
	{
		if (_updateBaseValueText == false)
			return;

		_currentBaseValue += _baseValueChangeSpeed * Time.deltaTime;
		if (_plusOrMinusForBase)
		{
			if (_currentBaseValue >= _targetBaseValue)
			{
				_currentBaseValue = _targetBaseValue;
				_updateBaseValueText = false;
			}
		}
		else
		{

			if (_currentBaseValue <= _targetBaseValue)
			{
				_currentBaseValue = _targetBaseValue;
				_updateBaseValueText = false;
			}
		}
		baseValueText.text = string.Format("+{0:0.##}%", _currentBaseValue * 100.0f);
	}
	#endregion
}
