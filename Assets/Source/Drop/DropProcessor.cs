using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using ActorStatusDefine;

public class DropProcessor : MonoBehaviour
{
	public enum eDropType
	{
		None,
		Exp,
		Gold,
		LevelPack,
		Heart,
		Gacha,
		Ultimate,
		Seal,
	}

	#region Static Fuction
	static bool FloatRange(eDropType dropType)
	{
		switch (dropType)
		{
			case eDropType.Gold:
			case eDropType.Ultimate:
				return true;
		}
		return false;
	}

	public static void Drop(Transform rootTransform, string dropId, string addDropId, bool onAfterBattle)
	{
		//Debug.Log("dropId : " + dropId + " / addDropId : " + addDropId);

		Vector3 dropPosition = rootTransform.position;
		dropPosition.y = 0.0f;
		DropProcessor dropProcess = BattleInstanceManager.instance.GetCachedDropProcessor(dropPosition);
		dropProcess.onAfterBattle = onAfterBattle;

		if (!string.IsNullOrEmpty(dropId))
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(dropId);
			if (dropTableData != null)
				Drop(dropProcess, dropTableData);
		}

		bool invalid = false;
		if (addDropId == "9752476" && (ExperienceCanvas.instance == null || ExperienceCanvas.instance.gameObject == null || ExperienceCanvas.instance.gameObject.activeSelf == false))
			invalid = true;

