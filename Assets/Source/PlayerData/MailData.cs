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
		// 다른 데이터들과 달리 메일은 5분마다 서버에 보내서 리스트를 업데이트 한다.
		// 받을 보상이 있다면 빨간색 알림 표시도 해준다.
		UpdateRefreshTime();
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
			PlayFabApiManager.instance.RequestRefreshMailList(OnRecvRefreshMail);
		}
		mailRefreshTime = ServerTime.UtcNow + TimeSpan.FromMinutes(5);
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

	void OnRecvRefreshMail(bool deleted, bool added, bool modified, string jsonMailDateList)
	{
		// 새로운 메일이 있다면 New표시를 해서 DotMainMenu 아이콘에 알려준다.
		// 보이지 않는 공지가 추가될땐 modified가 true로 오니 added인지 modified인지 구분해서 처리하면 될거다.

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listMyMailTime = serializer.DeserializeObject<List<MyMailData>>(jsonMailDateList);

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

		// 갱신 타이밍은 항상 동일
		mailRefreshTime += TimeSpan.FromMinutes(5);

		// WaitNetwork 없이 패킷 보내서 응답이 오면 갱신해둔다.
		PlayFabApiManager.instance.RequestRefreshMailList(OnRecvRefreshMail);
	}
}