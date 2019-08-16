using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActor : Actor
{
	public SkillProcessor skillProcessor { get; private set; }
	public PlayerAI playerAI { get; private set; }

	void Awake()
	{
		InitializeComponent();
	}

	void Start()
	{
		InitializeActor();
	}

	protected override void InitializeComponent()
	{
		base.InitializeComponent();

		actorStatus = GetComponent<ActorStatus>();
		if (actorStatus == null) actorStatus = gameObject.AddComponent<ActorStatus>();

		skillProcessor = GetComponent<SkillProcessor>();
		if (skillProcessor == null) skillProcessor = gameObject.AddComponent<SkillProcessor>();

		//castingProcessor = GetComponent<CastingProcessor>();
		//if (castingProcessor == null) castingProcessor = gameObject.AddComponent<CastingProcessor>();

		playerAI = GetComponent<PlayerAI>();
		if (playerAI == null) playerAI = gameObject.AddComponent<PlayerAI>();
	}

	protected override void InitializeActor()
	{
		base.InitializeActor();

		team.teamID = (int)Team.eTeamID.DefaultArmy;
		skillProcessor.InitializeSkill(actorId);
		actorStatus.InitializeActorStatus(actorId);

		BattleManager.instance.OnSpawnPlayer(this);
	}

	public override void OnChangedHP()
	{
		
	}

	public override void OnDie()
	{
		base.OnDie();

		//CharacterController cc = GetComponent<CharacterController>();
		//if (cc != null) cc.enabled = false;

		BattleManager.instance.OnDiePlayer(this);
	}
}
