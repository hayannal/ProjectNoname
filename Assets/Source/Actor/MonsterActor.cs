using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterActor : Actor
{
	void Awake()
	{
		InitializeComponent();
	}

	bool _started = false;
	void Start()
	{
		InitializeActor();
		_started = true;
	}

	#region ObjectPool
	void OnEnable()
	{
		if (_started)
			ReinitializeActor();
	}
	#endregion

	protected override void InitializeComponent()
	{
		base.InitializeComponent();

		// for monster status?
		actorStatus = GetComponent<ActorStatus>();
		if (actorStatus == null) actorStatus = gameObject.AddComponent<ActorStatus>();
	}

	protected override void InitializeActor()
	{
		base.InitializeActor();

		team.teamID = (int)Team.eTeamID.DefaultMonster;
		actorStatus.InitializeMonsterStatus(actorId);

		BattleManager.instance.OnSpawnMonster(this);
	}

	#region ObjectPool
	void ReinitializeActor()
	{
		actionController.PlayActionByActionName("Idle");
		actionController.idleAnimator.enabled = true;
		HitObject.EnableRigidbodyAndCollider(true, _rigidbody, _collider);

		actorStatus.InitializeMonsterStatus(actorId);

		BattleManager.instance.OnSpawnMonster(this);
	}
	#endregion

	public override void OnDie()
	{
		base.OnDie();

		//BehaviorDesigner.Runtime.BehaviorTree bt = GetComponent<BehaviorDesigner.Runtime.BehaviorTree>();
		//if (bt != null) bt.enabled = false;

		Invoke("DisableObject", 2.0f);

		BattleManager.instance.OnDieMonster(this);
	}

	void DisableObject()
	{
		gameObject.SetActive(false);
	}
}
