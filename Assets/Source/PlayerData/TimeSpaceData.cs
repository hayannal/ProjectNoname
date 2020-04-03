using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using PlayFab.ClientModels;

public class TimeSpaceData
{
	public static TimeSpaceData instance
	{
		get
		{
			if (_instance == null)
				_instance = new TimeSpaceData();
			return _instance;
		}
	}
	static TimeSpaceData _instance = null;

	public enum eEquipSlotType
	{
		Axe,
		Dagger,
		Bow,
		Staff,
		Hammer,
		Sword,
		Gun,
		Shield,
		TwoHanded,

		Amount,
	}

	EquipStatusList _cachedEquipStatusList = new EquipStatusList();
	public EquipStatusList cachedEquipStatusList { get { return _cachedEquipStatusList; } }

	List<EquipData> _listEquipData = new List<EquipData>();
	public List<EquipData> listEquipData { get { return _listEquipData; } }

	Dictionary<int, EquipData> _dicEquippedData = new Dictionary<int, EquipData>();

	public void OnRecvEquipInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData)
	{
		// list
		_listEquipData.Clear();
		for (int i = 0; i < userInventory.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(userInventory[i].ItemId);
			if (equipTableData == null)
				continue;

			EquipData newEquipData = new EquipData();
			newEquipData.uniqueId = userInventory[i].ItemInstanceId;
			newEquipData.equipId = userInventory[i].ItemId;
			newEquipData.Initialize(userInventory[i].CustomData);
			_listEquipData.Add(newEquipData);
		}

		// dictionary
		int invalidEquipSlotIndex = -1;
		_dicEquippedData.Clear();
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			string key = string.Format("eqPo{0}", i);
			if (userData.ContainsKey(key))
			{
				string uniqueId = userData[key].Value;
				if (string.IsNullOrEmpty(uniqueId))
					continue;
				EquipData equipData = FindEquipData(uniqueId);
				if (equipData == null)
				{
					invalidEquipSlotIndex = i;
					continue;
				}
				if (equipData.cachedEquipTableData.equipType != i)
				{
					// 슬롯에 맞지않는 아이템이 장착되어있다. 이것도 잘못된 케이스다.
					invalidEquipSlotIndex = i;
					continue;
				}
				_dicEquippedData.Add(i, equipData);
			}
		}
		if (invalidEquipSlotIndex != -1)
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidEquipType, false, invalidEquipSlotIndex);
		}

		// 
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		for (int i = 0; i < _cachedEquipStatusList.valueList.Length; ++i)
			_cachedEquipStatusList.valueList[i] = 0.0f;

		Dictionary<int, EquipData>.Enumerator e = _dicEquippedData.GetEnumerator();
		while (e.MoveNext())
		{
			EquipData equipData = e.Current.Value;
			if (equipData == null)
				continue;

			// 이제 모든 템은 기본값이 Attack이다.
			_cachedEquipStatusList.valueList[(int)eActorStatus.Attack] += equipData.mainStatusValue;

			for (int i = 0; i < _cachedEquipStatusList.valueList.Length; ++i)
				_cachedEquipStatusList.valueList[i] += equipData.equipStatusList.valueList[i];
		}
	}

	EquipData FindEquipData(string uniqueId)
	{
		for (int i = 0; i < _listEquipData.Count; ++i)
		{
			if (_listEquipData[i].uniqueId == uniqueId)
				return _listEquipData[i];
		}
		return null;
	}

	public bool IsEquipped(EquipData equipData)
	{
		bool equipped = false;
		int equipType = equipData.cachedEquipTableData.equipType;
		if (_dicEquippedData.ContainsKey(equipType))
		{
			if (_dicEquippedData[equipType].uniqueId == equipData.uniqueId)
				equipped = true;
		}
		return equipped;
	}

	#region Packet
	public void OnEquip(EquipData equipData)
	{
		int equipType = equipData.cachedEquipTableData.equipType;
		if (_dicEquippedData.ContainsKey(equipType))
			_dicEquippedData[equipType] = equipData;
		else
			_dicEquippedData.Add(equipType, equipData);

		OnChangedEquippedData();
	}

	public void OnUnequip(EquipData equipData)
	{
		int equipType = equipData.cachedEquipTableData.equipType;
		if (_dicEquippedData.ContainsKey(equipType))
			_dicEquippedData.Remove(equipType);

		OnChangedEquippedData();
	}
	#endregion






	public void OnChangedEquippedData()
	{
		// 장착되어있는 장비 중 하나가 변경된거다. 해당 장비는 장착 혹은 탈착 혹은 속성 변경으로 인한 데이터 갱신이 완료된 상태일테니
		// 전체 장비 재계산 후
		RefreshCachedStatus();

		// 모든 캐릭터의 스탯을 재계산 하도록 알려야한다.
	}
}
