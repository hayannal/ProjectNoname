#define PLAYFAB				// 싱글버전으로 돌아가는 디파인이다. 테스트용을 위해 남겨둔다.
#define NEWPLAYER_LEVEL1	// 실제 튜토리얼 들어갈때 무조건 없애야하는 디파인이다. 1레벨 임시 캐릭 생성용 버전.
#define NEWPLAYER_ADD_KEEP	// 사실 킵이 있는게 1챕터의 시작이라 위 LEVEL1가지고는 정상적인 흐름대로 진행하기가 어렵다. 위와 마찬가지로 지워야한다. 지울때 꼭!! 서버의 rules에서 OnCreatePlayer4 빼야함

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

// 별도의 로딩씬을 만들지 않고 메인씬에서 모든걸 처리한다. 이래야 로딩속도를 최대한 줄일 수 있다.
public class MainSceneBuilder : MonoBehaviour
{
	public static MainSceneBuilder instance;
	public static bool s_firstTimeAfterLaunch = true;

	void Awake()
	{
		instance = this;
	}

	public bool mainSceneBuilding { get; private set; }
	public bool waitSpawnFlag { get; set; }
	public bool lobby { get; private set; }
	public bool playAfterInstallation { get; private set; }

	void OnDestroy()
	{
		Addressables.Release<GameObject>(_handleTableDataManager);

		// 서버오류로 인해 접속못했을 경우 대비해서 체크해둔다.
		if (_handleStageManager.IsValid() == false && mainSceneBuilding) return;

		Addressables.Release<GameObject>(_handleStageManager);
		Addressables.Release<GameObject>(_handleStartCharacter);
		Addressables.Release<GameObject>(_handleLobbyCanvas);
		Addressables.Release<GameObject>(_handleCommonCanvasGroup);
		Addressables.Release<GameObject>(_handleTreasureChest);

		// 이벤트용이라 항상 로드되는게 아니다보니 IsValue체크가 필수다.
		if (_handleEventGatePillar.IsValid())
			Addressables.Release<GameObject>(_handleEventGatePillar);

		// 로딩속도를 위해 배틀매니저는 천천히 로딩한다. 그래서 다른 로딩 오브젝트와 달리 Valid 검사를 해야한다.
		if (_handleBattleManager.IsValid())
			Addressables.Release<GameObject>(_handleBattleManager);

		// 게임을 오래 켜두면 번들데이터가 점점 커지게 된다.
		// 해제를 할만한 가장 적당한 곳은 씬이 파괴될때이다.
		AddressableAssetLoadManager.CheckRelease();
	}

