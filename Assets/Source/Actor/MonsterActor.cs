using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterActor : Actor
{
	void Awake()
	{
		InitializeComponent();
	}

	void Start()
	{
		InitializeActor();
	}

	protected override void InitializeActor()
	{
		actionController.InitializeActionPlayInfo(actorId);
		actorStatus.InitializeMonsterStatus(actorId);
		team.teamID = (int)Team.eTeamID.DefaultMonster;
	}
}
