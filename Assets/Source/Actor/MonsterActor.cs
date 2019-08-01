using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterActor : Actor
{
	public PathFinderController pathFinderController { get; private set; }
	public MonsterAI monsterAI { get; private set; }

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

		pathFinderController = GetComponent<PathFinderController>();
		if (pathFinderController == null) pathFinderController = gameObject.AddComponent<PathFinderController>();

		monsterAI = GetComponent<MonsterAI>();
		if (monsterAI == null) monsterAI = gameObject.AddComponent<MonsterAI>();
	}

	protected override void InitializeActor()
	{
		base.InitializeActor();

		team.teamID = (int)Team.eTeamID.DefaultMonster;
		actorStatus.InitializeMonsterStatus(actorId);

		BattleManager.instance.OnSpawnMonster(this);
		BattleInstanceManager.instance.OnInitializePathFinderAgent(pathFinderController.agent.agentTypeID);
	}

	#region ObjectPool
	void ReinitializeActor()
	{
		actionController.PlayActionByActionName("Idle");
		actionController.idleAnimator.enabled = true;
		HitObject.EnableRigidbodyAndCollider(true, _rigidbody, _collider);

		actorStatus.InitializeMonsterStatus(actorId);

		BattleManager.instance.OnSpawnMonster(this);
		BattleInstanceManager.instance.OnInitializePathFinderAgent(pathFinderController.agent.agentTypeID);
	}
	#endregion

	public override void OnDie()
	{
		base.OnDie();

		//BehaviorDesigner.Runtime.BehaviorTree bt = GetComponent<BehaviorDesigner.Runtime.BehaviorTree>();
		//if (bt != null) bt.enabled = false;

		Invoke("DisableObject", 1.2f);

		BattleManager.instance.OnDieMonster(this);
		BattleInstanceManager.instance.OnFinalizePathFinderAgent(pathFinderController.agent.agentTypeID);
	}

	void DisableObject()
	{
		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.monsterDisableEffectObject, cachedTransform.position, Quaternion.identity);
		gameObject.SetActive(false);
	}
}
