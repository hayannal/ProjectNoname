using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using ActorStatusDefine;

public class EquipData
{
	public ObscuredString uniqueId;
	public ObscuredString equipId;

	ObscuredBool _isLock;
	ObscuredInt _enhanceLevel;
	ObscuredFloat _mainOption;
	public bool isLock { get { return _isLock; } }
	public int enhanceLevel { get { return _enhanceLevel; } }

	public int optionCount { get { return 0; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	ObscuredFloat _mainStatusValue = 0.0f;
	public float mainStatusValue { get { return _mainStatusValue; } }
	EquipStatusList _equipStatusList = new EquipStatusList();
	public EquipStatusList equipStatusList { get { return _equipStatusList; } }

	public static string KeyMainOp = "mainOp";
	public static string KeyLock = "lock";
	public static string KeyEnhan = "enhan";

	public void Initialize(Dictionary<string, string> customData)
	{
		bool lockState = false;
		int enhan = 0;
		float mainOp = 0.0f;
		if (customData.ContainsKey(KeyEnhan))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyEnhan], out intValue))
				enhan = intValue;
		}
		if (customData.ContainsKey(KeyLock))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyLock], out intValue))
				lockState = (intValue == 1);
		}
		if (customData.ContainsKey(KeyMainOp))
		{
			float floatValue = 0;
			if (float.TryParse(customData[KeyMainOp], out floatValue))
				mainOp = floatValue;
		}

		// 데이터 검증
		// 메인옵부터 체크. 메인옵의 범위가 테이블 범위를 넘어섰다면
		bool invalidEquipOption = false;
		if (mainOp < cachedEquipTableData.min || mainOp > cachedEquipTableData.max)
		{
			invalidEquipOption = true;
			mainOp = cachedEquipTableData.min;
		}
		if (invalidEquipOption)
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidEquipOption, false, 100);
		}

		// 타입에 따라 최대 강화레벨이 정해져있는데 그 범위를 넘어섰다면
		bool invalidEquipEnhance = false;
		if (invalidEquipEnhance)
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidEquipEnhance, false, enhan);

		_isLock = lockState;
		_enhanceLevel = enhan;
		_mainOption = mainOp;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		// 자리별 보너스 같은건 사라졌지만 UI표기를 위해서라도 따로 가지고 있는게 편하다. 이 캐싱엔 강화 정보까지 포함되어있다.
		_mainStatusValue = 0.0f;
		_mainStatusValue = _mainOption;// * cachedEquipTableData.multi;

		// 서브 옵션들을 돌면서 equipStatusList에 모아야한다. 같은 옵은 같은 옵션끼리.
		for (int i = 0; i < _equipStatusList.valueList.Length; ++i)
			_equipStatusList.valueList[i] = 0.0f;
	}

	public void SetLock(bool lockState)
	{
		_isLock = lockState;
	}

	public void OnEnhance()
	{
		_enhanceLevel += 1;

		RefreshCachedStatus();

		// 장착된 장비였을 경우엔 TimeSpace에도 알려서 모든 캐릭터를 재계산해야하니 
		if (TimeSpaceData.instance.IsEquipped(this))
			TimeSpaceData.instance.OnChangedEquippedData();
	}

	public float GetMainStatusRatio()
	{
		return ((_mainOption - cachedEquipTableData.min) / (cachedEquipTableData.max - cachedEquipTableData.min));
	}





	EquipTableData _cachedEquipTableData = null;
	public EquipTableData cachedEquipTableData
	{
		get
		{
			if (_cachedEquipTableData == null)
				_cachedEquipTableData = TableDataManager.instance.FindEquipTableData(equipId);
			return _cachedEquipTableData;
		}
	}
}
