using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
	}
	List<EventTypeInfo> _listEventTypeInfo;

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
		NewAccount,
		DailyBox,
		Clear7Chapter,

		LoginRepeat,
		DailyBoxRepeat,
		Comeback,

		ImageEvent1,
		ImageEvent2,

		Amount,
	}

	public ObscuredBool disableEvent { get; set; }

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

	public void OnRecvCumulativeEventData(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, bool newlyCreated)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		if (titleData.ContainsKey("newuEvnt") && string.IsNullOrEmpty(titleData["newuEvnt"]) == false)
			disableEvent = (titleData["newuEvnt"] == "0");

		_listEventTypeInfo = null;
		if (titleData.ContainsKey("evntTp"))
			_listEventTypeInfo = serializer.DeserializeObject<List<EventTypeInfo>>(titleData["evntTp"]);

		_listEventRewardInfo = null;
		if (titleData.ContainsKey("evntRw"))
			_listEventRewardInfo = serializer.DeserializeObject<List<EventRewardInfo>>(titleData["evntRw"]);

		// 먼저 NewAccount LoginEvent 부분 파싱
		if (_listEventTypeInfo != null)
		{
			for (int i = 0; i < _listEventTypeInfo.Count; ++i)
			{
				if (_listEventTypeInfo[i].id == "na")
					newAccountLoginEventTotalDays = _listEventTypeInfo[i].td;
				if (_listEventTypeInfo[i].id == "no")
					newAccountDailyBoxEventTotalDays = _listEventTypeInfo[i].td;
			}
		}

		#region NewAccountLoginEvent
		newAccountLoginRecorded = false;
		if (userReadOnlyData.ContainsKey("evtNewbLogDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtNewbLogDat"].Value) == false)
				OnRecvNewAccountLoginInfo(userReadOnlyData["evtNewbLogDat"].Value);
		}

		newAccountLoginEventCount = 0;
		if (userReadOnlyData.ContainsKey("evtNewbLogCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtNewbLogCnt"].Value, out intValue))
				newAccountLoginEventCount = intValue;
		}
		#endregion

		#region NewAccountDailyBoxEvent
		newAccountDailyBoxRecorded = false;
		if (userReadOnlyData.ContainsKey("evtNewbDayDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["evtNewbDayDat"].Value) == false)
				OnRecvNewAccountDailyBoxInfo(userReadOnlyData["evtNewbDayDat"].Value);
		}

		newAccountDailyBoxEventCount = 0;
		if (userReadOnlyData.ContainsKey("evtNewbDayCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["evtNewbDayCnt"].Value, out intValue))
				newAccountDailyBoxEventCount = intValue;
		}
		#endregion
	}

	public static string EventType2Id(eEventType eventType)
	{
		switch (eventType)
		{
			case eEventType.NewAccount: return "na";
			case eEventType.DailyBox: return "no";
			case eEventType.Clear7Chapter: return "cs";
			case eEventType.LoginRepeat: return "sl";
			case eEventType.DailyBoxRepeat: return "so";
			case eEventType.Comeback: return "cu";
			case eEventType.ImageEvent1: return "ie1";
			case eEventType.ImageEvent2: return "ie2";
		}
		return "";
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

	public void LateInitialize()
	{
		// 이건 반복 이벤트인데 메일처럼 체크해서 생성하거나 삭제하면서 관리해야한다.
		// 이벤트 타입 역시 로그인만 있는게 아니라 DailyBox 연거 노드워 클리어한거 등등이 추가될 예정이다.
		// 천천히 보내는거라 Late에서 처리
		//CheckRepeatCumulativeLEvent();
	}

	public void ResetEventInfo()
	{
		// 이벤트 역시 하루단위로 진행되기 때문에 도장찍은 것들을 날짜 갱신에 맞춰서 초기화 해야한다.
		newAccountLoginRecorded = false;
		newAccountDailyBoxRecorded = false;

		// 오브젝트나 UI 스스로 시간을 체크하지 않기 때문에 여기서 대신 호출해야한다.
		if (EventBoard.instance != null && EventBoard.instance.gameObject != null && EventBoard.instance.gameObject.activeSelf)
			EventBoard.instance.RefreshBoardOnOff();
		if (CumulativeEventCanvas.instance != null && CumulativeEventCanvas.instance.gameObject.activeSelf)
			CumulativeEventCanvas.instance.RefreshOpenTabSlot();
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
			case eEventType.Clear7Chapter:
				break;
			case eEventType.LoginRepeat:
				break;
			case eEventType.DailyBoxRepeat:
				break;
			case eEventType.Comeback:
				break;
			case eEventType.ImageEvent1:
				break;
			case eEventType.ImageEvent2:
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










	void CheckRepeatCumulativeLEvent()
	{
		/*
		if (_checkedUnfixedNodeWarInfo)
			return;
		if (ContentsManager.IsTutorialChapter())
			return;

		if (disableEvent)
			return;
		// 파싱 실패라면
		if (_newAccountLoginEventTotalDays == 0)
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
					int result = 0;
					int.TryParse(_nodeWarBonusString, out result);
					nodeWarBonusPowerSource = result;
				}
				else
					needRegister = true;
			}
		}
		_checkedUnfixedNodeWarInfo = true;

		if (needRegister == false)
			return;
		RegisterNodeWarBonusPowerSource();
		*/
	}

	void RegisterNodeWarBonusPowerSource()
	{
		/*
		nodeWarBonusPowerSource = 0;
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.NodeWar))
			nodeWarBonusPowerSource = UnityEngine.Random.Range(0, 4);

		PlayFabApiManager.instance.RequestRegisterNodeWarBonusPowerSource(nodeWarBonusPowerSource);
		*/
	}
}