using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoEquipResultCanvas : MonoBehaviour
{
	public static AutoEquipResultCanvas instance;

	public CanvasGroup canvasGroup;
	public GameObject textRootObject;
	public Text diffValueText;
	public Text scaleDiffValueText;

	Color _defaultFontColor;
	void Awake()
	{
		instance = this;
		_defaultFontColor = diffValueText.color;
	}

	void OnEnable()
	{
		canvasGroup.alpha = 1.0f;
	}

	void OnDisable()
	{
		textRootObject.SetActive(false);
		_currentValue = 0.0f;
	}

	public void ShowInfo(float sumPrevValue, float sumNextValue)
	{
		float prevDisplayValue = (sumPrevValue == 0.0f) ? 0.0f : ActorStatus.GetDisplayAttack(sumPrevValue);
		float nextDisplayValue = (sumNextValue == 0.0f) ? 0.0f : ActorStatus.GetDisplayAttack(sumNextValue);
		int diff = (int)(nextDisplayValue - prevDisplayValue);
		if (diff <= 0) diff = 1;
		_targetValue = diff;
		_valueChangeSpeed = _targetValue / valueChangeTime;

		diffValueText.color = _defaultFontColor;
		diffValueText.text = "0";
		scaleDiffValueText.text = "";
	}

	public void OnCompleteScaleAnimation()
	{
		textRootObject.SetActive(true);
		_lastValue = -1;
		_updateValueText = true;
	}


	const float valueChangeTime = 0.45f;
	float _valueChangeRemainTime;
	float _valueChangeSpeed = 0.0f;
	float _currentValue;
	int _lastValue;
	int _targetValue;
	bool _updateValueText;
	void Update()
	{
		if (_updateValueText == false)
			return;

		_currentValue += _valueChangeSpeed * Time.deltaTime;
		int currentValueInt = (int)_currentValue;
		if (currentValueInt >= _targetValue)
		{
			currentValueInt = _targetValue;
			diffValueText.color = Color.clear;
			scaleDiffValueText.text = _targetValue.ToString("N0");
			_updateValueText = false;
		}
		if (currentValueInt != _lastValue)
		{
			_lastValue = currentValueInt;
			diffValueText.text = _lastValue.ToString("N0");
		}
	}
}