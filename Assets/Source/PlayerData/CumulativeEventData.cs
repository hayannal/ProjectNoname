﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class CumulativeEventData : MonoBehaviour
{
	public static CumulativeEventData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("CumulativeEventData")).AddComponent<CumulativeEventData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static CumulativeEventData _instance = null;

	public class EventTypeInfo
	{
		public string id;
		public int td;

		public int sy;
		public int sm;
		public int sd;
		public int ey;
		public int em;
		public int ed;

		public int cc;
	}
	List<EventTypeInfo> _listEventTypeInfo;

	#region Repeat Events
	// 계속 늘어날수도 있으니 제대로 리스트로 관리하기로 한다.
	public class RepeatEventTypeInfo
	{
		public string id;
		public ObscuredInt totalDays;
		public int cc;

		public DateTime startDateTime { get; set; }
		public DateTime endDateTime { get; set; }
		
		public bool IsActiveEvent()
		{
			return (ServerTime.UtcNow > startDateTime && ServerTime.UtcNow < endDateTime);
		}
	}
	List<RepeatEventTypeInfo> _listRepeatEventTypeInfo;

	// RepeatEvent들의 달성도 역시 리스트로 관리하려다가 DB에 저장할때 각각 따로 저장하는 구조이기 때문에 리스트로 안하기로 한다.
	//public class MyRepeatEventData
	//{
	//	public string id;
	//	public string rcvDat;
	//	public ObscuredBool recorded;
	//	public ObscuredInt count;
	//}
	//List<MyRepeatEventData> _listMyRepeatEventData;
	#endregion

	public class EventRewardInfo
	{
		public string id;
		public int da;
		public int ad;

		public string tp1;
		public string vl1;
		public int cn1;
		public string tp2;
		public string vl2;
		public int cn2;

		public int day { get { return da; } }
		public bool alreadyDesigned { get { return ad == 1; } }
		public string type { get { return tp1; } }
		public string value { get { return vl1; } }
		public int count { get { return cn1; } }
		public string type2 { get { return tp2; } }
		public string value2 { get { return vl2; } }
		public int count2 { get { return cn2; } }
	}
	List<EventRewardInfo> _listEventRewardInfo;

	public enum eEventType
	{
		NewAccount,		// 신규계정 누적 로그인
		DailyBox,		// 신규계정 이벤트 후 DailyBox 여는 이벤트
		OpenChaos,		// 카오스 열렸을때 나오는 누적 로그인 이벤트
		Clear7Chapter,	// 7챕터 클리어 후 나오는 DailyBox 여는 이벤트

		LoginRepeat,	// 반복용 이벤트
		DailyBoxRepeat,	// 반복용 DailyBox 이벤트
		NodeWarRepeat,	// 반복용 NodeWar 이벤트
		Comeback,		// 복귀 유저

		ImageEvent1,
		ImageEvent2,

		PointShop,      // 포인트 상점 이벤트
		Review,			// 리뷰 요청 이벤트

		Amount,
	}

	public ObscuredBool disableEvent { get; set; }
	public ObscuredString iosUrl { get; set; }

	#region NewAccountLoginEvent
	public ObscuredInt newAccountLoginEventTotalDays { get; set; }

	public ObscuredBool newAccountLoginRecorded { get; set; }
	public ObscuredInt newAccountLoginEventCount { get; set; }
	#endregion

	#region NewAccountDailyBoxEvent
	public ObscuredInt newAccountDailyBoxEventTotalDays { get; set; }

	public ObscuredBool newAccountDailyBoxRecorded { get; set; }
	public ObscuredInt newAccountDailyBoxEventCount { get; set; }
	#endregion

	#region OpenChaosEvent
	public ObscuredInt openChaosEventTotalDays { get; set; }

	public ObscuredBool openChaosEventRecorded { get; set; }
	public ObscuredInt openChaosEventCount { get; set; }
	#endregion

	#region Repeat Events
	public string repeatLoginRcvDat { get; set; }
	public string repeatDailyBoxRcvDat { get; set; }
	public string repeatNodeWarRcvDat { get; set; }
	public ObscuredBool repeatLoginEventRecorded { get; set; }
	public ObscuredBool repeatDailyBoxEventRecorded { get; set; }
	public ObscuredBool repeatNodeWarEventRecorded { get; set; }
	public ObscuredInt repeatLoginEventCount { get; set; }
	public ObscuredInt repeatDailyBoxEventCount { get; set; }
	public ObscuredInt repeatNodeWarEventCount { get; set; }

	public ObscuredBool removeRepeatServerFailure { get; set; }
	#endregion

	#region Review Events
	public string reviewRcvDat { get; set; }
	public ObscuredBool reviewEventChecked { get; set; }
	#endregion

	#region Point Shop
	public string pointShopRcvDat { get; set; }	
	public ObscuredBool pointShopChecked { get; set; }  // 최초 확인용. 포인트 샵이 생겼음을 알려야한다. 리뷰처럼 최초로 열때 확인했다고 패킷 보내는 용도다.
	public ObscuredInt pointShopPoint { get; set; }		// 잔여 포인트. 기간 끝나면 리셋된다.
	#endregion

	public void OnRecvCumulativeEventData(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics, bool newlyCreated)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		if (titleData.ContainsKey("newuEvnt") && string.IsNullOrEmpty(titleData["newuEvnt"]) == false)
			disableEvent = (titleData["newuEvnt"] == "0");

		if (titleData.ContainsKey("iosUrl") && string.IsNullOrEmpty(titleData["iosUrl"]) == false)
			iosUrl = titleData["iosUrl"];

