﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

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

	public const int InventoryVisualMax = 200;
	public const int InventoryRealMax = 249;

	EquipStatusList _cachedEquipStatusList = new EquipStatusList();
	public EquipStatusList cachedEquipStatusList { get { return _cachedEquipStatusList; } }

	// 하나의 리스트로 관리하려고 하다가 아무리봐도 타입별 리스트로 관리하는게 이득이라 바꿔둔다.
	//List<EquipData> _listEquipData = new List<EquipData>();
	//public List<EquipData> listEquipData { get { return _listEquipData; } }
	List<List<EquipData>> _listEquipData = new List<List<EquipData>>();
	Dictionary<int, EquipData> _dicEquippedData = new Dictionary<int, EquipData>();

	// 그 외 변수들
	public ObscuredInt notStreakCount { get; set; }

	// 재구축 포인트
	public ObscuredInt reconstructPoint { get; set; }

	// 이제 강화 최대값은 고정이 아니다.
	int _lastHighestPlayChapter;
	int _cachedMaxEnhanceLevel;
	public int maxEnhanceLevel
	{
		get
		{
			// 캐싱된걸 써도 되는지 확인한다.
			if (_lastHighestPlayChapter == PlayerData.instance.highestPlayChapter)
				return _cachedMaxEnhanceLevel;

			_lastHighestPlayChapter = PlayerData.instance.highestPlayChapter;

			// 먼저 챕터 도달 수치에 따라서 뽑아온다.
			int enhanceLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("DefaultEnhanceLimit");
			if (_lastHighestPlayChapter > 21)
				enhanceLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Over21EnhanceLimit");
			else if (_lastHighestPlayChapter > 14)
				enhanceLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Over14EnhanceLimit");
			else if (_lastHighestPlayChapter > 7)
				enhanceLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Over7EnhanceLimit");

			// 이후 기획에서 제한된 값을 뽑아와서 비교
			int tableMaxEnhance = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxEquipLevel");
			_cachedMaxEnhanceLevel = Mathf.Min(enhanceLevel, tableMaxEnhance);
			return _cachedMaxEnhanceLevel;
		}
	}

	public void GetNextEnhanceLimitInfo(ref int nextLimitChapter, ref int nextLimitEnhance)
	{
		int highestPlayChapter = PlayerData.instance.highestPlayChapter;
		if (highestPlayChapter > 21)
		{
			// 들어올리 없다.
		}
		else if (highestPlayChapter > 14)
		{
			nextLimitChapter = 21;
			nextLimitEnhance = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Over21EnhanceLimit");
		}
		else if (highestPlayChapter > 7)
		{
			nextLimitChapter = 14;
			nextLimitEnhance = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Over14EnhanceLimit");
		}
		else
		{
			nextLimitChapter = 7;
			nextLimitEnhance = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Over7EnhanceLimit");
		}

		int tableMaxEnhance = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxEquipLevel");
		nextLimitEnhance = Mathf.Min(nextLimitEnhance, tableMaxEnhance);
	}

	public void OnRecvEquipInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		ClearInventory();

		// list
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

		// reconstruct
		reconstructPoint = 0;
		if (userReadOnlyData.ContainsKey("recon"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["recon"].Value, out intValue))
				reconstructPoint = intValue;
		}
	}

	public void ClearInventory()
	{
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
		_dicEquippedData.Clear();

		// status
		RefreshCachedStatus();
	}

	public void LateInitialize()
	{
		Timing.RunCoroutine(LoadProcess());
	}

	IEnumerator<float> LoadProcess()
	{
		// 아무래도 로비 진입 후 시공간 들어갈때 너무 렉이 심해서 장착중인 장비의 프리팹은 미리 로딩해두기로 한다.
		// 한번에 하나씩만 로드하기 위해 플래그를 건다.
		// 근데 이래도 오래 걸리는건 여전한데 아웃라인을 동적으로 생성하는데서 온다.
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)i);
			if (equipData == null)
				continue;

			bool waitLoad = true;
			AddressableAssetLoadManager.GetAddressableGameObject(equipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
			{
				waitLoad = false;
			});
			while (waitLoad == true)
				yield return Timing.WaitForOneFrame;
#if !UNITY_EDITOR
			Debug.LogFormat("TimeSpaceData Load Finish. Index : {0} / FrameCount : {1}", i, Time.frameCount);
