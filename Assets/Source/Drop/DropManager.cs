using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class DropManager : MonoBehaviour
{
	public static DropManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("DropManager")).AddComponent<DropManager>();
			return _instance;
		}
	}
	static DropManager _instance = null;

	ObscuredFloat _dropGold;
	ObscuredInt _dropSeal;

	public void AddDropGold(float gold)
	{
		// 이미 DropAdjustAffector.eDropAdjustType.GoldDropAmount 적용된채로 누적되어있는 값이다. 정산시 더해주면 끝이다.
		_dropGold += gold;
	}

	public int GetStackedDropGold()
	{
		return (int)_dropGold;
	}

	public void AddDropItem()
	{

	}

	public int GetDropItemCount()
	{
		return 0;
	}

	public void AddDropSeal(int amount)
	{
		_dropSeal += amount;
	}

	public int GetStackedDropSeal()
	{
		return _dropSeal;
	}
}