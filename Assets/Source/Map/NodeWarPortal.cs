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

	void OnDisable()
	{
		HideIndicator();
	}

	public void RefreshRemainTime()
	{
		// 재오픈 여부와 상관없이 nodeWarCleared 상태만 보고 처리. 클리어를 할 수 있는 상태면 대기 이펙트를 보여준다.
		if (PlayerData.instance.nodeWarCleared)
		{
			_nextResetDateTime = PlayerData.instance.nodeWarResetTime;
			_needUpdate = true;
			remainTimeText.gameObject.SetActive(true);
			standbyEffectObject.SetActive(false);
		}
		else
		{
			_needUpdate = false;
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

		if (ServerTime.UtcNow < _nextResetDateTime)
		{
			if (canvasGroup.alpha > 0.0f)
			{
				TimeSpan remainTime = _nextResetDateTime - ServerTime.UtcNow;
				if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
				{
					remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			_needRefresh = true;

			// 만약 갱신되는 타이밍에 포탈 위에 서있는 상태고 인디케이터가 떠있는 상태라면 지워줘야한다.
			if (_showIndicator)
			{
				HideIndicator();
				return;
			}
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

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
		{
			// DotMainMenu가 스택 내부에 있는거라면 캐시샵이든 캐릭터창이든 열고있다는 얘기다. 이땐 처리할 필요가 없다.
			if (StackCanvas.IsInStack(DotMainMenuCanvas.instance.gameObject))
				return;
		}

		if (PlayerData.instance.nodeWarCleared && PlayerData.instance.nodeWarAgainOpened)
		{
			canvasGroup.alpha = 1.0f;
			_canvasRemainTime = 5.0f;
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarDone"), 2.0f);
			return;
		}

		if (TimeSpaceData.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		if (PlayerData.instance.nodeWarCleared && PlayerData.instance.nodeWarAgainOpened == false)
		{
			canvasGroup.alpha = 1.0f;
			_canvasRemainTime = 5.0f;

			TimeSpan remainTimeSpan = _nextResetDateTime - ServerTime.UtcNow;
			if (remainTimeSpan < TimeSpan.FromMinutes(5.0))
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarNotEnoughTime"), 2.0f);
			else
				ShowIndicator();
			return;
		}

		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
		{
			// 그게 아니라 최상위에 있는거라면 자동으로 닫아주면 된다. 클리어 한 당일에는 할필요가 없어서 아래쪽에서 처리.
			DotMainMenuCanvas.instance.OnClickBackButton();
		}

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
		if (QuestSelectCanvas.instance != null && QuestSelectCanvas.instance.gameObject.activeSelf)
			return;
		if (QuestInfoCanvas.instance != null && QuestInfoCanvas.instance.gameObject.activeSelf)
			return;
		if (QuestEndCanvas.instance != null && QuestEndCanvas.instance.gameObject.activeSelf)
			return;
		if (StackCanvas.IsStacked())
			return;

		if (PlayerData.instance.nodeWarCleared)
			return;
		if (TimeSpaceData.instance.IsInventoryVisualMax())
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

		if (_showIndicator)
		{
			HideIndicator();
			return;
		}

		_enteredPortal = false;
		//openingEffectObject.SetActive(false);
		if (openingEffectObject.activeSelf)
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
		if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf && GatePillar.instance.processing)
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

		LobbyCanvas.instance.FadeOutQuestInfoGroup(0.0f, 0.2f, true);
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

		#region Effect Preload
		// 무적 버프 이펙트가 먹을때 유난히 끊겨서 캐싱을 해보기로 한다.
		SoundManager.instance.SetUiVolume(0.0f);
		yield return Timing.WaitForOneFrame;
		GameObject effectObject = BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.invincibleBuffEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity);
		yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;
		effectObject.SetActive(false);
		// 캐싱 오브젝트 끌때 바로 복구
		SoundManager.instance.SetUiVolume(OptionManager.instance.systemVolume);
		#endregion

		// 레벨업 이펙트 나올테니 평소보다 조금 더 길게 보여준다.
		FadeCanvas.instance.FadeIn(1.8f);

		_processing = false;
	}



	ObjectIndicatorCanvas _objectIndicatorCanvas;
	bool _showIndicator;
	void ShowIndicator()
	{
		_showIndicator = true;

		AddressableAssetLoadManager.GetAddressableGameObject("NodeWarEnterAgainIndicator", "Canvas", (prefab) =>
		{
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeInHierarchy == false) return;
			if (_showIndicator == false) return;

			_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;
		});
	}

	public void HideIndicator()
	{
		if (_objectIndicatorCanvas != null && _objectIndicatorCanvas.gameObject.activeSelf)
			_objectIndicatorCanvas.gameObject.SetActive(false);
		_showIndicator = false;
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
