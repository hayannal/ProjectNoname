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
		Diamond,
		Origin,
		PowerPoint,
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

	public static DropProcessor Drop(Transform rootTransform, string dropId, string addDropId, bool onAfterBattle, bool ignoreStartDrop)
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

		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
		{
			// drop event
			if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace) == false && ContentsManager.IsDropChapterStage(StageManager.instance.playChapter, StageManager.instance.playStage))
				dropProcess.AdjustDrop();
		}

		if (ignoreStartDrop == false)
			dropProcess.StartDrop();

		return dropProcess;
	}

	static void Drop(DropProcessor dropProcessor, DropTableData dropTableData)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);

		for (int i = 0; i < dropTableData.dropEnum.Length; ++i)
		{
			eDropType dropType = (eDropType)dropTableData.dropEnum[i];
			float probability = dropTableData.probability[i];

			// 보정처리 적용하는 것들.
			if (lobby)
			{
				// 드랍확률 보정처리.
				switch (dropType)
				{
					case eDropType.Origin:
						if (dropTableData.subValue[i] != "s")
							break;
						float weight = TableDataManager.instance.FindNotCharAdjustProb(DropManager.instance.GetCurrentNotSteakCharCount());
						// NotCharTable Adjust Prob 검증
						if (weight > 1.7f)
							CheatingListener.OnDetectCheatTable();
						probability *= weight;
						break;
					case eDropType.Gold:
					case eDropType.Diamond:
						if (dropTableData.dropId.Contains("Shop"))
							dropProcessor.AdjustDropDelay(0.05f);
						break;
				}
			}
			else
			{
				// suggested MaxPower Level 습득불가 처리
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
				if (dropType == eDropType.Gacha)
				{
					// 최대량을 넘지 못하게 처리
					if (TimeSpaceData.instance.inventoryItemCount + DropManager.instance.droppedStageItemCount >= TimeSpaceData.InventoryRealMax)
						continue;
				}
				// 드랍확률 보정처리.
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
			}
			if (Random.value > probability)
			{
				if (dropType == eDropType.Origin && dropTableData.subValue[i] == "s")
					++DropManager.instance.droppedNotStreakCharCount;
				continue;
			}

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
				if (dropType == eDropType.Gacha)
				{
					switch (dropTableData.subValue[i])
					{
						case "e": stringValue = DropManager.instance.GetStageDropEquipId(); break;
						case "g": stringValue = DropManager.instance.GetGachaEquipId(); break;
					}
				}
				else if (dropType == eDropType.Origin)
				{
					switch (dropTableData.subValue[i])
					{
						case "s": stringValue = DropManager.instance.GetGachaCharacterId(); break;
					}
					// Origin이나 아래 PowerPoint는 특정 조건에 의해(중복 방지라던지 등등) 안나올 수 있다. 이땐 건너뛰어야한다.
					if (stringValue == "")
						continue;
				}
				else if (dropType == eDropType.PowerPoint)
				{
					stringValue = DropManager.instance.GetGachaPowerPointId();
					if (stringValue == "")
						continue;
				}
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
					if (lobby) DropManager.instance.AddLobbyGold(floatValue);
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
					if (lobby) DropManager.instance.AddLobbyDropItemId(stringValue);
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
				case eDropType.Diamond:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddLobbyDia(intValue);
					break;
				case eDropType.Origin:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddOrigin(stringValue);
					break;
				case eDropType.PowerPoint:
					dropProcessor.Add(dropType, floatValue, intValue, stringValue);
					if (lobby) DropManager.instance.AddPowerPoint(stringValue, intValue);
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
				if (_adjustDropDelay > 0.0f) randomCount = Random.Range(20, 30);
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
			case eDropType.Origin:
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
			case eDropType.Diamond:
				int splitCount = Random.Range(1, 3);
				if (_adjustDropDelay > 0.0f) splitCount = Random.Range(12, 20);
				int quotient = intValue / splitCount;
				int remainder = intValue % splitCount;
				for (int i = 0; i < splitCount; ++i)
				{
					int currentCount = quotient + ((i < remainder) ? 1 : 0);
					newInfo = new DropObjectInfo();
					newInfo.dropType = dropType;
					newInfo.intValue = currentCount;
					newInfo.stringValue = stringValue;
					_listDropObjectInfo.Add(newInfo);
				}
				break;
			case eDropType.PowerPoint:
				newInfo = new DropObjectInfo();
				newInfo.dropType = dropType;
				newInfo.intValue = intValue;
				newInfo.stringValue = stringValue;
				_listDropObjectInfo.Add(newInfo);
				break;
		}
	}

	void OnDisable()
	{
		_adjustDropDelay = 0.0f;
		_adjustDropRange = 0.0f;
		_listDropObjectInfo.Clear();
	}

	public void StartDrop()
	{
		if (_listDropObjectInfo.Count == 0)
		{
			if (BattleInstanceManager.instance.IsLastDropProcessorInStage(this))
			{
				if (DropManager.instance.IsExistReservedLastDropObject())
					DropManager.instance.ApplyLastDropObject();
			}

			gameObject.SetActive(false);
			return;
		}

		Timing.RunCoroutine(DropProcess());
	}

	
	IEnumerator<float> DropProcess()
	{
		float delay = 0.2f;
		if (_adjustDropDelay > 0.0f)
			delay = _adjustDropDelay;

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
				yield return Timing.WaitForSeconds(delay);

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
		
		gameObject.SetActive(false);
	}

	// temp code
	GameObject GetDropObjectPrefab(string prefabName)
	{
		for (int i = 0; i < DropObjectGroup.instance.dropObjectPrefabList.Length; ++i)
		{
			if (DropObjectGroup.instance.dropObjectPrefabList[i].name == prefabName)
				return DropObjectGroup.instance.dropObjectPrefabList[i];
		}
		return null;
	}

	float _adjustDropRange = 0.0f;
	public void AdjustDropRange(float adjustRadius)
	{
		_adjustDropRange = adjustRadius;
	}

	Vector3 GetRandomDropPosition()
	{
		bool checkLocalPlayerPosition = false;
		float defaultRadius = 2.0f;
		if (_adjustDropRange > 0.0f)
		{
			defaultRadius = _adjustDropRange;
			checkLocalPlayerPosition = true;
		}

		Vector3 localPlayerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		int tryBreakCount = 0;
		while (true)
		{
			Vector2 randomOffset = Random.insideUnitCircle * Random.Range(0.2f, 1.0f) * defaultRadius;
			Vector3 desirePosition = cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);

			Vector3 diff = desirePosition - localPlayerPosition;
			if (checkLocalPlayerPosition == false || diff.x * diff.x + diff.z * diff.z > 1.5f)
				return desirePosition;

			++tryBreakCount;
			if (tryBreakCount > 200)
			{
				Debug.LogError("GetRandomDropPosition Error. Not found valid random position.");
				return desirePosition;
			}
		}
	}

	float _adjustDropDelay = 0.0f;
	public void AdjustDropDelay(float adjustDelay)
	{
		_adjustDropDelay = adjustDelay;
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
