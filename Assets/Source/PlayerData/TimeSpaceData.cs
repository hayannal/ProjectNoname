using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

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

	// 하나의 리스트로 관리하려고 하다가 아무리봐도 타입별 리스트로 관리하는게 이득이라 바꿔둔다.
	//List<EquipData> _listEquipData = new List<EquipData>();
	//public List<EquipData> listEquipData { get { return _listEquipData; } }
	List<List<EquipData>> _listEquipData = new List<List<EquipData>>();
	Dictionary<int, EquipData> _dicEquippedData = new Dictionary<int, EquipData>();

	public void OnRecvEquipInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData)
	{
		// list
		if (_listEquipData.Count == 0)
		{
			for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
			{
				List<EquipData> listEquipData = new List<EquipData>();
				_listEquipData.Add(listEquipData);
			}
		}
		for (int i = 0; i < _listEquipData.Count; ++i)
			_listEquipData[i].Clear();
		
		for (int i = 0; i < userInventory.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(userInventory[i].ItemId);
			if (equipTableData == null)
				continue;

			EquipData newEquipData = new EquipData();
			newEquipData.uniqueId = userInventory[i].ItemInstanceId;
			newEquipData.equipId = userInventory[i].ItemId;
			newEquipData.Initialize(userInventory[i].CustomData);
			_listEquipData[newEquipData.cachedEquipTableData.equipType].Add(newEquipData);
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
				EquipData equipData = FindEquipData(uniqueId, (eEquipSlotType)i);
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

	EquipData FindEquipData(string uniqueId, eEquipSlotType equipSlotType)
	{
		List<EquipData> listEquipData = GetEquipListByType(equipSlotType);
		for (int i = 0; i < listEquipData.Count; ++i)
		{
			if (listEquipData[i].uniqueId == uniqueId)
				return listEquipData[i];
		}
		return null;
	}

	public List<EquipData> GetEquipListByType(eEquipSlotType equipSlotType)
	{
		return _listEquipData[(int)equipSlotType];
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

	public EquipData GetEquippedDataByType(eEquipSlotType equipSlotType)
	{
		if (_dicEquippedData.ContainsKey((int)equipSlotType))
			return _dicEquippedData[(int)equipSlotType];
		return null;
	}

	public bool IsExistEquipByType(eEquipSlotType equipSlotType)
	{
		List<EquipData> listEquipData = GetEquipListByType(equipSlotType);
		return listEquipData.Count > 0;
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

	List<EquipData> _listAutoEquipData = new List<EquipData>();
	public void AutoEquip()
	{
		// 현재 장착된 장비보다 공격력이 높다면 auto리스트에 넣는다.
		_listAutoEquipData.Clear();
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
			if (listEquipData.Count == 0)
				continue;

			EquipData selectedEquipData = null;
			float maxValue = 0;
			EquipData equippedData = GetEquippedDataByType((eEquipSlotType)i);
			if (equippedData != null)
				maxValue = equippedData.mainStatusValue;

			for (int j = 0; j < listEquipData.Count; ++j)
			{
				if (maxValue < listEquipData[j].mainStatusValue)
				{
					maxValue = listEquipData[j].mainStatusValue;
					selectedEquipData = listEquipData[j];
				}
			}

			if (selectedEquipData != null)
				_listAutoEquipData.Add(selectedEquipData);
		}

		// auto리스트가 하나도 없다면 변경할게 없는거니 안내 토스트를 출력한다.
		if (_listAutoEquipData.Count == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompleteAuto"), 2.0f);
			return;
		}

		// 변경할게 있다면
		PlayFabApiManager.instance.RequestEquipList(_listAutoEquipData, () =>
		{
			// 변경 완료를 알리고
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_"), 2.0f);

			// 제단을 갱신한다.
			for (int i = 0; i < _listAutoEquipData.Count; ++i)
			{
				int positionIndex = _listAutoEquipData[i].cachedEquipTableData.equipType;
				TimeSpaceGround.instance.timeSpaceAltarList[positionIndex].RefreshEquipObject();
			}
			_listAutoEquipData.Clear();
		});
	}
	#endregion

	#region Grant
	public class ItemGrantRequest
	{
		public Dictionary<string, string> Data;
		public string ItemId;
	}

	List<ItemGrantRequest> _listGrantRequest = new List<ItemGrantRequest>();
	public List<ItemGrantRequest> GenerateGrantInfo(List<string> listEquipId, ref string checkSum)
	{
		if (_listGrantRequest == null)
			_listGrantRequest = new List<ItemGrantRequest>();
		_listGrantRequest.Clear();

		for (int i = 0; i < listEquipId.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listEquipId[i]);
			if (equipTableData == null)
				continue;

			ItemGrantRequest info = new ItemGrantRequest();
			info.ItemId = listEquipId[i];
			info.Data = new Dictionary<string, string>();
			info.Data.Add("mainOp", RandomOption.GetRandomEquipMainOption(equipTableData).ToString());
			info.Data.Add("lock", "0");
			_listGrantRequest.Add(info);
		}

		if (_listGrantRequest.Count > 0)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			string jsonItemGrants = serializer.SerializeObject(_listGrantRequest);
			checkSum = PlayFabApiManager.CheckSum(jsonItemGrants);
		}

		// 임시 리스트를 가지고 있을 필요 없으니 클리어
		_listEquipId.Clear();

		return _listGrantRequest;
	}

	List<string> _listEquipId = new List<string>();
	public List<ItemGrantRequest> GenerateGrantRequestInfo(List<ObscuredString> listDropEquipId, ref string checkSum)
	{
		_listGrantRequest.Clear();
		if (listDropEquipId == null || listDropEquipId.Count == 0)
			return _listGrantRequest;

		_listEquipId.Clear();
		for (int i = 0; i < listDropEquipId.Count; ++i)
			_listEquipId.Add(listDropEquipId[i]);
		return GenerateGrantInfo(_listEquipId, ref checkSum);
	}

	public List<ItemGrantRequest> GenerateGrantInfo(string equipId, ref string checkSum)
	{
		_listEquipId.Clear();
		_listEquipId.Add(equipId);
		return GenerateGrantInfo(_listEquipId, ref checkSum);
	}

	public class GrantItemsToUsersResult
	{
		public List<ItemInstance> ItemGrantResults;
	}

	public List<ItemInstance> DeserializeItemGrantResult(string jsonItemGrantResults)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		GrantItemsToUsersResult result = serializer.DeserializeObject<GrantItemsToUsersResult>(jsonItemGrantResults);
		return result.ItemGrantResults;
	}

	// 대부분의 아이템 획득은 이걸 써서 처리하게 될거다.
	public void OnRecvItemGrantResult(string jsonItemGrantResults)
	{
		List<ItemInstance> listItemInstance = DeserializeItemGrantResult(jsonItemGrantResults);
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listItemInstance[i].ItemId);
			if (equipTableData == null)
				continue;

			EquipData newEquipData = new EquipData();
			newEquipData.uniqueId = listItemInstance[i].ItemInstanceId;
			newEquipData.equipId = listItemInstance[i].ItemId;
			newEquipData.Initialize(listItemInstance[i].CustomData);
			_listEquipData[newEquipData.cachedEquipTableData.equipType].Add(newEquipData);
		}
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
