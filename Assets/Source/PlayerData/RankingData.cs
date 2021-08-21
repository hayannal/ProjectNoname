using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class RankingData : MonoBehaviour
{
	public static RankingData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("RankingData")).AddComponent<RankingData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static RankingData _instance = null;

	public bool disableRanking { get; set; }

	public class RankingStageAddInfo
	{
		public string disNa;
		public int val;
	}
	List<RankingStageAddInfo> _listRankingStageAddInfo;
	List<string> _listRankingStageDelInfo;

	public class DisplayStageRankingInfo
	{
		public string playFabId;
		public string displayName;
		public int value;
		public int ranking;
		public int orderIndex;
	}
	List<DisplayStageRankingInfo> _listDisplayStageRankingInfo = new List<DisplayStageRankingInfo>();
	public List<DisplayStageRankingInfo> listDisplayStageRankingInfo { get { return _listDisplayStageRankingInfo; } }

	public DateTime rankingRefreshTime { get; private set; }

	void Update()
	{
		UpdateRefreshRankingData();
	}

	public void OnRecvRankingData(Dictionary<string, string> titleData)
	{
		_listRankingStageAddInfo = null;
		_listRankingStageDelInfo = null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		if (titleData.ContainsKey("rnkSt"))
			_listRankingStageAddInfo = serializer.DeserializeObject<List<RankingStageAddInfo>>(titleData["rnkSt"]);

		if (titleData.ContainsKey("rnkBan"))
			_listRankingStageDelInfo = serializer.DeserializeObject<List<string>>(titleData["rnkBan"]);

		if (titleData.ContainsKey("useRank") && string.IsNullOrEmpty(titleData["useRank"]) == false)
			disableRanking = (titleData["useRank"] == "0");
	}

	bool _lateInitialized = false;
	public void LateInitialize()
	{
		_lateInitialized = true;
		rankingRefreshTime = ServerTime.UtcNow;
	}

	void UpdateRefreshRankingData()
	{
		if (_lateInitialized == false)
			return;
		if (DateTime.Compare(ServerTime.UtcNow, rankingRefreshTime) < 0)
			return;

		rankingRefreshTime = ServerTime.UtcNow + TimeSpan.FromMinutes(5);

		// WaitNetwork 없이 패킷 보내서 응답이 오면 갱신해둔다.
		PlayFabApiManager.instance.RequestGetRanking((rankLeaderboard, cheatLeaderboard) =>
		{
			RecreateRankingData(rankLeaderboard, cheatLeaderboard);
		});
	}

	void RecreateRankingData(List<PlayerLeaderboardEntry> listPlayerLeaderboardEntry, List<PlayerLeaderboardEntry> listCheatRankSusEntry)
	{
		// 100개씩 받을 수 있기 때문에 0 ~ 99 그룹과 100 ~ 199 그룹으로 받아서 합쳐놨을텐데 어느거가 앞에 있을진 모르니 리스트 구축하면서 찾아볼 것.
		_listDisplayStageRankingInfo.Clear();

		// 얼마나 빠질지 추가될지 모르니 우선 다 넣고 정렬 돌리는게 맞는거 같다.
		for (int i = 0; i < listPlayerLeaderboardEntry.Count; ++i)
		{
			if (_listRankingStageDelInfo.Contains(listPlayerLeaderboardEntry[i].PlayFabId))
				continue;

			bool cheatRankSus = false;
			if (listCheatRankSusEntry != null)
			{
				for (int j = 0; j < listCheatRankSusEntry.Count; ++j)
				{
					if (listCheatRankSusEntry[j].StatValue > 0 && listCheatRankSusEntry[j].PlayFabId == listPlayerLeaderboardEntry[i].PlayFabId)
					{
						cheatRankSus = true;
						break;
					}
				}
			}
			if (cheatRankSus)
				continue;

			DisplayStageRankingInfo info = new DisplayStageRankingInfo();
			info.playFabId = listPlayerLeaderboardEntry[i].PlayFabId;
			info.displayName = listPlayerLeaderboardEntry[i].Profile.DisplayName;
			if (string.IsNullOrEmpty(info.displayName)) info.displayName = string.Format("Nameless_{0}", info.playFabId.Substring(0, 5));
			info.value = listPlayerLeaderboardEntry[i].StatValue;
			info.orderIndex = i;
			_listDisplayStageRankingInfo.Add(info);
		}

		// 가짜 리스트도 추가한다.
		if (_listRankingStageAddInfo != null)
		{
			for (int i = 0; i < _listRankingStageAddInfo.Count; ++i)
			{
				DisplayStageRankingInfo info = new DisplayStageRankingInfo();
				info.playFabId = "addData";
				info.displayName = _listRankingStageAddInfo[i].disNa;
				info.value = _listRankingStageAddInfo[i].val;
				info.orderIndex = 1000 + i;
				_listDisplayStageRankingInfo.Add(info);
			}
		}

		// 정렬
		_listDisplayStageRankingInfo.Sort(delegate (DisplayStageRankingInfo x, DisplayStageRankingInfo y)
		{
			if (x.value > y.value) return -1;
			else if (x.value < y.value) return 1;
			if (x.orderIndex < y.orderIndex) return -1;
			else if (x.orderIndex > y.orderIndex) return 1;
			return 0;
		});

		if (_listDisplayStageRankingInfo.Count > 100)
			_listDisplayStageRankingInfo.RemoveRange(100, _listDisplayStageRankingInfo.Count - 100);

		// ranking을 매겨둔다.
		int ranking = 1;
		int duplicateCount = 0;
		int lastValue = 0;
		for (int i = 0; i < _listDisplayStageRankingInfo.Count; ++i)
		{
			if (i == 0)
			{
				_listDisplayStageRankingInfo[i].ranking = ranking;
				lastValue = _listDisplayStageRankingInfo[i].value;
				continue;
			}

			if (lastValue == _listDisplayStageRankingInfo[i].value)
			{
				_listDisplayStageRankingInfo[i].ranking = ranking;
				++duplicateCount;
			}
			else
			{
				ranking = ranking + duplicateCount + 1;
				_listDisplayStageRankingInfo[i].ranking = ranking;
				duplicateCount = 0;
				lastValue = _listDisplayStageRankingInfo[i].value;
			}
		}
	}
}