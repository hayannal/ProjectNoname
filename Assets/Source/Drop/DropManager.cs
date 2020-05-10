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

	public int droppedStageItemCount { get; set; }

	#region Legend Key
	// 전설키를 DropItem과 달리 따로 체크해야한다.
	// 위 DropItem은 습득하고 난 아이템 리스트를 관리하는건데
	// 전설키의 개수를 가지고 weight를 조정하는건 드랍되는 시점에서 바로 카운트에 반영되어야하는거라
	// DropItem에 들어있는 전설로 하게되면 틀어질 수 있다.(전설키가 1개 남은 상황에서 2개의 전설이 드랍될 수 있다.)
	//
	// 그래서 차라리 별도의 드랍 카운트 변수를 만들고
	// 드랍이 결정될때마다 증가시켜서 관리하기로 한다.
	// 초기화는 신경쓸필요 없는게 전투끝나고 돌아올때 어차피 DropManager가 삭제되고 새로 만들어지기 때문에 신경쓰지 않아도 된다.
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

		++droppedStageItemCount;

		// 바로 감소시켜놔야 다음번 드랍될때 _lastLegendKey가 달라지면서 드랍 리스트를 리프레쉬 하게 된다.
		// 인게임에서만 적용되는 수치로 장비뽑기할때는 적용받지 않는다.
		if (EquipData.IsUseLegendKey(_listRandomDropEquipInfo[index].equipTableData))
			++droppedLengendItemCount;
		return _listRandomDropEquipInfo[index].equipTableData.equipId;
	}

	int GetRemainLegendKey()
	{
		return CurrencyData.instance.legendKey - droppedLengendItemCount * 10;
	}
	#endregion



	
	/////////////////////////////////////////////////////////////////////////////////////////////////
	// 이 아래서부터는 Lobby에서 사용하는 가차용 뽑기 로직들이다.
	// 한번 드랍프로세서가 동작하고 나서는 패킷 주고받은 후 초기화를 해줘야한다.
	public void ClearLobbyDropInfo()
	{
		// 3연차 8연차 등등 하나의 연속가차 안에서 썼던 정보들이다.
		// 연차 중에는 이왕이면 pp를 각각 나눠서 뽑는다. 캐릭터는 동시에 같은 캐릭터를 뽑을 수 없다. 연속으로 전설템을 못뽑으면 확률이 증가한다. 등등의 조건을 처리하기 위해 사용하는 임시 변수들이다.
		_droppedNotStreakItemCount = 0;
		droppedNotStreakCharCount = 0;
		_listDroppedActorId.Clear();
		_listDroppedPowerPointId.Clear();

		// 위와 별개로 패킷으로 보낼때 쓴 정보도 초기화 해줘야한다.
		ClearLobbyDropPacketInfo();
	}

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
	public int GetCurrentNotSteakCount()
	{
		// 임시변수를 만들어서 계산하다가 서버에서 리턴받을때 적용해보자
		return PlayerData.instance.notStreakCount + _droppedNotStreakItemCount;
	}

	// not streak에 영향주지 않는 전설 뽑기다.
	List<RandomDropEquipInfo> _listRandomDropLegendEquipInfo = null;
	public string GetGachaLegendEquipId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			return "";

		if (_listRandomDropLegendEquipInfo == null)
		{
			_listRandomDropLegendEquipInfo = new List<RandomDropEquipInfo>();
			_listRandomDropLegendEquipInfo.Clear();

			float sumWeight = 0.0f;
			for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
			{
				float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
				if (weight <= 0.0f)
					continue;

				// 전설뽑기니 나머지 제외
				if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]) == false)
					continue;

				// equipGachaWeight 검증
				if (weight > 1.0f)
					CheatingListener.OnDetectCheatTable();

				sumWeight += weight;
				RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
				newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
				newInfo.sumWeight = sumWeight;
				_listRandomDropLegendEquipInfo.Add(newInfo);
			}
		}

		if (_listRandomDropLegendEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomDropLegendEquipInfo[_listRandomDropLegendEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropLegendEquipInfo.Count; ++i)
		{
			if (random <= _listRandomDropLegendEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listRandomDropLegendEquipInfo[index].equipTableData.equipId;
	}
	#endregion



	#region Gacha Character
	class RandomGachaActorInfo
	{
		public string actorId;
		public float sumWeight;
	}
	List<RandomGachaActorInfo> _listRandomGachaActorInfo = null;
	public string GetGachaCharacterId(bool onlyHeroicCharacter = false)
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

			if (onlyHeroicCharacter)
			{
				if (TableDataManager.instance.actorTable.dataArray[i].grade != 1)
					continue;
				if (PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
					continue;
			}

			// 초기 필수캐릭 얻었는지 체크 후 얻었다면 원래대로 진행
			if (IsCompleteFixedCharacterGroup() || onlyHeroicCharacter)
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
				if (getable && TableDataManager.instance.actorTable.dataArray[i].grade > 0)
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
			newInfo.actorId = TableDataManager.instance.actorTable.dataArray[i].actorId;
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
		string result = _listRandomGachaActorInfo[index].actorId;
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

	List<string> _listDroppedActorId = new List<string>();
	public bool GetableOrigin(string actorId)
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
	List<string> _listDroppedPowerPointId = new List<string>();
	const float _maxPowerPointRate = 1.5f;
	public string GetGachaPowerPointId(int grade = -1)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			return "";

		if (_listRandomGachaActorInfo == null)
			_listRandomGachaActorInfo = new List<RandomGachaActorInfo>();
		_listRandomGachaActorInfo.Clear();

		float maxPp = 0.0f;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
			maxPp = Mathf.Max(maxPp, PlayerData.instance.listCharacterData[i].pp);

		float baseWeight = maxPp * _maxPowerPointRate;
		float sumWeight = 0.0f;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
		{
			string actorId = PlayerData.instance.listCharacterData[i].actorId;
			// 원래는 중복해서 나오면 안되지만 캐릭터가 2개밖에 없는 상황에서 5개를 뽑아야한다면 중복을 허용한다.
			if (_listDroppedPowerPointId != null && PlayerData.instance.listCharacterData.Count == _listDroppedPowerPointId.Count)
				_listDroppedPowerPointId.Clear();
			if (_listDroppedPowerPointId != null && _listDroppedPowerPointId.Contains(actorId))
			{
				// 이번 드랍으로 결정된거면 두번 나오지는 않게 한다.
				continue;
			}

			if (grade == 0 || grade == 1)
			{
				ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
				if (actorTableData.grade != grade)
					continue;
			}

			float weight = baseWeight - PlayerData.instance.listCharacterData[i].pp;
			sumWeight += weight;
			RandomGachaActorInfo newInfo = new RandomGachaActorInfo();
			newInfo.actorId = PlayerData.instance.listCharacterData[i].actorId;
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
		string result = _listRandomGachaActorInfo[index].actorId;
		_listRandomGachaActorInfo.Clear();
		_listDroppedPowerPointId.Add(result);
		return result;
	}
	#endregion




	#region Lobby Drop Packet
	// 로비 가차 드랍은 따로 모아놔야 패킷 만들때 편하다.
	// 이 아래부터는 패킷 정보들이다.
	// 패킷 보낼때만 잠시 쓰는거라 Obscured안해놔도 될텐데 그냥 해둔다.
	// Drop과 동시에 계산되서 여기에 다 쌓여있게 되니 바로 서버로 보내면 된다.
	ObscuredInt _lobbyDia = 0;
	ObscuredFloat _lobbyGold = 0.0f;

	void ClearLobbyDropPacketInfo()
	{
		_lobbyDia = 0;
		_lobbyGold = 0.0f;
		_listCharacterPpRequest.Clear();
		_listGrantCharacterRequest.Clear();		
		_listCharacterLbpRequest.Clear();
		_listEquipIdRequest.Clear();
	}

	public void AddLobbyDia(int dia)
	{
		_lobbyDia += dia;
	}
	public int GetLobbyDiaAmount()
	{
		return _lobbyDia;
	}

	public void AddLobbyGold(float gold)
	{
		_lobbyGold += gold;
	}
	public int GetLobbyGoldAmount()
	{
		float lobbyGold = _lobbyGold;
		return (int)lobbyGold;
	}

	public class CharacterPpRequest
	{
		public string ChrId;
		public int pp;
		public int add;

		[System.NonSerialized]
		public string actorId;
	}
	List<CharacterPpRequest> _listCharacterPpRequest = new List<CharacterPpRequest>();
	public void AddPowerPoint(string actorId, int amount)
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		// 캐릭터가 둘밖에 없을때 5인분의 pp를 뽑으려면 같은 캐릭터에 두어개씩 들어오게 된다. 합산해줘야한다.
		bool find = false;
		for (int i = 0; i < _listCharacterPpRequest.Count; ++i)
		{
			if (_listCharacterPpRequest[i].ChrId == characterData.entityKey.Id)
			{
				_listCharacterPpRequest[i].pp += amount;
				_listCharacterPpRequest[i].add += amount;
				find = true;
				break;
			}
		}

		if (find == false)
		{
			// pp는 Add 대신 Set을 쓸거기 때문에 처음 찾아질때 기존의 값에 더하는 형태로 기록해둔다.
			CharacterPpRequest newInfo = new CharacterPpRequest();
			newInfo.actorId = characterData.actorId;
			newInfo.ChrId = characterData.entityKey.Id;
			newInfo.pp = characterData.pp + amount;
			newInfo.add = amount;
			_listCharacterPpRequest.Add(newInfo);
		}
	}
	public List<CharacterPpRequest> GetPowerPointInfo()
	{
		return _listCharacterPpRequest;
	}

	List<string> _listGrantCharacterRequest = new List<string>();
	public class CharacterLbpRequest
	{
		public string ChrId;
		public int lbp;

		[System.NonSerialized]
		public string actorId;
	}
	List<CharacterLbpRequest> _listCharacterLbpRequest = new List<CharacterLbpRequest>();
	public void AddOrigin(string actorId)
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
		{
			if (_listGrantCharacterRequest.Contains(actorId) == false)
				_listGrantCharacterRequest.Add(actorId);
		}
		else
		{
			// 두개 이상 뽑힐리 없으니 기존값 구해와서 1 증가시키면 된다.
			CharacterLbpRequest newInfo = new CharacterLbpRequest();
			newInfo.actorId = characterData.actorId;
			newInfo.ChrId = characterData.entityKey.Id;
			newInfo.lbp = characterData.limitBreakPoint + 1;
			_listCharacterLbpRequest.Add(newInfo);
		}

		// 이 함수에 들어왔다는거 자체가 캐릭터를 뽑고있다는걸 의미하니 연출 끝나고 나올 결과창에서 보여줄 캐릭터를 미리 로딩해두기로 한다.
		// 장비 아이콘의 경우엔 크기가 작기도 하고 그래서 패킷 받는 부분에서 했었는데
		// 캐릭터의 경우엔 보내기 전부터 하기로 한다.
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(actorId));
	}
	public List<string> GetGrantCharacterInfo()
	{
		return _listGrantCharacterRequest;
	}
	public List<CharacterLbpRequest> GetLimitBreakPointInfo()
	{
		return _listCharacterLbpRequest;
	}

	List<ObscuredString> _listEquipIdRequest = new List<ObscuredString>();
	public void AddLobbyDropItemId(string equipId)
	{
		_listEquipIdRequest.Add(equipId);
	}
	public List<ObscuredString> GetLobbyDropItemInfo()
	{
		return _listEquipIdRequest;
	}
	#endregion
}