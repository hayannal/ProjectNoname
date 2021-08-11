using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class AnalysisSimpleResultCanvas : MonoBehaviour
{
	public static AnalysisSimpleResultCanvas instance;

	public RectTransform toastBackImageRectTransform;
	public GameObject titleLineObject;
	public RectTransform goldGroupRectTransform;
	public Text goldValueText;
	public GameObject goldBigSuccessObject;
	public RectTransform diaGroupRectTransform;
	public Text diaValueText;
	public RectTransform energyGroupRectTransform;
	public Text energyValueText;
	public GameObject exitObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		toastBackImageRectTransform.gameObject.SetActive(false);
		titleLineObject.SetActive(false);
		goldGroupRectTransform.gameObject.SetActive(false);
		diaGroupRectTransform.gameObject.SetActive(false);
		energyGroupRectTransform.gameObject.SetActive(false);

		exitObject.SetActive(false);

		Timing.RunCoroutine(RewardProcess());
	}

	IEnumerator<float> RewardProcess()
	{
		_processed = true;

		// 0.1초 초기화 대기 후 시작
		yield return Timing.WaitForSeconds(0.1f);
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		RefreshInfo();

		yield return Timing.WaitForSeconds(0.5f);

		exitObject.SetActive(true);

		// 자꾸 exit가 보이는데도 안눌러진다고 해서 위로 올려둔다.
		_processed = false;

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		// DropManager.instance.ClearLobbyDropInfo(); 대신 
		AnalysisData.instance.ClearCachedInfo();
	}

	bool _processed = false;
	public void OnClickBackButton()
	{
		if (_processed)
			return;

		OnClickExitButton();
	}

	public void OnClickExitButton()
	{
		toastBackImageRectTransform.gameObject.SetActive(false);
		titleLineObject.SetActive(false);
		goldGroupRectTransform.gameObject.SetActive(false);
		diaGroupRectTransform.gameObject.SetActive(false);
		energyGroupRectTransform.gameObject.SetActive(false);
		exitObject.SetActive(false);
		gameObject.SetActive(false);
	}

	void Update()
	{
		UpdateGoldText();
		UpdateDiaText();
		UpdateEnergyText();
	}

	bool _goldBigSuccess;
	int _addGold;
	int _addDia;
	int _addEnergy;
	void RefreshInfo()
	{
		int addEnergy = AnalysisData.instance.cachedDropEnergy;
		int randomGold = AnalysisData.instance.cachedRandomGold;
		int addGold = DropManager.instance.GetLobbyGoldAmount();
		int addDia = DropManager.instance.GetLobbyDiaAmount();

		_goldBigSuccess = (addGold > 0);
		_addGold = addGold + randomGold;
		_addDia = addDia;
		_addEnergy = addEnergy;
		titleLineObject.SetActive(true);
		goldGroupRectTransform.gameObject.SetActive(_addGold > 0);
		diaGroupRectTransform.gameObject.SetActive(_addDia > 0);
		energyGroupRectTransform.gameObject.SetActive(_addEnergy > 0);

		if (_addGold > 0)
		{
			goldValueText.text = "0";
			_goldChangeRemainTime = goldChangeTime;
			_goldChangeSpeed = _addGold / _goldChangeRemainTime;
			_currentGold = 0.0f;
			_updateGoldText = true;

			goldBigSuccessObject.SetActive(false);
		}

		if (_addDia > 0)
		{
			diaValueText.text = "0";
			_diaChangeRemainTime = diaChangeTime;
			_diaChangeSpeed = _addDia / _diaChangeRemainTime;
			_currentDia = 0.0f;
			_updateDiaText = true;
		}

		if (_addEnergy > 0)
		{
			energyValueText.text = "0";
			_energyChangeRemainTime = energyChangeTime;
			_energyChangeSpeed = _addEnergy / _energyChangeRemainTime;
			_currentEnergy = 0.0f;
			_updateEnergyText = true;
		}
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

			if (_goldBigSuccess) goldBigSuccessObject.SetActive(true);
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}

	const float energyChangeTime = 0.6f;
	float _energyChangeRemainTime;
	float _energyChangeSpeed;
	float _currentEnergy;
	int _lastEnergy;
	bool _updateEnergyText;
	void UpdateEnergyText()
	{
		if (_updateEnergyText == false)
			return;

		_currentEnergy += _energyChangeSpeed * Time.unscaledDeltaTime;
		int currentEnergyInt = (int)_currentEnergy;
		if (currentEnergyInt >= _addEnergy)
		{
			currentEnergyInt = _addEnergy;
			_updateEnergyText = false;
		}
		if (currentEnergyInt != _lastEnergy)
		{
			_lastEnergy = currentEnergyInt;
			energyValueText.text = _lastEnergy.ToString("N0");
		}
	}
}