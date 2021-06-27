﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;

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
	// 그래서 맨앞에 안보이는 투명판을 깔고 클릭할때부터 이벤트가 진행되게 한다.
	// 클라 이벤트와 달리 이거 자체가 서버에 저장되는 키이자 완료를 판단하는 id로 쓰이니 겹치지 않게 만들어야한다. 그래서 소문자로 한다.
	public enum eServerEvent
	{
		chaos,
		node,
		research,
		balance,
		reconstruct,
		boss,
	}

	// 클라 이벤트는 메모리에만 기억되는 거라서 종료하면 더이상 볼 수 없다. 그래서 중요하지 않은 것들 위주다.
	// 서버 이벤트가 로비에서 이미 진행중이라면 기다렸다가 다 끝난 후에 재생된다.
	public enum eClientEvent
	{
		GainNewCharacter,
		NewChapter,
		OpenTimeSpace,
		ClearMaxChapter,
		OpenSecondDailyBox,
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

	public bool reservedOpenResearchEvent { get; set; }
	public bool reservedOpenEquipOptionEvent { get; set; }
	public bool reservedOpenBalanceEvent { get; set; }
	public bool reservedOpenReconstructEvent { get; set; }
	public bool reservedOpenBossBattleEvent { get; set; }

	#region OnEvent
	public void OnEventClearHighestChapter(int chapter, string newCharacterId)
	{
		// 정산창에서 호출될거다. 인벤 동기화 패킷을 따로 날려도 되긴한데 괜히 시간걸릴까봐 클라단에서 선처리해서 캐릭터 넣어둔다. 아이디는 전달받은거로 셋팅
		if (chapter == 1)
		{
			PlayerData.instance.AddNewCharacter("Actor1002", newCharacterId, 1);
			PlayerData.instance.mainCharacterId = "Actor1002";
			PushClientEvent(eClientEvent.GainNewCharacter, "Actor1002");
			DownloadManager.instance.CheckLobbyDownloadState();
		}
		else if (chapter == 2)
		{
			PlayerData.instance.AddNewCharacter("Actor2103", newCharacterId, 1);
			PlayerData.instance.mainCharacterId = "Actor2103";
			PushClientEvent(eClientEvent.GainNewCharacter, "Actor2103");
			MailData.instance.RefreshMailListImmediate();

			// 이제 2챕터 클리어 후에는 클라이언트 이벤트도 있지만 서버 이벤트도 추가로 생긴다.
			// 사실은 클라이언트상에서의 보스전 메뉴 오픈을 2-10으로 옮겼지만 디비에서 boss이벤트 설정하는건 여전히 2챕터 도착 시점으로 처리하기때문에
			// 동기를 맞추기 위해 클라 역시 이벤트만 이 시점에 설정한다.
			PushServerEvent(eServerEvent.boss);
		}
		else if (chapter == (int)ContentsManager.eOpenContentsByChapter.Research)
		{
			// Research와 Balance는 메뉴를 추가한거니 서버 이벤트로 변경하기로 한다.
			//reservedOpenResearchEvent = true;
			PushServerEvent(eServerEvent.research);
		}
		else if (chapter == (int)ContentsManager.eOpenContentsByChapter.NodeWar)
		{
			// 어차피 클리어시 1회만 들어올거라 검사 안해도 된다.
			//if (IsCompleteServerEvent(eServerEvent.node) == false)
			PushServerEvent(eServerEvent.node);
		}
		else if (chapter == (int)ContentsManager.eOpenContentsByChapter.EquipOption)
		{
			reservedOpenEquipOptionEvent = true;
		}
		else if (chapter == (int)ContentsManager.eOpenContentsByChapter.Balance)
		{
			PushServerEvent(eServerEvent.balance);
		}
		else if (chapter == (int)ContentsManager.eOpenContentsByChapter.SecondDailyBox)
		{
			// 유저간 불평들을 해결하기 위해 7챕터 클리어 한 유저가 이미 일일 오리진 박스를 열어둔 상태라면 +1 해준다.
			// 이래야 7챕터 깨고나서 오리진 박스를 여는 유저와 같은 카운트를 유지할 수 있다.
			if (PlayerData.instance.sharedDailyBoxOpened)
				++PlayerData.instance.secondDailyBoxFillCount;
			PushClientEvent(eClientEvent.OpenSecondDailyBox);
		}
		else if (chapter == (int)ContentsManager.eOpenContentsByChapter.Reconstruct)
		{
			PushServerEvent(eServerEvent.reconstruct);
		}

		int chapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
		if (chapter >= chapterLimit)
		{
			// 최종 챕터를 깬거라 더이상 진행하면 안되서
			// 곧바로 카오스 모드로 진입시켜야한다.
			PushClientEvent(eClientEvent.ClearMaxChapter);
		}

		// 챕터를 깨면 클라 이벤트로 새챕터 표시 이벤트도 넣어둔다.
		PushClientEvent(eClientEvent.NewChapter, "", chapter);
	}

	public void OnEventPlayHighestChapter(int chapter)
	{
		// ClearChapter와 달리 못깨고 그냥 플레이할때 오는거다. 카오스때는 들어오지 않는다.

		// 4챕터 이후에 최초로 죽었을때.
		// 최초를 구분하려면 결국 이 이벤트를 진행했는지에 대한 리스트가 필요하다.
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chaos) && IsCompleteServerEvent(eServerEvent.chaos) == false)
		{
			// 이벤트가 이미 진행중인 상태인지까지는 판단할 필요 없다. 어차피 이벤트가 걸려있다면 전투를 진행할 수 없을거다.
			PushServerEvent(eServerEvent.chaos);
		}
	}

	public void OnEventPlayHighestStage(int chapter, int prevStage, int stage)
	{
		// 최고 챕터의 스테이지를 갱신할때 오는 이벤트다. 클리어 할때도 50층으로 온다. 서버에서는 필요가 없어서 클라에만 있는 함수다. 카오스는 당연히 제외.
		if (ContentsManager.IsPlayable(ContentsManager.eOpenContentsByChapterStage.TimeSpace, chapter, prevStage, stage))
			PushClientEvent(eClientEvent.OpenTimeSpace);
	}
	#endregion

	#region Server
	public void OnRecvServerEvent(string json)
	{
		// 서버는 재설치해도 동작해야해서 UserData에 기록해놓는다.
		// 정산타이밍에는 괜히 UserData 받기 뭐하니 클라가 직접 넣었다가 메인씬으로 돌아왔을때 처리한다.
		// 이 타이밍에 재접하면 서버한테 다시 받을거고
		// 재접하지 않는다면 진행 후 마지막 스텝에서 서버로 보내 플래그를 끌거다.
		//
		// 원래는 이런식으로 파라미터까지 저장하려고 했다가
		// 사실 저장용으로는 아이디만 있어도 충분하기 때문에
		// 아이디 리스트로 판단하기로 한다.
		_queServerEventInfo.Clear();
		_listCompleteServerEvent.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		Dictionary<string, int> dicEventState = serializer.DeserializeObject<Dictionary<string, int>>(json);
		if (dicEventState.Count == 0)
			return;

		Dictionary<string, int>.Enumerator e = dicEventState.GetEnumerator();
		while (e.MoveNext())
		{
			eServerEvent serverEvent;
			if (System.Enum.TryParse<eServerEvent>(e.Current.Key, out serverEvent) == false)
				continue;

			// 1 : 진행 필요
			// 2 : 완료
			switch (e.Current.Value)
			{
				case 1:
					PushServerEvent(serverEvent);
					break;
				case 2:
					_listCompleteServerEvent.Add(serverEvent);
					break;
			}
		}

		#region Adjust Contents Open
		// 이미 기존 유저들이 있는 상태에서 초반 이벤트가 추가될때의 예외처리다.
		// 미리 시스템을 다 만들었으면 이런 코드가 필요없을텐데 나중에 추가해서 생기게 되었다.
		// 새 추가 컨텐츠 나올때마다 이 아래에다 붙이면 될거다.
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace))
		{
			// 컨텐츠는 오픈 상태인데 1도 아니고 2도 아니라면 강제로 이벤트를 재생 대기 상태로 설정해야한다.
			if (IsCompleteServerEvent(eServerEvent.boss) == false && ContainsStandbyServerEvent(eServerEvent.boss) == false)
				PushServerEvent(eServerEvent.boss);
		}
		#endregion
	}

	string CreateServerEventJson()
	{
		Dictionary<string, int> dicEventState = new Dictionary<string, int>();

		Queue<ServerEventInfo>.Enumerator e = _queServerEventInfo.GetEnumerator();
		while (e.MoveNext())
			dicEventState.Add(e.Current.eventType.ToString(), 1);
		for (int i = 0; i < _listCompleteServerEvent.Count; ++i)
			dicEventState.Add(_listCompleteServerEvent[i].ToString(), 2);

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.SerializeObject(dicEventState);
	}

	List<eServerEvent> _listCompleteServerEvent = new List<eServerEvent>();
	public bool IsCompleteServerEvent(eServerEvent serverEvent)
	{
		return _listCompleteServerEvent.Contains(serverEvent);
	}

	public bool IsStandbyServerEvent(eServerEvent serverEvent)
	{
		if (_queServerEventInfo.Count == 0)
			return false;

		ServerEventInfo serverEventInfo = _queServerEventInfo.Peek();
		return (serverEventInfo.eventType == serverEvent);
	}

	public bool IsStandbyServerEvent()
	{
		if (_queServerEventInfo.Count == 0)
			return false;
		return true;
	}

	bool ContainsStandbyServerEvent(eServerEvent serverEvent)
	{
		Queue<ServerEventInfo>.Enumerator e = _queServerEventInfo.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current.eventType == serverEvent)
				return true;
		}
		return false;
	}

	public void CompleteServerEvent(eServerEvent serverEvent)
	{
		_listCompleteServerEvent.Add(serverEvent);
		PlayFabApiManager.instance.RequestPushServerEvent(CreateServerEventJson());
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

	public bool IsStandbyClientEvent(eClientEvent clientEvent)
	{
		if (_queClientEventInfo.Count == 0)
			return false;

		ClientEventInfo clientEventInfo = _queClientEventInfo.Peek();
		return (clientEventInfo.eventType == clientEvent);
	}

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
		OnCompleteLobbyEvent();
	}

	public void OnCompleteLobbyEvent()
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
		switch (serverEventInfo.eventType)
		{
			case eServerEvent.chaos:
				StartCoroutine(ChaosProcess());
				break;
			case eServerEvent.node:
				StartCoroutine(NodeWarProcess());
				break;
			case eServerEvent.research:
				reservedOpenResearchEvent = true;
				// 플래그만 켜는거라서 서버 이벤트도 쌓여있다면 동시에 처리할 수 있다.
				OnLobby();
				break;
			case eServerEvent.balance:
				reservedOpenBalanceEvent = true;
				OnLobby();
				break;
			case eServerEvent.reconstruct:
				reservedOpenReconstructEvent = true;
				OnLobby();
				break;
			case eServerEvent.boss:
				reservedOpenBossBattleEvent = true;
				OnLobby();
				break;
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
			case eClientEvent.OpenTimeSpace:
				// 여긴 터치 받고 이펙트 보여주고 캔버스 띄워야하니 코루틴으로 처리한다.
				StartCoroutine(OpenTimeSpaceProcess());
				break;
			case eClientEvent.ClearMaxChapter:
				OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_WaitForUpdateEvent"), () =>
				{
					OnCompleteLobbyEvent();
				}, -1, true);
				break;
			case eClientEvent.OpenSecondDailyBox:
				// 이것도 터치 받고 이펙트 보여주고 해야하니 코루틴으로 처리한다.
				StartCoroutine(OpenSecondDailyBoxProcess());
				break;
		}
	}

	bool _waitTouch;
	bool _waitCompleteAnimation;
	IEnumerator ChaosProcess()
	{
		_waitTouch = true;
		UIInstanceManager.instance.ShowCanvasAsync("EventInputLockCanvas", null);

		// 등장 애니때문에 미리 멀리 만들어둔다.
		GameObject origGatePillarObject = BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, new Vector3(0.0f, 0.0f, -100.0f), Quaternion.identity);

		while (_waitTouch)
			yield return null;

		// 연출
		_waitCompleteAnimation = true;
		OpenChaosEventGatePillar.instance.OnTouch();

		while (_waitCompleteAnimation)
			yield return null;

		yield return new WaitForSeconds(0.2f);

		// 연출 이후
		UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
		{
			EventInputLockCanvas.instance.gameObject.SetActive(false);
			OpenChaosEventGatePillar.instance.gameObject.SetActive(false);

			// 멀리 생성해둔 게이트 필라 가져오면서 곧바로 indicator 뜨도록 설정.
			origGatePillarObject.transform.position = StageManager.instance.currentGatePillarSpawnPosition;
			GatePillar.instance.descriptionObjectIndicatorShowDelayTime = 0.5f;
			if (EnergyGaugeCanvas.instance != null && EnergyGaugeCanvas.instance.gameObject.activeSelf)
				EnergyGaugeCanvas.instance.cachedTransform.position = origGatePillarObject.transform.position;

			EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_OpenChaosName"), UIString.instance.GetString("GameUI_OpenChaosDesc"), UIString.instance.GetString("GameUI_OpenChaosMore"), () =>
			{
				CompleteServerEvent(eServerEvent.chaos);

				// 카오스는 챕터 클리어 실패시 뜨는거라 이벤트 연속처리를 하지 않아도 된다.
				//OnCompleteLobbyEvent();
			});
		});
	}

	public void OnClickScreen()
	{
		_waitTouch = false;
	}

	public void OnCompleteAnimation()
	{
		_waitCompleteAnimation = false;
	}

	IEnumerator OpenTimeSpaceProcess()
	{
		// 서버이벤트 chaos처럼 EventInputLockCanvas 깔고 진행하도록 한다.
		_waitTouch = true;
		UIInstanceManager.instance.ShowCanvasAsync("EventInputLockCanvas", null);

		while (_waitTouch)
			yield return null;

		// 연출
		_waitCompleteAnimation = true;
		OpenTimeSpacePortal.instance.OnTouch();

		while (_waitCompleteAnimation)
			yield return null;

		yield return new WaitForSeconds(0.2f);

		// 연출 이후
		UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
		{
			EventInputLockCanvas.instance.gameObject.SetActive(false);
			EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_OpenTimeSpaceName"), UIString.instance.GetString("GameUI_OpenTimeSpaceDesc"), UIString.instance.GetString("GameUI_OpenTimeSpaceMore"), () =>
			{
				// 이게 있어야 이벤트후에 새 챕터 표시가 남아있을때 띄울 수 있다.
				// 2챕터를 한번에 클리어하면 시공간과 뉴챕터 이벤트 둘다 떠야하니 처리해둬야한다.
				OnCompleteLobbyEvent();
			});
		});
	}

	IEnumerator OpenSecondDailyBoxProcess()
	{
		// 게이트필라나 TimeSpacePortal 이벤트에선 매번 새 프리팹 만들어서 했었는데
		// 이번 오리진 박스 표기는 이미 캔버스 자체가 보여주는 기능을 가지고 있어서 나눌 필요도 없었고,
		// 이펙트만 추가로 담는 Open용 스크립트 만드는게 번거로워서 이펙트만 어드레서블로 로딩해서 쓰기로 한다.
		//
		// 만약 이펙트가 엄청 무거웠다면 여기서 오래 걸릴테니 미리 프리로딩을 걸어놔야하겠지만,
		// 여기에서 쓰이는 이펙트는 매우 가벼운 이펙트라 이렇게 처리해도 괜찮을거 같아서 이대로 진행한다.
		GameObject effectPrefab = null;
		AddressableAssetLoadManager.GetAddressableGameObject("OpenSecondDailyBoxEffect", "Event", (prefab) =>
		{
			effectPrefab = prefab;
		});

		_waitTouch = true;
		UIInstanceManager.instance.ShowCanvasAsync("EventInputLockCanvas", null);

		while (effectPrefab == null)
			yield return null;

		while (_waitTouch)
			yield return null;

		BattleInstanceManager.instance.GetCachedObject(effectPrefab, new Vector3(-2.0f, 0.0f, 4.0f), Quaternion.identity);
		yield return new WaitForSeconds(1.3f);

		// 가장 밝아졌을때 캔버스 리프레쉬
		DailyBoxGaugeCanvas.instance.RefreshGauge();

		// 나머지 대기
		yield return new WaitForSeconds(1.5f);

		// 연출 이후
		UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
		{
			string text = UIString.instance.GetString("GameUI_OriginBigDesc");
			if (PlayerData.instance.sharedDailyBoxOpened)
				text = string.Format("{0}\n\n{1}", text, UIString.instance.GetString("GameUI_OriginBigDescDone"));

			EventInputLockCanvas.instance.gameObject.SetActive(false);
			EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_OriginBigName"), text, UIString.instance.GetString("GameUI_OriginBigMore"), () =>
			{
				OnCompleteLobbyEvent();
			});
		});
	}

	IEnumerator NodeWarProcess()
	{
		// SecondDailyBox와 마찬가지로 기존 프리팹은 기존 프리팹대로 나오면 되고
		// 그전에 사전 이펙트만 추가로 로딩하면 되는 구조라
		// 별도의 Open용 스크립트를 따로 추가하지 않기로 한다. 그래서 이펙트만 어드레서블로 로딩해서 쓰기로 한다.
		GameObject effectPrefab = null;
		AddressableAssetLoadManager.GetAddressableGameObject("OpenNodeWarPortalEffect", "Event", (prefab) =>
		{
			effectPrefab = prefab;
		});

		_waitTouch = true;
		UIInstanceManager.instance.ShowCanvasAsync("EventInputLockCanvas", null);

		while (effectPrefab == null)
			yield return null;

		while (_waitTouch)
			yield return null;

		BattleInstanceManager.instance.GetCachedObject(effectPrefab, NodeWarPortal.instance.cachedTransform.position, Quaternion.identity);
		yield return new WaitForSeconds(6.0f);

		// 가장 밝아졌을때 숨겨놓았던 포탈 다시 보여준다.
		NodeWarPortal.instance.gameObject.SetActive(true);

		// 나머지 대기
		yield return new WaitForSeconds(0.5f);

		// 연출 이후
		UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
		{
			EventInputLockCanvas.instance.gameObject.SetActive(false);
			
			EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_OpenNodeWarName"), UIString.instance.GetString("GameUI_OpenNodeWarDesc"), UIString.instance.GetString("GameUI_OpenNodeWarMore"), () =>
			{
				CompleteServerEvent(eServerEvent.node);

				// 카오스와 달리 스테이지 클리어 후 나오는거라 New Chapter 표시 이벤트도 연달아서 처리해야한다.
				OnCompleteLobbyEvent();
			});
		});
	}
	#endregion
}