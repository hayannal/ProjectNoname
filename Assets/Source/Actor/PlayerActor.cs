#define CHEAT_RESURRECT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using MEC;

public class PlayerActor : Actor
{
	public override bool IsPlayerActor() { return true; }

	public GameObject[] cachingObjectList;
	public Transform wingRootTransform;

	public SkillProcessor skillProcessor { get; private set; }
	public PlayerAI playerAI { get; private set; }
	//public CastingProcessor castingProcessor { get; private set; }
	public float actorRadius { get; private set; }
	public bool flying { get; private set; }

	void Awake()
	{
		InitializeComponent();
	}

	bool _started;
	void Start()
	{
		actorRadius = ColliderUtil.GetRadius(_collider);
		InitializeActor();
		_started = true;
	}

	#region Swap
	void OnEnable()
	{
		if (_started)
		{
			// 이미 생성당시에 날개 생성은 모두 완료했을거다. 여기서는 전투중에 꺼놨던 캐릭을 켰을때를 대비해서 Hide옵션 체크만 해도 충분하다.
			RefreshWingHide();
			RegisterBattleInstance();
		}
	}
	#endregion

	void OnDisable()
	{
		ShowUltimateIndicator(false);
	}

	protected override void InitializeComponent()
	{
		base.InitializeComponent();

		affectorProcessor.dontClearOnDisable = true;

		actorStatus = GetComponent<ActorStatus>();
		if (actorStatus == null) actorStatus = gameObject.AddComponent<ActorStatus>();

		skillProcessor = GetComponent<SkillProcessor>();
		if (skillProcessor == null) skillProcessor = gameObject.AddComponent<SkillProcessor>();

		playerAI = GetComponent<PlayerAI>();
		if (playerAI == null) playerAI = gameObject.AddComponent<PlayerAI>();

		//castingProcessor = GetComponent<CastingProcessor>();
		//if (castingProcessor == null) castingProcessor = gameObject.AddComponent<CastingProcessor>();
	}

	protected override void InitializeActor()
	{
		base.InitializeActor();

		team.SetTeamId((int)Team.eTeamID.DefaultAlly, true, gameObject, Team.eTeamLayer.TEAM0_ACTOR_LAYER, false);
		actorStatus.InitializeActorStatus();
		skillProcessor.InitializeSkill();
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		targetingProcessor.sphereCastRadiusForCheckWall = actorTableData.targetingSphereRadius;
		flying = actorTableData.flying;

		// 처음 캐릭을 만들땐 생성까진 해두고 Hide여부는 SetActive로 제어하기로 한다.
		RefreshWing();
		RegisterBattleInstance();
	}

	public void InitializeCanvas()
	{
		PlayerGaugeCanvas.instance.InitializeGauge(this);
		SkillSlotCanvas.instance.InitializeSkillSlot(this);
	}

	void RegisterBattleInstance()
	{
		BattleInstanceManager.instance.OnInitializePlayerActor(this);

		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);

		// 메인 캐릭터를 스왑할땐 항상 standbySwapPlayerActor 플래그가 켜있으니 이거로 구분하면 된다.
		// 이땐 Hp비율부터 레벨팩 등을 이전받아야한다.
		// 로비라면 할 필요 없으니 그냥 이전 캐릭터를 끄기만 하고 넘어간다.
		if (BattleInstanceManager.instance.standbySwapPlayerActor)
		{
			if (BattleInstanceManager.instance.playerActor != null)
			{
				if (lobby == false)
				{
					if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
					{
						// 바꾼 캐릭터에도 NodeWar용 레벨팩 적용
						NodeWarProcessor.ApplyNodeWarLevelPack(this);
					}
					else
					{
						// 한번이라도 썼던 캐릭터인지 확인
						bool firstEnter = !BattleInstanceManager.instance.IsInBattlePlayerList(actorId);

						// 레벨팩 이전
						LevelPackDataManager.instance.TransferLevelPackList(BattleInstanceManager.instance.playerActor, this);

						// Hp비율 Sp비율 이전
						float hpRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio();
						actorStatus.SetHpRatio(hpRatio);
						float spRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetSPRatio();
						actorStatus.SetSpRatio(spRatio);
#if CHEAT_RESURRECT
						bool cheatDontDie = BattleInstanceManager.instance.playerActor.actorStatus.cheatDontDie;
						actorStatus.cheatDontDie = cheatDontDie;
#endif

						// 처음 스왑이라면 힐과 sp회복 적용
						if (firstEnter)
						{
							actorStatus.SetSpRatio(1.0f);

							AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
							healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("SwapHeal");
							healAffectorValue.fValue3 += affectorProcessor.actor.actorStatus.GetValue(eActorStatus.SwapHealAddRate);
							affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, affectorProcessor.actor, false);
							BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.healEffectPrefab, cachedTransform.position, Quaternion.identity, cachedTransform);

							ClientSaveData.instance.OnChangedHpRatio(affectorProcessor.actor.actorStatus.GetHPRatio());
							ClientSaveData.instance.OnChangedSpRatio(affectorProcessor.actor.actorStatus.GetSPRatio());

							Timing.RunCoroutine(ScreenHealEffectProcess());
						}

						// 스테이지 디버프
						if (BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData != null)
							RefreshStagePenaltyAffector(BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData.stagePenaltyId, false);
					}
				}

				BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
			}

			// 이전받고 나서야 메인캐릭터로 교체.
			OnChangedMainCharacter();

			BattleInstanceManager.instance.standbySwapPlayerActor = false;

			BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerSpawnEffectPrefab, cachedTransform.position, Quaternion.identity);
		}
		else
		{
			// 처음 만들어지는 PlayerActor는 바로 등록하고 그게 아니라면 로비에서 다른 캐릭터 보여주려는 경우일거다.
			// 이 경우가 아닌데 캐릭이 추가로 등장하는거라면 씬에다 캐릭터 끌어서 추가했을 경우 일거다. 이럴땐 Change하지 않는다.
			if (BattleInstanceManager.instance.playerActor == null)
				OnChangedMainCharacter();
		}
	}

	IEnumerator<float> ScreenHealEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.6f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("AfterSwapUI_Heal"), 2.3f);
		FadeCanvas.instance.FadeIn(1.3f);
	}

	public void OnChangedMainCharacter()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			BattleInstanceManager.instance.AddBattlePlayer(actorId);

		BattleInstanceManager.instance.playerActor = this;
		CustomFollowCamera.instance.targetTransform = cachedTransform;
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.ForceSetTargetTransform(cachedTransform);

		// 첫 플레이 튜토에서는 궁극기 버튼도 보여주면 안되서 예외처리한다.
		bool showPlayerCanvas = true;
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.playAfterInstallation)
			showPlayerCanvas = false;
		if (lobby == false && showPlayerCanvas)
			InitializeCanvas();

		StageManager.instance.PreparePowerSource();
	}

	void Update()
	{
		UpdateUltimateIndicator();
	}

	#region Stage Penalty Affector
	// 메인캐릭터로 바뀌거나 진입 직전에 한번씩만 호출해주면 알아서 최신 어펙터로 적용한다.
	List<AffectorBase> _listStagePenaltyAffector = null;
	public StagePenaltyTableData currentStagePenaltyTableData { get; set; }
	public void RefreshStagePenaltyAffector(string stagePenaltyId, bool showAlarm)
	{
		currentStagePenaltyTableData = null;

		StagePenaltyTableData stagePenaltyTableData = TableDataManager.instance.FindStagePenaltyTableData(stagePenaltyId);
		if (stagePenaltyTableData == null)
			return;

		if (_listStagePenaltyAffector == null)
			_listStagePenaltyAffector = new List<AffectorBase>();

		for (int i = 0; i < _listStagePenaltyAffector.Count; ++i)
			_listStagePenaltyAffector[i].finalized = true;
		_listStagePenaltyAffector.Clear();

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = actorStatus.statusBase;
		SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, this);

		for (int i = 0; i < stagePenaltyTableData.affectorValueId.Length; ++i)
		{
			AffectorBase newAffector = affectorProcessor.ApplyAffectorValue(stagePenaltyTableData.affectorValueId[i], hitParameter, true);
			if (newAffector == null)
				continue;

			if (AffectorCustomCreator.IsContinuousAffector(newAffector.affectorType))
				_listStagePenaltyAffector.Add(newAffector);
			else
				Debug.LogErrorFormat("Non-continuous affector in a Stage Penalty! / StagePenaltyId = {0} / AffectorValueId = {1}", stagePenaltyId, stagePenaltyTableData.affectorValueId[i]);
		}

		currentStagePenaltyTableData = stagePenaltyTableData;

		if (!showAlarm)
			return;

		string[] penaltyMindParameterList = UIString.instance.ParseParameterString(stagePenaltyTableData.mindParameter);
		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString(stagePenaltyTableData.penaltyMindText, penaltyMindParameterList), 2.5f);

		ClientSaveData.instance.OnChangedStagePenalty(stagePenaltyId);
	}
	#endregion

	public override void OnChangedHP()
	{
		base.OnChangedHP();

		// 사실은 여기서 전투중에 피 깎일때만 저장을 해야하는데 이전값을 기억하고 있지도 않고있어서
		// 차라리 PlayerGaugeCanvas에게 위임해서 처리하기로 한다.
		PlayerGaugeCanvas.instance.OnChangedHP(this);
	}

	public override void OnChangedSP()
	{
		// hpRatio와 마찬가지. 클라이언트 저장을 SkillSlotCanvas에게 위임한다.
		SkillSlotCanvas.instance.OnChangedSP(this);
		ShowUltimateIndicator(actorStatus.GetSPRatio() == 1.0f);
	}

	public override void OnDie()
	{
		base.OnDie();

		//CharacterController cc = GetComponent<CharacterController>();
		//if (cc != null) cc.enabled = false;

		BattleManager.instance.OnDiePlayer(this);
	}

	public override void EnableAI(bool enable)
	{
		playerAI.enabled = enable;
	}

	public void Resurrect()
	{
		if (actorStatus.IsDie() == false)
			return;

		// 우선은 연출없이
		actionController.PlayActionByActionName("Idle");
		actionController.idleAnimator.enabled = true;
		HitObject.EnableRigidbodyAndCollider(true, _rigidbody, _collider);
		actorStatus.AddHP(actorStatus.GetValue(eActorStatus.MaxHp));
	}


	#region Ultimate Indicator
	Transform _cachedUltimateIndicatorTransform;
	void ShowUltimateIndicator(bool show)
	{
		if (show)
		{
			if (_cachedUltimateIndicatorTransform == null)
				_cachedUltimateIndicatorTransform = BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.ultimateCirclePrefab, null).transform;
			if (_cachedUltimateIndicatorTransform != null)
				_cachedUltimateIndicatorTransform.gameObject.SetActive(true);
		}
		else
		{
			if (_cachedUltimateIndicatorTransform != null)
				_cachedUltimateIndicatorTransform.gameObject.SetActive(false);
		}
	}

	void UpdateUltimateIndicator()
	{
		if (_cachedUltimateIndicatorTransform == null)
			return;
		if (_cachedUltimateIndicatorTransform.gameObject.activeSelf == false)
			return;
		_cachedUltimateIndicatorTransform.position = cachedTransform.position;
	}
	#endregion

	#region Wing
	GameObject _wingObject;
	void DisableWing()
	{
		if (_wingObject != null)
		{
			if (_wingObject.activeSelf)
				_wingObject.SetActive(false);
			_wingObject = null;
		}
	}

	public void RefreshWing()
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null || characterData.HasWing() == false || wingRootTransform == null)
		{
			DisableWing();
			return;
		}

		WingLookTableData wingLookTableData = TableDataManager.instance.FindWingLookTableData(characterData.wingLookId);
		if (wingLookTableData == null)
		{
			DisableWing();
			return;
		}

		AddressableAssetLoadManager.GetAddressableGameObject(wingLookTableData.prefabAddress, "Wing", (prefab) =>
		{
			// 장착중인 날개는 새로운거로 교체되는 시점에 하이드 시켜준다.
			DisableWing();

			_wingObject = BattleInstanceManager.instance.GetCachedObject(prefab, wingRootTransform);

			// 로드하고 나서는 항상 Hide 체크를 한다.
			RefreshWingHide();
		});
	}

	public void RefreshWingHide()
	{
		if (_wingObject == null)
			return;
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null || characterData.HasWing() == false)
			return;

		if (characterData.wingHide == false)
		{
			_wingObject.SetActive(true);
			return;
		}

		// hide상태일때는 현재 위치에 따라 나눠서 처리해야한다.
		bool hide = true;
		if (CharacterListCanvas.instance != null && StackCanvas.IsInStack(CharacterListCanvas.instance.gameObject, false))
			hide = false;
		_wingObject.SetActive(!hide);
	}
	#endregion
}
