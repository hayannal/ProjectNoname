using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class MailData : MonoBehaviour
{
	public static MailData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("MailData")).AddComponent<MailData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static MailData _instance = null;

	public class MailCreateInfo
	{
		public string id;
		public int sy;
		public int sm;
		public int sd;
		public int ey;
		public int em;
		public int ed;
		public string tp;
		public string vl;
		public int cn;
		public string nm;
		public string de;
		public int ti;
	}
	List<MailCreateInfo> _listMailCreateInfo;

	public class MyMailData
	{
		// 중복 수령 체크용 아이디값
		public string id;

		// 받은 날짜
		public string rcvDat;

		// 안에 들어있는 아이템을 수령했는지
		public int got;
	}
	List<MyMailData> _listMyMailTime;

	public DateTime mailRefreshTime { get; private set; }

	void Update()
	{
		// 기본적인 캔버스 로드가 이뤄지고 나서 호출되는 초기화 함수.
		// OnRecvMailData받고나서 1회 호출된다.
		UpdateLateInitialize();

		// 다른 데이터들과 달리 메일은 5분마다 서버에 보내서 리스트를 업데이트 한다.
		// 받을 보상이 있다면 빨간색 알림 표시도 해준다.
		UpdateRefreshTime();

		// 서버 점검 알림
		UpdateServerMaintenance();
	}

	public void OnRecvMailData(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		_listMailCreateInfo = null;
		if (titleData.ContainsKey("mail"))
			_listMailCreateInfo = serializer.DeserializeObject<List<MailCreateInfo>>(titleData["mail"]);

		_listMyMailTime = null;
		if (userReadOnlyData.ContainsKey("mailDatLst"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["mailDatLst"].Value) == false)
				_listMyMailTime = serializer.DeserializeObject<List<MyMailData>>(userReadOnlyData["mailDatLst"].Value);
		}

		// 최초에는 기록된 데이터로 초기화하고 이후 5분마다 서버에서 물어봐서 갱신된 리스트를 받으면 된다.
		bool needRemove = CheckRemove();
		bool needAdd = CheckAdd();
		if (needRemove || needAdd)
		{
			// 변경해야할 항목이 있다면 서버에 리프레쉬를 알린다.
			PlayFabApiManager.instance.RequestRefreshMailList(_listMailCreateInfo.Count, OnRecvRefreshMail);
		}
		mailRefreshTime = ServerTime.UtcNow;
		CalcNextRefreshTime();

		_lateInitialized = false;
		_updateLateInitialize = true;
	}

	bool CheckRemove()
	{
		if (_listMyMailTime == null)
			return false;

		// 삭제해야할게 있는지 확인한다.
		for (int i = 0; i < _listMyMailTime.Count; ++i)
		{
			MailCreateInfo info = FindCreateMailInfo(_listMyMailTime[i].id);
			// 서버에 정보가 없는 메일이면 엄청 오래된 메일이거나 잘못된 메일일 가능성이 높다.
			if (info == null)
				return true;

			// 정보가 있더라도 기간 확인 후 삭제가 필요하면 리턴 true
			DateTime startDateTime = new DateTime(info.sy, info.sm, info.sd);
			DateTime endDateTime = new DateTime(info.ey, info.em, info.ed);

			DateTime receiveTime = new DateTime();
			if (DateTime.TryParse(_listMyMailTime[i].rcvDat, out receiveTime))
			{
				DateTime universalTime = receiveTime.ToUniversalTime();
				DateTime validTime = receiveTime;
				validTime = validTime.AddDays(info.ti);

				// ev는 예외처리 해야한다. ev는 받은 날짜가 스스로의 start, end가 되야한다.
				if (_listMyMailTime[i].id == "ev")
				{
					startDateTime = new DateTime(universalTime.Year, universalTime.Month, universalTime.Day);
					endDateTime = startDateTime.AddDays(1);
				}
				if (universalTime > startDateTime && universalTime > endDateTime && ServerTime.UtcNow > validTime)
					return true;
			}
			else
				return true;
		}
		return false;
	}

	public MailCreateInfo FindCreateMailInfo(string id)
	{
		for (int i = 0; i < _listMailCreateInfo.Count; ++i)
		{
			if (_listMailCreateInfo[i].id == id)
				return _listMailCreateInfo[i];
		}
		return null;
	}

	bool CheckAdd()
	{
		for (int i = 0; i < _listMailCreateInfo.Count; ++i)
		{
			// 적혀있는 날짜를 보고 생성해야하는지를 체크
			bool inRange = false;
			DateTime startDateTime = new DateTime(_listMailCreateInfo[i].sy, _listMailCreateInfo[i].sm, _listMailCreateInfo[i].sd);
			DateTime endDateTime = new DateTime(_listMailCreateInfo[i].ey, _listMailCreateInfo[i].em, _listMailCreateInfo[i].ed);
			if (startDateTime <= ServerTime.UtcNow && ServerTime.UtcNow <= endDateTime)
				inRange = true;

			if (inRange == false)
				continue;

			// 내 메일리스트를 확인해서 이미 생성한거면 패스. 해야하는거면 리스트에 넣어서 서버로 보낸다.
			// 하나 예외 상황이 있는데 id가 "ev"면 매일 생성해야하는 메일이라서
			// start end를 당일 데이터로 바꿔치기 한다.
			if (_listMailCreateInfo[i].id == "ev")
			{
				startDateTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day);
				endDateTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
			}

			if (FindMyMail(_listMailCreateInfo[i].id, startDateTime, endDateTime) == false)
				return true;
		}
		return false;
	}

	bool FindMyMail(string id, DateTime startDateTime, DateTime endDateTime)
	{
		if (_listMyMailTime == null)
			return false;

		for (int i = 0; i < _listMyMailTime.Count; ++i)
		{
			if (_listMyMailTime[i].id != id)
				continue;

			// id가 같으면 수령 타임을 확인해서 해당 메일인지 확인한다.
			DateTime receiveTime = new DateTime();
			if (DateTime.TryParse(_listMyMailTime[i].rcvDat, out receiveTime))
			{
				DateTime universalTime = receiveTime.ToUniversalTime();
				if (startDateTime <= universalTime && universalTime <= endDateTime)
					return true;
			}
		}
		return false;
	}

	void OnRecvRefreshMail(bool deleted, bool added, bool modified, string jsonMailDateList, string jsonMailTable)
	{
		// 새로운 메일이 있다면 New표시를 해서 DotMainMenu 아이콘에 알려준다.
		// 보이지 않는 공지가 추가될땐 modified가 true로 오니 added인지 modified인지 구분해서 처리하면 될거다.

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listMyMailTime = serializer.DeserializeObject<List<MyMailData>>(jsonMailDateList);

		if (jsonMailTable != "")
			_listMailCreateInfo = serializer.DeserializeObject<List<MailCreateInfo>>(jsonMailTable);

		// 메일 창을 열고있을때 메일이 추가되면
		if (added && MailCanvas.instance != null && MailCanvas.instance.gameObject.activeSelf)
		{
			// 뭔가 작업중이거나 기다리는게 아니라면
			if (WaitingNetworkCanvas.IsShow() == false && DelayedLoadingCanvas.IsShow() == false)
			{
				MailCanvas.instance.gameObject.SetActive(false);
				MailCanvas.instance.gameObject.SetActive(true);
			}
		}

		// 여기서 받을 수 있는 메일의 수를 세서 빨간점 띄우는 작업을 해야한다.
		if (GetReceivableMailPresentCount() > 0)
		{

		}

		// 뭔가 변경이 감지될때 서버 점검 있는지 판단한다.
		if (deleted || added || modified)
			CheckServerMaintenance();
	}

	int GetReceivableMailPresentCount()
	{
		int count = 0;
		for (int i = 0; i < _listMyMailTime.Count; ++i)
		{
			string id = _listMyMailTime[i].id;
			if (id == "un")
				continue;
			if (_listMyMailTime[i].got != 0)
				continue;

			MailData.MailCreateInfo info = MailData.instance.FindCreateMailInfo(id);
			if (info == null)
				continue;
			if (string.IsNullOrEmpty(info.tp))
				continue;

			DateTime receiveTime = new DateTime();
			if (DateTime.TryParse(_listMyMailTime[i].rcvDat, out receiveTime))
			{
				DateTime universalTime = receiveTime.ToUniversalTime();
				DateTime validTime = universalTime;
				validTime = validTime.AddDays(info.ti);
				if (ServerTime.UtcNow > validTime)
					continue;
			}

			++count;
		}
		return count;
	}

	public bool OnRecvGetMail(string id, int receiveDay, string tp)
	{
		if (_listMyMailTime == null)
			return false;

		for (int i = 0; i < _listMyMailTime.Count; ++i)
		{
			if (_listMyMailTime[i].id != id)
				continue;
			if (_listMyMailTime[i].got == 1)
				continue;
			MailCreateInfo info = FindCreateMailInfo(id);
			if (info == null)
				continue;
			if (info.tp != tp)
				continue;

			// id가 ev일때는 매일 받을 수 있는거니 겹칠 수 있어서 받은 날짜를 한번 더 검사해야한다.
			if (id == "ev")
			{
				DateTime receiveTime = new DateTime();
				if (DateTime.TryParse(_listMyMailTime[i].rcvDat, out receiveTime))
				{
					DateTime universalTime = receiveTime.ToUniversalTime();
					if (universalTime.Day != receiveDay)
						continue;
				}
			}

			_listMyMailTime[i].got = 1;
			return true;
		}
		return false;
	}

	public List<MyMailData> listMyMailData { get { return _listMyMailTime; } }










	void UpdateRefreshTime()
	{
		if (DateTime.Compare(ServerTime.UtcNow, mailRefreshTime) < 0)
			return;

		CalcNextRefreshTime();

		// WaitNetwork 없이 패킷 보내서 응답이 오면 갱신해둔다.
		PlayFabApiManager.instance.RequestRefreshMailList(_listMailCreateInfo.Count, OnRecvRefreshMail);
	}

	void CalcNextRefreshTime()
	{
		// 갱신 타이밍은 평소엔 항상 동일하나 날짜가 변경되는 타이밍엔 짧게 잡는다.
		int currentDay = mailRefreshTime.Day;
		mailRefreshTime += TimeSpan.FromMinutes(5);
		if (currentDay == mailRefreshTime.Day)
			return;

		mailRefreshTime = new DateTime(mailRefreshTime.Year, mailRefreshTime.Month, mailRefreshTime.Day) + TimeSpan.FromSeconds(3);
	}



	#region Server Maintenance
	bool _updateLateInitialize = false;
	bool _lateInitialized = false;
	void UpdateLateInitialize()
	{
		if (_updateLateInitialize == false)
			return;
		if (CommonCanvasGroup.instance == null)
			return;

		// UI가 로딩된 후 1회 강제로 체크한다. 이후엔 Refresh 타임때마다 필요한지 보고 체크한다.
		// MaintenanceCanvas를 띄우려면 CommonCanvasGroup이 로딩되어있어야해서 이렇게 구조를 짜게 되었다.
		_updateLateInitialize = false;
		_lateInitialized = true;
		CheckServerMaintenance();
	}

	// 숨겨진 우편 "un"으로 서버 점검 타임을 체크하는 기능이다.
	DateTime _serverMaintenanceTime;
	int _serverMaintenanceRemainMinute;
	public void CheckServerMaintenance()
	{
		bool find = false;
		bool reached = false;
		if (_listMyMailTime != null)
		{
			for (int i = 0; i < _listMyMailTime.Count; ++i)
			{
				string id = _listMyMailTime[i].id;
				if (id != "un")
					continue;
				MailCreateInfo info = FindCreateMailInfo(id);
				if (info == null)
					continue;

				// 서버 점검 시간을 구한다. 이미 지난거라면 아무것도 하지 않는다.
				DateTime endDateTime = new DateTime(info.ey, info.em, info.ed);
				_serverMaintenanceTime = endDateTime.AddMinutes(info.cn);
				if (ServerTime.UtcNow > _serverMaintenanceTime)
					continue;

				// 서버점검이 예정되어있다. 적절한 타이밍을 구해야한다.
				// 1시간전, 30분전, 10분전, 5분전, 3분전 순서대로 체크해본다.
				_reserveMaintenanceAlarmTime = _serverMaintenanceTime.AddHours(-1);
				if (ServerTime.UtcNow < _reserveMaintenanceAlarmTime)
				{
					_serverMaintenanceRemainMinute = 60;
					find = true;
					break;
				}
				_reserveMaintenanceAlarmTime = _serverMaintenanceTime.AddMinutes(-30);
				if (ServerTime.UtcNow < _reserveMaintenanceAlarmTime)
				{
					_serverMaintenanceRemainMinute = 30;
					find = true;
					break;
				}
				_reserveMaintenanceAlarmTime = _serverMaintenanceTime.AddMinutes(-10);
				if (ServerTime.UtcNow < _reserveMaintenanceAlarmTime)
				{
					_serverMaintenanceRemainMinute = 10;
					find = true;
					break;
				}
				_reserveMaintenanceAlarmTime = _serverMaintenanceTime.AddMinutes(-5);
				if (ServerTime.UtcNow < _reserveMaintenanceAlarmTime)
				{
					_serverMaintenanceRemainMinute = 5;
					find = true;
					break;
				}
				_reserveMaintenanceAlarmTime = _serverMaintenanceTime.AddMinutes(-3);
				if (ServerTime.UtcNow < _reserveMaintenanceAlarmTime)
				{
					_serverMaintenanceRemainMinute = 3;
					find = true;
					break;
				}
				_reserveMaintenanceAlarmTime = _serverMaintenanceTime.AddMinutes(-2);
				if (ServerTime.UtcNow < _reserveMaintenanceAlarmTime)
				{
					_serverMaintenanceRemainMinute = 2;
					find = true;
					break;
				}
				// 2분 이내면 이미 임박한거다.
				find = true;
				reached = true;
				break;
			}
		}

		if (find == false)
		{
			// 혹시 예약했다가 취소한걸수도 있으니 예약을 지워야한다.
			_reserveMaintenanceAlarm = false;
			_reserveMaintenance = false;

			// 보이던 도중에 점검 데이터가 수정되었다면 떠있는 창을 닫아줘야한다.
			MaintenanceCanvas.Show(false, "", 0.0f);
		}
		else
		{
			if (reached)
			{
				// 곧 서버 점검임을 알리고 창을 띄워야한다.
				_reserveMaintenance = true;
				_reserveMaintenanceAlarm = false;
				if (_lateInitialized)
					MaintenanceCanvas.Show(true, UIString.instance.GetString("GameUI_MaintenanceReached"), 180.0f);

				// 확 끊어지는걸 방지하기 위해
				_serverMaintenanceTime += TimeSpan.FromSeconds(3.0f);
			}
			else
			{
				// 이미 Time은 위에서 계산하면서 셋팅해놨으니 여기선 플래그만 켜면 된다.
				_reserveMaintenance = false;
				_reserveMaintenanceAlarm = true;
			}
		}
	}

	bool _reserveMaintenance = false;
	bool _reserveMaintenanceAlarm = false;
	DateTime _reserveMaintenanceAlarmTime;
	void UpdateServerMaintenance()
	{
		if (_lateInitialized == false)
			return;

		if (_reserveMaintenanceAlarm)
		{
			if (ServerTime.UtcNow > _reserveMaintenanceAlarmTime)
			{
				// 예약된 시간을 지날때 메세지를 띄우고
				MaintenanceCanvas.Show(true, UIString.instance.GetString("GameUI_Maintenance", _serverMaintenanceRemainMinute), 10.0f);

				// 다음번 예약을 걸어야하니 체크함수를 다시 호출한다.
				_reserveMaintenanceAlarm = false;
				CheckServerMaintenance();
			}
		}

		if (_reserveMaintenance)
		{
			if (ServerTime.UtcNow > _serverMaintenanceTime)
			{
				_reserveMaintenance = false;

				// 예약된 점검 시간이 왔다.
				PlayFabApiManager.instance.HandleCommonError();
			}
		}
	}
	#endregion
}