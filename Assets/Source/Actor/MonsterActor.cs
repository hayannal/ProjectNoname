using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class MonsterActor : Actor
{
	public float monsterHpGaugeWidth = 1.0f;
	public float monsterHpGaugeOffsetY = 0.0f;

	public bool bossMonster { get; private set; }
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

	void Update()
	{
		UpdateDieDissolve();
	}

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

		MonsterTableData monsterTableData = TableDataManager.instance.FindMonsterTableData(actorId);
		bossMonster = monsterTableData.boss;

		team.teamID = (int)Team.eTeamID.DefaultMonster;
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

		actorStatus.InitializeMonsterStatus(actorId);
		monsterAI.InitializeAI();

		if (bossMonster)
			BossMonsterGaugeCanvas.instance.InitializeGauge(this);

		monsterAI.OnEventAnimatorParameter(MonsterAI.eAnimatorParameterForAI.fHpRatio, actorStatus.GetHPRatio());

		BattleManager.instance.OnSpawnMonster(this);
		BattleInstanceManager.instance.OnInitializePathFinderAgent(pathFinderController.agent.agentTypeID);
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

	void DisableObject()
	{
		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.monsterDisableEffectObject, cachedTransform.position, Quaternion.identity);
		gameObject.SetActive(false);
	}

	#region Die Dissolve
	static int s_dissolvePropertyID;
	static int s_cutoffPropertyID;
	List<Material> _listCachedMaterial;
	float _dissolveStartTime;
	IEnumerator<float> DieProcess()
	{
		yield return Timing.WaitForSeconds(1.2f);

		if (s_dissolvePropertyID == 0) s_dissolvePropertyID = Shader.PropertyToID("_UseDissolve");
		if (s_cutoffPropertyID == 0) s_cutoffPropertyID = Shader.PropertyToID("_Cutoff");

		if (_listCachedMaterial == null)
		{
			_listCachedMaterial = new List<Material>();
			Renderer[] renderers = GetComponentsInChildren<Renderer>();
			for (int i = 0; i < renderers.Length; ++i)
			{
				for (int j = 0; j < renderers[i].materials.Length; ++j)
				{
					if (renderers[i].materials[j].HasProperty(s_dissolvePropertyID))
						_listCachedMaterial.Add(renderers[i].materials[j]);
				}
			}
		}

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].EnableKeyword("_DISSOLVE");
		}

		_dissolveStartTime = Time.time;
		_updateDissolveCutoff = true;
		yield break;
	}

	bool _updateDissolveCutoff = false;
	void UpdateDieDissolve()
	{
		if (!_updateDissolveCutoff)
			return;

		float fTime = Time.time - _dissolveStartTime;

		AnimationCurveAsset curveAsset = bossMonster ? BattleManager.instance.bossMonsterDieDissolveCurve : BattleManager.instance.monsterDieDissolveCurve;
		if (fTime > curveAsset.curve.keys[curveAsset.curve.length-1].time)
		{
			ResetDissolve();
			gameObject.SetActive(false);
			return;
		}
		float result = curveAsset.curve.Evaluate(fTime);

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].SetFloat(s_cutoffPropertyID, result);
		}
	}

	void ResetDissolve()
	{
		if (_listCachedMaterial == null)
			return;

		for (int i = 0; i < _listCachedMaterial.Count; ++i)
		{
			if (_listCachedMaterial[i] == null)
				continue;
			_listCachedMaterial[i].DisableKeyword("_DISSOLVE");
			_listCachedMaterial[i].SetFloat(s_cutoffPropertyID, 0.0f);
		}
		_updateDissolveCutoff = false;
	}
	#endregion
}
