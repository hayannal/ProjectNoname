using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false)
			InitializeCanvas();

		RegisterBattleInstance();
	}

	public void InitializeCanvas()
	{
		PlayerGaugeCanvas.instance.InitializeGauge(this);
		SkillSlotCanvas.instance.InitializeSkillSlot(this);
	}

	void RegisterBattleInstance()
	{
		// 씬 들어와서 처음 만들어지는 PlayerActor는 바로 등록하고 그 이후엔 교체할때만 등록하도록 한다.
		if (BattleInstanceManager.instance.playerActor == null)
		{
			BattleInstanceManager.instance.playerActor = this;
			CustomFollowCamera.instance.targetTransform = cachedTransform;
			StageManager.instance.PreparePowerSource();
		}
	}

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
