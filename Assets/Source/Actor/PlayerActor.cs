using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActor : Actor
{
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

		//castingProcessor = GetComponent<CastingProcessor>();
		//if (castingProcessor == null) castingProcessor = gameObject.AddComponent<CastingProcessor>();
	}

	protected override void InitializeActor()
	{
		base.InitializeActor();

		actorStatus.InitializeActorStatus(actorId);
		team.teamID = (int)Team.eTeamID.DefaultArmy;
	}

	public override void OnDie()
	{
		base.OnDie();

		//CharacterController cc = GetComponent<CharacterController>();
		//if (cc != null) cc.enabled = false;

		BattleManager.instance.OnDiePlayer(this);
	}
}
