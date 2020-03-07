using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CurrencyData : MonoBehaviour
{
	public static CurrencyData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("CurrencyData")).AddComponent<CurrencyData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static CurrencyData _instance = null;

	public ObscuredInt gold { get; set; }
	public ObscuredInt energy { get; set; }
	public ObscuredInt dia { get; set; }    // 서버가 모아서 보내주는 기능이 없으니 클라가 합산한다.
	public ObscuredInt diaFree { get; set; }
	public int diaTotal { get { return dia + diaFree; } }

	public ObscuredInt energyMax { get; set; }

	public void OnRecvCurrencyData(Dictionary<string, int> userVirtualCurrency, Dictionary<string, VirtualCurrencyRechargeTime> userVirtualCurrencyRechargeTimes)
	{
		if (userVirtualCurrency.ContainsKey("GO"))
			gold = userVirtualCurrency["GO"];
		if (userVirtualCurrency.ContainsKey("EN"))
			energy = userVirtualCurrency["EN"];
		if (userVirtualCurrency.ContainsKey("DI"))
			dia = userVirtualCurrency["DI"];
		if (userVirtualCurrency.ContainsKey("DF"))
			diaFree = userVirtualCurrency["DF"];

		if (userVirtualCurrencyRechargeTimes != null && userVirtualCurrencyRechargeTimes.ContainsKey("EN"))
		{
			energyMax = userVirtualCurrencyRechargeTimes["EN"].RechargeMax;
			if (userVirtualCurrencyRechargeTimes["EN"].SecondsToRecharge > 0)
			{
				_rechargingEnergy = true;
				_energyRechargeTime = userVirtualCurrencyRechargeTimes["EN"].RechargeTime;
			}
			//TimeSpan timeSpan = userVirtualCurrencyRechargeTimes["EN"].RechargeTime - DateTime.UtcNow;
			//int totalSeconds = (int)timeSpan.TotalSeconds;
		}
	}

	void Update()
	{
		UpdateRechargeEnergy();
	}

	bool _rechargingEnergy = false;
	DateTime _energyRechargeTime;
	public DateTime energyRechargeTime { get { return _energyRechargeTime; } }
	void UpdateRechargeEnergy()
	{
		// 홈키 눌러서 내릴거 대비해서 MEC쓰려다가 DateTime검사로 처리한다.
		if (_rechargingEnergy == false)
			return;

		if (DateTime.Compare(DateTime.UtcNow, _energyRechargeTime) >= 0)
		{
			energy += 1;
			if (energy == energyMax)
				_rechargingEnergy = false;
			else
				_energyRechargeTime += TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneEnergy"));
		}
	}
	
	public bool SpendEnergy(int amount)
	{
		if (energy < amount)
			return false;

		bool full = (energy == energyMax);
		energy -= amount;
		if (full)
			_energyRechargeTime = DateTime.UtcNow + TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneEnergy"));
		return true;
	}
}