	AsyncOperationHandle<GameObject> _handleTableDataManager;
	AsyncOperationHandle<GameObject> _handleStageManager;
	AsyncOperationHandle<GameObject> _handleBattleManager;
	AsyncOperationHandle<GameObject> _handleStartCharacter;
	AsyncOperationHandle<GameObject> _handleTitleCanvas;
	AsyncOperationHandle<GameObject> _handleLobbyCanvas;
	AsyncOperationHandle<GameObject> _handleCommonCanvasGroup;
	AsyncOperationHandle<GameObject> _handleTreasureChest;
	AsyncOperationHandle<GameObject> _handleEventGatePillar;
	IEnumerator Start()
    {
		// 씬 빌더는 항상 이 씬이 시작될때 1회만 동작하며 로딩씬을 띄워놓고 현재 상황에 맞춰서 스텝을 진행한다.
		// 나중에 어드레서블 에셋시스템에도 적어두겠지만, 이번 구조는 1챕터까진 추가 다운로드 없이 진행하는게 목표고 이후 번들을 받는 구조가 되어야한다.
		// 그렇다고 씬에 넣어두면 Start때 로드하는거라 로딩창이 늦게 나오게 된다. 그러니 결국 Resources 폴더에 넣어두고 로딩하는 방법 말고는 없다.
		mainSceneBuilding = true;
		LoadingCanvas.instance.gameObject.SetActive(true);
		// 2번은 호출해야 로딩화면이 온전히 보인다.
		yield return new WaitForEndOfFrame();
		LoadingCanvas.instance.SetProgressBarPoint(0.1f, 0.0f, true);
		yield return new WaitForEndOfFrame();

		// 초기화 해야할 항목들은 다음과 같다.
		// 1. 테이블 번들패치. 테이블은 다른 번들과 달리 물어보지 않고 곧바로 패치한다. LoadorCache 함수 쓸테니 변경시에만 받게될거다. 현재는 번들구조가 없으므로 그냥 로드
		// 2. 테이블매니저에는 타이틀 스트링만 있을거다. 일반 스트링은 최초 1회는 물어보지 않고 받아야하고(애플) 이후부터는 물어보고 받아야할거다.
		// 3. 로그인을 해야한다. 최초 기동시엔 자동으로 게스트로 들어가고 이후 연동을 하고나면 해당 로그인으로 진행해서 플레이어 데이터를 받는다. 현재는 임시로 처리.
		// - 플레이할 캐릭터와 마지막 스테이지 정보를 받았으면 이 정보를 가지고 데이터 로딩을 시작한다.
		// 4. 데이터들을 로드하기전에 우선 이곳이 로비라는 것을 알려둔다.(강종되서 복구하는 중이더라도 로비에서 시작하고 복구 팝업을 띄우는게 맞다.)
		// 5. 이미 씬에 컨트롤러 캔버스 같은건 다 들어있다. 로컬 플레이어 캐릭터를 만들어야한다.
		// 6. 맵도 로드해야하는데 맵을 알기 위해선 StageManager도 필요하다.
		// - 5, 6번 스탭은 이전의 로드와 달리 동시에 이뤄져도 상관없는 항목들이다. 조금이라도 로딩 시간을 줄이기 위해 한번에 로드한다.
		// 7. 스테이지 매니저가 만들어지면 맵을 생성할 수 있으므로 로비맵을 로드한다.
		// 8. 게임에 진입할 수 있게 게이트 필라를 소환한다.(원래 몬스터 다 잡고 나오는거라 SceneBuild중에는 이렇게 직접 호출해야한다.)
		// 9. 로비 UI를 구축한다.
		// 10. 플레이어의 첫공격 렉을 없애기 위해 플레이어 액터에 등록된 캐시 오브젝트들을 하나씩 만들어낸다.
		// - 바로 다음에 오는 Update는 렌더하기 전이기 때문에 다다음에 오는 Update가 지나야 렌더링이 되었다고 판단할 수 있다. 2번 기다리자.
		// - 이제부터 하단은 로비인지 아닌지를 판단해서 처리해야한다.(강종 복구라면 로비가 아니다.)
		// 11. 앱을 켰을때인지 판단해서 (s_firstTimeAfterLaunch 사용) 타이틀 UI를 띄워준다. 페이드 연출 처리도 같이 한다. 회사 로고도 이때 같이 띄워준다.
		// 12. 필수로딩은 끝. 가운데 돌고있는 로딩과 하단 로딩게이지를 페이드로 지우고
		//
		// 13. 상자를 어싱크로 로딩해서 등장 연출과 함께 나온다. 만약에 이때 너무 이동이 느려지면 위로 올릴 수도 있다.
		// 14. 번들이 없는 채로 상자를 열려고 하거나 좌하단 메뉴를 누르면 튜토중이라거나 번들을 받아야함을 알린다.
		// 15. 현재 로비의 다음판을 미리 어싱크로 로딩한다. 1-0이라면 1-1의 Plane, Ground, Wall등 맵 정보를 모두 로딩해놔야한다.
		// 16. 게이트필라를 치는 순간 배틀매니저를 어싱크로 로딩하고 화면이 하얗게 된 상태에서 배틀매니저 및 다음판 로딩이 끝남을 체크한다. 끝나면 페이드인되면서 전투가 시작된다.
		// - 만약 이 로딩이 오래 걸려서 1초를 넘어가면 우하단에 작게 로딩중을 표시해준다.
		// - 매판 몹을 다 죽이고 게이트필라가 뜨는 순간마다 다음판의 맵 정보를 어싱크로 로딩해둔다.

		// step 1. 테이블 임시 로드
		// 지금은 우선 apk넣고 하지만 나중에 서버에서 받는거로 바꿔야한다. 이땐 확인창 안띄운다.
		LoadingCanvas.instance.SetProgressBarPoint(0.3f);
		_handleTableDataManager = Addressables.LoadAssetAsync<GameObject>("TableDataManager");
		yield return _handleTableDataManager;
		Instantiate<GameObject>(_handleTableDataManager.Result);

		// step 1-2. 옵션 매니저
		if (OptionManager.instance != null) { }

		// step 2. font & string
		UIString.instance.InitializeFont(OptionManager.instance.language);

		// step 3. login
#if PLAYFAB
		if (AuthManager.instance.IsCachedLastLoginInfo() == false)
		{
#if NEWPLAYER_LEVEL1
#if NEWPLAYER_ADD_KEEP
			PlayerData.instance.newPlayerAddKeep = true;
#endif
			// 원래라면 아래 PlayAfterInstallationCoroutine호출하는게 맞다.
			// 그러나 튜토를 나중에 만들거고 설령 지금 만든다해도 매번 튜토챕터로 시작하는게 불편해서
			// 개발용으로 쓸 신캐 생성버전을 이 디파인에 묶어서 쓰도록 한다.
			// 처음 캐릭터를 만들면 게스트로그인으로 생성되며 챕터는 1이 선택되어있고 0스테이지 로비에서 시작된다.
			float createAccountStartTime = Time.time;
			AuthManager.instance.RequestCreateGuestAccount();
			while (PlayerData.instance.loginned == false) yield return null;
			Debug.LogFormat("Create Account Time : {0:0.###}", Time.time - createAccountStartTime);
#else
			// 사이사이에 플래그 쓰면서 할까 하다가 너무 코드가 지저분해져서 그냥 따로 빼기로 한다.
			yield return PlayAfterInstallationCoroutine();
			yield break;
#endif
		}

		if (PlayerData.instance.loginned == false)
		{
			float serverLoginStartTime = Time.time;
			AuthManager.instance.LoginWithLastLoginType();
			while (PlayerData.instance.loginned == false) yield return null;
			Debug.LogFormat("Server Login Time : {0:0.###}", Time.time - serverLoginStartTime);
		}
#if !UNITY_EDITOR
		Debug.LogWarning("000000000");
#endif
#else
		// only client
		if (PlayerData.instance.loginned == false)
		{
			// login and recv player data
			PlayerData.instance.OnRecvPlayerInfoForClient();
			PlayerData.instance.OnRecvCharacterListForClient();
		}
#endif

		// 서버와 연동 후 64비트 빌드를 뽑아봤는데 하필 요 부분쯤부터-40% 근처 프로그래스바가 올라가지 않으면서 프리징 되는 현상이 발생했다.
		// 이상하게도 32비트도 정상이고 64비트인데 PLAYFAB디파인을 주석처리한 빌드도 정상인데
		// PLAYFAB 디파인 켠 빌드에서만 거의 0.5%? 수준으로 프리징이 발생한다.
		// 아무리봐도 네트워크 문제는 아닌거 같은게
		// 딱 한번 70%쯤에서 멈췄었고 한번은 100% 다 차고 로딩화면 없어지려는 알파 반투명쯤에서 프리징이 걸렸기에
		// 아무래도 Addressables.LoadAssetAsync의 문제로 의심하고 있다.
		// 로그캣으로 로그도 찍어봤는데
		// 유니티 로그는 멈추고나서 한참 후 Timeout while trying to pause the Unity Engine. 뜨는게 전부고
		// 일반 로그로 찍어봐도 정상일때랑 아닐때랑 별다른 차이가 없다.
		// 그러다가 유니티에 Addressables.LoadAssetAsync 함수 프리징 된다는 글이 있어서 보니
		// 엔진팀에서 수정중이라 한다.
		// 우선은 버전업을 해서 고쳐지길 기대하는 수밖에 없을 듯 하다.

		// step 4. set lobby
		lobby = true;
#if !UNITY_EDITOR
		Debug.LogWarning("111111111");
#endif
		_handleLobbyCanvas = Addressables.LoadAssetAsync<GameObject>("LobbyCanvas");
#if !UNITY_EDITOR
		Debug.LogWarning("222222222");
#endif
		_handleCommonCanvasGroup = Addressables.LoadAssetAsync<GameObject>("CommonCanvasGroup");
#if !UNITY_EDITOR
		Debug.LogWarning("333333333");
#endif
		if (s_firstTimeAfterLaunch)
			_handleTitleCanvas = Addressables.LoadAssetAsync<GameObject>("TitleCanvas");
#if !UNITY_EDITOR
		Debug.LogWarning("444444444");
#endif

		// step 5, 6
		LoadingCanvas.instance.SetProgressBarPoint(0.6f);
#if !UNITY_EDITOR
		Debug.LogWarning("555555555");
#endif
		_handleStageManager = Addressables.LoadAssetAsync<GameObject>("StageManager");
#if !UNITY_EDITOR
		Debug.LogWarning("666666666");
#endif
		_handleStartCharacter = Addressables.LoadAssetAsync<GameObject>(CharacterData.GetAddressByActorId(PlayerData.instance.mainCharacterId));
#if !UNITY_EDITOR
		Debug.LogWarning("777777777");
#endif
		while (!_handleStageManager.IsDone || !_handleStartCharacter.IsDone) yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("888888888");
#endif
		Instantiate<GameObject>(_handleStageManager.Result);
#if !UNITY_EDITOR
		Debug.LogWarning("888888888-1");
#endif
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(_handleStartCharacter.Result);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		Instantiate<GameObject>(_handleStartCharacter.Result);
#endif

		// 흠.. 어드레서블 에셋으로 뺐더니 5.7초까지 늘어났다. 번들에서 읽으니 어쩔 수 없는건가.

		// 그냥 Resources.Load는 4.111초 4.126초 이정도 걸린다.
		// Resoures.LoadAsync는 4.025초 정도. 로딩화면 갱신도 못하는데 느려서 안쓴다.
		//Instantiate<GameObject>(Resources.Load<GameObject>("Manager"));
		//Instantiate<GameObject>(Resources.Load<GameObject>("Character/Ganfaul"));

		// step 7. 스테이지
		// 차후에는 챕터의 0스테이지에서 시작하게 될텐데 0스테이지에서 쓸 맵을 알아내려면
		// 진입전에 아래 함수를 수행해서 캐싱할 수 있어야한다.
		// 방법은 세가지인데,
		// 1. static으로 빼서 데이터 처리만 먼저 할 수 있게 하는 방법
		// 2. DataManager 를 분리해서 데이터만 처리할 수 있게 하는 방법
		// 3. 스테이지 매니저가 언제나 살아있는 싱글톤 클래스가 되는 방법
		// 3은 다른 리소스도 들고있는데 살려둘 순 없으니 패스고 1은 너무 어거지다.
		// 결국 재부팅시 데이터 캐싱등의 처리까지 하려면 2번이 제일 낫다.
#if !UNITY_EDITOR
		Debug.LogWarning("999999999");
#endif
		LoadingCanvas.instance.SetProgressBarPoint(0.9f);
#if !UNITY_EDITOR
		Debug.LogWarning("AAAAAAAAA");
#endif
		_handleTreasureChest = Addressables.LoadAssetAsync<GameObject>("TreasureChest");
#if !UNITY_EDITOR
		Debug.LogWarning("BBBBBBBBB");
#endif
		StageManager.instance.InitializeStage(PlayerData.instance.selectedChapter, 0);
#if !UNITY_EDITOR
		Debug.LogWarning("CCCCCCCCC");
#endif
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("DDDDDDDDD");
#endif
		StageManager.instance.MoveToNextStage(true);
#if !UNITY_EDITOR
		Debug.LogWarning("EEEEEEEEE");
#endif

		// step 8. gate pillar & TreasureChest
		yield return new WaitUntil(() => waitSpawnFlag);
#if !UNITY_EDITOR
		Debug.LogWarning("FFFFFFFFF");
#endif
		if (EventManager.instance.IsStandbyServerEvent(EventManager.eServerEvent.chaos))
		{
			_handleEventGatePillar = Addressables.LoadAssetAsync<GameObject>("OpenChaosGatePillar");
			Debug.LogWarning("GGGGGGGGG-1");
			yield return _handleEventGatePillar;
			Instantiate<GameObject>(_handleEventGatePillar.Result, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
		}
		else
		{
			BattleInstanceManager.instance.GetCachedObject(GetCurrentGatePillarPrefab(), StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
			Debug.LogWarning("GGGGGGGGG");
			HitRimBlink.ShowHitRimBlink(GatePillar.instance.cachedTransform, Vector3.forward, true);
		}
#if !UNITY_EDITOR
		Debug.LogWarning("HHHHHHHHH");
#endif
		yield return _handleTreasureChest;
#if UNITY_EDITOR
		newObject = Instantiate<GameObject>(_handleTreasureChest.Result);
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		Instantiate<GameObject>(_handleTreasureChest.Result);
#endif

		// 현재맵의 로딩이 끝나면 다음맵의 프리팹을 로딩해놔야 게이트 필라로 이동시 곧바로 이동할 수 있게 된다.
		// 원래라면 몹 다 죽이고 호출되는 함수인데 초기 씬 구축에선 할 타이밍이 로비맵 로딩 직후밖에 없다.
		StageManager.instance.GetNextStageInfo();

#if !UNITY_EDITOR
		Debug.LogWarning("IIIIIIIII");
#endif
		// step 9-1. 첫번재 UI를 소환하기 전에 UIString Font의 로드가 완료되어있는지 체크해야한다.
		while (UIString.instance.IsDoneLoadAsyncFont() == false)
			yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("JJJJJJJJJ");
#endif
		// step 9-2. lobby ui
		while (!_handleLobbyCanvas.IsDone || !_handleCommonCanvasGroup.IsDone) yield return null;
#if !UNITY_EDITOR
		Debug.LogWarning("KKKKKKKKK");
#endif
		Instantiate<GameObject>(_handleLobbyCanvas.Result);
		Instantiate<GameObject>(_handleCommonCanvasGroup.Result);
#if !UNITY_EDITOR
		Debug.LogWarning("LLLLLLLLL");
#endif

		// step 10. player hit object caching
		LoadingCanvas.instance.SetProgressBarPoint(1.0f, 0.0f, true);
#if !UNITY_EDITOR
		Debug.LogWarning("MMMMMMMMM");
#endif
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], Vector3.right, Quaternion.identity));
		}
