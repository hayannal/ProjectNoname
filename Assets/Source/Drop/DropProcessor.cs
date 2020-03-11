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

		if (!string.IsNullOrEmpty(addDropId))
		{
			DropTableData dropTableData = TableDataManager.instance.FindDropTableData(addDropId);
			if (dropTableData != null)
				Drop(dropProcess, dropTableData);
		}

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
					if (chapterTableData != null && mainCharacterData.powerLevel > chapterTableData.suggestedMaxPowerLevel)
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
			if (FloatRange(dropType))
				floatValue = Random.Range(dropTableData.minValue[i], dropTableData.maxValue[i]);
			else
			{
				int minValue = Mathf.RoundToInt(dropTableData.minValue[i]);
				int maxValue = Mathf.RoundToInt(dropTableData.maxValue[i]);
				intValue = Random.Range(minValue, maxValue + 1);
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
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.LevelPack:
					++BattleManager.instance.reservedLevelPackCount;
					// check no hit levelpack
					if (BattleManager.instance.GetDamageCountOnStage() == 0)
						floatValue = 1.0f;
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.Heart:
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.Gacha:
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
				case eDropType.Ultimate:
					DropSp(floatValue);
					break;
				case eDropType.Seal:
					// if (PlayerData.instance.CheckDailyBoxOpened
					//	break;
					if (PlayerData.instance.highestPlayChapter != PlayerData.instance.selectedChapter)
						break;
					dropProcessor.Add(dropType, floatValue, intValue);
					break;
			}
		}
	}

	public static void DropSp(float dropSpValue)
	{
		if (dropSpValue == 0.0f)
			return;

		float spGainAddRate = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.SpGainAddRate);
		if (spGainAddRate != 0.0f) dropSpValue *= (1.0f + spGainAddRate);
		BattleInstanceManager.instance.playerActor.actorStatus.AddSP(dropSpValue);
	}

	static void DropExp(int dropExpValue)
	{
		if (dropExpValue == 0)
			return;

		//Debug.Log("dropExp : " + dropExpValue);

		BattleManager.instance.StackDropExp(dropExpValue);
	}
	#endregion

	class DropObjectInfo
	{
		public eDropType dropType;
		public float floatValue;
		public int intValue;
	}
	List<DropObjectInfo> _listDropObjectInfo = new List<DropObjectInfo>();

    public void Add(eDropType dropType, float floatValue, int intValue)
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
					_listDropObjectInfo.Add(newInfo);
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
		for (int i = 0; i < _listDropObjectInfo.Count; ++i)
		{
			string prefabName = string.Format("Drop{0}", _listDropObjectInfo[i].dropType.ToString());
			if (_listDropObjectInfo[i].dropType == eDropType.LevelPack && _listDropObjectInfo[i].floatValue > 0.0f)
				prefabName = string.Format("NoHit{0}", prefabName);

			GameObject dropObjectPrefab = GetDropObjectPrefab(prefabName);
			if (dropObjectPrefab == null)
				continue;

			DropObject cachedItem = BattleInstanceManager.instance.GetCachedDropObject(dropObjectPrefab, GetRandomDropPosition(), Quaternion.identity);
			cachedItem.Initialize(_listDropObjectInfo[i].dropType, _listDropObjectInfo[i].floatValue, _listDropObjectInfo[i].intValue, onAfterBattle);

			// 여러개의 드랍프로세서가 서로 다른 드랍오브젝트를 만들고 있을때는 누가 마지막 골드 드랍인지를 알수가 없게된다.
			// 그래서 생성시 라스트를 등록하고 있다가
			if (cachedItem.getAfterAllDropAnimationInStage && cachedItem.useIncreaseSearchRange == false)
				BattleInstanceManager.instance.ReserveLastDropObject(cachedItem);

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
			BattleInstanceManager.instance.ApplyLastDropObject();

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
