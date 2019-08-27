using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActor : Actor
{
	public float playerGaugeOffsetY = 0.0f;

	public SkillProcessor skillProcessor { get; private set; }
	public PlayerAI playerAI { get; private set; }
	//public CastingProcessor castingProcessor { get; private set; }
	public float actorRadius { get; private set; }

	void Awake()
	{
		InitializeComponent();
	}

	void Start()
	{
		actorRadius = ColliderUtil.GetRadius(_collider);
		InitializeActor();
	}

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

		team.teamID = (int)Team.eTeamID.DefaultArmy;
		skillProcessor.InitializeSkill(actorId);
		actorStatus.InitializeActorStatus(actorId);

		if (BattleManager.instance != null)
		{
			PlayerGaugeCanvas.instance.InitializeGauge(this);
			SkillSlotCanvas.instance.InitializeSkillSlot(this);
			BattleManager.instance.OnSpawnPlayer(this);
		}
	}

	public override void OnChangedHP()
	{
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
}
