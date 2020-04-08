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


	#region Drop Info
	ObscuredFloat _dropGold;
	ObscuredInt _dropSeal;
	List<ObscuredString> _listDropEquip;

	public void AddDropGold(float gold)
	{
		// 이미 DropAdjustAffector.eDropAdjustType.GoldDropAmount 적용된채로 누적되어있는 값이다. 정산시 더해주면 끝이다.
		_dropGold += gold;
	}

	public int GetStackedDropGold()
	{
		return (int)_dropGold;
	}

	public void AddDropItem(string equipId)
	{
		if (string.IsNullOrEmpty(equipId))
			return;

		if (_listDropEquip == null)
			_listDropEquip = new List<ObscuredString>();

		_listDropEquip.Add(equipId);

		// 장비 획득이 되면 서버에 카운트를 증가시켜둔다. EndGame에서 검증하기 위함.
		//PlayFabApiManager.instance.RequestAddDropEquipCount(1);
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


	int _stackDropExp = 0;
	public void StackDropExp(int exp)
	{
		_stackDropExp += exp;
	}

	public void GetStackedDropExp()
	{
		// Stack된걸 적용하기 직전에 현재 맵의 보정치를 적용시킨다.
		_stackDropExp += StageManager.instance.addDropExp;
		_stackDropExp = (int)(_stackDropExp * StageManager.instance.currentStageTableData.DropExpAdjustment);

		//Debug.LogFormat("Drop Exp Add {0} / Get Exp : {1}", StageManager.instance.addDropExp, _stackDropExp);

		if (_stackDropExp < 0)
			Debug.LogError("Invalid Drop Exp : Negative Total Exp!");

		// 경험치 얻는 처리를 한다.
		// 이펙트가 먼저 나오고 곧바로 렙업창이 뜬다. 두번 이상 렙업 되는걸 처리하기 위해 업데이트 돌면서 스택에 쌓아둔채 꺼내쓰는 방법으로 해야할거다.
		StageManager.instance.AddExp(_stackDropExp);

		_stackDropExp = 0;
	}

	// 레벨팩이 드랍되면 체크해놨다가 먹어야 GatePillar가 나오게 해야한다.
	public int reservedLevelPackCount { get; set; }
	#endregion



	#region Drop Object
	List<DropObject> _listDropObject = new List<DropObject>();
	public void OnInitializeDropObject(DropObject dropObject)
	{
		_listDropObject.Add(dropObject);
	}

	public void OnFinalizeDropObject(DropObject dropObject)
	{
		_listDropObject.Remove(dropObject);
	}

	DropObject _reservedLastDropObject;
	public void ReserveLastDropObject(DropObject dropObject)
	{
		_reservedLastDropObject = dropObject;
	}

	public bool IsExistReservedLastDropObject()
	{
		return (_reservedLastDropObject != null);
	}

	public void ApplyLastDropObject()
	{
		if (_reservedLastDropObject != null)
		{
			_reservedLastDropObject.ApplyLastDropObject();
			_reservedLastDropObject = null;
		}
	}

	public void OnDropLastMonsterInStage()
	{
		for (int i = 0; i < _listDropObject.Count; ++i)
			_listDropObject[i].OnAfterBattle();
	}

	public void OnFinishLastDropAnimation()
	{
		for (int i = 0; i < _listDropObject.Count; ++i)
		{
			// 다음 스테이지에 드랍된 템들은 켜져있지 않을거다. 패스.
			if (_listDropObject[i].onAfterBattle == false)
				continue;

			_listDropObject[i].OnAfterAllDropAnimation();
		}
	}
	#endregion


	#region EndGame
	public bool IsExistAcquirableDropObject()
	{
		// 정산 직전에 쓰는 함수다. 획득할 수 있는 드랍 오브젝트가 하나도 없어야 정산이 가능하다.

		// 드랍 오브젝트가 생성되기 전이라서 드랍정보만 가지고는 알기 어렵기 때문에 DropProcessor가 하나라도 살아있다면 우선 기다린다.
		if (BattleInstanceManager.instance.IsAliveAnyDropProcessor())
			return true;

		// 생성되어있는 DropObject를 뒤져서 획득할 수 있는게 하나도 없어야 한다.
		for (int i = 0; i < _listDropObject.Count; ++i)
		{
			if (_listDropObject[i].IsAcquirableForEnd())
				return true;
		}
		return false;
	}
	#endregion
}