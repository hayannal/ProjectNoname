using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using ActorStatusDefine;

public class EquipData
{
	public int uniqueId;
	public string equipId;
	public bool isLock;

	public enum eEquipType
	{
		Weapon,
		Armor,
	}
	//public bool isWeapon { get { return cachedEquipTableData.equipType == 0; } }

	ObscuredFloat _mainStatusValue = 0.0f;
	public float mainStatusValue { get { return _mainStatusValue; } }
	EquipStatusList _equipStatusList = new EquipStatusList();
	public EquipStatusList equipStatusList { get { return _equipStatusList; } }

	public void OnRecvEquipInfo()
	{
		// 아마도 스트링으로 받은 스탯을 equipStatusList에 넣어둘 듯
	}

	void RefreshCachedStatus()
	{
		// 메인 스탯은 따로 가지고 있어야 자리별 보너스를 받을때 계산하기 편해진다. 이 캐싱엔 강화 정보까지 포함되어있다.
		_mainStatusValue = 0.0f;


		// 서브 옵션들을 돌면서 equipStatusList에 모아야한다. 같은 옵은 같은 옵션끼리.
		for (int i = 0; i < _equipStatusList.valueList.Length; ++i)
			_equipStatusList.valueList[i] = 0.0f;


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
