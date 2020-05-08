using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyBoxResultCanvas : MonoBehaviour
{
	public static CurrencyBoxResultCanvas instance;

	public RectTransform goldGroupRectTransform;
	public Text goldValueText;
	public RectTransform diaGroupRectTransform;
	public Text diaValueText;
	public GameObject includeTodayRewardText;

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		UpdateGoldText();
		UpdateDiaText();
	}

	int _addGold;
	int _addDia;
	public void RefreshInfo(int addGold, int addDia, bool showIncludeFirstDayReward = false)
	{
		_addGold = addGold;
		_addDia = addDia;
		goldGroupRectTransform.gameObject.SetActive(addGold > 0);
		diaGroupRectTransform.gameObject.SetActive(addDia > 0);

		if (addGold > 0)
		{
			goldValueText.text = "0";
			_goldChangeRemainTime = goldChangeTime;
			_goldChangeSpeed = _addGold / _goldChangeRemainTime;
			_currentGold = 0.0f;
			_updateGoldText = true;
		}

		if (addDia > 0)
		{
			diaValueText.text = "0";
			_diaChangeRemainTime = diaChangeTime;
			_diaChangeSpeed = _addDia / _diaChangeRemainTime;
			_currentDia = 0.0f;
			_updateDiaText = true;
		}

		includeTodayRewardText.SetActive(showIncludeFirstDayReward);
	}

	const float diaChangeTime = 0.6f;
	float _diaChangeRemainTime;
	float _diaChangeSpeed;
	float _currentDia;
	int _lastDia;
	bool _updateDiaText;
	void UpdateDiaText()
	{
		if (_updateDiaText == false)
			return;

		_currentDia += _diaChangeSpeed * Time.deltaTime;
		int currentDiaInt = (int)_currentDia;
		if (currentDiaInt >= _addDia)
		{
			currentDiaInt = _addDia;
			_updateDiaText = false;
		}
		if (currentDiaInt != _lastDia)
		{
			_lastDia = currentDiaInt;
			diaValueText.text = _lastDia.ToString("N0");
		}
	}

	const float goldChangeTime = 0.6f;
	float _goldChangeRemainTime;
	float _goldChangeSpeed;
	float _currentGold;
	int _lastGold;
	bool _updateGoldText;
	void UpdateGoldText()
	{
		if (_updateGoldText == false)
			return;

		_currentGold += _goldChangeSpeed * Time.unscaledDeltaTime;
		int currentGoldInt = (int)_currentGold;
		if (currentGoldInt >= _addGold)
		{
			currentGoldInt = _addGold;
			_updateGoldText = false;
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}

	public void OnClickExitButton()
	{
		gameObject.SetActive(false);
		RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
	}
}