#if !UNITY_EDITOR
		Debug.LogWarning("OOOOOOOOO");
#endif

		// step 11. title ui
		if (s_firstTimeAfterLaunch)
		{
			LoadingCanvas.instance.onlyObjectFade = true;
			yield return _handleTitleCanvas;
			Instantiate<GameObject>(_handleTitleCanvas.Result);
		}

		// 마무리 셋팅
		_waitUpdateRemainCount = 2;
		mainSceneBuilding = false;
		s_firstTimeAfterLaunch = false;
	}

	GameObject GetCurrentGatePillarPrefab()
	{
		if (PlayerData.instance.currentChallengeMode && EventManager.instance.IsCompleteServerEvent(EventManager.eServerEvent.chaos))
			return StageManager.instance.challengeGatePillarPrefab;
		return StageManager.instance.gatePillarPrefab;
	}

	// Update is called once per frame
	List<GameObject> _listCachingObject = null;
	int _waitUpdateRemainCount;
    void LateUpdate()
    {
		if (_waitUpdateRemainCount > 0)
		{
			_waitUpdateRemainCount -= 1;
			if (_waitUpdateRemainCount == 0)
			{
				if (_listCachingObject != null)
				{
					for (int i = 0; i < _listCachingObject.Count; ++i)
						_listCachingObject[i].SetActive(false);
					_listCachingObject.Clear();
				}
				// step 12. fade out
				if (playAfterInstallation)
				{
					// 0챕터 1스테이지에서 시작하는거라 강제로 전투모드로 바꿔준다.
					StartCoroutine(LateInitialize());
					LobbyCanvas.instance.OnExitLobby();
					// 튜토때만 보이는 계정연동 버튼 처리
				}
				else
				{
					// 일반적인 경우엔 가운데 오브젝트만 FadeOut하고 LateInitialize를 호출해둔다.
					LoadingCanvas.instance.FadeOutObject();
					StartCoroutine(LateInitialize());
				}
			}
		}
    }

	IEnumerator LateInitialize()
	{
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
			AddressableAssetLoadManager.GetAddressableSprite(TableDataManager.instance.actorTable.dataArray[i].portraitAddress, "Icon", null);
		_handleBattleManager = Addressables.LoadAssetAsync<GameObject>("BattleManager");
		yield return _handleBattleManager;
		Instantiate<GameObject>(_handleBattleManager.Result);

		if (playAfterInstallation)
		{
			BattleManager.instance.OnSpawnFlag();
			LoadingCanvas.instance.FadeOutObject();
		}
	}

	public bool IsDoneLateInitialized()
	{
		return _handleBattleManager.IsValid();
	}

	public void OnFinishTitleCanvas()
	{
		Addressables.Release<GameObject>(_handleTitleCanvas);
	}

	public void OnExitLobby()
	{
		lobby = false;
		TreasureChest.instance.gameObject.SetActive(false);
		LobbyCanvas.instance.OnExitLobby();
		if (EnergyGaugeCanvas.instance != null)
			EnergyGaugeCanvas.instance.gameObject.SetActive(false);
		if (DailyBoxGaugeCanvas.instance != null)
			DailyBoxGaugeCanvas.instance.gameObject.SetActive(false);
		if (NewChapterCanvas.instance != null)
			NewChapterCanvas.instance.gameObject.SetActive(false);
		if (BattleInstanceManager.instance.playerActor != null)
			BattleInstanceManager.instance.playerActor.InitializeCanvas();
	}


