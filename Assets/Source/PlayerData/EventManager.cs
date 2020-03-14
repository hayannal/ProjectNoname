using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
	public static EventManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("EventManager")).AddComponent<EventManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static EventManager _instance = null;

	// 서버이벤트는 서버에 저장되서 클라를 지워도 유지되게 해준다. 사실상 퀘스트 느낌.
	public enum eServerEvent
	{
		GainNewCharacter,
		OpenChaos,
	}

	// 클라 이벤트는 메모리에만 기억되는 거라서 종료하면 더이상 볼 수 없다. 그래서 중요하지 않은 것들 위주다.
	// 서버 이벤트가 로비에서 이미 진행중이라면 기다렸다가 다 끝난 후에 재생된다.
	public enum eClientEvent
	{
		NewChapter,
	}

	struct ServerEventInfo
	{
		public eServerEvent eventType;
		public string sValue;
		public int iValue;
	}
	struct ClientEventInfo
	{
		public eClientEvent eventType;
		public string sValue;
		public int iValue;
	}
	Queue<ServerEventInfo> _queServerEventInfo = new Queue<ServerEventInfo>();
	Queue<ClientEventInfo> _queClientEventInfo = new Queue<ClientEventInfo>();

	#region OnEvent
	public void OnEventClearHighestChapter(int chapter, string newCharacterId)
	{
	}

	public void OnEventPlayHighestChapter(int chapter)
	{
	}

	public void OnRecvServerEvent(string json)
	{
	}
	#endregion

	void PushServerEvent(eServerEvent serverEvent, string sValue = "", int iValue = 0)
	{
		ServerEventInfo serverEventInfo = new ServerEventInfo();
		serverEventInfo.eventType = serverEvent;
		serverEventInfo.sValue = sValue;
		serverEventInfo.iValue = iValue;
		_queServerEventInfo.Enqueue(serverEventInfo);
	}

	void PushClientEvent(eClientEvent clientEvent, string sValue = "", int iValue = 0)
	{
		ClientEventInfo clientEventInfo = new ClientEventInfo();
		clientEventInfo.eventType = clientEvent;
		clientEventInfo.sValue = sValue;
		clientEventInfo.iValue = iValue;
		_queClientEventInfo.Enqueue(clientEventInfo);
	}

	#region Play on lobby
	public void OnLobby()
	{
	}

	void PlayEventProcess(ServerEventInfo serverEventInfo)
	{
		// 이벤트에 쓸 Canvas나 오브젝트들을 로딩할때까지 인풋이 들어와 씬이 넘어가면 안되므로 먼저 화면을 막아야한다.
		DelayedLoadingCanvas.Show(true);

		switch (serverEventInfo.eventType)
		{
			case eServerEvent.GainNewCharacter:
				break;
		}
	}

	void PlayEventProcess(ClientEventInfo clientEventInfo)
	{
		// 클라이언트 이벤트 중에선 인풋락 없이 되는게 있을거 같아서 조건문 처리.
		switch (clientEventInfo.eventType)
		{
			case eClientEvent.NewChapter:
				DelayedLoadingCanvas.Show(true);
				break;
		}

		switch (clientEventInfo.eventType)
		{
			case eClientEvent.NewChapter:
				UIInstanceManager.instance.ShowCanvasAsync("NewChapterCanvas", () => { NewChapterCanvas.instance.RefreshChapterInfo(clientEventInfo.iValue); });
				break;
		}
	}
	#endregion
}