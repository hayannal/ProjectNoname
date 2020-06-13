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

// NodeWar 인게임에서 쓰는 포탈이다. 탈출할때 사용되는 포탈이라서 Exit를 붙여둔다. 생긴건 똑같이 생겼지만 처리 로직이 완전히 인게임 전용이다.
public class NodeWarExitPortal : MonoBehaviour
{
	public static NodeWarExitPortal instance;

	// 포탈 활성화까지는 30초
	public static float WaitActivePortalTime = 30.0f;

	// 포탈 활성화 되고나서부터 다시 종료될때까지 걸리는 시간
	public static float ActivePortalTime = 30.0f;

	public GameObject standbyEffectObject;
	public GameObject openingEffectObject;
	public Canvas worldCanvas;
	public CanvasGroup canvasGroup;
	public DOTweenAnimation fadeTweenAnimation;
	public Text remainTimeText;
	public Transform healAreaEffectTransform;

	public GameObject arrowIndicatorPrefab;

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		canvasGroup.alpha = 0.0f;

		// 처음 발견할땐 standbyEffect조차 꺼있는채로 발견되면 된다.
		//RefreshRemainTime();
		standbyEffectObject.SetActive(false);

		// 화살표는 ExitPortal이 관리한다.
		_arrowIndicatorTransform = Instantiate<GameObject>(arrowIndicatorPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity).transform;
	}

	void Update()
	{
		if (_enteredPortal && _openRemainTime > 0.0f && BattleInstanceManager.instance.playerActor.actorStatus.IsDie() == false)
		{
			_openRemainTime -= Time.deltaTime;
			if (_openRemainTime <= 0.0f)
			{
				_openRemainTime = 0.0f;
				Timing.RunCoroutine(MoveProcess());
			}
		}
		if (_enteredPortal && _openRemainTime > 0.0f && BattleInstanceManager.instance.playerActor.actorStatus.IsDie())
		{
			// openRemainTime이 0보다 큰 상황에서 IsDie라면 포탈을 타려다가 죽은 경우일거다.
			_enteredPortal = false;
			DisableParticleEmission.DisableEmission(openingEffectObject.transform);
		}

		UpdateArrowIndicator();
		UpdateHealArea();
		UpdateRemainTime();
		UpdateActiveTime();
	}

	Transform _arrowIndicatorTransform;
	void UpdateArrowIndicator()
	{
		_arrowIndicatorTransform.position = BattleInstanceManager.instance.playerActor.cachedTransform.position;

		Vector3 diff = cachedTransform.position - _arrowIndicatorTransform.position;
		diff.y = 0.0f;
		Quaternion lookRotation = Quaternion.LookRotation(diff);
		_arrowIndicatorTransform.rotation = Quaternion.Slerp(_arrowIndicatorTransform.rotation, lookRotation, 4.0f * Time.deltaTime);

		bool close = (diff.sqrMagnitude < 1.7f * 1.7f);
		_arrowIndicatorTransform.GetChild(0).gameObject.SetActive(!close);
	}

	// 힐영역 시작범위는 9m
	const float BaseHealAreaRange = 9.0f;
	// 힐영역 시작 스케일값은 4.0
	const float BaseHealAreaScaleX = 4.0f;
	// 힐영역 시간 및 Tick
	const float HealAreaTime = 50.0f;
	const float HealAreaTick = 0.3333f;
	float _healAreaRemainTime;
	float _healAreaTickRemainTime;
	AffectorValueLevelTableData _healAreaAffectorValue;
	void UpdateHealArea()
	{
		if (_healAreaAffectorValue == null)
		{
			_healAreaAffectorValue = new AffectorValueLevelTableData();
			_healAreaAffectorValue.fValue3 = 0.15f;
		}

		// 시간체크
		if (_healAreaRemainTime <= 0.0f)
			return;

		_healAreaRemainTime -= Time.deltaTime;
		if (_healAreaRemainTime <= 0.0f)
		{
			healAreaEffectTransform.gameObject.SetActive(false);
			_healAreaRemainTime = 0.0f;
			return;
		}

		// Heal Tick 체크
		if (_healAreaTickRemainTime > 0.0f)
		{
			_healAreaTickRemainTime -= Time.deltaTime;
			if (_healAreaTickRemainTime <= 0.0f)
				_healAreaTickRemainTime = 0.0f;
			return;
		}
		_healAreaTickRemainTime += HealAreaTick;

		// 거리체크
		// 거리를 재는건 오브젝트의 스케일을 받아다가 사용하기로 한다.
		float scale = healAreaEffectTransform.localScale.x;
		// 오브젝트의 스케일을 바탕으로 월드좌표계의 스케일을 구한다.
		float worldRange = scale / BaseHealAreaScaleX * BaseHealAreaRange;
		Vector3 diff = cachedTransform.position - BattleInstanceManager.instance.playerActor.cachedTransform.position;
		diff.y = 0.0f;
		if (diff.sqrMagnitude < worldRange * worldRange)
			BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, _healAreaAffectorValue, BattleInstanceManager.instance.playerActor, false);
	}

	float _remainTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;
		if (canvasGroup.alpha == 0.0f)
			return;

		_remainTime -= Time.deltaTime;
		if (_remainTime > 0.0f)
		{
			if (_lastRemainTimeSecond != (int)_remainTime)
			{
				remainTimeText.text = string.Format("00:00:{0:00}", (int)_remainTime + 1);
				_lastRemainTimeSecond = (int)_remainTime;
			}
		}
		else
		{
			BattleManager.instance.OnActiveExitPortal();
			standbyEffectObject.SetActive(true);
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			fadeTweenAnimation.DORestart();
			_activeRemainTime = ActivePortalTime;
		}
	}

	// 포탈은 한번 열리고나면 30초 동안의 기회를 준다. 이게 지나면 도로 닫히면서 실패로 처리된다.
	float _activeRemainTime;
	void UpdateActiveTime()
	{
		if (_activeRemainTime > 0.0f)
		{
			_activeRemainTime -= Time.deltaTime;
			if (_activeRemainTime <= 0.0f)
			{
				standbyEffectObject.SetActive(false);
				if (_enteredPortal)
				{
					_enteredPortal = false;
					DisableParticleEmission.DisableEmission(openingEffectObject.transform);
				}

				// 아예 processing 상태로 바꿔버려서 진행이 안되도록 막아둔다.
				_processing = true;
			}
		}
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

		// ExitPortal을 찾고있는 중이었다면 standbyEffect와 canvasGroup 모두 보이지 않는 상태였을거다.
		if (standbyEffectObject.activeSelf == false && canvasGroup.alpha == 0.0f)
		{
			BattleManager.instance.OnTryRepairExitPortal();
			canvasGroup.alpha = 1.0f;
			_remainTime = WaitActivePortalTime;
			_needUpdate = true;
			remainTimeText.gameObject.SetActive(true);
			_healAreaRemainTime = HealAreaTime;
			healAreaEffectTransform.gameObject.SetActive(true);
			Timing.RunCoroutine(DelayedHealAreaInfoText());
			return;
		}

		if (standbyEffectObject.activeSelf && canvasGroup.alpha == 0.0f)
			StartOpen();
	}

	IEnumerator<float> DelayedHealAreaInfoText()
	{
		yield return Timing.WaitForSeconds(15.0f);
		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarPortalHealWeak"), 2.5f);
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

		if (standbyEffectObject.activeSelf && canvasGroup.alpha == 0.0f)
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

		if (standbyEffectObject.activeSelf && canvasGroup.alpha == 0.0f && _enteredPortal)
		{
			_enteredPortal = false;
			//openingEffectObject.SetActive(false);
			DisableParticleEmission.DisableEmission(openingEffectObject.transform);
		}
	}

	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> MoveProcess(bool inProgressGame = false)
	{
		if (_processing)
			yield break;

		_processing = true;

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

		CustomRenderer.instance.bloom.ResetDirtIntensity();

		// 유일하게 전투중에 타는 포탈이라서 Die검사를 해야한다.
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie())
		{
			FadeCanvas.instance.FadeIn(0.4f);
			// 알파가 어느정도 빠지면 _processing을 풀어준다.
			yield return Timing.WaitForSeconds(0.2f);
			_processing = false;
			yield break;
		}

		// position 이동. 안전지대쪽으로 보내야한다.
		BattleManager.instance.OnSuccessExitPortal();
		BattleInstanceManager.instance.playerActor.cachedTransform.position = NodeWarProcessor.EndSafeAreaPosition;
		TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 15);
		CustomFollowCamera.instance.immediatelyUpdate = true;

		gameObject.SetActive(false);

		// 도착하자마자 잠시 후에 성공 결과창을 띄울테니 너무 오래 보여주진 않는다.
		FadeCanvas.instance.FadeIn(1.0f);

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