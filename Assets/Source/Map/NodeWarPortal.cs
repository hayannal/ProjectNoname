using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using DG.Tweening;

public class NodeWarPortal : MonoBehaviour
{
	public static NodeWarPortal instance;

	public GameObject standbyEffectObject;
	public GameObject openingEffectObject;
	public Canvas worldCanvas;
	public CanvasGroup canvasGroup;
	public DOTweenAnimation fadeTweenAnimation;
	public Text remainTimeText;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		canvasGroup.alpha = 0.0f;
		RefreshRemainTime();
	}

	void RefreshRemainTime()
	{
		if (PlayerData.instance.nodeWarCleared)
		{
			_nextResetDateTime = PlayerData.instance.nodeWarResetTime;
			_needUpdate = true;
			remainTimeText.gameObject.SetActive(true);
			standbyEffectObject.SetActive(false);
		}
		else
		{
			remainTimeText.gameObject.SetActive(false);
			standbyEffectObject.SetActive(true);
		}
	}

	float _canvasRemainTime = 0.0f;
	void Update()
	{
		if (_enteredPortal && _openRemainTime > 0.0f)
		{
			_openRemainTime -= Time.deltaTime;
			if (_openRemainTime <= 0.0f)
			{
				_openRemainTime = 0.0f;
				Timing.RunCoroutine(MoveProcess());
			}
		}

		if (_canvasRemainTime > 0.0f)
		{
			_canvasRemainTime -= Time.deltaTime;
			if (_canvasRemainTime <= 0.0f)
			{
				_canvasRemainTime = 0.0f;
				fadeTweenAnimation.DORestart();
			}
		}

		UpdateRemainTime();
		UpdateRefresh();
	}

	DateTime _nextResetDateTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;
		if (canvasGroup.alpha == 0.0f)
			return;

		if (ServerTime.UtcNow < _nextResetDateTime)
		{
			TimeSpan remainTime = _nextResetDateTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (PlayerData.instance.nodeWarCleared == false)
		{
			RefreshRemainTime();
			_needRefresh = false;
		}
	}

	void ResetFlagForServerFailure()
	{
		//changeEffectParticleRootObject.SetActive(false);
	}

	public bool enteredPortal { get { return _enteredPortal; } }
	bool _enteredPortal = false;
	const float PortalOpenTime = 4.5f;
	float _openRemainTime;
	void OnTriggerEnter(Collider other)
	{
		if (_processing)
			return; 

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;
		if (DelayedLoadingCanvas.IsShow())
			return;
		if (GatePillar.instance.processing)
			return;
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.processing)
			return;
		if (RandomBoxScreenCanvas.instance != null && RandomBoxScreenCanvas.instance.gameObject.activeSelf)
			return;

		if (PlayerData.instance.nodeWarCleared)
		{
			canvasGroup.alpha = 1.0f;
			_canvasRemainTime = 5.0f;
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarDone"), 2.0f);
			return;
		}

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.OnClickBackButton();

		StartOpen();
	}

	void OnTriggerStay(Collider other)
	{
		if (_processing)
			return;
		if (_enteredPortal)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;
		if (DelayedLoadingCanvas.IsShow())
			return;
		if (GatePillar.instance.processing)
			return;
		if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.processing)
			return;
		if (RandomBoxScreenCanvas.instance != null && RandomBoxScreenCanvas.instance.gameObject.activeSelf)
			return;

		if (PlayerData.instance.nodeWarCleared)
			return;

		StartOpen();
	}

	void StartOpen()
	{
		_enteredPortal = true;
		_openRemainTime = PortalOpenTime;
		openingEffectObject.SetActive(false);
		openingEffectObject.SetActive(true);
	}

	void OnTriggerExit(Collider other)
	{
		if (_processing)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		_enteredPortal = false;
		//openingEffectObject.SetActive(false);
		DisableParticleEmission.DisableEmission(openingEffectObject.transform);
	}


	// 포탈에서 이동할때 할일 리스트
	// 1. 하루 1회 클리어시 체크. 클리어 안하고 끄면 재도전 가능.
	// 2. 번쩍이는 화이트는 게이트 필라때와 똑같이 있는건가? 혹은 게이트 구슬이 없으니 그거 대신 화면 페이드만 사용하나?


	// 패킷을 날려놓고 페이드아웃쯤에 오는 서버 응답에 따라 처리가 나뉜다. 
	bool _waitEnterServerResponse;
	bool _enterNodeWarServerFailure;
	bool _networkFailure;
	void PrepareNodeWar()
	{
		// 입장패킷 보내서 서버로부터 제대로 응답오는지 기다려야한다.
		PlayFabApiManager.instance.RequestEnterNodeWar((serverFailure) =>
		{
			if (_waitEnterServerResponse)
			{
				// 오늘 클리어 했는데 또 도전
				_enterNodeWarServerFailure = serverFailure;
				_waitEnterServerResponse = false;
			}
		}, () =>
		{
			if (_waitEnterServerResponse)
			{
				// 그외 접속불가 네트워크 에러
				_networkFailure = true;
				_waitEnterServerResponse = false;
			}
		});
		_waitEnterServerResponse = true;
	}

	GameObject _nodeWarGroundPrefab = null;
	GameObject _randomNodeWarPlanePrefab = null;
	GameObject _randomNodeWarEnvPrefab = null;
	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> MoveProcess(bool inProgressGame = false)
	{
		if (_processing)
			yield break;

		_processing = true;

		// 기존과 달리 여기선 처음 진입 후 나오는 영역 이펙트 말고도 Plane은 랜덤맵 형태로 해야한다.
		// 그래서 체험모드처럼 영역 이펙트 가지고 있는 Ground 하나를 기본으로 깔고
		// 추가로 랜덤맵처럼 쓸 Plane을 로드해서 이 Ground에 전달하기로 한다.
		if (_nodeWarGroundPrefab == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("NodeWarGround", "Map", (prefab) =>
			{
				_nodeWarGroundPrefab = prefab;
			});
		}

		if (_randomNodeWarPlanePrefab == null)
		{
			// 테이블에서 누적 후 랜덤하게 뽑아서 로드
			AddressableAssetLoadManager.GetAddressableGameObject(NodeWarGround.GetRandomPlaneAddress(), "Map", (prefab) =>
			{
				_randomNodeWarPlanePrefab = prefab;
			});
		}

		if (_randomNodeWarEnvPrefab == null)
		{
			// 환경까지 로드해서 넘겨야 NodeWarGround띄우는 타이밍에 딱 맞춰서 교체할 수 있다.
			AddressableAssetLoadManager.GetAddressableGameObject(NodeWarGround.GetRandomEnvAddress(), "Map", (prefab) =>
			{
				_randomNodeWarEnvPrefab = prefab;
			});
		}

		// 보안 이슈로 Enter Flag는 받아둔다. 기존꺼랑 겹치지 않게 별도의 enterFlag다.
		PrepareNodeWar();

		//yield return Timing.WaitForSeconds(0.2f);
		//changeEffectParticleRootObject.SetActive(true);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(gameObject);
#endif
		CustomRenderer.instance.bloom.AdjustDirtIntensity(1.5f);

		yield return Timing.WaitForSeconds(0.5f);

		FadeCanvas.instance.FadeOut(0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		while (_waitEnterServerResponse)
			yield return Timing.WaitForOneFrame;
		if (_enterNodeWarServerFailure || _networkFailure)
		{
			ResetFlagForServerFailure();
			FadeCanvas.instance.FadeIn(0.4f);
			if (_enterNodeWarServerFailure)
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarDone"), 2.0f);
			_enterNodeWarServerFailure = false;
			_networkFailure = false;
			// 알파가 어느정도 빠지면 _processing을 풀어준다.
			yield return Timing.WaitForSeconds(0.2f);
			_processing = false;
			yield break;
		}
		while (MainSceneBuilder.instance.IsDoneLateInitialized() == false)
			yield return Timing.WaitForOneFrame;
		if (TitleCanvas.instance != null)
			TitleCanvas.instance.gameObject.SetActive(false);
		if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
			GatePillar.instance.gameObject.SetActive(false);
		MainSceneBuilder.instance.OnExitLobby();
		BattleManager.instance.Initialize(BattleManager.eBattleMode.NodeWar);
		BattleManager.instance.OnStartBattle();

		while (_nodeWarGroundPrefab == null || _randomNodeWarPlanePrefab == null || _randomNodeWarEnvPrefab == null)
			yield return Timing.WaitForOneFrame;
		CustomRenderer.instance.bloom.ResetDirtIntensity();
		StageManager.instance.DeactiveCurrentMap();
		Instantiate<GameObject>(_nodeWarGroundPrefab, Vector3.zero, Quaternion.identity);
		NodeWarGround.instance.InitializeGround(_randomNodeWarPlanePrefab, _randomNodeWarEnvPrefab);
		gameObject.SetActive(false);

		// 레벨업 이펙트 나올테니 평소보다 조금 더 길게 보여준다.
		FadeCanvas.instance.FadeIn(1.8f);

		_processing = false;
	}





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