#if UNITY_IOS
		if (PlayerData.instance.reviewVersion)
			disableEvent = true;
#endif

		_listEventTypeInfo = null;
		if (titleData.ContainsKey("evntTp"))
			_listEventTypeInfo = serializer.DeserializeObject<List<EventTypeInfo>>(titleData["evntTp"]);

		_listEventRewardInfo = null;
		if (titleData.ContainsKey("evntRw"))
			_listEventRewardInfo = serializer.DeserializeObject<List<EventRewardInfo>>(titleData["evntRw"]);

		if (_listRepeatEventTypeInfo == null)
			_listRepeatEventTypeInfo = new List<RepeatEventTypeInfo>();
		_listRepeatEventTypeInfo.Clear();

		if (_listEventTypeInfo != null)
		{
			for (int i = 0; i < _listEventTypeInfo.Count; ++i)
			{
				if (_listEventTypeInfo[i].id == EventType2Id(eEventType.NewAccount))
					newAccountLoginEventTotalDays = _listEventTypeInfo[i].td;
				if (_listEventTypeInfo[i].id == EventType2Id(eEventType.DailyBox))
					newAccountDailyBoxEventTotalDays = _listEventTypeInfo[i].td;
				if (_listEventTypeInfo[i].id == EventType2Id(eEventType.OpenChaos))
					openChaosEventTotalDays = _listEventTypeInfo[i].td;
				if (_listEventTypeInfo[i].id == EventType2Id(eEventType.LoginRepeat) || _listEventTypeInfo[i].id == EventType2Id(eEventType.DailyBoxRepeat) ||
					_listEventTypeInfo[i].id == EventType2Id(eEventType.NodeWarRepeat) ||
					// 기간제 형태로 날짜 검사해서 IsActiveEvent 로 보여질지 체크하는 항목들은 다 이 listRepeatEventTypeInfo에 넣어둔다.
					_listEventTypeInfo[i].id == EventType2Id(eEventType.ImageEvent1) || _listEventTypeInfo[i].id == EventType2Id(eEventType.ImageEvent2) ||
					_listEventTypeInfo[i].id == EventType2Id(eEventType.Review) || _listEventTypeInfo[i].id == EventType2Id(eEventType.PointShop))
				{
					RepeatEventTypeInfo repeatEventTypeInfo = new RepeatEventTypeInfo();
					repeatEventTypeInfo.id = _listEventTypeInfo[i].id;
					repeatEventTypeInfo.totalDays = _listEventTypeInfo[i].td;
					repeatEventTypeInfo.cc = _listEventTypeInfo[i].cc;
					repeatEventTypeInfo.startDateTime = new DateTime(_listEventTypeInfo[i].sy, _listEventTypeInfo[i].sm, _listEventTypeInfo[i].sd);
					repeatEventTypeInfo.endDateTime = new DateTime(_listEventTypeInfo[i].ey, _listEventTypeInfo[i].em, _listEventTypeInfo[i].ed);
					_listRepeatEventTypeInfo.Add(repeatEventTypeInfo);
				}
			}
		}

		#region NewAccountLoginEvent
		newAccountLoginRecorded = false;
		if (userReadOnlyData.ContainsKey("evtNewbLogDat2"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtNewbLogDat2"].Value) == false)
				OnRecvNewAccountLoginInfo(userReadOnlyData["evtNewbLogDat2"].Value);
		}

		newAccountLoginEventCount = 0;
		if (userReadOnlyData.ContainsKey("evtNewbLogCnt2"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtNewbLogCnt2"].Value, out intValue))
				newAccountLoginEventCount = intValue;
		}
		#endregion

		#region NewAccountDailyBoxEvent
		newAccountDailyBoxRecorded = false;
		if (userReadOnlyData.ContainsKey("evtNewbDbxDat2"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtNewbDbxDat2"].Value) == false)
				OnRecvNewAccountDailyBoxInfo(userReadOnlyData["evtNewbDbxDat2"].Value);
		}

		newAccountDailyBoxEventCount = 0;
		if (userReadOnlyData.ContainsKey("evtNewbDbxCnt2"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtNewbDbxCnt2"].Value, out intValue))
				newAccountDailyBoxEventCount = intValue;
		}
		#endregion

		#region OpenChaosEvent
		openChaosEventRecorded = false;
		if (userReadOnlyData.ContainsKey("evtOpnChaDat2"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtOpnChaDat2"].Value) == false)
				OnRecvOpenChaosEventRecordInfo(userReadOnlyData["evtOpnChaDat2"].Value);
		}

		openChaosEventCount = 0;
		if (userReadOnlyData.ContainsKey("evtOpnChaCnt2"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtOpnChaCnt2"].Value, out intValue))
				openChaosEventCount = intValue;
		}
		#endregion

		#region Repeat Events
		removeRepeatServerFailure = false;
		repeatLoginEventRecorded = false;
		repeatDailyBoxEventRecorded = false;
		repeatNodeWarEventRecorded = false;
		repeatLoginRcvDat = "";
		repeatDailyBoxRcvDat = "";
		repeatNodeWarRcvDat = "";
		if (userReadOnlyData.ContainsKey("evtRptLogDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtRptLogDat"].Value) == false)
				OnRecvRepeatLoginInfo(userReadOnlyData["evtRptLogDat"].Value);
		}
		if (userReadOnlyData.ContainsKey("evtRptDbxDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtRptDbxDat"].Value) == false)
				OnRecvRepeatDailyBoxInfo(userReadOnlyData["evtRptDbxDat"].Value);
		}
		if (userReadOnlyData.ContainsKey("evtRptNdwDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtRptNdwDat"].Value) == false)
				OnRecvRepeatNodeWarInfo(userReadOnlyData["evtRptNdwDat"].Value);
		}

		repeatLoginEventCount = 0;
		repeatDailyBoxEventCount = 0;
		repeatNodeWarEventCount = 0;
		if (userReadOnlyData.ContainsKey("evtRptLogCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtRptLogCnt"].Value, out intValue))
				repeatLoginEventCount = intValue;
		}
		if (userReadOnlyData.ContainsKey("evtRptDbxCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtRptDbxCnt"].Value, out intValue))
				repeatDailyBoxEventCount = intValue;
		}
		if (userReadOnlyData.ContainsKey("evtRptNdwCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtRptNdwCnt"].Value, out intValue))
				repeatNodeWarEventCount = intValue;
		}
		#endregion

		#region Review Events
		reviewRcvDat = "";
		if (userReadOnlyData.ContainsKey("evtRvDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtRvDat"].Value) == false)
				OnRecvReviewInfo(userReadOnlyData["evtRvDat"].Value);
		}
		#endregion

		#region Point Shop
		pointShopRcvDat = "";
		if (userReadOnlyData.ContainsKey("evtPsDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtPsDat"].Value) == false)
				OnRecvPointShopInfo(userReadOnlyData["evtPsDat"].Value);
		}
		pointShopPoint = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "pointShopPoint")
			{
				pointShopPoint = playerStatistics[i].Value;
				break;
			}
		}
		#endregion
	}

	public bool OnRecvGetEventReward(eEventType eventType, string lastRecordedTimeString)
	{
		RepeatEventTypeInfo info = null;
		switch (eventType)
		{
			case eEventType.NewAccount:
				if (newAccountLoginEventCount >= newAccountLoginEventTotalDays)
					return false;

				OnRecvNewAccountLoginInfo(lastRecordedTimeString);
				++newAccountLoginEventCount;

				// DailyBox로 연결되는 이벤트라서 이렇게 호출한다.
				if (newAccountLoginEventCount >= newAccountLoginEventTotalDays)
				{
					if (CumulativeEventCanvas.instance != null)
						CumulativeEventCanvas.instance.RefreshOpenTabSlot();
					if (EventBoard.instance != null)
						EventBoard.instance.RefreshBoardOnOff();
				}
				break;
			case eEventType.DailyBox:
				if (newAccountDailyBoxEventCount >= newAccountDailyBoxEventTotalDays)
					return false;

				OnRecvNewAccountDailyBoxInfo(lastRecordedTimeString);
				++newAccountDailyBoxEventCount;
				break;
			case eEventType.OpenChaos:
				if (openChaosEventCount >= openChaosEventTotalDays)
					return false;

				OnRecvOpenChaosEventRecordInfo(lastRecordedTimeString);
				++openChaosEventCount;
				break;
			case eEventType.LoginRepeat:
				info = FindRepeatEventTypeInfo(eventType);
				if (info == null || info.IsActiveEvent() == false)
					return false;
				if (repeatLoginEventCount >= info.totalDays)
					return false;

				OnRecvRepeatLoginInfo(lastRecordedTimeString);
				++repeatLoginEventCount;
				break;
			case eEventType.DailyBoxRepeat:
				info = FindRepeatEventTypeInfo(eventType);
				if (info == null || info.IsActiveEvent() == false)
					return false;
				if (repeatDailyBoxEventCount >= info.totalDays)
					return false;

				OnRecvRepeatDailyBoxInfo(lastRecordedTimeString);
				++repeatDailyBoxEventCount;
				break;
			case eEventType.NodeWarRepeat:
				info = FindRepeatEventTypeInfo(eventType);
				if (info == null || info.IsActiveEvent() == false)
					return false;
				if (repeatNodeWarEventCount >= info.totalDays)
					return false;

				OnRecvRepeatNodeWarInfo(lastRecordedTimeString);
				++repeatNodeWarEventCount;
				break;
		}
		return true;
	}

	public static string EventType2Id(eEventType eventType)
	{
		switch (eventType)
		{
			case eEventType.NewAccount: return "na";
			case eEventType.DailyBox: return "no";
			case eEventType.OpenChaos: return "co";
			case eEventType.Clear7Chapter: return "cs";
			case eEventType.LoginRepeat: return "sl";
			case eEventType.DailyBoxRepeat: return "so";
			case eEventType.NodeWarRepeat: return "sn";
			case eEventType.Comeback: return "cu";
			case eEventType.ImageEvent1: return "ie1";
			case eEventType.ImageEvent2: return "ie2";
			case eEventType.PointShop: return "ps";
			case eEventType.Review: return "rv";
		}
		return "";
	}

	public static bool IsNodeWarEvent(eEventType eventType)
	{
		switch (eventType)
		{
			case eEventType.NodeWarRepeat:
				return true;
		}
		return false;
	}

	public static bool IsDailyBoxEvent(eEventType eventType)
	{
		switch (eventType)
		{
			case eEventType.DailyBox:
			case eEventType.Clear7Chapter:
			case eEventType.DailyBoxRepeat:
				return true;
		}
		return false;
	}

	public static bool IsRepeatEvent(eEventType eventType)
	{
		switch (eventType)
		{
			case eEventType.LoginRepeat:
			case eEventType.DailyBoxRepeat:
			case eEventType.NodeWarRepeat:
			case eEventType.PointShop:
				return true;
		}
		return false;
	}

	public EventRewardInfo FindRewardInfo(eEventType eventType, int day)
	{
		if (_listEventRewardInfo == null)
			return null;

		string id = EventType2Id(eventType);
		if (id == "")
			return null;

		for (int i = 0; i < _listEventRewardInfo.Count; ++i)
		{
			if (_listEventRewardInfo[i].id == id && _listEventRewardInfo[i].day == day)
				return _listEventRewardInfo[i];
		}
		return null;
	}

	public RepeatEventTypeInfo FindRepeatEventTypeInfo(eEventType eventType)
	{
		if (_listRepeatEventTypeInfo == null)
			return null;

		string id = EventType2Id(eventType);
		if (id == "")
			return null;

		for (int i = 0; i < _listRepeatEventTypeInfo.Count; ++i)
		{
			if (_listRepeatEventTypeInfo[i].id == id)
				return _listRepeatEventTypeInfo[i];
		}
		return null;
	}

	public void LateInitialize()
	{
		// 이건 반복 이벤트인데 메일처럼 체크해서 생성하거나 삭제하면서 관리해야한다.
		// 이벤트 타입 역시 로그인만 있는게 아니라 DailyBox 연거 노드워 클리어한거 등등이 추가될 예정이다.
		// 천천히 보내는거라 Late에서 처리
		CheckRepeatCumulativeEvent();

		// 포인트 상점은 조금 특별하다.
		CheckPointShopEvent();

		// 고정장비 보상 아이콘 리소스 로드
		StartCoroutine(LoadRewardEquipIconAsync());
	}

	void CheckRepeatCumulativeEvent()
	{
		// 메일에서 하던 방식 비슷하지만 간소하다.
		//
		// 메일과 다른점은 CheckAdd나 CheckRemove 없이
		// 기록되어있는 마지막 날짜가 현재 진행중인 이벤트 기간 내의 날짜인지 확인 후
		// 기간내 기록이 아니라 예전거라면 수치를 0으로 읽고 서버로 리셋을 보내둬야한다. 매일 하나씩 받는 로직이 수치를 +1 하는 구조라서 초기화 해놔야한다.
		// 기간내 기록이라면 카운트 읽어서 반영해주면 된다.
		// 메일과 달리 일정 주기로 리프레쉬는 필요없다.
		if (CheckRemove())
		{
			// 하나라도 지울게 있다면 서버보고 체크하라고 보낸다.
			PlayFabApiManager.instance.RequestRemoveRepeatEvent(OnRecvRemoveRepeatEvent);
		}
		else
		{
			// 지울게 없다면 현재 들고있는 값이 최신이라는거니 그냥 쓰면 된다.
		}
	}

	void CheckPointShopEvent()
	{
		// 포인트 상점은 개별 컨텐츠에 점수 올리고 연동하는 로직을 넣었다 뺐다 할 수 없어서
		// 차라리 로비 올때마다 한번씩 서버에 물어봐서 점수를 받고 표시하는 형태로 가기로 했다.
		// 
		RepeatEventTypeInfo pointShopEventInfo = FindRepeatEventTypeInfo(eEventType.PointShop);
		if (pointShopEventInfo == null)
			return;
		if (pointShopEventInfo.IsActiveEvent() == false)
			return;

		PlayFabApiManager.instance.RequestGetPointShopPoint(() =>
		{
			// 포인트를 받으면 느낌표를 띄울 수 있는지 확인해야한다.
			if (EventBoard.instance != null && EventBoard.instance.gameObject != null && EventBoard.instance.gameObject.activeSelf)
				EventBoard.instance.RefreshBoardOnOff();
		});
	}

	public void OnRecvRemoveRepeatEvent(bool resetRepeatLogin, bool resetRepeatDailyBox, bool resetRepeatNodeWar, bool resetReview, bool resetPointShop)
	{
		if (resetRepeatLogin)
		{
			repeatLoginRcvDat = "";
			repeatLoginEventRecorded = false;
			repeatLoginEventCount = 0;
		}
		if (resetRepeatDailyBox)
		{
			repeatDailyBoxRcvDat = "";
			repeatDailyBoxEventRecorded = false;
			repeatDailyBoxEventCount = 0;
		}
		if (resetRepeatNodeWar)
		{
			repeatNodeWarRcvDat = "";
			repeatNodeWarEventRecorded = false;
			repeatNodeWarEventCount = 0;
		}
		if (resetReview)
		{
			reviewRcvDat = "";
			reviewEventChecked = false;
		}
		if (resetPointShop)
		{
			pointShopRcvDat = "";
			pointShopChecked = false;
			pointShopPoint = 0;
		}

		if (resetRepeatLogin || resetRepeatDailyBox || resetRepeatNodeWar || resetReview || resetPointShop)
		{
			// 반복이벤트가 삭제되었다면 갱신해서 느낌표 표시 역시 바뀔 수 있으므로 갱신시켜줘야한다.
			if (EventBoard.instance != null && EventBoard.instance.gameObject != null && EventBoard.instance.gameObject.activeSelf)
				EventBoard.instance.RefreshBoardOnOff();
		}
	}

	bool CheckRemove()
	{
#if UNITY_IOS
		if (PlayerData.instance.reviewVersion)
			return false;
#endif

		// 마지막 등록일을 셋팅해둔게 있다면 삭제할 필요가 있는지 체크
		bool deleteRepeatLogin = false;
		if (repeatLoginRcvDat != "")
		{
			RepeatEventTypeInfo info = FindRepeatEventTypeInfo(eEventType.LoginRepeat);
			// 서버에 정보가 없는 반복퀘면 오래된 이벤트일 가능성이 높다.
			if (info == null)
				deleteRepeatLogin = true;

			if (deleteRepeatLogin == false)
			{
				bool inRange = false;
				DateTime lastRecordTime = new DateTime();
				if (DateTime.TryParse(repeatLoginRcvDat, out lastRecordTime))
				{
					if (lastRecordTime > info.startDateTime && lastRecordTime < info.endDateTime)
						inRange = true;
					if (inRange && ServerTime.UtcNow > info.startDateTime && ServerTime.UtcNow > info.endDateTime)
						inRange = false;
				}

				// 만료된 상태라면 기록을 삭제해야한다.
				if (!inRange)
					deleteRepeatLogin = true;
			}
		}

		// RepeatDailyBox 역시 같은 방법으로 체크
		bool deleteRepeatDailyBox = false;
		if (repeatDailyBoxRcvDat != "")
		{
			RepeatEventTypeInfo info = FindRepeatEventTypeInfo(eEventType.DailyBoxRepeat);
			if (info == null)
				deleteRepeatDailyBox = true;

			if (deleteRepeatDailyBox == false)
			{
				bool inRange = false;
				DateTime lastRecordTime = new DateTime();
				if (DateTime.TryParse(repeatDailyBoxRcvDat, out lastRecordTime))
				{
					if (lastRecordTime > info.startDateTime && lastRecordTime < info.endDateTime)
						inRange = true;
					if (inRange && ServerTime.UtcNow > info.startDateTime && ServerTime.UtcNow > info.endDateTime)
						inRange = false;
				}
				if (!inRange)
					deleteRepeatDailyBox = true;
			}
		}

		// RepeatDailyBox 역시 같은 방법으로 체크
		bool deleteRepeatNodeWar = false;
		if (repeatNodeWarRcvDat != "")
		{
			RepeatEventTypeInfo info = FindRepeatEventTypeInfo(eEventType.NodeWarRepeat);
			if (info == null)
				deleteRepeatNodeWar = true;

			if (deleteRepeatNodeWar == false)
			{
				bool inRange = false;
				DateTime lastRecordTime = new DateTime();
				if (DateTime.TryParse(repeatNodeWarRcvDat, out lastRecordTime))
				{
					if (lastRecordTime > info.startDateTime && lastRecordTime < info.endDateTime)
						inRange = true;
					if (inRange && ServerTime.UtcNow > info.startDateTime && ServerTime.UtcNow > info.endDateTime)
						inRange = false;
				}
				if (!inRange)
					deleteRepeatNodeWar = true;
			}
		}

		// Review 역시 같은 방법으로 체크
		bool deleteReviewEvent = false;
		if (reviewRcvDat != "")
		{
			RepeatEventTypeInfo info = FindRepeatEventTypeInfo(eEventType.Review);
			if (info == null)
				deleteReviewEvent = true;

			if (deleteReviewEvent == false)
			{
				bool inRange = false;
				DateTime lastRecordTime = new DateTime();
				if (DateTime.TryParse(reviewRcvDat, out lastRecordTime))
				{
					if (lastRecordTime > info.startDateTime && lastRecordTime < info.endDateTime)
						inRange = true;
					if (inRange && ServerTime.UtcNow > info.startDateTime && ServerTime.UtcNow > info.endDateTime)
						inRange = false;
				}
				if (!inRange)
					deleteReviewEvent = true;
			}
		}

		// Review 같은 방법으로 체크
		bool deletePointShopEvent = false;
		if (pointShopRcvDat != "")
		{
			RepeatEventTypeInfo info = FindRepeatEventTypeInfo(eEventType.PointShop);
			if (info == null)
				deletePointShopEvent = true;

			if (deletePointShopEvent == false)
			{
				bool inRange = false;
				DateTime lastRecordTime = new DateTime();
				if (DateTime.TryParse(pointShopRcvDat, out lastRecordTime))
				{
					if (lastRecordTime > info.startDateTime && lastRecordTime < info.endDateTime)
						inRange = true;
					if (inRange && ServerTime.UtcNow > info.startDateTime && ServerTime.UtcNow > info.endDateTime)
						inRange = false;
				}
				if (!inRange)
					deletePointShopEvent = true;
			}
		}
		if (deleteRepeatLogin || deleteRepeatDailyBox || deleteRepeatNodeWar || deleteReviewEvent || deletePointShopEvent)
		{
			// 이렇게 검사해서 하나라도 지워야할게 있으면 서버로 패킷을 보내는데 RefreshMailList 패킷처럼 실패한다면 지워지지 않게된다.
			// 어차피 서버로부터 받는 테이블값이 이상해질리도 없다면
			// 클라에서 지워야한다고 판단해서 지우는건 선처리해도 되지 않을까.. 해서 선처리로 지워둘까 하다가
			//OnRecvRemoveRepeatEvent(deleteRepeatLogin, deleteRepeatDailyBox);
			// 서버에서 처리가 안되었다면 보상 받는것도 꼬일 수 있기 때문에 선처리 하지 않기로 한다.
			return true;
		}
		return false;
	}

	public void ResetEventInfo()
	{
		// 이벤트 역시 하루단위로 진행되기 때문에 도장찍은 것들을 날짜 갱신에 맞춰서 초기화 해야한다.
		newAccountLoginRecorded = false;
		newAccountDailyBoxRecorded = false;
		openChaosEventRecorded = false;
		repeatLoginEventRecorded = false;
		repeatDailyBoxEventRecorded = false;
		repeatNodeWarEventRecorded = false;

		// 오브젝트나 UI 스스로 시간을 체크하지 않기 때문에 여기서 대신 호출해야한다.
		if (EventBoard.instance != null && EventBoard.instance.gameObject != null && EventBoard.instance.gameObject.activeSelf)
			EventBoard.instance.RefreshBoardOnOff();
		if (CumulativeEventCanvas.instance != null)
		{
			CumulativeEventCanvas.instance.RefreshOpenTabSlot();
			if (CumulativeEventCanvas.instance.gameObject.activeSelf)
			{
				CumulativeEventCanvas.instance.innerMenuRootTransform.gameObject.SetActive(false);
				CumulativeEventCanvas.instance.innerMenuRootTransform.gameObject.SetActive(true);
			}
		}

		// RepeatEvent들은 그래도 한번 호출해서 리셋해야 하지 않나.
		CheckRepeatCumulativeEvent();

		// 포인트샵의 포인트가 갑자기 바뀔일은 없을테니 딱히 호출할 필요 없을듯
		//CheckPointShopEvent();

		// DailyShopData와 달리 매일 구성품이 바뀌는것도 아니라서 굳이 다음날엔 호출하지 않는다.
		//StartCoroutine(LoadRewardEquipIconAsync());
	}

	List<string> _listLoadKey = new List<string>();
	IEnumerator LoadRewardEquipIconAsync()
	{
		// DailyShopData 의 LoadDailyEquipIconAsync 보고 비슷하게 만들어둔다.
		_listLoadKey.Clear();
		if (_listEventRewardInfo != null)
		{
			for (int i = 0; i < _listEventRewardInfo.Count; ++i)
			{
				if (_listEventRewardInfo[i].type == "fe")
				{
					EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(_listEventRewardInfo[i].value);
					if (equipTableData == null)
						continue;
					_listLoadKey.Add(equipTableData.shotAddress);
				}
				if (_listEventRewardInfo[i].type2 == "fe")
				{
					EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(_listEventRewardInfo[i].value2);
					if (equipTableData == null)
						continue;
					_listLoadKey.Add(equipTableData.shotAddress);
				}
			}
		}

		if (_listLoadKey.Count == 0)
			yield break;

		AsyncOperationHandle<long> handle = Addressables.GetDownloadSizeAsync(_listLoadKey);
		yield return handle;
		long downloadSize = handle.Result;
		Addressables.Release<long>(handle);
		if (downloadSize > 0)
		{
			Debug.LogFormat("EventReward EquipIcon Size = {0}", downloadSize / 1024);
			yield break;
		}

		for (int i = 0; i < _listLoadKey.Count; ++i)
			AddressableAssetLoadManager.GetAddressableSprite(_listLoadKey[i], "Icon", null);
	}

	public int GetActiveEventCount()
	{
		int activeCount = 0;
		for (int i = 0; i < (int)eEventType.Amount; ++i)
		{
			if (IsActiveEvent((eEventType)i))
				++activeCount;
		}
		return activeCount;
	}

	public bool IsActiveEvent(eEventType eventType)
	{
		if (disableEvent)
			return false;

		RepeatEventTypeInfo info = null;
		switch (eventType)
		{
			case eEventType.NewAccount:
				if (newAccountLoginEventCount < newAccountLoginEventTotalDays)
					return true;
				// 마지막 7일을 등록한 날이라면 이것도 활성화로 쳐준다.
				if (newAccountLoginEventCount == newAccountLoginEventTotalDays && newAccountLoginRecorded)
					return true;
				break;
			case eEventType.DailyBox:
				if (newAccountLoginEventCount < newAccountLoginEventTotalDays)
					return false;
				if (newAccountDailyBoxEventCount < newAccountDailyBoxEventTotalDays)
					return true;
				if (newAccountDailyBoxEventCount == newAccountDailyBoxEventTotalDays && newAccountDailyBoxRecorded)
					return true;
				break;
			case eEventType.OpenChaos:
				if (PlayerData.instance.chaosModeOpened == false)
					return false;
				if (openChaosEventCount < openChaosEventTotalDays)
					return true;
				if (openChaosEventCount == openChaosEventTotalDays && openChaosEventRecorded)
					return true;
				break;
			case eEventType.Clear7Chapter:
				//if (PlayerData.instance.highestPlayChapter < 7)
				//	return false;
				break;
			case eEventType.LoginRepeat:
			case eEventType.DailyBoxRepeat:
			case eEventType.NodeWarRepeat:
			case eEventType.Comeback:
			case eEventType.ImageEvent1:
			case eEventType.ImageEvent2:
				info = FindRepeatEventTypeInfo(eventType);
				if (info != null && info.IsActiveEvent())
					return true;
				break;
			case eEventType.Review:
				if (ContentsData.instance.GetBossBattleTotalCount() < 2)
					return false;
				info = FindRepeatEventTypeInfo(eventType);
				if (info != null && info.IsActiveEvent())
					return true;
				break;
			case eEventType.PointShop:
				info = FindRepeatEventTypeInfo(eventType);
				if (info == null)
					return false;
				// 다른 이벤트와 달리 유일하게 cc 체크한다.
				if (info.cc != 0 && PlayerData.instance.highestPlayChapter < info.cc)
					return false;
				if (info.IsActiveEvent())
					return true;
				break;
		}
		return false;
	}

	public bool IsReceivableEvent(eEventType eventType)
	{
		if (disableEvent)
			return false;

		if (IsDailyBoxEvent(eventType) && PlayerData.instance.sharedDailyBoxOpened == false)
			return false;

		if (IsNodeWarEvent(eventType) && PlayerData.instance.nodeWarCleared == false)
			return false;

		RepeatEventTypeInfo info = null;
		switch (eventType)
		{
			case eEventType.NewAccount:
				if (newAccountLoginEventCount < newAccountLoginEventTotalDays && newAccountLoginRecorded == false)
					return true;
				break;
			case eEventType.DailyBox:
				if (newAccountLoginEventCount < newAccountLoginEventTotalDays)
					return false;
				if (newAccountDailyBoxEventCount < newAccountDailyBoxEventTotalDays && newAccountDailyBoxRecorded == false)
					return true;
				break;
			case eEventType.OpenChaos:
				if (PlayerData.instance.chaosModeOpened && openChaosEventCount < openChaosEventTotalDays && openChaosEventRecorded == false)
					return true;
				break;
			case eEventType.Clear7Chapter:
				break;
			case eEventType.LoginRepeat:
				if (removeRepeatServerFailure)
					return false;
				info = FindRepeatEventTypeInfo(eventType);
				if (info == null || info.IsActiveEvent() == false)
					return false;
				if (repeatLoginEventCount < info.totalDays && repeatLoginEventRecorded == false)
					return true;
				break;
			case eEventType.DailyBoxRepeat:
				if (removeRepeatServerFailure)
					return false;
				info = FindRepeatEventTypeInfo(eventType);
				if (info == null || info.IsActiveEvent() == false)
					return false;
				if (repeatDailyBoxEventCount < info.totalDays && repeatDailyBoxEventRecorded == false)
					return true;
				break;
			case eEventType.NodeWarRepeat:
				if (removeRepeatServerFailure)
					return false;
				info = FindRepeatEventTypeInfo(eventType);
				if (info == null || info.IsActiveEvent() == false)
					return false;
				if (repeatNodeWarEventCount < info.totalDays && repeatNodeWarEventRecorded == false)
					return true;
				break;
			case eEventType.Comeback:
				break;
			case eEventType.ImageEvent1:
				break;
			case eEventType.ImageEvent2:
				break;
			case eEventType.Review:
				if (removeRepeatServerFailure)
					return false;
				// BossBattleTotalCount 까지 검사하려면 아래 IsActiveEvent 함수로 체크하는게 코드 중복도 없다. 초반 유저들은 보이지 않으니 느낌표도 뜨면 안된다.
				if (IsActiveEvent(eventType) == false)
					return false;
				if (reviewEventChecked == false)
					return true;
				break;
			case eEventType.PointShop:
				if (removeRepeatServerFailure)
					return false;
				if (IsActiveEvent(eventType) == false)
					return false;
				if (pointShopChecked == false)
					return true;
				// 추가로 구매 가능한지도 확인해야한다.
				// 그런데 이 항목이 규격이 없어서
				bool buyable = false;
				for (int i = 1; i <= 10; ++i)
				{
					EventRewardInfo pointShopRewardInfo = FindRewardInfo(eEventType.PointShop, i);
					if (pointShopRewardInfo == null)
					{
						// 순서대로 돌다가 null나오면 break;
						break;
					}

					// 하나라도 살 수 있으면 가능으로 판단.
					if (pointShopPoint >= pointShopRewardInfo.cn1)
					{
						buyable = true;
						break;
					}
				}
				if (buyable)
					return true;
				break;
		}
		return false;
	}

	void OnRecvNewAccountLoginInfo(DateTime lastNewAccountLoginRecordTime)
	{
		if (ServerTime.UtcNow.Year == lastNewAccountLoginRecordTime.Year && ServerTime.UtcNow.Month == lastNewAccountLoginRecordTime.Month && ServerTime.UtcNow.Day == lastNewAccountLoginRecordTime.Day)
			newAccountLoginRecorded = true;
		else
			newAccountLoginRecorded = false;
	}

	public void OnRecvNewAccountLoginInfo(string lastNewAccountLoginRecordTimeString)
	{
		DateTime lastNewAccountLoginRecordTime = new DateTime();
		if (DateTime.TryParse(lastNewAccountLoginRecordTimeString, out lastNewAccountLoginRecordTime))
		{
			DateTime universalTime = lastNewAccountLoginRecordTime.ToUniversalTime();
			OnRecvNewAccountLoginInfo(universalTime);
		}
	}

	void OnRecvNewAccountDailyBoxInfo(DateTime lastNewAccountDailyBoxRecordTime)
	{
		if (ServerTime.UtcNow.Year == lastNewAccountDailyBoxRecordTime.Year && ServerTime.UtcNow.Month == lastNewAccountDailyBoxRecordTime.Month && ServerTime.UtcNow.Day == lastNewAccountDailyBoxRecordTime.Day)
			newAccountDailyBoxRecorded = true;
		else
			newAccountDailyBoxRecorded = false;
	}

	public void OnRecvNewAccountDailyBoxInfo(string lastNewAccountDailyBoxRecordTimeString)
	{
		DateTime lastNewAccountDailyBoxRecordTime = new DateTime();
		if (DateTime.TryParse(lastNewAccountDailyBoxRecordTimeString, out lastNewAccountDailyBoxRecordTime))
		{
			DateTime universalTime = lastNewAccountDailyBoxRecordTime.ToUniversalTime();
			OnRecvNewAccountDailyBoxInfo(universalTime);
		}
	}

	void OnRecvOpenChaosEventRecordInfo(DateTime lastOpenChaosEventRecordTime)
	{
		if (ServerTime.UtcNow.Year == lastOpenChaosEventRecordTime.Year && ServerTime.UtcNow.Month == lastOpenChaosEventRecordTime.Month && ServerTime.UtcNow.Day == lastOpenChaosEventRecordTime.Day)
			openChaosEventRecorded = true;
		else
			openChaosEventRecorded = false;
	}

	public void OnRecvOpenChaosEventRecordInfo(string lastOpenChaosEventRecordTimeString)
	{
		DateTime lastOpenChaosEventRecordTime = new DateTime();
		if (DateTime.TryParse(lastOpenChaosEventRecordTimeString, out lastOpenChaosEventRecordTime))
		{
			DateTime universalTime = lastOpenChaosEventRecordTime.ToUniversalTime();
			OnRecvOpenChaosEventRecordInfo(universalTime);
		}
	}

	#region Repeat Events
	void OnRecvRepeatLoginInfo(DateTime lastRepeatLoginEventRecordTime)
	{
		if (ServerTime.UtcNow.Year == lastRepeatLoginEventRecordTime.Year && ServerTime.UtcNow.Month == lastRepeatLoginEventRecordTime.Month && ServerTime.UtcNow.Day == lastRepeatLoginEventRecordTime.Day)
			repeatLoginEventRecorded = true;
		else
			repeatLoginEventRecorded = false;
	}

	public void OnRecvRepeatLoginInfo(string lastRepeatLoginEventRecordTimeString)
	{
		repeatLoginRcvDat = lastRepeatLoginEventRecordTimeString;
		DateTime lastRepeatLoginEventRecordTime = new DateTime();
		if (DateTime.TryParse(lastRepeatLoginEventRecordTimeString, out lastRepeatLoginEventRecordTime))
		{
			DateTime universalTime = lastRepeatLoginEventRecordTime.ToUniversalTime();
			OnRecvRepeatLoginInfo(universalTime);
		}
	}

	void OnRecvRepeatDailyBoxInfo(DateTime lastRepeatDailyBoxEventRecordTime)
	{
		if (ServerTime.UtcNow.Year == lastRepeatDailyBoxEventRecordTime.Year && ServerTime.UtcNow.Month == lastRepeatDailyBoxEventRecordTime.Month && ServerTime.UtcNow.Day == lastRepeatDailyBoxEventRecordTime.Day)
			repeatDailyBoxEventRecorded = true;
		else
			repeatDailyBoxEventRecorded = false;
	}

	public void OnRecvRepeatDailyBoxInfo(string lastRepeatDailyBoxEventRecordTimeString)
	{
		repeatDailyBoxRcvDat = lastRepeatDailyBoxEventRecordTimeString;
		DateTime lastRepeatDailyBoxEventRecordTime = new DateTime();
		if (DateTime.TryParse(lastRepeatDailyBoxEventRecordTimeString, out lastRepeatDailyBoxEventRecordTime))
		{
			DateTime universalTime = lastRepeatDailyBoxEventRecordTime.ToUniversalTime();
			OnRecvRepeatDailyBoxInfo(universalTime);
		}
	}

	void OnRecvRepeatNodeWarInfo(DateTime lastRepeatNodeWarEventRecordTime)
	{
		if (ServerTime.UtcNow.Year == lastRepeatNodeWarEventRecordTime.Year && ServerTime.UtcNow.Month == lastRepeatNodeWarEventRecordTime.Month && ServerTime.UtcNow.Day == lastRepeatNodeWarEventRecordTime.Day)
			repeatNodeWarEventRecorded = true;
		else
			repeatNodeWarEventRecorded = false;
	}

	public void OnRecvRepeatNodeWarInfo(string lastRepeatNodeWarEventRecordTimeString)
	{
		repeatNodeWarRcvDat = lastRepeatNodeWarEventRecordTimeString;
		DateTime lastRepeatNodeWarEventRecordTime = new DateTime();
		if (DateTime.TryParse(lastRepeatNodeWarEventRecordTimeString, out lastRepeatNodeWarEventRecordTime))
		{
			DateTime universalTime = lastRepeatNodeWarEventRecordTime.ToUniversalTime();
			OnRecvRepeatNodeWarInfo(universalTime);
		}
	}
	#endregion

	#region Review Event
	void OnRecvReviewInfo(DateTime lastReviewEventRecordTime)
	{
		// 여기가 다른 항목들과 다른점인데
		// 이벤트 기간내에 있는 날짜라면 한번 본거니 봤음으로 체크해두면 된다.
		RepeatEventTypeInfo info = FindRepeatEventTypeInfo(eEventType.Review);
		if (info == null)
			return;
		if (lastReviewEventRecordTime > info.startDateTime && lastReviewEventRecordTime < info.endDateTime)
			reviewEventChecked = true;
		else
			reviewEventChecked = false;
	}

	public void OnRecvReviewInfo(string lastReviewEventRecordTimeString)
	{
		reviewRcvDat = lastReviewEventRecordTimeString;
		DateTime lastReviewEventRecordTime = new DateTime();
		if (DateTime.TryParse(lastReviewEventRecordTimeString, out lastReviewEventRecordTime))
		{
			DateTime universalTime = lastReviewEventRecordTime.ToUniversalTime();
			OnRecvReviewInfo(universalTime);
		}
	}
	#endregion

	#region Point Shop
	void OnRecvPointShopInfo(DateTime lastPointShopEventRecordTime)
	{
		// 리뷰와 동일한 기능을 한다. 처음 확인하는 날짜 기록용
		RepeatEventTypeInfo info = FindRepeatEventTypeInfo(eEventType.PointShop);
		if (info == null)
			return;
		if (lastPointShopEventRecordTime > info.startDateTime && lastPointShopEventRecordTime < info.endDateTime)
			pointShopChecked = true;
		else
			pointShopChecked = false;
	}

	public void OnRecvPointShopInfo(string lastPointShopEventRecordTimeString)
	{
		pointShopRcvDat = lastPointShopEventRecordTimeString;
		DateTime lastPointShopEventRecordTime = new DateTime();
		if (DateTime.TryParse(lastPointShopEventRecordTimeString, out lastPointShopEventRecordTime))
		{
			DateTime universalTime = lastPointShopEventRecordTime.ToUniversalTime();
			OnRecvPointShopInfo(universalTime);
		}
	}
	#endregion
}