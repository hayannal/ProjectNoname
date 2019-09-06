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

	// 서버에서 받는거로 고치기 전까지만 쓰는 임시값이다.
	// temp code
	public int playChapter = 1;
	public int playStage = 0;
	public int lastClearChapter = 1;
	public int lastClearStage = 0;

	void Awake()
	{
		instance = this;
	}

	public bool mainSceneBuilding { get; private set; }
	public bool waitSpawnFlag { get; set; }
	public bool lobby { get; private set; }

	void OnDestroy()
	{
		Addressables.Release<GameObject>(_handleTableDataManager);
		Addressables.Release<GameObject>(_handleStageManager);
		Addressables.Release<GameObject>(_handleStartCharacter);

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
		// 4. 데이터들을 로드하기전에 우선 이곳이 로비라는 것을 알려둔다.(그러나 강종되서 복구하는 중이라면 false로 체크한다.)
		// 5. 이미 씬에 컨트롤러 캔버스 같은건 다 들어있다. 로컬 플레이어 캐릭터를 만들어야한다.
		// 6. 맵도 로드해야하는데 맵을 알기 위해선 StageManager도 필요하다.
		// - 5, 6번 스탭은 이전의 로드와 달리 동시에 이뤄져도 상관없는 항목들이다. 조금이라도 로딩 시간을 줄이기 위해 한번에 로드한다.
		// 7. 스테이지 매니저가 만들어지면 맵을 생성할 수 있으므로 로비맵을 로드한다.
		// 8. 게임에 진입할 수 있게 게이트 필라를 소환한다.(원래 몬스터 다 잡고 나오는거라 SceneBuild중에는 이렇게 직접 호출해야한다.)
		// 9. 플레이어의 첫공격 렉을 없애기 위해 플레이어 액터에 등록된 캐시 오브젝트들을 하나씩 만들어낸다.
		// - 바로 다음에 오는 Update는 렌더하기 전이기 때문에 다다음에 오는 Update가 지나야 렌더링이 되었다고 판단할 수 있다. 2번 기다리자.
		// - 이제부터 하단은 로비인지 아닌지를 판단해서 처리해야한다.(강종 복구라면 로비가 아니다.)
		// 11. 좌하단 메뉴 진입 UI를 보여준다.
		// 12. 설정버튼은 우상단이다.(이 안에 연동버튼도 있다.)
		// 13. 필수로딩은 끝. 가운데 돌고있는 로딩과 하단 로딩게이지를 페이드로 지우고
		// 14. 앱을 켰을때인지 판단해서 (s_firstTimeAfterLaunch 사용) 타이틀 UI를 띄워준다. 페이드 연출 처리도 같이 한다. 회사 로고도 이때 같이 띄워준다.
		//
		// 15. 상자를 어싱크로 로딩해서 등장 연출과 함께 나온다. 만약에 이때 너무 이동이 느려지면 위로 올릴 수도 있다.
		// 16. 번들이 없는 채로 상자를 열려고 하거나 좌하단 메뉴를 누르면 튜토중이라거나 번들을 받아야함을 알린다.
		// 17. 현재 로비의 다음판을 미리 어싱크로 로딩한다. 1-0이라면 1-1의 Plane, Ground, Wall등 맵 정보를 모두 로딩해놔야한다.
		// 18. 게이트필라를 치는 순간 배틀매니저를 어싱크로 로딩하고 화면이 하얗게 된 상태에서 배틀매니저 및 다음판 로딩이 끝남을 체크한다. 끝나면 페이드인되면서 전투가 시작된다.
		// - 만약 이 로딩이 오래 걸려서 1초를 넘어가면 우하단에 작게 로딩중을 표시해준다.
		// - 매판 몹을 다 죽이고 게이트필라가 뜨는 순간마다 다음판의 맵 정보를 어싱크로 로딩해둔다.
		// - 근데 만약 도중플레이를 끊어먹을정도로 느리면 초반에 전부다 로딩하는 구조로 바꿔야한다.
		// 19. 교체 가능은 이동후에 뜬다.

		// step 1. 테이블 임시 로드
		// 지금은 우선 apk넣고 하지만 나중에 서버에서 받는거로 바꿔야한다. 이땐 확인창 안띄운다.
		LoadingCanvas.instance.SetProgressBarPoint(0.3f);
		_handleTableDataManager = Addressables.LoadAssetAsync<GameObject>("TableDataManager");
		yield return _handleTableDataManager;
		Instantiate<GameObject>(_handleTableDataManager.Result);

		// step 2. string

		// step 3. temp login
		if (PlayerData.instance.loginned == false)
		{

		}

		// step 4. set lobby
		lobby = true;

		// step 5, 6, 7
		// 차후에 5는 캐릭터 아이디에 따라 번들에서 로드해야할거다.
		LoadingCanvas.instance.SetProgressBarPoint(0.6f);
		_handleStageManager = Addressables.LoadAssetAsync<GameObject>("StageManager");
		_handleStartCharacter = Addressables.LoadAssetAsync<GameObject>("Ganfaul");
		while (!_handleStageManager.IsDone || !_handleStartCharacter.IsDone) yield return null;
		Instantiate<GameObject>(_handleStageManager.Result);
		GameObject newObject = Instantiate<GameObject>(_handleStartCharacter.Result);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif

		// 흠.. 어드레서블 에셋으로 뺐더니 5.7초까지 늘어났다. 번들에서 읽으니 어쩔 수 없는건가.

		// 그냥 Resources.Load는 4.111초 4.126초 이정도 걸린다.
		// Resoures.LoadAsync는 4.025초 정도. 로딩화면 갱신도 못하는데 느려서 안쓴다.
		//Instantiate<GameObject>(Resources.Load<GameObject>("Manager"));
		//Instantiate<GameObject>(Resources.Load<GameObject>("Character/Ganfaul"));

		// step 8. 스테이지
		// 차후에는 챕터의 0스테이지에서 시작하게 될텐데 0스테이지에서 쓸 맵을 알아내려면
		// 진입전에 아래 함수를 수행해서 캐싱할 수 있어야한다.
		// 방법은 세가지인데,
		// 1. static으로 빼서 데이터 처리만 먼저 할 수 있게 하는 방법
		// 2. DataManager 를 분리해서 데이터만 처리할 수 있게 하는 방법
		// 3. 스테이지 매니저가 언제나 살아있는 싱글톤 클래스가 되는 방법
		// 3은 다른 리소스도 들고있는데 살려둘 순 없으니 패스고 1은 너무 어거지다.
		// 결국 재부팅시 데이터 캐싱등의 처리까지 하려면 2번이 제일 낫다.
		LoadingCanvas.instance.SetProgressBarPoint(0.9f);
		StageManager.instance.InitializeStage(playChapter, playStage, lastClearChapter, lastClearStage);
		while (StageManager.instance.IsDoneLoadAsyncNextStage() == false)
			yield return null;
		StageManager.instance.MoveToNextStage(true);

		// step 9. gate pillar
		yield return new WaitUntil(() => waitSpawnFlag);
		BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

		// 현재맵의 로딩이 끝나면 다음맵의 프리팹을 로딩해놔야 게이트 필라로 이동시 곧바로 이동할 수 있게 된다.
		// 원래라면 몹 다 죽이고 호출되는 함수인데 초기 씬 구축에선 할 타이밍이 로비맵 로딩 직후밖에 없다.
		StageManager.instance.GetNextStageInfo();

		// step 10. player hit object caching
		LoadingCanvas.instance.SetProgressBarPoint(1.0f, 0.0f, true);
		if (BattleInstanceManager.instance.playerActor.cachingObjectList != null && BattleInstanceManager.instance.playerActor.cachingObjectList.Length > 0)
		{
			_listCachingObject = new List<GameObject>();
			for (int i = 0; i < BattleInstanceManager.instance.playerActor.cachingObjectList.Length; ++i)
				_listCachingObject.Add(BattleInstanceManager.instance.GetCachedObject(BattleInstanceManager.instance.playerActor.cachingObjectList[i], null));
		}

		if (lobby)
		{
			// step 11. main ui

			// step 12. box

			// step 13. title ui
			if (s_firstTimeAfterLaunch)
			{

			}
		}

		_waitUpdateRemainCount = 2;
		mainSceneBuilding = false;
		s_firstTimeAfterLaunch = false;
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
				LoadingCanvas.instance.FadeOut();
				StartCoroutine(LateInitialize());
			}
		}
    }

	IEnumerator LateInitialize()
	{
		_handleBattleManager = Addressables.LoadAssetAsync<GameObject>("BattleManager");
		yield return _handleBattleManager;
		Instantiate<GameObject>(_handleBattleManager.Result);

		yield return new WaitForSeconds(3.0f);
		if (lobby)
			PlayerIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform);
	}

	public bool IsDoneLateInitialized()
	{
		return _handleBattleManager.IsValid();
	}

	public void OnExitLobby()
	{
		lobby = false;
		if (BattleInstanceManager.instance.playerActor != null)
			BattleInstanceManager.instance.playerActor.InitializeCanvas();
	}
}
