using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class PlayerActor : Actor
{
	public GameObject[] cachingObjectList;

	public SkillProcessor skillProcessor { get; private set; }
	public PlayerAI playerAI { get; private set; }
	//public CastingProcessor castingProcessor { get; private set; }
	public float actorRadius { get; private set; }

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

	void OnDisable()
	{
		if (BattleInstanceManager.instance.playerActor == this)
			BattleInstanceManager.instance.playerActor = null;
	}
	#endregion

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

		team.SetTeamId((int)Team.eTeamID.DefaultAlly, true, gameObject, Team.eTeamLayer.TEAM0_ACTOR_LAYER);
		actorStatus.InitializeActorStatus();
		skillProcessor.InitializeSkill();

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

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false)
		{
			// 전투 진입 후 교체하는 거라면 분명 스왑상태에서 캐릭터를 전환한 것이다.
			// 이땐 Hp비율부터 레벨팩 등을 이전받아야한다.
			// 그런데 여기서 그냥 다 자동으로 처리하니 씬에다가 두 캐릭터 빼고싶어도 볼 수가 없다.
			// 그래서 Swap플래그 하나 걸어놓고 처리하기로 한다.
			if (BattleInstanceManager.instance.standbySwapPlayerActor)
			{
				if (BattleInstanceManager.instance.playerActor != null)
				{
					// 레벨팩 이전
					LevelPackDataManager.instance.TransferLevelPackList(BattleInstanceManager.instance.playerActor, this);

					// Hp비율 이전
					float hpRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio();
					actorStatus.SetHPRatio(hpRatio);

					// Sp는?

					// 스왑 힐 적용
					AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
					healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("SwapHeal");
					healAffectorValue.fValue3 += affectorProcessor.actor.actorStatus.GetValue(eActorStatus.SwapHealRate);
					affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, affectorProcessor.actor, false);

					BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
				}

				// 이전받고 나서야 메인캐릭터로 교체.
				OnChangedMainCharacter();

				BattleInstanceManager.instance.standbySwapPlayerActor = false;
			}
		}
		else
		{
			// 로비에선 처음 만들어지는 PlayerActor는 바로 등록하고 그 이후엔 교체할때만 등록하도록 한다.
			// 이래야 다른 캐릭터들 생성해서 캐릭터창 가더라도 메인 캐릭터를 유지할 수 있다.
			if (BattleInstanceManager.instance.playerActor == null)
				OnChangedMainCharacter();
		}
	}

	void OnChangedMainCharacter(bool experience = false)
	{
		BattleInstanceManager.instance.playerActor = this;
		CustomFollowCamera.instance.targetTransform = cachedTransform;

		if (experience)
			return;

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false)
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

	#region Experience
	// 아마도 대표캐릭터 셋팅하는 UI로 옮겨야할거 같다.
	PlayerActor _prevPlayerActor;
	public void ExperienceCharacter()
	{
		// UI에서 체함하기 누를때 쓰는 함수
		// 이때도 ChangeMainCharacter 함수와 마찬가지로 교체할 캐릭터는 이미 만들어진 상태고 OnChangedMainCharacter함수는 호출이 패스된 상태다.
		_prevPlayerActor = BattleInstanceManager.instance.playerActor;
		_prevPlayerActor.gameObject.SetActive(false);
		OnChangedMainCharacter(true);

		// 혹시 체험모드에선 HP 리셋 시켜야하나. 항상 맥스라 안해도 될거 같은데..
		//actorStatus.SetHPRatio(1.0f);

		PlayerGaugeCanvas.instance.InitializeGauge(this);
		SkillSlotCanvas.instance.InitializeSkillSlot(this);
	}

	public void FinishExperienceCharacter()
	{
		if (_prevPlayerActor == null)
			return;

		BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
		_prevPlayerActor.OnChangedMainCharacter(true);
		_prevPlayerActor = null;

		//PlayerGaugeCanvas.instance.InitializeGauge(this);
		SkillSlotCanvas.instance.HideSkillSlot();
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
}
