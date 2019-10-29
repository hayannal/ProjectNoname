using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

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

	EquipStatusList _cachedEquipStatusList = new EquipStatusList();
	public EquipStatusList cachedEquipStatusList { get { return _cachedEquipStatusList; } }

	List<EquipData> _listEquipData = new List<EquipData>();
	Dictionary<int, EquipData> _dicEquipedData = new Dictionary<int, EquipData>();

	public void OnRecvEquipInventory()
	{
		// 지금은 패킷 구조를 모르니.. 형태만 만들어두기로 한다.
		// list를 먼저 쭉 받아서 기억해두고 equip 여부에 따라서 dictionary 에 넣어둔다.

		// list

		// dictionary

		// 
		RefreshCachedStatus();
	}

	public void Equip(EquipData equipData, int positionId)
	{

	}

	public void UnEquip(int positionId)
	{

	}

	void RefreshCachedStatus()
	{
		for (int i = 0; i < _cachedEquipStatusList.valueList.Length; ++i)
			_cachedEquipStatusList.valueList[i] = 0.0f;

		Dictionary<int, EquipData>.Enumerator e = _dicEquipedData.GetEnumerator();
		while (e.MoveNext())
		{
			EquipData equipData = e.Current.Value;
			if (equipData == null)
				continue;

			float mainStatusMulti = TableDataManager.instance.FindTimeSpacePositionTableData(e.Current.Key).multi;
			switch ((EquipData.eEquipType)equipData.cachedEquipTableData.equipType)
			{
				case EquipData.eEquipType.Weapon: _cachedEquipStatusList.valueList[(int)eActorStatus.Attack] += (equipData.mainStatusValue * mainStatusMulti); break;
				case EquipData.eEquipType.Armor: _cachedEquipStatusList.valueList[(int)eActorStatus.MaxHp] += (equipData.mainStatusValue * mainStatusMulti); break;
			}

			for (int i = 0; i < _cachedEquipStatusList.valueList.Length; ++i)
				_cachedEquipStatusList.valueList[i] += equipData.equipStatusList.valueList[i];
		}
	}
}
