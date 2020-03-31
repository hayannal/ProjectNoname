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
	ObscuredFloat _mainStatusValue = 0.0f;
	public bool isLock { get { return _isLock; } }
	public int enhanceLevel { get { return _enhanceLevel; } }
	public float mainStatusValue { get { return _mainStatusValue; } }
	
	// 랜덤옵 합산
	EquipStatusList _equipStatusList = new EquipStatusList();
	public EquipStatusList equipStatusList { get { return _equipStatusList; } }

	public void Initialize(Dictionary<string, string> customData)
	{
		bool lockState = false;
		int enhan = 0;
		if (customData.ContainsKey("enhan"))
		{
			int intValue = 0;
			if (int.TryParse(customData["enhan"], out intValue))
				enhan = intValue;
		}
		if (customData.ContainsKey("lock"))
		{
			int intValue = 0;
			if (int.TryParse(customData["lock"], out intValue))
				lockState = (intValue == 1);
		}

		// 데이터 검증

		// 타입에 따라 최대 강화레벨이 정해져있는데 그 범위를 넘어섰다면

		_isLock = lockState;
		_enhanceLevel = enhan;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		// 자리별 보너스 같은건 사라졌지만 UI표기를 위해서라도 따로 가지고 있는게 편하다. 이 캐싱엔 강화 정보까지 포함되어있다.
		_mainStatusValue = 0.0f;

		// test
		_mainStatusValue = cachedEquipTableData.min;

		// 서브 옵션들을 돌면서 equipStatusList에 모아야한다. 같은 옵은 같은 옵션끼리.
		for (int i = 0; i < _equipStatusList.valueList.Length; ++i)
			_equipStatusList.valueList[i] = 0.0f;
	}

	public void OnEnhance()
	{
		_enhanceLevel += 1;

		RefreshCachedStatus();

		// 장착된 장비였을 경우엔 TimeSpace에도 알려서 모든 캐릭터를 재계산해야하니 
		if (TimeSpaceData.instance.IsEquipped(this))
			TimeSpaceData.instance.OnChangedEquippedData();
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
