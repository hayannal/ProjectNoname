using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;
using MEC;

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

	public enum eCurrencyType
	{
		Gold,
		Diamond,
		// Ticket
	}

	public static string DiamondCode() { return "DI"; }
	public static string GoldCode() { return "GO"; }

	public ObscuredInt gold { get; set; }
	public ObscuredInt energy { get; set; }
	public ObscuredInt dia { get; set; }    // 서버 상점에서 모아서 처리하는 기능이 없어서 free와 구매 다 합쳐서 처리하기로 한다.

	public ObscuredInt energyMax { get; set; }

	public static string CurrencyType2Address(eCurrencyType currencyType)
	{
		switch (currencyType)
		{
			case eCurrencyType.Gold: return "Currency_Gold";
			case eCurrencyType.Diamond: return "Currency_Diamond";
		}
		return "";
	}

	public void OnRecvCurrencyData(Dictionary<string, int> userVirtualCurrency, Dictionary<string, VirtualCurrencyRechargeTime> userVirtualCurrencyRechargeTimes)
	{
		if (userVirtualCurrency.ContainsKey("GO"))
			gold = userVirtualCurrency["GO"];
		if (userVirtualCurrency.ContainsKey("EN"))
			energy = userVirtualCurrency["EN"];
		if (userVirtualCurrency.ContainsKey("DI"))
			dia = userVirtualCurrency["DI"];

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
		// MEC쓰려다가 홈키 눌러서 내릴거 대비해서 DateTime검사로 처리한다.
		if (_rechargingEnergy == false)
			return;

		// 한번만 계산하고 넘기니 한번에 여러번 해야하는 상황에서 프레임 단위로 조금씩 밀리게 된다.
		// 어차피 싱크는 맞출테지만 그래도 이왕이면 여러번 체크하게 해둔다. 120회 정도면 24시간도 버틸만할거다.
		int loopCount = 0;
		for (int i = 0; i < 120; ++i)
		{
			if (DateTime.Compare(DateTime.UtcNow, _energyRechargeTime) < 0)
				break;

			loopCount += 1;
			energy += 1;
			if (energy == energyMax)
			{
				_rechargingEnergy = false;
				break;
			}
			else
				_energyRechargeTime += TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneEnergy"));
		}

		// 여러번 건너뛰었단건 홈키 같은거 눌러서 한동안 업데이트 안되다가 몰아서 업데이트 되었단 얘기다. 이럴땐 강제 UI 업데이트
		if (loopCount > 5)
		{
			if (EnergyGaugeCanvas.instance != null)
				EnergyGaugeCanvas.instance.RefreshEnergy();
		}
	}
	
	public bool UseEnergy(int amount)
	{
		if (energy < amount)
			return false;

		bool full = (energy >= energyMax);
		energy -= amount;
		if (full && energy < energyMax)
			_energyRechargeTime = DateTime.UtcNow + TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneEnergy"));
		return true;
	}

	// 던전 입장 후 WaitingNetworkCanvas없이 패킷만 주고받아서 rechargeTime 동기화
	public IEnumerator<float> DelayedSyncEnergyRechargeTime(float delay)
	{
		yield return Timing.WaitForSeconds(delay);

		// avoid gc
		if (this == null)
			yield break;

		// check lobby
		if (MainSceneBuilder.instance.lobby)
			yield break;

		PlayFabApiManager.instance.RequestSyncEnergyRechargeTime();
	}

	public void OnRecvRefillEnergy(int refillAmount)
	{
		energy += refillAmount;

		if (energy >= energyMax)
			_rechargingEnergy = false;

		if (EnergyGaugeCanvas.instance != null)
			EnergyGaugeCanvas.instance.RefreshEnergy();
	}
}