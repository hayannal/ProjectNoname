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
		OpenChaos,
	}

	// 클라 이벤트는 메모리에만 기억되는 거라서 종료하면 더이상 볼 수 없다. 그래서 중요하지 않은 것들 위주다.
	// 서버 이벤트가 로비에서 이미 진행중이라면 기다렸다가 다 끝난 후에 재생된다.
	public enum eClientEvent
	{
		GainNewCharacter,
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
	Queue<ClientEventInfo> _queClientEventInfoForBattleResult = new Queue<ClientEventInfo>();

	#region OnEvent
	public void OnEventClearHighestChapter(int chapter, string newCharacterId)
	{
		// 정산창에서 호출될거다. 인벤 동기화 패킷을 따로 날려도 되긴한데 괜히 시간걸릴까봐 클라단에서 선처리해서 캐릭터 넣어둔다. 아이디는 전달받은거로 셋팅
		if (chapter == 1)
		{
			PlayerData.instance.AddNewCharacter("Actor002", newCharacterId, 1);
			PlayerData.instance.mainCharacterId = "Actor002";
			PushClientEvent(eClientEvent.GainNewCharacter, "Actor002");
		}
		else if (chapter == 2)
		{
			PlayerData.instance.AddNewCharacter("Actor003", newCharacterId, 2);
			PlayerData.instance.mainCharacterId = "Actor003";
			PushClientEvent(eClientEvent.GainNewCharacter, "Actor003");
		}

		// 챕터를 깨면 클라 이벤트로 새챕터 표시 이벤트도 넣어둔다.
		PushClientEvent(eClientEvent.NewChapter, "", chapter);
	}

	public void OnEventPlayHighestChapter(int chapter)
	{
	}

	public void OnRecvServerEvent(string json)
	{
	}

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

		// 원래 기본적으로 다 로비에서 발생하는 이벤트인데 캐릭터 영입창만 유일하게 전투결과창 끝나고 나오는 이벤트다.
		if (clientEvent == eClientEvent.GainNewCharacter)
		{
			_queClientEventInfoForBattleResult.Enqueue(clientEventInfo);

			// 캐릭터만큼은 미리 로딩해놓지 않으면 창 뜨는 시간이 오래 걸릴거 같다. 그래서 이벤트 Push해둘때 해당 액터도 걸어두기로 한다.
			AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(clientEventInfo.sValue));
			return;
		}

		_queClientEventInfo.Enqueue(clientEventInfo);
	}
	#endregion

	#region Play
	public bool OnExitBattleResult()
	{
		if (_queClientEventInfoForBattleResult.Count > 0)
		{
			ClientEventInfo clientEventInfo = _queClientEventInfoForBattleResult.Dequeue();
			PlayEventProcess(clientEventInfo);
			return true;
		}
		return false;
	}

	public void OnLobby()
	{
		// 이벤트 진행할게 있다면 진행. 먼저 서버이벤트를 체크한다.
		if (_queServerEventInfo.Count > 0)
		{
			ServerEventInfo serverEventInfo = _queServerEventInfo.Dequeue();
			PlayEventProcess(serverEventInfo);
			return;
		}

		// 서버이벤트 없을때만 즉시 실행
		OnCompleteServerLobbyEvent();
	}

	public void OnCompleteServerLobbyEvent()
	{
		// 로비에서 진행된 서버 이벤트가 끝나면 클라 이벤트를 1회 실행시켜준다.
		if (_queClientEventInfo.Count > 0)
		{
			ClientEventInfo clientEventInfo = _queClientEventInfo.Dequeue();
			PlayEventProcess(clientEventInfo);
		}
	}

	void PlayEventProcess(ServerEventInfo serverEventInfo)
	{
		// 이벤트에 쓸 Canvas나 오브젝트들을 로딩할때까지 인풋이 들어와 씬이 넘어가면 안되므로 먼저 화면을 막아야한다.
		DelayedLoadingCanvas.Show(true);

		switch (serverEventInfo.eventType)
		{
			
		}
	}

	void PlayEventProcess(ClientEventInfo clientEventInfo)
	{
		switch (clientEventInfo.eventType)
		{
			case eClientEvent.GainNewCharacter:
				UIInstanceManager.instance.ShowCanvasAsync("RecruitCanvas", () =>
				{
					RecruitCanvas.instance.ShowCanvas(clientEventInfo.sValue);
				});
				break;
			case eClientEvent.NewChapter:
				UIInstanceManager.instance.ShowCanvasAsync("NewChapterCanvas", () =>
				{
					NewChapterCanvas.instance.RefreshChapterInfo(clientEventInfo.iValue);
				}, false);
				break;
		}
	}
	#endregion
}