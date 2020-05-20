using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EquipSellGround : MonoBehaviour
{
	public static EquipSellGround instance;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		_currentGold = 0.0f;
		goldValueText.text = "0";
	}

	public Text goldValueText;
	public DOTweenAnimation goldImageTweenAnimation;

	void Update()
	{
		UpdateGoldText();
	}

	int _targetGold;
	public void SetTargetPrice(int price)
	{
		_targetGold = price;

		_goldChangeRemainTime = goldChangeTime;
		_goldChangeSpeed = (_targetGold - _currentGold) / _goldChangeRemainTime;
		_plusOrMinus = (_goldChangeSpeed > 0.0f) ? true : false;
		//_currentGold = 0.0f;
		_updateGoldText = true;

		goldImageTweenAnimation.DOPlay();
	}

	const float goldChangeTime = 0.4f;
	float _goldChangeRemainTime;
	float _goldChangeSpeed;
	bool _plusOrMinus;
	float _currentGold;
	int _lastGold;
	bool _updateGoldText;
	void UpdateGoldText()
	{
		if (_updateGoldText == false)
			return;

		_currentGold += _goldChangeSpeed * Time.deltaTime;
		int currentGoldInt = (int)_currentGold;
		if (_plusOrMinus)
		{
			if (currentGoldInt >= _targetGold)
			{
				currentGoldInt = _targetGold;
				_updateGoldText = false;
			}
		}
		else
		{
			if (currentGoldInt <= _targetGold)
			{
				currentGoldInt = _targetGold;
				_updateGoldText = false;
			}
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}
}