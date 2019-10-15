using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class MonsterActor : Actor
{
	public float monsterHpGaugeWidth = 1.0f;

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

		team.SetTeamId((int)Team.eTeamID.DefaultMonster, true, gameObject, Team.eTeamLayer.TEAM1_ACTOR_LAYER);
		bossMonster = cachedMonsterTableData.boss;
		if (cachedTransform.parent != null)
			group = cachedTransform.parent.GetComponent<GroupMonster>();

		// common
		InitializeMonster();
	}

	void InitializeMonster()
	{
		InitializePassiveSkill();
		actorStatus.InitializeMonsterStatus(actorId);
		monsterAI.InitializeAI();

		if (bossMonster)
			BossMonsterGaugeCanvas.instance.InitializeGauge(this);

		monsterAI.OnEventAnimatorParameter(MonsterAI.eAnimatorParameterForAI.fHpRatio, actorStatus.GetHPRatio());

		#region Drop SP
		_dropSpValue = cachedMonsterTableData.initialDropSp;
		_dropSpRefreshPeriod = (StageManager.instance.currentStageTableData != null) ? StageManager.instance.currentStageTableData.spDecreasePeriod : 0.0f;
		_nextDropSpRefreshTime = Time.time + _dropSpRefreshPeriod;
		#endregion

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

	#region Passive Skill
	List<AffectorBase> _listPassiveAffector;
	void InitializePassiveSkill()
	{
		if (cachedMonsterTableData.passiveAffectorValueId.Length == 0)
			return;

		if (_listPassiveAffector == null)
			_listPassiveAffector = new List<AffectorBase>();
		_listPassiveAffector.Clear();

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = actorStatus.statusBase;
		SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, this);

		for (int i = 0; i < cachedMonsterTableData.passiveAffectorValueId.Length; ++i)
		{
			AffectorBase passiveAffector = affectorProcessor.ApplyAffectorValue(cachedMonsterTableData.passiveAffectorValueId[i], hitParameter, true);
			if (passiveAffector == null)
				continue;

			if (AffectorCustomCreator.IsContinuousAffector(passiveAffector.affectorType))
				_listPassiveAffector.Add(passiveAffector);
			else
				Debug.LogErrorFormat("Non-continuous affector in a passive skill! / AffectorValueId = {1}", cachedMonsterTableData.passiveAffectorValueId[i]);
		}
	}
	#endregion

	void Update()
	{
		#region Drop SP
		UpdateDropSp();
		#endregion
	}

	MonsterHPGauge _monsterHPGauge;
	public override void OnChangedHP()
	{
		base.OnChangedHP();

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

		Drop();

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

		// avoid gc
		if (this == null)
			yield break;

		DieDissolve.ShowDieDissolve(cachedTransform, bossMonster);
		DieAshParticle.ShowParticle(cachedTransform, bossMonster);

		yield break;
	}

	public override void EnableAI(bool enable)
	{
		if (!enable)
			pathFinderController.agent.ResetPath();
		monsterAI.enabled = enable;
	}

	#region Drop Item
	float _dropSpValue;
	float _dropSpRefreshPeriod;
	float _nextDropSpRefreshTime;

	void UpdateDropSp()
	{
		if (_dropSpValue == 0.0f || _dropSpRefreshPeriod == 0.0f)
			return;

		if (Time.time > _nextDropSpRefreshTime)
		{
			_dropSpValue *= BattleInstanceManager.instance.GetCachedGlobalConstantFloat("SpDecreaseRate");
			_nextDropSpRefreshTime += _dropSpRefreshPeriod;
		}
	}

	void Drop()
	{
		// 보스 몬스터는 보스몬스터끼리만 검사해서 마지막 보스몹에서만 드랍되게 해야한다.
		// 이래야 혹시 여러 그룹의 보스들을 소환해도 마지막 보스한테서만 드랍이 1회 발동되게 된다.
		if (bossMonster)
		{
			// 리스트로 들고있는게 이 gaugeCanvas밖에 없어서 여기에 물어본다.
			if (BossMonsterGaugeCanvas.instance.IsLastAliveMonster(this) == false)
				return;
		}
		else
		{
			// 보스가 아닌 몹들 중에서 그룹이라면 그룹 내 마지막 몹만 드랍해야한다.
			if (groupMonster)
			{
				if (group.IsLastAliveMonster(this) == false)
					return;
			}
		}

		// 마지막 몹을 죽여서 드랍하는 순간에
		// 기존 드랍템들에 회수 표시를 걸어두고 해당 몹이 드랍할 템들에도 회수 표시를 걸어둔다. 이래야 다음판 드랍템과 섞여도 현재판 드랍템들만 회수할 수 있다.
		bool lastMonsterInStage = (BattleManager.instance.GetSpawnedMonsterCount() == 1);
		if (lastMonsterInStage)
			BattleInstanceManager.instance.OnDropLastMonsterInStage();

		string dropId = "";
		if (cachedMonsterTableData.defaultDropUse && StageManager.instance.currentStageTableData != null)
		{
			if (bossMonster) dropId = StageManager.instance.currentStageTableData.defaultBossDropId;
			else dropId = StageManager.instance.currentStageTableData.defaultNormalDropId;
		}
		string addDropId = cachedMonsterTableData.addDropId;
		DropProcessor.Drop(cachedTransform, dropId, addDropId, lastMonsterInStage);

		// sp drop
		DropProcessor.DropSp(_dropSpValue);
	}
	#endregion







	MonsterTableData _cachedMonsterTableData = null;
	MonsterTableData cachedMonsterTableData
	{
		get
		{
			if (_cachedMonsterTableData == null)
				_cachedMonsterTableData = TableDataManager.instance.FindMonsterTableData(actorId);
			return _cachedMonsterTableData;
		}
	}
}
