using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;

public class MercenaryData : MonoBehaviour
{
	public static MercenaryData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("MercenaryData")).AddComponent<MercenaryData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static MercenaryData _instance = null;


	public class MercenarySlotInfo
	{
		public int dy;							// day
		public int sl;							// slotId
		public string id;						// actor
		public Dictionary<string, int> stats;	// stats
	}
	List<MercenarySlotInfo> _listMercenarySlotInfo;

	// 일일 갱신시간. 전투중엔 갱신되지 않는다.
	public DateTime characterDataRefreshTime { get; private set; }



	public static bool IsMercenaryActor(string actorId)
	{
		if (string.IsNullOrEmpty(actorId))
			return false;
		return (actorId[actorId.Length - 1] == 'm');
	}

	public static string ToMercenaryActorId(string actorId)
	{
		return string.Format("{0}m", actorId);
	}

	public static bool IsUsableMercenary()
	{
		if (BattleManager.instance != null && BattleManager.instance.IsDefaultBattle() && PlayerData.instance.currentChaosMode && MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false)
			return true;
		return false;
	}

	List<CharacterData> _listCharacterData = new List<CharacterData>();
	public List<CharacterData> listCharacterData { get { return _listCharacterData; } }

	public CharacterData GetCharacterData(string actorId, bool needConvert = false)
	{
		// PlayerActor 단에서 호출될때는 프리팹에 적혀있는 actorId를 사용하기 때문에 actorId에 m이 안붙어있다.
		// 이럴때를 대비해서 
		if (needConvert && IsMercenaryActor(actorId) == false)
			actorId = ToMercenaryActorId(actorId);

		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
				return _listCharacterData[i];
		}
		return null;
	}

	void Update()
	{
		UpdateRefreshCharacterDataList();
	}

	public void OnRecvMercenaryData(Dictionary<string, string> titleData, bool refreshCharacterData)
	{
		_listMercenarySlotInfo = null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		if (titleData.ContainsKey("mcLst"))
			_listMercenarySlotInfo = serializer.DeserializeObject<List<MercenarySlotInfo>>(titleData["mcLst"]);

		characterDataRefreshTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);

		if (refreshCharacterData == false)
			return;
		RefreshCharacterDataList();
	}

	void RefreshCharacterDataList()
	{
		_listCharacterData.Clear();

		if (_listMercenarySlotInfo == null)
			return;

		// powerLevel과 pp는 최고레벨 캐릭터꺼를 가져와야하므로 PlayerData.instance.listCharacterData를 돌려서 찾아야한다.
		int highestPowerLevel = 0;
		int highestPp = 0;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
		{
			if (highestPowerLevel < PlayerData.instance.listCharacterData[i].powerLevel)
			{
				highestPowerLevel = PlayerData.instance.listCharacterData[i].powerLevel;
				highestPp = PlayerData.instance.listCharacterData[i].pp;
			}
			else if (highestPowerLevel == PlayerData.instance.listCharacterData[i].powerLevel && highestPp < PlayerData.instance.listCharacterData[i].pp)
			{
				highestPowerLevel = PlayerData.instance.listCharacterData[i].powerLevel;
				highestPp = PlayerData.instance.listCharacterData[i].pp;
			}
		}

		for (int i = 0; i < _listMercenarySlotInfo.Count; ++i)
		{
			if (_listMercenarySlotInfo[i].dy != ServerTime.UtcNow.Day)
				continue;

			CharacterData newCharacterData = new CharacterData();
			newCharacterData.actorId = _listMercenarySlotInfo[i].id;
			newCharacterData.entityKey = new PlayFab.DataModels.EntityKey { Id = "none", Type = "character" };

			// override
			if (_listMercenarySlotInfo[i].stats.ContainsKey("pow") == false)
				_listMercenarySlotInfo[i].stats.Add("pow", highestPowerLevel);
			else
				_listMercenarySlotInfo[i].stats["pow"] = highestPowerLevel;

			if (_listMercenarySlotInfo[i].stats.ContainsKey("pp") == false)
				_listMercenarySlotInfo[i].stats.Add("pp", highestPp);
			else
				_listMercenarySlotInfo[i].stats["pp"] = highestPp;

			newCharacterData.Initialize(_listMercenarySlotInfo[i].stats, null);
			_listCharacterData.Add(newCharacterData);
		}
	}

	void UpdateRefreshCharacterDataList()
	{
		if (DateTime.Compare(ServerTime.UtcNow, characterDataRefreshTime) < 0)
			return;

		// 무조건 갱신해야한다.
		characterDataRefreshTime += TimeSpan.FromDays(1);

		RefreshCharacterDataList();
	}
}