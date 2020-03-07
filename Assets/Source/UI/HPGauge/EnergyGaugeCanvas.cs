using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyGaugeCanvas : MonoBehaviour
{
	public static EnergyGaugeCanvas instance;

	public Slider energyRatioSlider;
	public Text energyText;
	public Text fillRemainTimeText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		cachedTransform.position = GatePillar.instance.cachedTransform.position;
		RefreshEnergy();
	}

	void Update()
	{
		if (PlayerData.instance.clientOnly)
			return;

		UpdateFillRemainTime();
		UpdateRefresh();
	}

	public void RefreshEnergy()
	{
		if (PlayerData.instance.clientOnly)
			return;

		int current = CurrencyData.instance.energy;
		int max = CurrencyData.instance.energyMax;
		energyRatioSlider.value = (float)current / max;
		energyText.text = string.Format("{0}/{1}", current, max);
		_lastCurrent = current;
		if (current == max)
		{
			fillRemainTimeText.text = "";
			_needUpdate = false;
		}
		else
		{
			_nextFillDateTime = CurrencyData.instance.energyRechargeTime;
			_needUpdate = true;
			_lastRemainTimeSecond = -1;
		}
	}

	bool _needUpdate = false;
	DateTime _nextFillDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateFillRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (DateTime.UtcNow < _nextFillDateTime)
		{
			TimeSpan remainTime = _nextFillDateTime - DateTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				fillRemainTimeText.text = string.Format("{0}:{1:00}", remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			// 우선 클라단에서 하기로 했으니 서버랑 통신해서 바꾸진 않는다.
			// 대신 CurrencyData의 값과 비교하면서 바뀌는지 확인한다.
			_needUpdate = false;
			fillRemainTimeText.text = "0:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (_lastCurrent != CurrencyData.instance.energy)
		{
			RefreshEnergy();
			_needRefresh = false;
		}
	}


	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