		if (!string.IsNullOrEmpty(addDropId) && invalid == false)
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(addDropId);
			if (dropTableData != null)
				Drop(dropProcess, dropTableData);
		}

		// drop event
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace) == false && ContentsManager.IsDropChapterStage(StageManager.instance.playChapter, StageManager.instance.playStage))
			dropProcess.AdjustDrop();

		dropProcess.StartDrop();
	}

	static void Drop(DropProcessor dropProcessor, DropTableData dropTableData)
	{
		for (int i = 0; i < dropTableData.dropEnum.Length; ++i)
		{
			eDropType dropType = (eDropType)dropTableData.dropEnum[i];
			switch (dropType)
			{
				case eDropType.Gold:
				case eDropType.Gacha:
					ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(StageManager.instance.playChapter);
					CharacterData mainCharacterData = PlayerData.instance.GetCharacterData(PlayerData.instance.mainCharacterId);
					if (chapterTableData != null && mainCharacterData != null && mainCharacterData.powerLevel > chapterTableData.suggestedMaxPowerLevel)
						continue;
					break;
			}
			float probability = dropTableData.probability[i];
			switch (dropType)
			{
				case eDropType.Gacha:
					float itemDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.ItemDropRate);
					if (itemDropAdjust != 0.0f)
						probability *= (1.0f + itemDropAdjust);
					break;
				case eDropType.Heart:
					probability *= StageManager.instance.currentStageTableData.DropHeartAdjustment;
					float heartDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.HeartDropRate);
					if (heartDropAdjust != 0.0f)
						probability *= (1.0f + heartDropAdjust);
					break;
			}
			if (Random.value > probability)
				continue;

			float floatValue = 0.0f;
			int intValue = 0;
			string stringValue = "";
			if (FloatRange(dropType))
				floatValue = Random.Range(dropTableData.minValue[i], dropTableData.maxValue[i]);
			else
			{
				int minValue = Mathf.RoundToInt(dropTableData.minValue[i]);
				int maxValue = Mathf.RoundToInt(dropTableData.maxValue[i]);
				intValue = Random.Range(minValue, maxValue + 1);

				// subValue 확인
				if (dropType == eDropType.Gacha && dropTableData.subValue[i] == "e")
					stringValue = GetStageDropEquipId();
			}

			switch (dropType)
			{
				case eDropType.Exp:
					DropExp(intValue);
					break;
				case eDropType.Gold:
					float goldDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.GoldDropAmount);
					if (goldDropAdjust != 0.0f)
						floatValue = floatValue * (1.0f + goldDropAdjust);
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
				case eDropType.LevelPack:
					++DropManager.instance.reservedLevelPackCount;
					// check no hit levelpack
					if (BattleManager.instance.GetDamageCountOnStage() == 0)
						floatValue = 1.0f;
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
				case eDropType.Heart:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
				case eDropType.Gacha:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
				case eDropType.Ultimate:
					DropSp(floatValue);
					break;
				case eDropType.Seal:
					if (PlayerData.instance.sharedDailyBoxOpened)
						break;
					if (PlayerData.instance.highestPlayChapter != PlayerData.instance.selectedChapter)
						break;
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					break;
			}
		}
	}

	public static void DropSp(float dropSpValue)
	{
		if (dropSpValue == 0.0f)
			return;

		PlayerActor playerActor = BattleInstanceManager.instance.playerActor;
		if (ExperienceCanvas.instance != null && ExperienceCanvas.instance.gameObject.activeSelf)
			playerActor = CharacterListCanvas.instance.selectedPlayerActor;
		float spGainAddRate = playerActor.actorStatus.GetValue(eActorStatus.SpGainAddRate);
		if (spGainAddRate != 0.0f) dropSpValue *= (1.0f + spGainAddRate);
		playerActor.actorStatus.AddSP(dropSpValue);
	}

	static void DropExp(int dropExpValue)
	{
		if (dropExpValue == 0)
			return;

		//Debug.Log("dropExp : " + dropExpValue);

		DropManager.instance.StackDropExp(dropExpValue);
	}
	#endregion

	class DropObjectInfo
	{
		public eDropType dropType;
		public float floatValue;
		public int intValue;
		public string stringValue;
	}
	List<DropObjectInfo> _listDropObjectInfo = new List<DropObjectInfo>();

    public void Add(eDropType dropType, float floatValue, int intValue, string stringValue)
	{
		DropObjectInfo newInfo = null;
		switch (dropType)
		{
			case eDropType.Exp:
				break;
			case eDropType.Gold:
				int randomCount = Random.Range(4, 7);
				float goldDropAdjust = DropAdjustAffector.GetValue(BattleInstanceManager.instance.playerActor.affectorProcessor, DropAdjustAffector.eDropAdjustType.GoldDropAmount);
				if (goldDropAdjust > 0.0f) randomCount += 1;
				for (int i = 0; i < randomCount; ++i)
				{
					newInfo = new DropObjectInfo();
					newInfo.dropType = dropType;
					newInfo.floatValue = floatValue / randomCount;
					_listDropObjectInfo.Add(newInfo);
				}
				break;
			case eDropType.LevelPack:
			case eDropType.Heart:
			case eDropType.Gacha:
			case eDropType.Seal:
				for (int i = 0; i < intValue; ++i)
				{
					newInfo = new DropObjectInfo();
					newInfo.dropType = dropType;
					if (newInfo.dropType == eDropType.LevelPack) newInfo.floatValue = floatValue;
					newInfo.intValue = 1;
					newInfo.stringValue = stringValue;
					_listDropObjectInfo.Add(newInfo);
				}
				if (dropType == eDropType.Gacha)
				{
					// 가차인 경우 사용할 오브젝트를 미리 로드 걸어둔다.
					// 앞에 골드들 떨어지는 동안 최대한 로딩이 끝나길 기대.
					EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(stringValue);
					if (equipTableData != null)
						AddressableAssetLoadManager.GetAddressableGameObject(equipTableData.prefabAddress, "Equip", null);
				}
				break;
			case eDropType.Ultimate:
				break;
		}
	}

	public void StartDrop()
	{
		if (_listDropObjectInfo.Count == 0)
		{
			gameObject.SetActive(false);
			return;
		}

		Timing.RunCoroutine(DropProcess());
	}

	IEnumerator<float> DropProcess()
	{
		DropObject dropObjectForException = null;
		for (int i = 0; i < _listDropObjectInfo.Count; ++i)
		{
			string prefabName = string.Format("Drop{0}", _listDropObjectInfo[i].dropType.ToString());
			if (_listDropObjectInfo[i].dropType == eDropType.LevelPack && _listDropObjectInfo[i].floatValue > 0.0f)
				prefabName = string.Format("NoHit{0}", prefabName);

			GameObject dropObjectPrefab = GetDropObjectPrefab(prefabName);
			if (dropObjectPrefab == null)
				continue;

			DropObject cachedItem = BattleInstanceManager.instance.GetCachedDropObject(dropObjectPrefab, GetRandomDropPosition(), Quaternion.identity);
			cachedItem.Initialize(_listDropObjectInfo[i].dropType, _listDropObjectInfo[i].floatValue, _listDropObjectInfo[i].intValue, _listDropObjectInfo[i].stringValue, onAfterBattle);

			// 여러개의 드랍프로세서가 서로 다른 드랍오브젝트를 만들고 있을때는 누가 마지막 골드 드랍인지를 알수가 없게된다.
			// 그래서 생성시 라스트를 등록하고 있다가
			if (cachedItem.getAfterAllDropAnimationInStage && cachedItem.useIncreaseSearchRange == false)
				DropManager.instance.ReserveLastDropObject(cachedItem);

			// 저 아래 템을 못얻는 상황에 대한 예외처리를 위한 코드다.
			if (cachedItem.getAfterAllDropAnimationInStage && cachedItem.useIncreaseSearchRange)
				dropObjectForException = cachedItem;

			// 마지막 스폰이 끝날땐 드랍프로세서가 바로 사라지게 yield return 하지 않는다.
			if (i < _listDropObjectInfo.Count - 1)
				yield return Timing.WaitForSeconds(0.2f);

			if (this == null)
				yield break;
		}

		// 스테이지 내의 마지막 드랍 프로세서가 종료될때
		// 마지막으로 등록된 드랍 오브젝트를 진짜 라스트 드랍으로 지정하기로 한다.
		// 그럼 이게 발동될때 떨어져있던 골드나 레벨팩이 흡수될거다.
		if (BattleInstanceManager.instance.IsLastDropProcessorInStage(this))
		{
			if (DropManager.instance.IsExistReservedLastDropObject())
				DropManager.instance.ApplyLastDropObject();
			else
			{
				// 템을 못얻는 상황에서는 골드가 드랍되지 않기 때문에 _reservedLastDropObject값이 null인 상태일거다.
				// 이럴땐 어쩔 수 없이 마지막 드랍 프로세서의 드랍 오브젝트 리스트의 역 순서대로 검사해서
				// getAfterAllDropAnimationInStage 값이 true인 오브젝트를 강제로 LastDropObject로 설정해야한다.
				if (dropObjectForException != null)
				{
					DropManager.instance.ReserveLastDropObject(dropObjectForException);
					DropManager.instance.ApplyLastDropObject();
				}
			}
		}

		_listDropObjectInfo.Clear();
		gameObject.SetActive(false);
	}

	// temp code
	GameObject GetDropObjectPrefab(string prefabName)
	{
		for (int i = 0; i < BattleManager.instance.dropObjectPrefabList.Length; ++i)
		{
			if (BattleManager.instance.dropObjectPrefabList[i].name == prefabName)
				return BattleManager.instance.dropObjectPrefabList[i];
		}
		return null;
	}

	Vector3 GetRandomDropPosition()
	{
		Vector2 randomOffset = Random.insideUnitCircle * Random.value * 2.0f;
		return cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
	}

	// 마지막 몹을 잡을때 기존몹의 드랍 프로세서가 진행중일 수 있다.
	// 이땐 afterBattle을 전달받아서 기록해두고 전달한다.
	public bool onAfterBattle { get; set; }




	#region Drop Event
	public void AdjustDrop()
	{
		// 최초로 2챕터 10스테이지를 깰때 강제 드랍처리를 해준다.
		// 먼저 리스트 내에 드랍할 장비가 있는지 판단.
		int gachaCount = 0;
		int firstGachaIndex = -1;
		for (int i = 0; i < _listDropObjectInfo.Count; ++i)
		{
			if (_listDropObjectInfo[i].dropType == eDropType.Gacha)
			{
				if (firstGachaIndex == -1)
					firstGachaIndex = i;
				++gachaCount;
			}
		}
		if (gachaCount == 0)
			Add(eDropType.Gacha, 0.0f, 1, "Equip0001");
		else if (gachaCount > 1)
		{
			// 최초 드랍이니 한개만 드랍되게 마지막걸 지운다.
			for (int i = _listDropObjectInfo.Count - 1; i >= 0; --i)
			{
				if (_listDropObjectInfo[i].dropType == eDropType.Gacha)
				{
					if (firstGachaIndex != i)
						_listDropObjectInfo.RemoveAt(i);
				}
			}
		}
	}
	#endregion



	#region Stage Drop Equip
	public class RandomDropEquipInfo
	{
		public EquipTableData equipTableData;
		public float sumWeight;
	}
	static List<RandomDropEquipInfo> _listRandomDropEquipInfo = null;
	static int _lastDropChapter = -1;
	static int _lastDropStage = -1;
	static int _lastLegendKey = -1;
	static string GetStageDropEquipId()
	{
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
		float result = Random.Range(0.0f, _listRandomDropEquipInfo[_listRandomDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropEquipInfo.Count; ++i)
		{
			if (result <= _listRandomDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";

		// 바로 감소시켜놔야 다음번 드랍될때 _lastLegendKey가 달라지면서 드랍 리스트를 리프레쉬 하게 된다.
		if (EquipData.IsUseLegendKey(_listRandomDropEquipInfo[index].equipTableData))
			++DropManager.instance.droppedLengendItemCount;
		return _listRandomDropEquipInfo[index].equipTableData.equipId;
	}

	static int GetRemainLegendKey()
	{
		return CurrencyData.instance.legendKey - DropManager.instance.droppedLengendItemCount * 10;
	}
	#endregion




	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
