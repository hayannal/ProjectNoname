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

	//void Update()
	//{
	//	UpdateRefreshCharacterDataList();
	//}

	public void OnRecvMercenaryData(Dictionary<string, string> titleData, bool refreshCharacterData)
	{
		_listMercenarySlotInfo = null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		if (titleData.ContainsKey("mcLst"))
			_listMercenarySlotInfo = serializer.DeserializeObject<List<MercenarySlotInfo>>(titleData["mcLst"]);

		//characterDataRefreshTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
		//if (refreshCharacterData == false)
		//	return;

		// 이렇게 로그인 시점에만 호출해두면 문제가 있는게 로그인 후 리스트 구축해둔담에 캐릭터들 레벨업 하면 그 레벨업이 반영되어야하기 때문에
		// 아예 들어가자마자 리스트 구축을 하지 않고 카오스 전투를 시작할때 구축하기로 한다.
		// 여기서 초기화 안하게되면서 UpdateRefreshCharacterDataList도 호출하지 않기로 한다.
		//RefreshCharacterDataList();

		// 대신 재로그인등으로 OnRecv호출될때에는 초기화하고 다시 만들기로 한다.
		_lastValue = "";
		_listCharacterData.Clear();
	}

	string _lastValue = "";
	public void RefreshCharacterDataList()
	{
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

		string newValue = string.Format("{0}_{1}_{2}", ServerTime.UtcNow.Day, highestPowerLevel, highestPp);
		if (newValue == _lastValue)
			return;

		_listCharacterData.Clear();

		if (_listMercenarySlotInfo == null)
			return;

		for (int i = 0; i < _listMercenarySlotInfo.Count; ++i)
		{
			if (_listMercenarySlotInfo[i].dy != ServerTime.UtcNow.Day)
				continue;
			if (string.IsNullOrEmpty(_listMercenarySlotInfo[i].id))
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

		// 매번 갱신할 필요는 없기때문에 highest정보와 날짜 정보를 조합해서 캐시값을 만들어두기로 한다.
		_lastValue = string.Format("{0}_{1}_{2}", ServerTime.UtcNow.Day, highestPowerLevel, highestPp);
	}

	void UpdateRefreshCharacterDataList()
	{
		// 다른 항목들과 달리 다음날 되었다고 바로 갱신하면 안되는 이유가 있다.
		// 카오스 전투중에서 지우면 캐릭터 정보가 날아가기 때문. 그러나 전투를 안하면 또 호출해도 되긴 하다.
		// 그래서 전투중에는 갱신로직을 돌리지 않다가 전투가 끝나면 호출하기로 한다.
		if (IsUsableMercenary())
			return;

		if (DateTime.Compare(ServerTime.UtcNow, characterDataRefreshTime) < 0)
			return;

		// 무조건 갱신해야한다.
		characterDataRefreshTime += TimeSpan.FromDays(1);

		RefreshCharacterDataList();
	}
}