#if PLAYFAB
#region Play After Installation
	// 설치 직후 플레이 혹은 데이터 리셋 후 플레이
	IEnumerator PlayAfterInstallationCoroutine()
	{
		playAfterInstallation = true;

		// 캐릭터 만드는 패킷
		float createAccountStartTime = Time.time;
		AuthManager.instance.RequestCreateGuestAccount();
		while (PlayerData.instance.loginned == false) yield return null;
		Debug.LogFormat("Create Account Time : {0:0.###}", Time.time - createAccountStartTime);

		// step 4. set lobby
		_handleLobbyCanvas = Addressables.LoadAssetAsync<GameObject>("LobbyCanvas");
		_handleCommonCanvasGroup = Addressables.LoadAssetAsync<GameObject>("CommonCanvasGroup");

		// step 5, 6
		LoadingCanvas.instance.SetProgressBarPoint(0.6f);
		_handleStageManager = Addressables.LoadAssetAsync<GameObject>("StageManager");
		_handleStartCharacter = Addressables.LoadAssetAsync<GameObject>(CharacterData.GetAddressByActorId(PlayerData.instance.mainCharacterId));
		while (!_handleStageManager.IsDone || !_handleStartCharacter.IsDone) yield return null;
		Instantiate<GameObject>(_handleStageManager.Result);
#if UNITY_EDITOR
		Vector3 tutorialPosition = new Vector3(BattleInstanceManager.instance.GetCachedGlobalConstantFloat("TutorialStartX"), 0.0f, BattleInstanceManager.instance.GetCachedGlobalConstantFloat("TutorialStartZ"));
		GameObject newObject = Instantiate<GameObject>(_handleStartCharacter.Result, tutorialPosition, Quaternion.identity);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		Instantiate<GameObject>(_handleStartCharacter.Result);
#endif

		// 로딩 자체를 안해버리면 handle없어서 오류 날 수 있으니 Instantiate는 안해도 로딩은 해두자.
		LoadingCanvas.instance.SetProgressBarPoint(0.9f);
		_handleTreasureChest = Addressables.LoadAssetAsync<GameObject>("TreasureChest");

		// 강제로 시작하는거니 항상 0챕터 1스테이지
		StageManager.instance.InitializeStage(0, 1);
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return null;
		StageManager.instance.MoveToNextStage(true);

		// step 8. gate pillar & TreasureChest
		yield return new WaitUntil(() => waitSpawnFlag);

		StageManager.instance.GetNextStageInfo();
		while (UIString.instance.IsDoneLoadAsyncFont() == false)
			yield return null;
		// step 9-2. lobby ui
		while (!_handleLobbyCanvas.IsDone || !_handleCommonCanvasGroup.IsDone) yield return null;
		Instantiate<GameObject>(_handleLobbyCanvas.Result);
		Instantiate<GameObject>(_handleCommonCanvasGroup.Result);

		// step 10. player hit object caching
		LoadingCanvas.instance.SetProgressBarPoint(1.0f, 0.0f, true);
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], Vector3.right, Quaternion.identity));
		}

		// 마무리 셋팅
		_waitUpdateRemainCount = 2;
		mainSceneBuilding = false;
	}
#endregion
#endif
}
