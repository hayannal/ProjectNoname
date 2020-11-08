using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class SupportData : MonoBehaviour
{
	public static SupportData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("SupportData")).AddComponent<SupportData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static SupportData _instance = null;

	// 플레이팹에 적혀있는 데이터 크기는 30만 바이트지만 25만으로 제한걸고 쓰도록 하자.
	const int TotalDataSizeLimit = 250000;

	// 6개월이 지나면 삭제될거다.
	public const int OldDataMonth = 6;

	public class MySupportData
	{
		// 등록 날짜. 클라이언트에서는 이걸 파싱할 필요가 없다.
		//public string rcdDat;

		// 타입
		public int type;

		// 스트링 아이디
		public string sid;

		// 내용
		public string body;
	}
	List<MySupportData> _listMySupportData;
	public List<MySupportData> listMySupportData { get { return _listMySupportData; } }

	public DateTime supportRefreshTime { get; private set; }

	public void OnRecvSupportData(Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		_listMySupportData = null;
		if (userReadOnlyData.ContainsKey("sptDatLst"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["sptDatLst"].Value) == false)
				_listMySupportData = serializer.DeserializeObject<List<MySupportData>>(userReadOnlyData["sptDatLst"].Value);
		}

		// 하나라도 문의한게 있다면 5초 뒤에 리프레쉬 후 한시간 간격으로 갱신(삭제처리를 위해서)
		// 하나도 문의한게 없다면 운영자가 먼저 적을 경우는 거의 없을테니 한시간 간격으로 갱신
		if (_listMySupportData != null && _listMySupportData.Count > 0)
			supportRefreshTime = ServerTime.UtcNow + TimeSpan.FromSeconds(5);
		else
			supportRefreshTime = ServerTime.UtcNow + TimeSpan.FromHours(1);
	}

	// 우편과 달리 엄청 중요하지도 않은 정보인데 5분마다 갱신해줘야하나 싶어서 한시간 단위로만 하기로 한다.
	// 날짜 넘어가는 체크같은거도 필요없다.
	void Update()
	{
		UpdateRefreshTime();
	}

	void UpdateRefreshTime()
	{
		if (DateTime.Compare(ServerTime.UtcNow, supportRefreshTime) < 0)
			return;

		//supportRefreshTime += TimeSpan.FromHours(1);
		supportRefreshTime = ServerTime.UtcNow + TimeSpan.FromHours(1);

		// WaitNetwork 없이 패킷 보내서 응답이 오면 갱신해둔다. 로그아웃 없이 받으려면 이렇게 해야한다
		// 삭제는 클라가 하지 않고 서버가 수행하니 보내주는거 받기만 하면 된다.
		PlayFabApiManager.instance.RequestRefreshInquiryList(OnRecvRefreshInquiry);
	}

	void OnRecvRefreshInquiry(string jsonInquiryData)
	{
		if (jsonInquiryData == "")
			jsonInquiryData = "[]";

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listMySupportData = serializer.DeserializeObject<List<MySupportData>>(jsonInquiryData);

		// 문의 창을 열고있을때 변경되면
		if (SupportListCanvas.instance != null && SupportListCanvas.instance.gameObject.activeSelf)
		{
			// 뭔가 작업중이거나 기다리는게 아니라면
			if (WaitingNetworkCanvas.IsShow() == false && DelayedLoadingCanvas.IsShow() == false)
			{
				SupportListCanvas.instance.gameObject.SetActive(false);
				SupportListCanvas.instance.gameObject.SetActive(true);
			}
		}

		// 읽거나 쓰고있을땐 어차피 리스트로 돌아갈때 갱신되면 되니까 아무것도 하지 않는다.
	}

	public void OnRecvWriteInquiry(string body)
	{
		// 서버에 저장할 수 있는 최대값이 300k 인데 한번에 쓰는건 2000자로 제한했으니 적어도 100개는 이상은 포함될 수 있다.
		// 그러니 꽉찬더라도 앞에거 지우고 새로 추가하면 되는거라서
		// 문의 등록이 실패할 일은 없다고 봐도 된다.
		//
		// 대신 서버에서는 쓰기 할때 앞에있던 문의를 지우겠지만
		// 클라에서는 쓰기할때 지워지면 이상하므로 이땐 냅두고
		// 다음번 리프레쉬때(한시간마다 하는 리프레쉬) 혹은 재접속때 지우기로 한다.

		if (_listMySupportData == null)
			_listMySupportData = new List<MySupportData>();

		MySupportData newData = new MySupportData();
		newData.type = 0;
		newData.sid = "";
		newData.body = body;
		_listMySupportData.Add(newData);
	}
}