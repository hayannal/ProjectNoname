using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class MonsterActor : Actor
{
	public override bool IsMonsterActor() { return true; }

	public float monsterHpGaugeWidth = 1.0f;

	public PathFinderController pathFinderController { get; private set; }
	public MonsterAI monsterAI { get; private set; }
	public bool bossMonster { get; private set; }
	public GroupMonster group { get; private set; }
	public bool groupMonster { get { return group != null; } }
	public SequentialMonster sequentialMonster { get; set; }
	public bool summonMonster { get; set; }

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
		sequentialMonster = null;
		summonMonster = false;
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
		actorStatus.InitializeMonsterStatus();
		InitializePassiveSkill();
		monsterAI.InitializeAI();

		if (bossMonster && BattleInstanceManager.instance.bossGaugeSequentialMonster == null)
			BossMonsterGaugeCanvas.instance.InitializeGauge(this);

		monsterAI.OnEventAnimatorParameter(MonsterAI.eAnimatorParameterForAI.fHpRatio, actorStatus.GetHPRatio());

		#region Drop SP
		_dropSpValue = cachedMonsterTableData.initialDropSp;
		if (StageManager.instance.currentStageTableData != null)
			_dropSpValue *= StageManager.instance.currentStageTableData.initialDropSpAdjustment;
		_dropSpRefreshPeriod = (StageManager.instance.currentStageTableData != null) ? StageManager.instance.currentStageTableData.spDecreasePeriod : 0.0f;
		_nextDropSpRefreshTime = Time.time + _dropSpRefreshPeriod;
		#endregion

		BattleManager.instance.OnSpawnMonster(this);
		BattleInstanceManager.instance.OnInitializeMonster(this);
		BattleInstanceManager.instance.OnInitializePathFinderAgent(pathFinderController.agent.agentTypeID);
		_needNavMeshAgentWarp = true;
	}

	#region ObjectPool
	void ReinitializeActor()
	{
		actionController.PlayActionByActionName("Idle");
		actionController.idleAnimator.enabled = true;
		HitObject.EnableRigidbodyAndCollider(true, _rigidbody, _collider);
		ResetAdjustMass();

		monsterAI.enabled = true;
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

	bool _needNavMeshAgentWarp = false;
	void Update()
	{
		if (_needNavMeshAgentWarp)
		{
			// 실시간으로 굽는거다보니 포지션이 틀어질 수 있다. 첫번째 업데이트때 강제로 동기를 맞춰준다.
			pathFinderController.agent.Warp(cachedTransform.position);
			_needNavMeshAgentWarp = false;
		}

		#region Drop SP
		UpdateDropSp();
		#endregion

		if (checkOverlapPositionFrameCount > 0)
		{
			if (cachedTransform.position.y > 0.0f)
				cachedTransform.position = new Vector3(cachedTransform.position.x, 0.0f, cachedTransform.position.z);
			checkOverlapPositionFrameCount -= 1;
		}
	}
	public int checkOverlapPositionFrameCount { get; set; }

	MonsterHPGauge _monsterHPGauge;
	public override void OnChangedHP()
	{
		base.OnChangedHP();

		if (bossMonster)
		{
			BossMonsterGaugeCanvas.instance.OnChangedHP(this);
		}
		
		if (bossMonster == false || groupMonster)
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

		if (sequentialMonster != null)
			sequentialMonster.OnDieMonster(this);

		Drop();

		if (bossMonster)
		{
			BossMonsterGaugeCanvas.instance.OnDie(this);
		}
		
		if (bossMonster == false || groupMonster)
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
		BattleInstanceManager.instance.OnDieMonster(this);
		BattleInstanceManager.instance.OnFinalizePathFinderAgent(pathFinderController.agent.agentTypeID);
	}

	IEnumerator<float> DieProcess()
	{
		if (BurrowAffector.CheckDie(affectorProcessor))
			yield break;

		yield return Timing.WaitForSeconds(bossMonster ? 1.7f : 1.2f);

		// avoid gc
		if (this == null)
			yield break;

		if (cachedMonsterTableData.flakeMultiplier > 0.0f)
		{
			DieDissolve.ShowDieDissolve(cachedTransform, bossMonster);
			DieAshParticle.ShowParticle(cachedTransform, bossMonster, cachedMonsterTableData.flakeMultiplier);
		}
		else
		{
			// only for MonsterBomb and no allow Always Animate
			Renderer renderer = cachedTransform.GetComponentInChildren<Renderer>();
			if (renderer != null) renderer.enabled = true;

			gameObject.SetActive(false);
		}

		yield break;
	}

	public override void EnableAI(bool enable)
	{
		if (!enable)
			pathFinderController.agent.ResetPath();

		// pause기능을 별도로 만들까 하다가 어차피 변수들만 잘 관리할 수 있다면 pause는 괜히 업뎃 돌게하는거니 직접 enabled를 컨트롤 하기로 한다.
		monsterAI.enabled = enable;
	}

	#region Collision Damage
	// OnCollisionEnter 호출되는 프레임부터 같이 호출되기 때문에 Stay에서만 처리해도 괜찮다.
	// 원래 PlayerActor에서 처리하던건데 이랬더니 둘러쌓여도 Tick당 한번밖에 데미지를 입지 않아서 몬스터쪽으로 옮긴다.
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

			if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
				continue;

			if (CheckCollisionStayInterval())
				ApplyCollisionDamage(affectorProcessor);
		}
	}

	void ApplyCollisionDamage(AffectorProcessor defenderAffectorProcessor)
	{
		RushAffector rushAffector = (RushAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Rush);
		if (rushAffector != null)
		{
			eAffectorType affectorType = eAffectorType.BaseDamage;
			AffectorValueLevelTableData baseDamageAffectorValue = new AffectorValueLevelTableData();
			baseDamageAffectorValue.fValue1 = cachedMonsterTableData.collisionDamageRate * rushAffector.GetCollisionDamageRate();
			baseDamageAffectorValue.fValue4 = 1.0f;
			defenderAffectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, baseDamageAffectorValue, this, false);
		}
		else
		{
			eAffectorType affectorType = eAffectorType.CollisionDamage;
			AffectorValueLevelTableData collisionDamageAffectorValue = new AffectorValueLevelTableData();
			collisionDamageAffectorValue.fValue1 = cachedMonsterTableData.collisionDamageRate;
			collisionDamageAffectorValue.iValue1 = 0;
			defenderAffectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, collisionDamageAffectorValue, this, false);
		}
	}

	float _collisionStayInterval = 0.0f;
	float _lastCollisionStayTime = 0.0f;
	bool CheckCollisionStayInterval()
	{
		if (_lastCollisionStayTime == 0.0f)
		{
			ApplyCollisionStayInterval();
			return true;
		}
		if (Time.time > _lastCollisionStayTime + _collisionStayInterval)
		{
			_lastCollisionStayTime = Time.time;
			return true;
		}
		return false;
	}

	public void ApplyCollisionStayInterval()
	{
		_lastCollisionStayTime = Time.time;
		if (_collisionStayInterval == 0.0f)
			_collisionStayInterval = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("CollisionDamageInterval");
	}
	#endregion

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

		// summon 시그널로 만들어진 몬스터는 아무것도 드랍하지 않는다.
		if (summonMonster)
			return;

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

	#region Adjust Mass
	bool _adjustMassState = false;
	float _origMass = 0.0f;
	public void AdjustMass(float rate)
	{
		if (_adjustMassState == false)
		{
			_origMass = _rigidbody.mass;
		}

		_rigidbody.mass = _origMass * rate;
		_adjustMassState = true;
	}

	public void ResetAdjustMass()
	{
		if (_adjustMassState == false)
			return;

		_rigidbody.mass = _origMass;
		_adjustMassState = false;
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
