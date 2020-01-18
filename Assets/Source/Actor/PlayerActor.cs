using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class PlayerActor : Actor
{
	public override bool IsPlayerActor() { return true; }

	public GameObject[] cachingObjectList;

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
			RegisterBattleInstance();
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
					// 한번이라도 썼던 캐릭터인지 확인
					bool firstEnter = !StageManager.instance.IsInBattlePlayerList(actorId);

					// 레벨팩 이전
					LevelPackDataManager.instance.TransferLevelPackList(BattleInstanceManager.instance.playerActor, this);

					// Hp비율 이전. Sp는 최대로 회복
					float hpRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio();
					actorStatus.SetHpRatio(hpRatio);

					// 처음 스왑이라면 힐과 sp회복 적용
					if (firstEnter)
					{
						actorStatus.SetSpRatio(1.0f);

						AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
						healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("SwapHeal");
						healAffectorValue.fValue3 += affectorProcessor.actor.actorStatus.GetValue(eActorStatus.SwapHealRate);
						affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, affectorProcessor.actor, false);
					}

					// 스테이지 디버프
					if (BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData != null)
						RefreshStagePenaltyAffector(BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData.stagePenaltyId, false);
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

	void OnChangedMainCharacter(bool experience = false)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			StageManager.instance.AddBattlePlayer(actorId);

		BattleInstanceManager.instance.playerActor = this;
		CustomFollowCamera.instance.targetTransform = cachedTransform;

		if (experience)
			return;

		if (lobby == false)
			InitializeCanvas();

		StageManager.instance.PreparePowerSource();
	}

	public void ChangeMainCharacter()
	{
		// UI에서 대표캐릭터 바꿀때 쓰는 함수
		// OnChangedMainCharacter 호출이 패스되면서 BattleInstanceManager.instance.playerActor 에 설정되어있지 않은 상태다.
		// 이미 교체할 캐릭터는 만들어진 상태니 GetCachedPlayerActor 함수로 찾으면 찾아져야한다.

		// 먼저 이전 playerActor를 비활성화
		Vector3 position = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);

		// 메인 캐릭터 처리.
		OnChangedMainCharacter();
		BattleInstanceManager.instance.playerActor.cachedTransform.position = position;
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
	}
	#endregion

	#region Experience
	// 아마도 대표캐릭터 셋팅하는 UI로 옮겨야할거 같다.
	PlayerActor _prevPlayerActor;
	bool _experienceMode = false;
	public void ExperienceCharacter()
	{
		_experienceMode = true;

		// UI에서 체함하기 누를때 쓰는 함수
		// 이때도 ChangeMainCharacter 함수와 마찬가지로 교체할 캐릭터는 이미 만들어진 상태고 OnChangedMainCharacter함수는 호출이 패스된 상태다.
		_prevPlayerActor = BattleInstanceManager.instance.playerActor;
		_prevPlayerActor.gameObject.SetActive(false);
		OnChangedMainCharacter(true);

		// 체험모드에서도 플레이어를 때리는 몹이 있을테니 HP 리셋
		actorStatus.SetHpRatio(1.0f);
		actorStatus.SetSpRatio(0.0f);

		// 들어갈때 피 게이지는 안떠도 상관없다고 해서 패스.
		//PlayerGaugeCanvas.instance.InitializeGauge(this);
		SkillSlotCanvas.instance.InitializeSkillSlot(this);
	}

	public void FinishExperienceCharacter()
	{
		if (_prevPlayerActor == null)
			return;

		// 체험모드에서도 플레이어를 때리는 몹이 있을테니 HP 리셋
		actorStatus.SetHpRatio(1.0f);
		actorStatus.SetSpRatio(0.0f);

		BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
		_prevPlayerActor.OnChangedMainCharacter(true);
		_prevPlayerActor = null;

		SkillSlotCanvas.instance.HideSkillSlot();

		_experienceMode = false;
	}
	#endregion

	public override void OnChangedHP()
	{
		base.OnChangedHP();

		PlayerGaugeCanvas.instance.OnChangedHP(this);
	}

	public override void OnChangedSP()
	{
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


	#region Collision Damage
	// OnCollisionEnter 호출되는 프레임부터 같이 호출되기 때문에 Stay에서만 처리해도 괜찮다.
	void OnCollisionStay(Collision collision)
	{
		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			if (col.isTrigger)
				continue;

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor == null)
				continue;

			if (affectorProcessor.actor == null)
				continue;

			if (affectorProcessor.actor.team.teamId != (int)Team.eTeamID.DefaultMonster)
				continue;

			if (CheckCollisionStayInterval())
				ApplyCollisionDamageAffector(affectorProcessor.actor);
		}
	}

	void ApplyCollisionDamageAffector(Actor attackerActor)
	{
		eAffectorType affectorType = eAffectorType.CollisionDamage;
		AffectorValueLevelTableData collisionDamageAffectorValue = new AffectorValueLevelTableData();
		collisionDamageAffectorValue.fValue1 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("CollisionDamageRate");
		collisionDamageAffectorValue.iValue1 = 0;
		affectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, collisionDamageAffectorValue, attackerActor, false, true);
	}

	float _collisionStayInterval = 0.0f;
	float _lastCollisionStayTime = 0.0f;
	bool CheckCollisionStayInterval()
	{
		if (_lastCollisionStayTime == 0.0f)
		{
			_collisionStayInterval = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("CollisionDamageInterval");
			_lastCollisionStayTime = Time.time;
			return true;
		}
		if (Time.time > _lastCollisionStayTime + _collisionStayInterval)
		{
			_lastCollisionStayTime = Time.time;
			return true;
		}
		return false;
	}
	#endregion

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
}
