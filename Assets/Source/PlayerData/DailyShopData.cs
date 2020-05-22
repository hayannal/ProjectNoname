using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class DailyShopData : MonoBehaviour
{
	public static DailyShopData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("DailyShopData")).AddComponent<DailyShopData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static DailyShopData _instance = null;

	public const int ShopSlotMax = 8;

	public class DailyShopSlotInfo
	{
		public int dy;		// day
		public int sl;		// slotId	원래는 id 형태로 하려고 했는데 결국 서버 저장때문에 index형태로도 쓴다. 0부터 4는 Minor항목. 5,6은 Major항목. 7,8은 Main항목이다.
		public string tp;	// type
		public string vl;	// value
		public int cn;		// count 
		public string pt;	// priceType
		public int pp;		// prevPrice
		public int pr;      // price

		public int day { get { return dy; } }
		public int slotId { get { return sl; } }
		public string type { get { return tp; } }
		public string value { get { return vl; } }
		public int count { get { return cn; } }
		public string priceType { get { return pt; } }
		public int prevPrice { get { return pp; } }
		public int price { get { return pr; } }
	}
	List<DailyShopSlotInfo> _listDailyShopSlotInfo;

	public class DailyFreeItemInfo
	{
		public int dy;
		public string cd;
		public int cn;
	}
	List<DailyFreeItemInfo> _listDailyFreeItemInfo;

	public ObscuredBool dailyFreeItemReceived { get; set; }
	public DateTime dailyFreeItemResetTime { get; private set; }

	// 상점 갱신시간. 구매와 상관없이 갱신에 사용한다.
	public DateTime dailyShopRefreshTime { get; private set; }

	// 구매여부 리스트. 5일짜리로 보이는 7번 8번 항목도 사실은 일일 구매 여부로 관리된다. 대신 같은 캐릭터로 연속되어있어서 한번 사면 보이지 않을뿐인거다.
	List<ObscuredBool> _listShopSlotPurchased;
	public DateTime dailyShopSlotPurchasedResetTime { get; private set; }

	void Update()
	{
		UpdateDailyShopRefreshTime();
		UpdateDailyShopSlotResetTime();
		UpdateDailyFreeItemResetTime();
	}

	void OnRecvShopData(Dictionary<string, string> titleData)
	{
		_listDailyShopSlotInfo = null;
		_listDailyFreeItemInfo = null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		// 일일상점 데이터
		if (titleData.ContainsKey("daShp"))
			_listDailyShopSlotInfo = serializer.DeserializeObject<List<DailyShopSlotInfo>>(titleData["daShp"]);

		// 일일 무료 아이템
		if (titleData.ContainsKey("daFre"))
			_listDailyFreeItemInfo = serializer.DeserializeObject<List<DailyFreeItemInfo>>(titleData["daFre"]);
	}

	public void OnRecvShopData(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		// PlayerData.ResetData 호출되면 다시 여기로 들어올테니 플래그들 초기화 시켜놓는다.
		_checkedUnfixedItemInfo = false;

		OnRecvShopData(titleData);

		#region Unfixed Item Info
		// 일일 상점에는 unfixed항목들이 있어서 그냥 받기만 해선 안된다.
		// 임의의 날짜에 최초 접속시 unfixed에 대한 클라 결과값을 올려두고 하루동안 쓰기로 한다.
		// 그런데 이건 구축할때 캐릭터 리스트가 있어야한단 점 때문에 아무때나 호출할 수는 없다.
		// 심지어 최초 계정 생성땐 캐릭터 리스트조차 날아오지 않기 때문에(+ 전투맵에서 시작하기 때문에) 이 상태에서 랜덤 굴렸다간 빈 값만 나올 뿐이다.
		// 그래서 차라리 멤버로 기억해두고 있다가 적절한 타이밍에 CheckUnfixedItemInfo 함수를 호출하기로 한다.
		if (userReadOnlyData.ContainsKey("lasUnfxDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasUnfxDat"].Value) == false)
				_lastUnfixedDateTimeString = userReadOnlyData["lasUnfxDat"].Value;
		}

		if (userReadOnlyData.ContainsKey("unfxInfo"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["unfxInfo"].Value) == false)
				_unfixedDataString = userReadOnlyData["unfxInfo"].Value;
		}
		// 적절한 타이밍은 바로 MainSceneBuilder의 LateInitialize다.
		//CheckUnfixedItemInfo();
		#endregion

		// 일일 무료 아이템 수령기록 데이터. 마지막 오픈 시간을 받는건 일퀘 때와 비슷한 구조다. 상점 슬롯과 별개로 처리된다.
		if (userReadOnlyData.ContainsKey("lasFreDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasFreDat"].Value) == false)
				OnRecvDailyFreeItemInfo(userReadOnlyData["lasFreDat"].Value);
		}

		// 일일 상점 항목별 구매기록 데이터. 각각의 여부는 따로 관리되며 리셋 타이밍은 하나를 공유해서 쓴다.
		if (_listShopSlotPurchased == null)
			_listShopSlotPurchased = new List<ObscuredBool>();
		_listShopSlotPurchased.Clear();
		for (int i = 0; i <= ShopSlotMax; ++i)
			_listShopSlotPurchased.Add(false);

		for (int i = 0; i <= ShopSlotMax; ++i)
		{
			string key = string.Format("lasShpDat{0}", i);
			if (userReadOnlyData.ContainsKey(key))
			{
				if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
					OnRecvDailyShopSlotInfo(userReadOnlyData[key].Value, i);
			}
		}

		// 구매기록은 정시에 갱신된다.
		dailyShopSlotPurchasedResetTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);

		// 그런데 하나 중요한건 일일 상점이기때문에 매일마다 새로 받아야한다는거다.
		// 이 시간이 되면 현재 데이터들을 무시하고 새로 받아야한다.
		// 특이한건 딱 날짜 넘어가는 타이밍에 받으면 잠깐 데이터가 틀어질 수 있기 때문에 5분전에 미리 받는거로 해둔다.
		// 이럼 다음날에 되자마자 바로 갱신할 수 있게된다.
		// 사실 당일 데이터를 바꿔놨다면 저 5분 사이에 다른템이 나올 수 있다는건데
		// 이런식으로 당일 데이터를 바꾸는 일은 없을테니까 할 수 있는 방식이다.
		dailyShopRefreshTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1) - TimeSpan.FromMinutes(5);

		// 그런데 만약 서버 리셋타임 5분도 안남기고 접속한거라면 괜히 또 받아질테니 리셋 타임과 비교해봐서 하루를 밀어둔다.
		if (DateTime.Compare(ServerTime.UtcNow, dailyShopRefreshTime) < 0)
		{
		}
		else
			dailyShopRefreshTime += TimeSpan.FromDays(1);
	}

	public DailyShopSlotInfo GetTodayShopData(int slotId)
	{
		return GetShopSlotData(ServerTime.UtcNow.Day, slotId);
	}

	public DailyShopSlotInfo GetShopSlotData(int day, int slotId)
	{
		if (_listDailyShopSlotInfo == null)
			return null;

		for (int i = 0; i < _listDailyShopSlotInfo.Count; ++i)
		{
			if (_listDailyShopSlotInfo[i].type == "")
				continue;
			if (_listDailyShopSlotInfo[i].dy == day && _listDailyShopSlotInfo[i].sl == slotId)
				return _listDailyShopSlotInfo[i];
		}
		return null;
	}

	public bool IsPurchasedTodayShopData(int slotId)
	{
		if (_listShopSlotPurchased == null)
			return false;

		// 현재 slotId는 아이디 겸 인덱스로 동시에 쓰고있다.
		if (slotId < _listShopSlotPurchased.Count)
			return _listShopSlotPurchased[slotId];
		return false;
	}

	void OnRecvDailyShopSlotInfo(DateTime lastDailyShopSlotPurchasedTime, int index)
	{
		//if (ServerTime.UtcNow < lastDailyShopSlotPurchasedTime)
		//{
		//	// 어떻게 미래로 설정되어있을 수가 있나. 이건 무효.
		//	_listShopSlotPurchased[index] = false;
		//	return;
		//}

		if (ServerTime.UtcNow.Year == lastDailyShopSlotPurchasedTime.Year && ServerTime.UtcNow.Month == lastDailyShopSlotPurchasedTime.Month && ServerTime.UtcNow.Day == lastDailyShopSlotPurchasedTime.Day)
			_listShopSlotPurchased[index] = true;
		else
			_listShopSlotPurchased[index] = false;
	}

	public void OnRecvDailyShopSlotInfo(string lastDailyShopSlotPurchasedTimeString, int index)
	{
		DateTime lastDailyShopSlotPurchasedTime = new DateTime();
		if (DateTime.TryParse(lastDailyShopSlotPurchasedTimeString, out lastDailyShopSlotPurchasedTime))
		{
			DateTime universalTime = lastDailyShopSlotPurchasedTime.ToUniversalTime();
			OnRecvDailyShopSlotInfo(universalTime, index);
		}
	}

	#region Unifxed Item Info
	string _lastUnfixedDateTimeString = "";
	string _unfixedDataString = "";
	// 클라 구동 후 상점 열기 전에 1회만 체크하면 된다.
	bool _checkedUnfixedItemInfo = false;
	public void CheckUnfixedItemInfo()
	{
		if (_checkedUnfixedItemInfo)
			return;
		if (ContentsManager.IsTutorialChapter())
			return;

		bool needRegister = false;
		if (_lastUnfixedDateTimeString == "")
			needRegister = true;
		if (needRegister == false)
		{
			DateTime lastUnfixedItemDateTime = new DateTime();
			if (DateTime.TryParse(_lastUnfixedDateTimeString, out lastUnfixedItemDateTime))
			{
				DateTime universalTime = lastUnfixedItemDateTime.ToUniversalTime();
				if (ServerTime.UtcNow.Year == universalTime.Year && ServerTime.UtcNow.Month == universalTime.Month && ServerTime.UtcNow.Day == universalTime.Day)
				{
					var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
					_dicUnfixedData = serializer.DeserializeObject<Dictionary<string, string>>(_unfixedDataString);
				}
				else
					needRegister = true;
			}
		}
		_checkedUnfixedItemInfo = true;

		if (needRegister == false)
			return;
		RegisterUnfixedInfo();
	}

	Dictionary<string, string> _dicUnfixedData;
	void RegisterUnfixedInfo()
	{
		if (_dicUnfixedData == null)
			_dicUnfixedData = new Dictionary<string, string>();
		_dicUnfixedData.Clear();

		int seed = 0;
		string actorId = "";
		for (int i = 0; i <= ShopSlotMax; ++i)
		{
			DailyShopSlotInfo info = GetTodayShopData(i);
			if (info == null)
				continue;

			bool unfixed = false;
			switch (info.type)
			{
				case "uch":
					int.TryParse(info.value, out seed);
					UnityEngine.Random.InitState(seed);
					actorId = DropManager.instance.GetGachaCharacterId(1);
					unfixed = true;
					break;
				case "upn":
					int.TryParse(info.value, out seed);
					UnityEngine.Random.InitState(seed);
					actorId = DropManager.instance.GetGachaPowerPointId(0);
					unfixed = true;
					break;
				case "uph":
					int.TryParse(info.value, out seed);
					UnityEngine.Random.InitState(seed);
					actorId = DropManager.instance.GetGachaPowerPointId(1);
					unfixed = true;
					break;
			}
			if (unfixed)
			{
				DropManager.instance.ClearLobbyDropInfo();
				UnityEngine.Random.InitState(Time.frameCount);
				if (actorId != "" && CheckDuplicate(i, info.type, actorId))
					actorId = "";
				_dicUnfixedData.Add(i.ToString(), actorId);
			}
		}

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonUnfixedItemData = serializer.SerializeObject(_dicUnfixedData);
		PlayFabApiManager.instance.RequestRegisterDailyShopUnfixedInfo(jsonUnfixedItemData);
	}

	bool CheckDuplicate(int slotId, string type, string actorId)
	{
		for (int i = 0; i <= ShopSlotMax; ++i)
		{
			// 자기 자신은 제외
			if (slotId == i)
				continue;

			DailyShopSlotInfo diffInfo = GetTodayShopData(i);
			if (diffInfo == null)
				continue;

			switch (type)
			{
				case "uch":
					// 같은 캐릭터가 있는지 확인한다.
					switch (diffInfo.type)
					{
						case "fc":
							if (actorId == diffInfo.value)
								return true;
							break;
						case "uch":
							// unfixed면 셋팅이 된건지 확인해야한다.
							// slotId 순서대로 셋팅하다보니 뒤에 있는 슬롯은 나중에 셋팅된다.
							if (_dicUnfixedData.ContainsKey(diffInfo.slotId.ToString()))
							{
								if (actorId == GetUnfixedResult(diffInfo.slotId))
									return true;
							}
							break;
					}
					break;
				case "upn":
					// 같은 캐릭터 pp가 있는지 확인한다.
					switch (diffInfo.type)
					{
						case "fp":
							if (actorId == diffInfo.value)
								return true;
							break;
						case "upn":
							if (_dicUnfixedData.ContainsKey(diffInfo.slotId.ToString()))
							{
								if (actorId == GetUnfixedResult(diffInfo.slotId))
									return true;
							}
							break;
					}
					break;
				case "uph":
					switch (diffInfo.type)
					{
						case "fp":
							if (actorId == diffInfo.value)
								return true;
							break;
						case "uph":
							if (_dicUnfixedData.ContainsKey(diffInfo.slotId.ToString()))
							{
								if (actorId == GetUnfixedResult(diffInfo.slotId))
									return true;
							}
							break;
					}
					break;
			}
		}
		return false;
	}

	public string GetUnfixedResult(int slotId)
	{
		if (_dicUnfixedData.ContainsKey(slotId.ToString()))
			return _dicUnfixedData[slotId.ToString()];

		Debug.LogError("GetUnfixedResult Error. Not found slotId.");
		return "";
	}
	#endregion

	List<string> _listTitleKey;
	void UpdateDailyShopRefreshTime()
	{
		if (_listDailyShopSlotInfo == null)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dailyShopRefreshTime) < 0)
			return;

		// 상품의 구매 여부와 상관없이 무조건 갱신해야한다.
		dailyShopRefreshTime += TimeSpan.FromDays(1);

		if (_listTitleKey == null)
		{
			_listTitleKey = new List<string>();
			_listTitleKey.Add("daShp");
			_listTitleKey.Add("daFre");
		}

		// 패킷을 보내서 새 정보를 받아와야한다.
		PlayFabApiManager.instance.RequestGetTitleData(_listTitleKey, (dicData) =>
		{
			// 새 테이블로 갱신하면 된다.
			OnRecvShopData(dicData);

			// 이땐 절대 구매 내역을 초기화 하면 안된다. 이 타이밍은 날짜 갱신 5분전에 상점 리스트를 새로 받는거라 구매 내역은 실제로 날짜가 갱신되는 타이밍에 해야한다.
		});
	}

	void UpdateDailyShopSlotResetTime()
	{
		if (_listDailyShopSlotInfo == null)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dailyShopSlotPurchasedResetTime) < 0)
			return;

		// 갱신 타이밍은 항상 동일
		dailyShopSlotPurchasedResetTime += TimeSpan.FromDays(1);

		// 기존의 구매 내역을 초기화 해야한다.
		if (_listShopSlotPurchased != null)
		{
			for (int i = 0; i < _listShopSlotPurchased.Count; ++i)
				_listShopSlotPurchased[i] = false;
		}

		// 이 타이밍에 unfixed 갱신처리도 해줘야한다. 이땐 날짜 비교안해도 되니 바로 갱신하면 된다.
		RegisterUnfixedInfo();
	}

	// 전부다 리셋된 상태인지 확인한다. UI 갱신 확인용 함수다.
	public bool IsClearedShopSlotPurchasedInfo()
	{
		if (_listShopSlotPurchased != null)
		{
			for (int i = 0; i < _listShopSlotPurchased.Count; ++i)
				if (_listShopSlotPurchased[i])
					return false;
		}
		return true;
	}




	#region Daily Free Item
	public DailyFreeItemInfo GetTodayFreeItemData()
	{
		if (_listDailyFreeItemInfo == null)
			return null;

		int serverDay = ServerTime.UtcNow.Day;
		for (int i = 0; i < _listDailyFreeItemInfo.Count; ++i)
		{
			if (_listDailyFreeItemInfo[i].dy == serverDay)
				return _listDailyFreeItemInfo[i];
		}
		return null;
	}

	void OnRecvDailyFreeItemInfo(DateTime lastDailyFreeItemReceiveTime)
	{
		//if (ServerTime.UtcNow < lastDailyFreeItemReceiveTime)
		//{
		//	// 어떻게 미래로 설정되어있을 수가 있나. 이건 무효.
		//	dailyFreeItemReceived = false;
		//	return;
		//}

		if (ServerTime.UtcNow.Year == lastDailyFreeItemReceiveTime.Year && ServerTime.UtcNow.Month == lastDailyFreeItemReceiveTime.Month && ServerTime.UtcNow.Day == lastDailyFreeItemReceiveTime.Day)
		{
			dailyFreeItemReceived = true;
			dailyFreeItemResetTime = new DateTime(lastDailyFreeItemReceiveTime.Year, lastDailyFreeItemReceiveTime.Month, lastDailyFreeItemReceiveTime.Day) + TimeSpan.FromDays(1);
		}
		else
			dailyFreeItemReceived = false;
	}

	public void OnRecvDailyFreeItemInfo(string lastDailyFreeItemReceiveTimeString)
	{
		DateTime lastDailyFreeItemReceiveTime = new DateTime();
		if (DateTime.TryParse(lastDailyFreeItemReceiveTimeString, out lastDailyFreeItemReceiveTime))
		{
			DateTime universalTime = lastDailyFreeItemReceiveTime.ToUniversalTime();
			OnRecvDailyFreeItemInfo(universalTime);
		}
	}

	void UpdateDailyFreeItemResetTime()
	{
		if (dailyFreeItemReceived == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dailyFreeItemResetTime) < 0)
			return;

		// 일퀘와 달리 창을 열어야만 보이기도 하고 노출되는 횟수가 적을거 같아서 하루 갱신될때 서버에 알리지 않고 클라가 선처리 하기로 한다.
		dailyFreeItemReceived = false;
		dailyFreeItemResetTime += TimeSpan.FromDays(1);

		// 일일 다이아는 안가지고 있는 유저가 있겠지만 FreeItem은 모두에게 적용된다. 여기서 처리하기로 한다.
		//LobbyCanvas.instance.RefreshAlarm
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.RefreshCashShopAlarmObject();
	}
	#endregion
}