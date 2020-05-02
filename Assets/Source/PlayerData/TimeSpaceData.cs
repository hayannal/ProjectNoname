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

	public const int InventoryVisualMax = 90;
	public const int InventoryRealMax = 99;

	EquipStatusList _cachedEquipStatusList = new EquipStatusList();
	public EquipStatusList cachedEquipStatusList { get { return _cachedEquipStatusList; } }

	// 하나의 리스트로 관리하려고 하다가 아무리봐도 타입별 리스트로 관리하는게 이득이라 바꿔둔다.
	//List<EquipData> _listEquipData = new List<EquipData>();
	//public List<EquipData> listEquipData { get { return _listEquipData; } }
	List<List<EquipData>> _listEquipData = new List<List<EquipData>>();
	Dictionary<int, EquipData> _dicEquippedData = new Dictionary<int, EquipData>();

	// 그 외 변수들
	public ObscuredInt notStreakCount { get; set; }

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

		// status
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

	public void PreloadEquipIcon()
	{
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
			for (int j = 0; j < listEquipData.Count; ++j)
				AddressableAssetLoadManager.GetAddressableSprite(listEquipData[j].cachedEquipTableData.shotAddress, "Icon", null);
		}	
	}

	public bool IsInventoryVisualMax()
	{
		return (inventoryItemCount >= InventoryVisualMax);
	}

	public int inventoryItemCount
	{
		get
		{
			int count = 0;
			for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
			{
				List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
				count += listEquipData.Count;
			}
			return count;
		}
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
	float _sumPrevValue = 0.0f;
	float _sumNextValue = 0.0f;
	public void AutoEquip()
	{
		// 현재 장착된 장비보다 공격력이 높다면 auto리스트에 넣는다.
		_listAutoEquipData.Clear();
		_sumPrevValue = 0.0f;
		_sumNextValue = 0.0f;
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
			_sumPrevValue += maxValue;

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

			_sumNextValue += maxValue;
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
			UIInstanceManager.instance.ShowCanvasAsync("AutoEquipResultCanvas", () =>
			{
				AutoEquipResultCanvas.instance.ShowInfo(_sumPrevValue, _sumNextValue);
			});

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
		_listGrantRequest.Clear();

		for (int i = 0; i < listEquipId.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listEquipId[i]);
			if (equipTableData == null)
				continue;

			ItemGrantRequest info = new ItemGrantRequest();
			info.ItemId = listEquipId[i];
			info.Data = new Dictionary<string, string>();
			info.Data.Add(EquipData.KeyMainOp, RandomOption.GetRandomEquipMainOption(equipTableData).ToString());

			int createOptionCount = RandomOption.GetRandomOptionCount(equipTableData.innerGrade);
			for (int j = 0; j < createOptionCount; ++j)
			{
				eActorStatus eType = eActorStatus.ExAmount;
				float value = 0.0f;
				RandomOption.GenerateRandomOption(equipTableData.optionType, equipTableData.innerGrade, ref eType, ref value);
				info.Data.Add(string.Format("{0}{1}", EquipData.KeyRandomOp, j), string.Format("{0}:{1}", eType.ToString(), value.ToString()));
			}
			if (createOptionCount > 0)
				info.Data.Add(EquipData.KeyTransmuteRemainCount, RandomOption.GetTransmuteRemainCount(equipTableData.innerGrade).ToString());
			// 이거 5개 제한이라서 lock대신 옵션 변경 제한횟수에 사용하기로 한다.
			//info.Data.Add(EquipData.KeyLock, "0");
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
	public void OnRecvItemGrantResult(string jsonItemGrantResults, bool useLegendKey)
	{
		int legendItemCount = 0;
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

			if (EquipData.IsUseLegendKey(newEquipData.cachedEquipTableData))
				++legendItemCount;
		}
		if (useLegendKey)
		{
			bool invalid = false;
			if (legendItemCount != DropManager.instance.droppedLengendItemCount)
				invalid = true;
			if ((legendItemCount * 10) > CurrencyData.instance.legendKey)
				invalid = true;
			if (invalid)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidLegendKey);

			CurrencyData.instance.legendKey -= legendItemCount * 10;
		}
		else
		{
			// 전설키를 안쓴다는건 뽑기라는 얘기이므로 연속전설뽑기 못한 처리를 해준다.
			if (legendItemCount == 0)
				PlayerData.instance.notStreakCount += listItemInstance.Count;
			else
				PlayerData.instance.notStreakCount = 0;
		}
	}
	#endregion

	#region Revoke
	public class RevokeInventoryItemRequest
	{
		public string ItemInstanceId;
	}
	List<RevokeInventoryItemRequest> _listRevokeInventoryItemRequest = new List<RevokeInventoryItemRequest>();

	public List<RevokeInventoryItemRequest> GenerateRevokeInfo(List<EquipData> listRevokeEquipData, int price, string additionalData, ref string checkSum)
	{
		_listRevokeInventoryItemRequest.Clear();

		for (int i = 0; i < listRevokeEquipData.Count; ++i)
		{
			RevokeInventoryItemRequest info = new RevokeInventoryItemRequest();
			info.ItemInstanceId = listRevokeEquipData[i].uniqueId;
			_listRevokeInventoryItemRequest.Add(info);
		}

		if (_listRevokeInventoryItemRequest.Count > 0)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			string jsonRevokeInventory = serializer.SerializeObject(_listRevokeInventoryItemRequest);
			checkSum = PlayFabApiManager.CheckSum(string.Format("{0}_{1}_{2}", jsonRevokeInventory, price, additionalData));
		}

		return _listRevokeInventoryItemRequest;
	}

	List<EquipData> _listRevokeEquipData = new List<EquipData>();
	public List<RevokeInventoryItemRequest> GenerateRevokeInfo(EquipData revokeEquipData, int price, string additionalData, ref string checkSum)
	{
		_listRevokeEquipData.Clear();
		_listRevokeEquipData.Add(revokeEquipData);
		return GenerateRevokeInfo(_listRevokeEquipData, price, additionalData, ref checkSum);
	}

	public void OnRevokeInventory(bool checkEquipped = false)
	{
		// 재료가 하나였을땐 내부 _listRevokeEquipData를 사용해서 한걸테니 그대로 전달한다.
		OnRevokeInventory(_listRevokeEquipData, checkEquipped);
	}

	public void OnRevokeInventory(List<EquipData> listRevokeEquipData, bool checkEquipped = false)
	{
		bool unequip = false;
		for (int i = 0; i < listRevokeEquipData.Count; ++i)
		{
			TimeSpaceData.eEquipSlotType equipType = (TimeSpaceData.eEquipSlotType)listRevokeEquipData[i].cachedEquipTableData.equipType;
			if (checkEquipped && IsEquipped(listRevokeEquipData[i]))
			{
				_dicEquippedData.Remove((int)equipType);
				unequip = true;
			}

			List<EquipData> listEquipData = TimeSpaceData.instance.GetEquipListByType(equipType);
			if (listEquipData.Contains(listRevokeEquipData[i]))
			{
				listEquipData.Remove(listRevokeEquipData[i]);
			}
			else
			{
				Debug.LogErrorFormat("Revoke Inventory Error. Not found Equip : {0}", listRevokeEquipData[i].uniqueId);
			}
		}

		// 장착된걸 지웠을땐 바로 스탯을 재계산한다.
		if (unequip)
			OnChangedEquippedData();
	}
	#endregion






	public void OnChangedEquippedData()
	{
		// 장착되어있는 장비 중 하나가 변경된거다. 해당 장비는 장착 혹은 탈착 혹은 속성 변경으로 인한 데이터 갱신이 완료된 상태일테니
		// 전체 장비 재계산 후
		RefreshCachedStatus();

		// 모든 캐릭터의 스탯을 재계산 하도록 알려야한다.
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
		{
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(PlayerData.instance.listCharacterData[i].actorId);
			if (playerActor == null)
				continue;
			playerActor.actorStatus.InitializeActorStatus();
		}
	}
}
