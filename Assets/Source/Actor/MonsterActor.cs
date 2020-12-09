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
	public bool excludeMonsterCount { get; set; }
	public bool eliteMonster { get; private set; }

	// 다른 옵션들과 달리 team설정은 초기화때 한번만 하기 때문에 팀을 바꿔가면서 재활용할 순 없다.
	// 게임 도중에 팀을 섞어쓸일이 없을거 같아서 우선 이대로 간다.
	public bool reservedAllyTeam { get; set; }

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
		if (team.teamId != (int)Team.eTeamID.DefaultMonster || excludeMonsterCount)
		{
			// 해당 몹들은 안잡고 넘어갈 수 있기 때문에 이렇게 예외처리를 해준다.
			// Summon시그널로 생성되기 때문에 해제할때 별도로 처리하기가 어려워서 여기서 처리하는거다.
			if (actorStatus.GetHP() > 0.0f)
			{
				actorStatus.SetHpRatio(0.0f);
				DisableForNodeWar();
			}
		}
		if (groupMonster)
			group.CheckAllDisable();
		sequentialMonster = null;
		summonMonster = false;
		eliteMonster = false;
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

		if (reservedAllyTeam)
			team.SetTeamId((int)Team.eTeamID.DefaultAlly, true, gameObject, Team.eTeamLayer.TEAM0_ACTOR_LAYER);
		else
			team.SetTeamId((int)Team.eTeamID.DefaultMonster, true, gameObject, Team.eTeamLayer.TEAM1_ACTOR_LAYER);
		bossMonster = cachedMonsterTableData.boss;
		if (cachedTransform.parent != null)
			group = cachedTransform.parent.GetComponent<GroupMonster>();

		if (reservedAllyTeam)
		{
			// 테이블에 넣기에도 애매하고 MeSummon 에 넣기에도 애매하다. 우선 하드코딩.
			targetingProcessor.sphereCastRadiusForCheckWall = 0.1f;
		}

		// common
		InitializeMonster();
	}

	void InitializeMonster()
	{
		// 엘리트 체크는 몬스터 초기화 직전에 해둔다. eliteMonter가 켜지면 그에 따라 스탯이랑 AI랑 다르게 적용하면 된다.
		CheckEliteMonster();

		actorStatus.InitializeMonsterStatus(eliteMonster, bossMonster);
		InitializePassiveSkill();
		monsterAI.InitializeAI();

		if (bossMonster && BattleInstanceManager.instance.bossGaugeSequentialMonster == null)
			BossMonsterGaugeCanvas.instance.InitializeGauge(this);

		monsterAI.OnEventAnimatorParameter(MonsterAI.eAnimatorParameterForAI.fHpRatio, actorStatus.GetHPRatio());

		#region Drop SP
		_dropSpValue = cachedMonsterTableData.initialDropSp;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
		{
			_dropSpRefreshPeriod = 0.0f;
			_nextDropSpRefreshTime = 0.0f;
		}
		else
		{
			if (StageManager.instance.currentStageTableData != null)
				_dropSpValue *= StageManager.instance.currentStageTableData.initialDropSpAdjustment;
			_dropSpRefreshPeriod = (StageManager.instance.currentStageTableData != null) ? StageManager.instance.currentStageTableData.spDecreasePeriod : 0.0f;
			_nextDropSpRefreshTime = Time.time + _dropSpRefreshPeriod;
		}
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
			AffectorBase passiveAffector = affectorProcessor.ApplyAffectorValue(cachedMonsterTableData.passiveAffectorValueId[i], hitParameter, false);
			if (passiveAffector == null)
				continue;

			if (AffectorCustomCreator.IsContinuousAffector(passiveAffector.affectorType))
				_listPassiveAffector.Add(passiveAffector);
			else
				Debug.LogErrorFormat("Non-continuous affector in a passive skill! / AffectorValueId = {1}", cachedMonsterTableData.passiveAffectorValueId[i]);
		}
	}
	#endregion

	void CheckEliteMonster()
	{
		if (reservedAllyTeam || excludeMonsterCount)
			return;
		if (summonMonster || sequentialMonster != null)
			return;

		// 개발 편의성을 위해 엘리트 몬스터는 기존에 배치된 몬스터를 상황에 따라 수정해서 쓰는 형태로 개발하게 되었다.
		// 그러면서도 엘리트 몬스터가 나오지 않는 중저렙 챕터를 플레이할때 괜히 부하가 발생하는 것을 막기 위해
		// 사용할때만 마테리얼 변화 같은걸 적용하고 다 쓰고 꺼질때 초기형태로 되돌리기로 한다.
		// 그래서 엘리트 몬스터라고 따로 풀을 사용하는 것도 아니고 다 함께 같은 풀을 사용하기로 한다.

		bool needEliteMonster = (StageManager.instance.currentStageTableData.createEliteRate > 0.0f);
		// 현재 플레이 중인 스테이지에서 엘리트 몬스터를 필요로 하지 않는다면 그냥 리턴하면 끝.
		if (needEliteMonster == false)
			return;

		// 엘리트 몬스터가 필요한 상황이라면
		// 새로 스테이지를 진입해서 생성된 몬스터인지 혹은 강종으로 인한 재진입인지를 판단해서 처리해야한다.
		// Start에서 호출될때는 이미 IsLoadingInProgressGame가 끝나있는 상태일테니 별도로 기억해둔 값으로 판단해야한다.
		//if (ClientSaveData.instance.IsLoadingInProgressGame())
		if (BattleInstanceManager.instance.useCachedEliteInfo)
		{
			//Debug.Log("Use Cached Elite Monster Info");
			List<int> listEliteMonsterIndex = ClientSaveData.instance.GetCachedEliteMonsterIndexList();
			if (listEliteMonsterIndex.Contains(BattleInstanceManager.instance.monsterIndex))
				eliteMonster = true;
		}
		else
		{
			//Debug.Log("Check Elite Monster Info");

			// 재진입이 아니라면 랜덤하게 엘리트 속성을 부여하고
			float eliteRate = StageManager.instance.currentStageTableData.createEliteRate;
			if (eliteRate > 0.0f && Random.value <= eliteRate)
				eliteMonster = true;

			if (eliteMonster)
			{
				// 재진입을 위해서 기록을 해놓아야한다.
				ClientSaveData.instance.OnAddedEliteMonsterIndex(BattleInstanceManager.instance.monsterIndex);
			}
		}

		// 인덱스는 선별되지 않은 몹이더라도 항상 증가시켜야한다.
		++BattleInstanceManager.instance.monsterIndex;

		if (eliteMonster == false)
			return;

		// 마테리얼을 바꾸는 컴포넌트 적용하고(리셋도 이 스크립트가 담당한다.)
		EliteMonsterRim.ShowRim(cachedTransform);
	}

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
		
		if (bossMonster == false || (groupMonster && group.shareCurrentHp == false))
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
		
		if (bossMonster == false || (groupMonster && group.shareCurrentHp == false))
		{
			if (_monsterHPGauge != null)
			{
				_monsterHPGauge.gameObject.SetActive(false);
				_monsterHPGauge = null;
			}
		}

		monsterAI.ResetPath();
		//BehaviorDesigner.Runtime.BehaviorTree bt = GetComponent<BehaviorDesigner.Runtime.BehaviorTree>();
		//if (bt != null) bt.enabled = false;

		//Invoke("DisableObject", 1.2f);
		Timing.RunCoroutine(DieProcess());

		BattleManager.instance.OnDieMonster(this);
		BattleInstanceManager.instance.OnDieMonster(this);
		BattleInstanceManager.instance.OnFinalizePathFinderAgent(pathFinderController.agent.agentTypeID);
	}

	public void DisableForNodeWar()
	{
		// 실제 OnDie 처리를 호출하지 않은채 몬스터를 비활성화하는데에 필요한 코드들만 남긴다. Drop도 호출하지 않아야한다.
		monsterAI.ResetPath();

		if (bossMonster == false || (groupMonster && group.shareCurrentHp == false))
		{
			if (_monsterHPGauge != null)
			{
				_monsterHPGauge.gameObject.SetActive(false);
				_monsterHPGauge = null;
			}
		}

		// Destroy때도 호출이 되는 바람에 BattleInstanceManager.instance에 잘못 접근할때가 있다.
		if (StageManager.instance == null)
			return;
		if (CustomFollowCamera.instance == null || CameraFovController.instance == null || LobbyCanvas.instance == null)
			return;
		if (CustomFollowCamera.instance.gameObject == null)
			return;

		BattleManager.instance.OnDieMonster(this);
		BattleInstanceManager.instance.OnDieMonster(this);
		BattleInstanceManager.instance.OnFinalizePathFinderAgent(pathFinderController.agent.agentTypeID);
	}

	public void DieForNodeWar()
	{
		// 마법진 발동에 의해 죽는 연출을 해야하는거다.
		actionController.animator.speed = 0.0f;
		HitObject.EnableRigidbodyAndCollider(false, _rigidbody, _collider, null, false);
		DisableForNodeWar();

		Timing.RunCoroutine(DieProcessForNodeWar());
	}

	IEnumerator<float> DieProcessForNodeWar()
	{
		// 모두가 한번에 DieProcess되는걸 방지하기 위해 랜덤하게 기다린다.
		yield return Timing.WaitForSeconds(Random.Range(0.0f, 1.5f));
		Timing.RunCoroutine(DieProcess());
	}

	IEnumerator<float> DieProcess()
	{
		if (BurrowAffector.CheckDie(affectorProcessor))
			yield break;

		// 위 Burrow와 달리 추가작업만 하면 되는거라 yield break는 하지 않는다.
		BurrowOnStartAffector.CheckDie(affectorProcessor);

		bool needRestoreSuicide = false;
		float suicideLifeTime = -1.0f;
		bool suicide = SuicideAffector.CheckSuicide(affectorProcessor, ref suicideLifeTime, ref needRestoreSuicide);

		float waitTime = bossMonster ? 1.7f : 1.2f;
		if (cachedMonsterTableData.burnTime > 0.0f) waitTime = cachedMonsterTableData.burnTime;
		if (suicide && suicideLifeTime != -1.0f)
			waitTime = suicideLifeTime;
		yield return Timing.WaitForSeconds(waitTime);

		// avoid gc
		if (this == null)
			yield break;

		if (needRestoreSuicide)
		{
			SuicideAffector.RestoreRenderer(affectorProcessor);
			gameObject.SetActive(false);
		}
		else if (cachedMonsterTableData.flakeMultiplier > 0.0f)
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
			monsterAI.ResetPath();

		// pause기능을 별도로 만들까 하다가 어차피 변수들만 잘 관리할 수 있다면 pause는 괜히 업뎃 돌게하는거니 직접 enabled를 컨트롤 하기로 한다.
		monsterAI.enabled = enable;
	}

	#region Collision Damage
	// OnCollisionEnter 호출되는 프레임부터 같이 호출되기 때문에 Stay에서만 처리해도 괜찮다.
	// 원래 PlayerActor에서 처리하던건데 이랬더니 둘러쌓여도 Tick당 한번밖에 데미지를 입지 않아서 몬스터쪽으로 옮긴다.
	void OnCollisionStay(Collision collision)
	{
		if (team.teamId != (int)Team.eTeamID.DefaultMonster || excludeMonsterCount)
			return;

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

			// 일반 몬스터에 아군 몬스터가 닿으면 여기로 들어온다. 이때도 continue
			if (affectorProcessor.actor.IsMonsterActor())
				continue;

			if (CheckCollisionStayInterval())
				ApplyCollisionDamage(affectorProcessor);
		}
	}

	void ApplyCollisionDamage(AffectorProcessor defenderAffectorProcessor)
	{
		RushAffector rushAffector = (RushAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Rush);
		//JumpAffector jumpAffector = (JumpAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Jump);
		if (rushAffector != null)
		{
			eAffectorType affectorType = eAffectorType.BaseDamage;
			AffectorValueLevelTableData baseDamageAffectorValue = new AffectorValueLevelTableData();
			baseDamageAffectorValue.fValue1 = cachedMonsterTableData.collisionDamageRate * rushAffector.GetCollisionDamageRate();
			baseDamageAffectorValue.fValue4 = 1.0f;
			defenderAffectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, baseDamageAffectorValue, this, false);
		}
		//else if (jumpAffector != null)
		//{
		//	// 어차피 컬리더를 끄기 때문에 충돌뎀 OnCollisionStay 가 발생하지도 않는다. 패스
		//}
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
			// sequentialMonster몬스터의 경우 여기서 Last 체크가 된다. SummonMonster와 달리 총량을 알고있기 때문에 가능하다.
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
		// delayedSummonMonsterRefCount은 꼭 체크해줘야한다.
		// Summon 몬스터들은 플레이 중간에 나오는 애들이라 bossMonster로 처리되지도 않고 수량을 미리 알 방법도 없다.
		bool lastMonsterInStage = (BattleManager.instance.GetSpawnedMonsterCount() == 1 && BattleInstanceManager.instance.delayedSummonMonsterRefCount == 0);
		if (lastMonsterInStage)
		{
			// 스폰된 드랍템에 AfterBattle을 적용
			DropManager.instance.OnDropLastMonsterInStage();

			// 막타 이전에 죽은 몬스터의 DropProcessor에서 아직 스폰되지 않은 아이템이 남아있을 수 있으니
			// DropProcessor에도 적용해야한다.
			BattleInstanceManager.instance.OnDropLastMonsterInStage();
		}

		// summon 시그널로 만들어진 몬스터는 아무것도 드랍하지 않는다.
		if (summonMonster)
		{
			// EvilRich의 경우 잔몹을 소환하는데 하필 보스몹이 먼저 죽고 잔몹이 죽게되면
			// LastDropObject로 설정하는 부분이 호출되지 않아 습득이 안되게 된다.
			// 그래서 이렇게 강제로 호출해주기로 한다.
			if (lastMonsterInStage && BattleInstanceManager.instance.IsAliveAnyDropProcessor() == false && DropManager.instance.IsExistReservedLastDropObject())
				DropManager.instance.ApplyLastDropObject();
			return;
		}

		string dropId = "";
		if (cachedMonsterTableData.defaultDropUse && StageManager.instance.currentStageTableData != null)
		{
			if (bossMonster) dropId = StageManager.instance.currentStageTableData.defaultBossDropId;
			else dropId = StageManager.instance.currentStageTableData.defaultNormalDropId;
		}
		string addDropId = cachedMonsterTableData.addDropId;
		DropProcessor.Drop(cachedTransform, dropId, addDropId, lastMonsterInStage, false);

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
