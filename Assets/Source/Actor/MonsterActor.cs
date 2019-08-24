using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class MonsterActor : Actor
{
	public float monsterHpGaugeWidth = 1.0f;
	public float monsterHpGaugeOffsetY = 0.0f;

	public PathFinderController pathFinderController { get; private set; }
	public MonsterAI monsterAI { get; private set; }
	public bool bossMonster { get; private set; }
	public GroupMonster group { get; private set; }
	public bool groupMonster { get { return group != null; } }

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

	void OnDisable()
	{
		if (groupMonster)
			group.CheckAllDisable();
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
		MonsterTableData monsterTableData = TableDataManager.instance.FindMonsterTableData(actorId);
		bossMonster = monsterTableData.boss;
		if (cachedTransform.parent != null)
			group = cachedTransform.parent.GetComponent<GroupMonster>();

		// common
		InitializeMonster();
	}

	void InitializeMonster()
	{
		actorStatus.InitializeMonsterStatus(actorId);
		monsterAI.InitializeAI();

		if (bossMonster)
			BossMonsterGaugeCanvas.instance.InitializeGauge(this);

		monsterAI.OnEventAnimatorParameter(MonsterAI.eAnimatorParameterForAI.fHpRatio, actorStatus.GetHPRatio());

		BattleManager.instance.OnSpawnMonster(this);
		BattleInstanceManager.instance.OnInitializePathFinderAgent(pathFinderController.agent.agentTypeID);
	}

	#region ObjectPool
	void ReinitializeActor()
	{
		actionController.PlayActionByActionName("Idle");
		actionController.idleAnimator.enabled = true;
		HitObject.EnableRigidbodyAndCollider(true, _rigidbody, _collider);

		InitializeMonster();
	}
	#endregion

	MonsterHPGauge _monsterHPGauge;
	public override void OnChangedHP()
	{
		if (bossMonster)
		{
			BossMonsterGaugeCanvas.instance.OnChangedHP(this);
		}
		else
		{
			if (_monsterHPGauge == null)
			{
				_monsterHPGauge = UIInstanceManager.instance.GetCachedMonsterHPgauge(BattleManager.instance.monsterHPGaugePrefab);
				_monsterHPGauge.InitializeGauge(this);
			}
			_monsterHPGauge.OnChangedHP(actorStatus.GetHPRatio());
		}

		monsterAI.OnEventAnimatorParameter(MonsterAI.eAnimatorParameterForAI.fHpRatio, actorStatus.GetHPRatio());
	}

	public override void OnDie()
	{
		base.OnDie();

		if (bossMonster)
		{
			BossMonsterGaugeCanvas.instance.OnDie();
		}
		else
		{
			if (_monsterHPGauge != null)
			{
				_monsterHPGauge.gameObject.SetActive(false);
				_monsterHPGauge = null;
			}
		}

		if (pathFinderController.agent.hasPath)
			pathFinderController.agent.ResetPath();
		//BehaviorDesigner.Runtime.BehaviorTree bt = GetComponent<BehaviorDesigner.Runtime.BehaviorTree>();
		//if (bt != null) bt.enabled = false;

		//Invoke("DisableObject", 1.2f);
		Timing.RunCoroutine(DieProcess());

		BattleManager.instance.OnDieMonster(this);
		BattleInstanceManager.instance.OnFinalizePathFinderAgent(pathFinderController.agent.agentTypeID);
	}

	IEnumerator<float> DieProcess()
	{
		yield return Timing.WaitForSeconds(1.2f);

		DieDissolve.ShowDieDissolve(cachedTransform, bossMonster);
		DieAshParticle.ShowParticle(cachedTransform, bossMonster);

		yield break;
	}
}
