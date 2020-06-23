using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;
using DG.Tweening;

// NodeWar 인게임에서 쓰는 탈출용 마법진.
public class NodeWarExitArea : MonoBehaviour
{
	public static NodeWarExitArea instance;

	// 희생양
	const int SacrificeMax = 100;

	public Transform areaBaseTransform;
	public Transform areaOverTransform;
	public GameObject sacrificeEffectPrefab;
	public GameObject outAreaAttackEffectPrefab;
	public GameObject areaActiveEffectPrefab;
	public Canvas worldCanvas;
	public CanvasGroup canvasGroup;
	public DOTweenAnimation fadeTweenAnimation;
	public Text sacrificeCountText;

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

		// 화살표는 ExitArea가 관리한다.
		_arrowIndicatorTransform = Instantiate<GameObject>(arrowIndicatorPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity).transform;

		// 생성되는 타이밍에 플레이 중인 캐릭터의 MonsterBoost 타이밍을 얻어온다.
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(BattleInstanceManager.instance.playerActor.actorId);
		_currentLastCountBase = actorTableData.nodeWarLastCount;
		if (_currentLastCountBase >= SacrificeMax)
			CheatingListener.OnDetectCheatTable();
	}

	void Update()
	{
		UpdateArrowIndicator();
		UpdateSacrificeArea();
		//UpdateRemainTime();
	}

	Transform _arrowIndicatorTransform;
	void UpdateArrowIndicator()
	{
		if (_arrowIndicatorTransform.gameObject.activeSelf == false)
			return;

		_arrowIndicatorTransform.position = BattleInstanceManager.instance.playerActor.cachedTransform.position;

		Vector3 diff = cachedTransform.position - _arrowIndicatorTransform.position;
		diff.y = 0.0f;
		Quaternion lookRotation = Quaternion.LookRotation(diff);
		_arrowIndicatorTransform.rotation = Quaternion.Slerp(_arrowIndicatorTransform.rotation, lookRotation, 4.0f * Time.deltaTime);

		bool close = (diff.sqrMagnitude < 3.0f * 3.0f);
		_arrowIndicatorTransform.GetChild(0).gameObject.SetActive(!close);
	}

	// 영역 안에 들어있는지는 항상 검사한다.
	// 장판이 켜지고나서 영역 밖으로 나가면 일정 주기마다 데미지를 강제로 입힌다.
	const float OutAreaDamageTimeMin = 3.0f;
	const float OutAreaDamageTimeMax = 8.0f;
	const float OutAreaMultiAtk = 1.1f;
	bool _outAreaForText;
	float _outAreaRemainTime;
	// 힐영역 시작범위는 9m
	const float BaseHealAreaRange = 9.0f;
	// 힐영역 시작 스케일값은 4.0
	const float BaseHealAreaScaleX = 4.0f;
	// 힐영역 시간 및 Tick
	const float HealAreaTick = 0.6666f;
	float _healAreaTickRemainTime;
	AffectorValueLevelTableData _healAreaAffectorValue;
	AffectorValueLevelTableData _damageAreaAffectorValue;
	bool inAreaForEffect = false;
	float _lastHealAreaWorldRange;
	public float lastHealAreaRange { get { return _lastHealAreaWorldRange; } }
	void UpdateSacrificeArea()
	{
		if (_triggerEntered == false)
			return;
		if (_killProcessed)
			return;

		// 거리체크. 거리를 재는건 마법진 오브젝트의 스케일을 받아다가 사용하기로 한다. TweenAnimation으로 알아서 크기가 변하고 있다.
		float scale = healAreaEffectTransform.localScale.x;
		// 오브젝트의 스케일을 바탕으로 월드좌표계의 스케일을 구한다.
		float worldRange = scale / BaseHealAreaScaleX * BaseHealAreaRange;
		_lastHealAreaWorldRange = worldRange;

		bool inRange = IsInHealAreaRange();
		if (inRange)
		{
		}
		else
		{
			// 범위 밖으로 나갔을때 최초 1회에는 경고메세지를 보여준다.
			if (_outAreaForText == false)
			{
				_outAreaForText = true;
				BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarWarningOutside"), 2.5f);
			}

			// 범위 밖으로 나가면 나간 시간을 누적해둔다.
			if (_outAreaRemainTime > 0.0f)
			{
				_outAreaRemainTime -= Time.deltaTime;
				if (_outAreaRemainTime <= 0.0f)
				{
					_outAreaRemainTime += UnityEngine.Random.Range(OutAreaDamageTimeMin, OutAreaDamageTimeMax);

					if (_damageAreaAffectorValue == null)
					{
						_damageAreaAffectorValue = new AffectorValueLevelTableData();
						_damageAreaAffectorValue.fValue1 = OutAreaMultiAtk;
						_damageAreaAffectorValue.iValue1 = 1;
					}
					BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.CollisionDamage, _damageAreaAffectorValue, BattleInstanceManager.instance.playerActor, false);
					BattleInstanceManager.instance.GetCachedObject(outAreaAttackEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity);
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// 이미 여기서 거리검사를 했기 때문에 괜히 Update함수 하나 더 파서 힐 처리하면 부하만 늘어날거라 같이 처리하기로 한다.
		if (_healAreaAffectorValue == null)
		{
			_healAreaAffectorValue = new AffectorValueLevelTableData();
			_healAreaAffectorValue.fValue3 = 0.3f;
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

		if (inRange)
		{
			BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, _healAreaAffectorValue, BattleInstanceManager.instance.playerActor, false);

			// 처음 area안으로 들어올때 스크린 이펙트를 보여준다. 최초 1회만 보여준다.
			if (inAreaForEffect == false)
			{
				inAreaForEffect = true;
				BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.healEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);
				Timing.RunCoroutine(ScreenHealEffectProcess());
			}
		}
	}

	public bool IsInHealAreaRange()
	{
		Vector3 diff = cachedTransform.position - BattleInstanceManager.instance.playerActor.cachedTransform.position;
		diff.y = 0.0f;
		return (diff.sqrMagnitude < _lastHealAreaWorldRange * _lastHealAreaWorldRange);
	}

	IEnumerator<float> ScreenHealEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.6f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		FadeCanvas.instance.FadeIn(1.0f);
	}

	int _sacrificeCount;
	public void OnSacrifice(MonsterActor monsterActor)
	{
		if (_sacrificeCount >= SacrificeMax)
			return;

		++_sacrificeCount;

		BattleInstanceManager.instance.GetCachedObject(sacrificeEffectPrefab, monsterActor.cachedTransform.position, Quaternion.identity);
		sacrificeCountText.text = string.Format("{0:N0} / {1:N0}", _sacrificeCount, SacrificeMax);

		if (_sacrificeCount == SacrificeMax)
		{
			// 여기서 마법진 활성화 시키는 코드 호출
			Timing.RunCoroutine(KillProcess());
		}
	}

	int _currentLastCountBase = 0;
	public bool AvailableLastCount()
	{
		if (_sacrificeCount < _currentLastCountBase)
			return false;
		return true;
	}

	float _remainTime;
	bool _needUpdate = false;
	float[] AlarmTimeList = { 15.0f, 10.0f, 5.0f };
	bool[] _alarmShowList = { false, false, false };
	void UpdateRemainTime()
	{
		// 샘플용으로 남겨둔다. 현재 호출되진 않는다.
		if (_needUpdate == false)
			return;
		if (canvasGroup.alpha == 0.0f)
			return;

		_remainTime -= Time.deltaTime * 3.0f;
		if (_remainTime > 0.0f)
		{
			for (int i = 0; i < _alarmShowList.Length; ++i)
			{
				if (_alarmShowList[i] == false && _remainTime <= AlarmTimeList[i])
				{
					// 마지막 알람의 경우에는 표시하지 않은채 몬스터 스폰 증폭되는 트리거로 사용된다.
					//if (i == (_alarmShowList.Length - 1))
					//	BattleManager.instance.On5SecondAgoActiveExitPortal();
					//else
					//	BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarActivatingTime", (int)_remainTime + 1), 1.7f);
					_alarmShowList[i] = true;
				}
			}
		}
		else
		{
			//BattleManager.instance.OnActiveExitPortal();
			//standbyEffectObject.SetActive(true);
			_needUpdate = false;
			fadeTweenAnimation.DORestart();
		}
	}

	bool _triggerEntered = false;
	void OnTriggerEnter(Collider other)
	{
		if (_triggerEntered)
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

		// base는 안건드리기로 한다. Over 이펙트만 위에 추가로 보여준다.
		//DisableParticleEmission.DisableEmission(areaBaseTransform);
		areaOverTransform.gameObject.SetActive(true);

		BattleManager.instance.OnTryActiveExitArea();
		canvasGroup.alpha = 1.0f;
		_sacrificeCount = 0;
		sacrificeCountText.text = string.Format("{0:N0} / {1:N0}", _sacrificeCount, SacrificeMax);

		healAreaEffectTransform.gameObject.SetActive(true);
		_outAreaRemainTime = UnityEngine.Random.Range(OutAreaDamageTimeMin, OutAreaDamageTimeMax);
		_triggerEntered = true;

		Timing.RunCoroutine(DelayedHealAreaInfoText());
	}

	IEnumerator<float> DelayedHealAreaInfoText()
	{
		// 10초 뒤에 침공이 시작된다.
		yield return Timing.WaitForSeconds(10.0f);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;
		if (_killProcessed)
			yield break;

		// 더이상 침공은 시간에 의해 결정되지 않는다. 우선 삭제하진 않고 구조 변경중.
		//BattleManager.instance.On10SecondAgoActiveExitArea();

		// 회복마법진은 15초 후부터 줄어든다.
		yield return Timing.WaitForSeconds(5.0f);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;
		if (_killProcessed)
			yield break;

		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarPortalHealWeak"), 2.5f);
	}

	bool _processing = false;
	bool _killProcessed = false;
	IEnumerator<float> KillProcess(bool inProgressGame = false)
	{
		if (_processing)
			yield break;

		_processing = true;

		Instantiate<GameObject>(areaActiveEffectPrefab, cachedTransform.position, Quaternion.identity);
		yield return Timing.WaitForSeconds(0.8f);

		// avoid gc
		if (this == null)
			yield break;

		CustomRenderer.instance.bloom.AdjustDirtIntensity(1.5f);

		// avoid gc
		if (this == null)
			yield break;

		FadeCanvas.instance.FadeOut(0.2f);
		yield return Timing.WaitForSeconds(0.2f);

		// avoid gc
		if (this == null)
			yield break;

		CustomRenderer.instance.bloom.ResetDirtIntensity();

		// 전투중이라서 Die검사를 해야한다. 여기서 죽으면 발동 안하고 취소되는거다.
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie())
		{
			FadeCanvas.instance.FadeIn(0.4f);
			// 알파가 어느정도 빠지면 _processing을 풀어준다.
			yield return Timing.WaitForSeconds(0.2f);
			_processing = false;
			yield break;
		}

		// 페이즈의 변경을 알린다. 여기서 몬스터 DieProcess처리도 할거다.
		BattleManager.instance.OnSuccessExitArea();

		_killProcessed = true;
		_arrowIndicatorTransform.gameObject.SetActive(false);
		areaOverTransform.gameObject.SetActive(false);
		healAreaEffectTransform.gameObject.SetActive(false);
		canvasGroup.gameObject.SetActive(false);

		// 몬스터 삭제 연출 보여줘야할테니 너무 오래 끌지는 않는다.
		FadeCanvas.instance.FadeIn(0.8f);

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