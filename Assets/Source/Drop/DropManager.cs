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
	List<ObscuredString> _listDropEquipId;

	public void AddDropGold(float gold)
	{
		// 이미 DropAdjustAffector.eDropAdjustType.GoldDropAmount 적용된채로 누적되어있는 값이다. 정산시 더해주면 끝이다.
		_dropGold += gold;
	}

	public int GetStackedDropGold()
	{
		return (int)_dropGold;
	}

	public void AddDropSeal(int amount)
	{
		_dropSeal += amount;
	}

	public int GetStackedDropSeal()
	{
		return _dropSeal;
	}

	public void AddDropItem(string equipId)
	{
		if (string.IsNullOrEmpty(equipId))
			return;

		if (_listDropEquipId == null)
			_listDropEquipId = new List<ObscuredString>();

		_listDropEquipId.Add(equipId);

		// 장비 획득이 되면 서버에 카운트를 증가시켜둔다. EndGame에서 검증하기 위함으로 하려다가 오히려 선량한 유저의 플레이를 방해할까봐 안하기로 한다.
		// 적어도 전설에 대해선 황금열쇠 체크를 하니 패스하기로 해본다.
		//PlayFabApiManager.instance.RequestAddDropEquipCount();
	}

	public List<ObscuredString> GetStackedDropEquipList()
	{
		return _listDropEquipId;
	}

	#region Legend Key
	// 전설키를 DropItem과 달리 따로 체크해야한다.
	// 위 DropItem은 습득하고 난 아이템 리스트를 관리하는건데
	// 전설키의 개수를 가지고 weight를 조정하는건 드랍되는 시점에서 바로 카운트에 반영되어야하는거라
	// DropItem에 들어있는 전설로 하게되면 틀어질 수 있다.(전설키가 1개 남은 상황에서 2개의 전설이 드랍될 수 있다.)
	//
	// 그래서 차라리 별도의 드랍 카운트 변수를 만들고
	// 드랍이 결정될때마다 증가시켜서 관리하기로 한다.
	public int droppedLengendItemCount { get; set; }
	#endregion


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






	/// ///////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 여기서부터는 뽑기로직
	#region Stage Drop Equip
	class RandomDropEquipInfo
	{
		public EquipTableData equipTableData;
		public float sumWeight;
	}
	List<RandomDropEquipInfo> _listRandomDropEquipInfo = null;
	int _lastDropChapter = -1;
	int _lastDropStage = -1;
	int _lastLegendKey = -1;
	public string GetStageDropEquipId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby)
			return "";

		bool needRefresh = false;
		if (_lastDropChapter != StageManager.instance.playChapter || _lastDropStage != StageManager.instance.playStage || _lastLegendKey != GetRemainLegendKey())
		{
			needRefresh = true;
			_lastDropChapter = StageManager.instance.playChapter;
			_lastDropStage = StageManager.instance.playStage;
			_lastLegendKey = GetRemainLegendKey();
		}

		if (needRefresh)
		{
			if (_listRandomDropEquipInfo == null)
				_listRandomDropEquipInfo = new List<RandomDropEquipInfo>();
			_listRandomDropEquipInfo.Clear();

			float sumWeight = 0.0f;
			for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
			{
				float weight = TableDataManager.instance.equipTable.dataArray[i].stageDropWeight;
				if (weight <= 0.0f)
					continue;

				bool add = false;
				if (StageManager.instance.playChapter > TableDataManager.instance.equipTable.dataArray[i].startingDropChapter)
					add = true;
				// MonsterActor.OnDie 함수 안에서 드랍의 모든 정보가 결정되기때문에 StageManager.instance.playStage을 사용해도 괜찮다.
				// 다음 스테이지로 넘어가서 드랍아이템이 생성되더라도 이미 정보는 다 킬 시점에 결정되기 때문.
				if (add == false && StageManager.instance.playChapter == TableDataManager.instance.equipTable.dataArray[i].startingDropChapter && StageManager.instance.playStage >= TableDataManager.instance.equipTable.dataArray[i].startingDropStage)
					add = true;
				if (add == false)
					continue;

				if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]))
				{
					float adjustWeight = 0.0f;
					RemainTableData remainTableData = TableDataManager.instance.FindRemainTableData(GetRemainLegendKey());
					if (remainTableData != null)
						adjustWeight = remainTableData.adjustWeight;
					// adjustWeight 검증
					if (adjustWeight > 1.0f)
						CheatingListener.OnDetectCheatTable();
					weight *= adjustWeight;
					if (weight <= 0.0f)
						continue;
				}

				sumWeight += weight;
				RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
				newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
				newInfo.sumWeight = sumWeight;
				_listRandomDropEquipInfo.Add(newInfo);
			}
		}

		if (_listRandomDropEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomDropEquipInfo[_listRandomDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropEquipInfo.Count; ++i)
		{
			if (random <= _listRandomDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";

		// 바로 감소시켜놔야 다음번 드랍될때 _lastLegendKey가 달라지면서 드랍 리스트를 리프레쉬 하게 된다.
		if (EquipData.IsUseLegendKey(_listRandomDropEquipInfo[index].equipTableData))
			++droppedLengendItemCount;
		return _listRandomDropEquipInfo[index].equipTableData.equipId;
	}

	int GetRemainLegendKey()
	{
		return CurrencyData.instance.legendKey - droppedLengendItemCount * 10;
	}
	#endregion



	#region Gacha Drop Equip
	float _lastNotStreakAdjustWeight = -1.0f;
	public string GetGachaEquipId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			return "";
		bool needRefresh = false;
		float notStreakAdjustWeight = TableDataManager.instance.FindNotStreakAdjustWeight(GetCurrentNotSteakCount());
		if (_lastNotStreakAdjustWeight != notStreakAdjustWeight)
		{
			needRefresh = true;
			_lastNotStreakAdjustWeight = notStreakAdjustWeight;
		}

		if (needRefresh)
		{
			// AdjustWeight 검증
			if (notStreakAdjustWeight > 1.7f)
				CheatingListener.OnDetectCheatTable();

			if (_listRandomDropEquipInfo == null)
				_listRandomDropEquipInfo = new List<RandomDropEquipInfo>();
			_listRandomDropEquipInfo.Clear();

			float sumWeight = 0.0f;
			for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
			{
				float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
				if (weight <= 0.0f)
					continue;

				// equipGachaWeight 검증
				if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]) && weight > 1.0f)
					CheatingListener.OnDetectCheatTable();

				if (EquipData.IsUseNotStreakGacha(TableDataManager.instance.equipTable.dataArray[i]))
					weight *= notStreakAdjustWeight;

				sumWeight += weight;
				RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
				newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
				newInfo.sumWeight = sumWeight;
				_listRandomDropEquipInfo.Add(newInfo);
			}
		}

		if (_listRandomDropEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomDropEquipInfo[_listRandomDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropEquipInfo.Count; ++i)
		{
			if (random <= _listRandomDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";

		// 전설이 나오지 않으면 바로 누적시켜놔야 다음번 드랍될때 GetCurrentNotSteakCount()값이 달라지면서 체크할 수 있게된다.
		if (EquipData.IsUseLegendKey(_listRandomDropEquipInfo[index].equipTableData) == false)
			++_droppedNotStreakItemCount;
		return _listRandomDropEquipInfo[index].equipTableData.equipId;
	}
	int _droppedNotStreakItemCount = 0;
	int GetCurrentNotSteakCount()
	{
		// 임시변수를 만들어서 계산하다가 서버에서 리턴받을때 적용해보자
		return PlayerData.instance.notStreakCount + _droppedNotStreakItemCount;
	}
	#endregion



	#region Gacha Character
	class RandomGachaActorInfo
	{
		public ActorTableData actorTableData;
		public float sumWeight;
	}
	List<RandomGachaActorInfo> _listRandomGachaActorInfo = null;
	public string GetGachaCharacterId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			return "";

		if (_listRandomGachaActorInfo == null)
			_listRandomGachaActorInfo = new List<RandomGachaActorInfo>();
		_listRandomGachaActorInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.actorTable.dataArray[i].charGachaWeight;
			if (weight <= 0.0f)
				continue;

			// 초기 필수캐릭 얻었는지 체크 후 얻었다면 원래대로 진행
			if (IsCompleteFixedCharacterGroup())
			{
				// 획득가능한지 물어봐야한다.
				if (GetableOrigin(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
					continue;
			}
			else
			{
				// 얻지 못했다면 필수 캐릭터 리스트인지 확인해서 이 캐릭들만 후보 리스트에 넣어야한다.
				bool getable = false;
				if (IsFixedCharacterIncompleteGroup(TableDataManager.instance.actorTable.dataArray[i].actorId))
					getable = true;
				// FixedCharTable 검증
				if (TableDataManager.instance.actorTable.dataArray[i].grade > 0)
					CheatingListener.OnDetectCheatTable();
				// 필수캐릭이 아니더라도 이미 인벤에 들어있는 캐릭터라면(ganfaul, keepseries) 초월재료를 얻을 수 있게 해줘야한다.
				if (getable == false && PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) && GetableOrigin(TableDataManager.instance.actorTable.dataArray[i].actorId))
					getable = true;
				if (getable == false)
					continue;
			}

			// charGachaWeight 검증
			if (CharacterData.IsUseLegendWeight(TableDataManager.instance.actorTable.dataArray[i]) && weight > 1.0f)
				CheatingListener.OnDetectCheatTable();

			sumWeight += weight;
			RandomGachaActorInfo newInfo = new RandomGachaActorInfo();
			newInfo.actorTableData = TableDataManager.instance.actorTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listRandomGachaActorInfo.Add(newInfo);
		}

		if (_listRandomGachaActorInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomGachaActorInfo[_listRandomGachaActorInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomGachaActorInfo.Count; ++i)
		{
			if (random <= _listRandomGachaActorInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		string result = _listRandomGachaActorInfo[index].actorTableData.actorId;
		_listRandomGachaActorInfo.Clear();
		_listDroppedActorId.Add(result);
		return result;
	}
	public int droppedNotStreakCharCount { get; set; }
	public int GetCurrentNotSteakCharCount()
	{
		// 임시변수를 만들어서 계산하다가 서버에서 리턴받을때 적용해보자
		return PlayerData.instance.notStreakCharCount + droppedNotStreakCharCount;
	}

	List<string> _listDroppedActorId = null;
	bool GetableOrigin(string actorId)
	{
		//if (actorId != "Actor0201")
		//	return false;

		if (_listDroppedActorId != null && _listDroppedActorId.Contains(actorId))
		{
			// 이번 드랍으로 결정된거면 두번 나오지는 않게 한다.
			return false;
		}

		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return true;

		if (characterData.needLimitBreak && characterData.limitBreakPoint <= characterData.limitBreakLevel)
			return true;

		return false;
	}

	#region Sub-Region Fixed Character Group
	bool IsCompleteFixedCharacterGroup()
	{
		for (int i = 0; i < TableDataManager.instance.fixedCharTable.dataArray.Length; ++i)
		{
			bool contains = false;
			for (int j = 0; j < TableDataManager.instance.fixedCharTable.dataArray[i].actorId.Length; ++j)
			{
				if (PlayerData.instance.ContainsActor(TableDataManager.instance.fixedCharTable.dataArray[i].actorId[j]))
				{
					contains = true;
					break;
				}
			}

			if (contains == false)
				return false;
		}
		return true;
	}

	bool IsFixedCharacterIncompleteGroup(string actorId)
	{
		for (int i = 0; i < TableDataManager.instance.fixedCharTable.dataArray.Length; ++i)
		{
			bool contains = false;
			for (int j = 0; j < TableDataManager.instance.fixedCharTable.dataArray[i].actorId.Length; ++j)
			{
				if (PlayerData.instance.ContainsActor(TableDataManager.instance.fixedCharTable.dataArray[i].actorId[j]))
				{
					contains = true;
					break;
				}
			}

			if (contains)
				continue;

			for (int j = 0; j < TableDataManager.instance.fixedCharTable.dataArray[i].actorId.Length; ++j)
			{
				if (TableDataManager.instance.fixedCharTable.dataArray[i].actorId[j] == actorId)
					return true;
			}
		}
		return false;
	}
	#endregion

	#endregion

	#region Gacha PowerPoint
	List<string> _listDroppedPowerPointId = null;
	float _maxPowerPointRate = 1.5f;
	public string GetGachaPowerPointId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			return "";

		float maxPp = 0.0f;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
			maxPp = Mathf.Max(maxPp, PlayerData.instance.listCharacterData[i].pp);

		float baseWeight = maxPp * _maxPowerPointRate;
		float sumWeight = 0.0f;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
		{
			string actorId = PlayerData.instance.listCharacterData[i].actorId;
			if (_listDroppedPowerPointId != null && _listDroppedPowerPointId.Contains(actorId))
			{
				// 이번 드랍으로 결정된거면 두번 나오지는 않게 한다.
				continue;
			}

			float weight = baseWeight - PlayerData.instance.listCharacterData[i].pp;
			sumWeight += weight;
			RandomGachaActorInfo newInfo = new RandomGachaActorInfo();
			newInfo.actorTableData = TableDataManager.instance.FindActorTableData(PlayerData.instance.listCharacterData[i].actorId);
			newInfo.sumWeight = sumWeight;
			_listRandomGachaActorInfo.Add(newInfo);
		}

		if (_listRandomGachaActorInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomGachaActorInfo[_listRandomGachaActorInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomGachaActorInfo.Count; ++i)
		{
			if (random <= _listRandomGachaActorInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		string result = _listRandomGachaActorInfo[index].actorTableData.actorId;
		_listRandomGachaActorInfo.Clear();
		_listDroppedPowerPointId.Add(result);
		return result;
	}
	#endregion

}