#endif
		}
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

	public float GetCachedEquipAttackStatusWithEfficiency(float[] equipEfficiency)
	{
		float result = 0.0f;
		Dictionary<int, EquipData>.Enumerator e = _dicEquippedData.GetEnumerator();
		while (e.MoveNext())
		{
			EquipData equipData = e.Current.Value;
			if (equipData == null)
				continue;

			// 이제 모든 템은 기본값이 Attack인데 이걸 그냥 쓰는게 아니라 efficiency에 따라 곱해져서 누적해야한다.
			float mainValue = equipData.mainStatusValue;
			if (equipData.cachedEquipTableData.innerGrade < equipEfficiency.Length)
				mainValue *= equipEfficiency[equipData.cachedEquipTableData.innerGrade];
			result += mainValue;

			// 옵션으로 붙은 공격력의 경우엔 보정 처리 없이 그냥 더해준다. 보정 안받는단 얘기.
			if ((int)eActorStatus.Attack < equipData.equipStatusList.valueList.Length)
				result += equipData.equipStatusList.valueList[(int)eActorStatus.Attack];
		}
		return result;
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
	public void OnEquip(EquipData equipData, bool showEquipEmptySlotTypeCanvas)
	{
		// 장비가 없는 곳에다가 장비를 장착할땐 팀원 전체의 공격력이 오른다는 메세지를 띄우기로 한다. 튜토를 겸해서 하는거고 한번 보여줬다고 더이상 안보여주거나 그러지 않는다.
		int equipType = equipData.cachedEquipTableData.equipType;
		bool emptySlot = false;
		if (showEquipEmptySlotTypeCanvas)
			emptySlot = GetEquippedDataByType((eEquipSlotType)equipType) == null;

		if (_dicEquippedData.ContainsKey(equipType))
			_dicEquippedData[equipType] = equipData;
		else
			_dicEquippedData.Add(equipType, equipData);

		OnChangedEquippedData();

		if (showEquipEmptySlotTypeCanvas && emptySlot)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EquipEmptySlotTypeCanvas", () =>
			{
				EquipEmptySlotTypeCanvas.instance.ShowInfo();
			});
		}
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
		float sumPrevValue = 0.0f;
		float sumNextValue = 0.0f;
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
			sumPrevValue += maxValue;

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

			sumNextValue += maxValue;
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
				AutoEquipResultCanvas.instance.ShowInfo(sumPrevValue, sumNextValue);
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

	public List<ItemGrantRequest> GenerateGrantRequestInfo(string equipId, ref string checkSum)
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
			newEquipData.newEquip = true;
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
		grantNewEquip = true;
	}

	// DailyShop에서 직접 구매가 추가되면서 notStreakCount도 수정하지 않고 legendKey에도 영향을 주지않는 기본적인 아이템 추가가 필요해졌다.
	public EquipData OnRecvGrantEquip(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = DeserializeItemGrantResult(jsonItemGrantResults);
		if (expectCount != 0 && listItemInstance.Count != expectCount)
			return null;

		grantNewEquip = true;

		// 1개일때를 가정하고 등록된 마지막거를 리턴하기로 한다.
		EquipData newEquipData = null;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listItemInstance[i].ItemId);
			if (equipTableData == null)
				continue;

			newEquipData = new EquipData();
			newEquipData.uniqueId = listItemInstance[i].ItemInstanceId;
			newEquipData.equipId = listItemInstance[i].ItemId;
			newEquipData.Initialize(listItemInstance[i].CustomData);
			newEquipData.newEquip = true;
			_listEquipData[newEquipData.cachedEquipTableData.equipType].Add(newEquipData);
		}
		return newEquipData;
	}

	// 로비 포탈 검사할땐 for loop돌면서 newEquip 있는지 확인하는 거보다 플래그 하나 검사하는게 훨씬 편하다.
	public bool grantNewEquip { get; set; }
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

	#region AlarmObject
	public void ResetNewEquip()
	{
		if (grantNewEquip == false)
			return;

		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
			if (listEquipData.Count == 0)
				continue;

			for (int j = 0; j < listEquipData.Count; ++j)
				listEquipData[j].newEquip = false;
		}
		grantNewEquip = false;

		// 시공간에서 마을로 돌아갈땐 항상 TimeSpacePortal이 파티클 재생때문에 꺼졌다 켜진다. 이때 알아서 알람 갱신이 되니 여기서 처리할 필요 없다.
		//if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.gameObject.activeSelf)
		//	TimeSpacePortal.instance.RefreshAlarmObject();

		// 하지만 리셋 타이밍에 제단에 붙어있는 New표시는 삭제해야하므로 호출해준다.
		TimeSpaceGround.instance.RefreshAlarmObjectList();
	}
	#endregion






	public void OnChangedEquippedData()
	{
		// 장착되어있는 장비 중 하나가 변경된거다. 해당 장비는 장착 혹은 탈착 혹은 속성 변경으로 인한 데이터 갱신이 완료된 상태일테니
		// 전체 장비 재계산 후
		RefreshCachedStatus();
		PlayerData.instance.ReinitializeActorStatus();
	}
}
