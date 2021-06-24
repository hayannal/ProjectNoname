﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
	public static BattleManager instance;

	public GameObject targetCircleObject;
	public GameObject targetCircleSleepObject;
	public GameObject monsterDieAshParticlePrefab;
	public AnimationCurveAsset monsterDieDissolveCurve;
	public AnimationCurveAsset bossMonsterDieDissolveCurve;
	public GameObject playerSpawnEffectPrefab;
	public GameObject rangeIndicatorPrefab;

	public GameObject monsterHPGaugeRootCanvasPrefab;
	public GameObject monsterHPGaugePrefab;
	public GameObject bossMonsterHPGaugePrefab;
	public GameObject playerHPGaugePrefab;
	public GameObject playerIgnoreEvadeCanvasPrefab;
	public GameObject skillSlotCanvasPrefab;
	public GameObject ultimateCirclePrefab;
	public GameObject battleToastCanvasPrefab;

	public GameObject playerIndicatorPrefab;
	public GameObject levelUpIndicatorPrefab;
	public GameObject playerLevelUpEffectPrefab;
	public GameObject levelPackGainEffectPrefab;
	public GameObject healEffectPrefab;

	public GameObject damageCanvasPrefab;
	public GameObject floatingDamageTextRootCanvasPrefab;
	public GameObject pauseCanvasPrefab;
	public GameObject battleResultPrefab;

	public GameObject portalPrefab;
	public GameObject portalMoveEffectPrefab;
	public GameObject portalGaugePrefab;

	public GameObject onOffColliderAreaPrefab;

	// for level pack
	public GameObject diagonalNwayGeneratorPrefab;
	public GameObject leftRightNwayGeneratorPrefab;
	public GameObject backNwayGeneratorPrefab;

	void Awake()
	{
		instance = this;

		if (_currentBattleMode == null)
			Initialize(eBattleMode.DefaultMode);
	}

	public enum eBattleMode
	{
		DefaultMode,
		NodeWar,
		BossBattle,
	}

	BattleModeProcessorBase _currentBattleMode = null;

	eBattleMode _battleMode;
	public void Initialize(eBattleMode battleMode = eBattleMode.DefaultMode)
	{
		switch (battleMode)
		{
			case eBattleMode.DefaultMode:
				_currentBattleMode = new BattleModeProcessorBase();
				break;
			case eBattleMode.NodeWar:
				_currentBattleMode = new NodeWarProcessor();
				break;
			case eBattleMode.BossBattle:
				_currentBattleMode = new BossBattleProcessor();
				break;
		}
		_battleMode = battleMode;
	}

	public bool IsDefaultBattle()
	{
		return (_battleMode == eBattleMode.DefaultMode);
	}

	public bool IsNodeWar()
	{
		return (_battleMode == eBattleMode.NodeWar);
	}

	public bool IsBossBattle()
	{
		return (_battleMode == eBattleMode.BossBattle);
	}

	void Update()
	{
		_currentBattleMode.Update();
	}

	public void OnStartBattle()
	{
		_currentBattleMode.OnStartBattle();
	}

	public void OnPreInstantiateMap()
	{
		_currentBattleMode.OnPreInstantiateMap();
	}

	public void OnLoadedMap()
	{
		_currentBattleMode.OnLoadedMap();
	}

	public void OnSpawnFlag()
	{
		_currentBattleMode.OnSpawnFlag();
	}

	public void OnSpawnMonster(MonsterActor monsterActor)
	{
		_currentBattleMode.OnSpawnMonster(monsterActor);
	}

	public void OnDiePlayer(PlayerActor playerActor)
	{
		_currentBattleMode.OnDiePlayer(playerActor);
	}

	public void OnDieMonster(MonsterActor monsterActor)
	{
		_currentBattleMode.OnDieMonster(monsterActor);
	}

	public void OnClearStage()
	{
		_currentBattleMode.OnClearStage();
	}

	public bool IsAutoPlay()
	{
		return _currentBattleMode.IsAutoPlay();
	}

	public int GetSpawnedMonsterCount()
	{
		return _currentBattleMode.GetSpawnedMonsterCount();
	}

	public void AddDamageCountOnStage()
	{
		_currentBattleMode.AddDamageCountOnStage();
	}

	public int GetDamageCountOnStage()
	{
		return _currentBattleMode.GetDamageCountOnStage();
	}

	public int GetClearPoint()
	{
		return _currentBattleMode.GetClearPoint();
	}

	public void AddClearPoint(int add)
	{
		_currentBattleMode.AddClearPoint(add);
	}

	public int GetAppliedChallengeRetryBonusClearPoint()
	{
		return _currentBattleMode.GetAppliedChallengeRetryBonusClearPoint();
	}

	public void ResetAppliedChallengeRetryBonusClearPoint()
	{
		_currentBattleMode.ResetAppliedChallengeRetryBonusClearPoint();
	}

	#region NodeWar
	public void OnSelectedNodeWarLevel(int level)
	{
		_currentBattleMode.OnSelectedNodeWarLevel(level);
	}

	public NodeWarTableData GetSelectedNodeWarTableData()
	{
		return _currentBattleMode.GetSelectedNodeWarTableData();
	}

	public bool IsFirstClearNodeWar()
	{
		return _currentBattleMode.IsFirstClear();
	}

	public virtual bool IsSacrificePhase()
	{
		return _currentBattleMode.IsSacrificePhase();
	}

	public virtual float GetSpawnCountRate(string monsterId)
	{
		return _currentBattleMode.GetSpawnCountRate(monsterId);
	}

	public void OnGetSoul(Vector3 getPosition)
	{
		_currentBattleMode.OnGetSoul(getPosition);
	}

	public void OnGetHealOrb(Vector3 getPosition)
	{
		_currentBattleMode.OnGetHealOrb(getPosition);
	}

	public void OnGetSpHealOrb(Vector3 getPosition)
	{
		_currentBattleMode.OnGetSpHealOrb(getPosition);
	}

	public void OnGetBoostOrb(Vector3 getPosition)
	{
		_currentBattleMode.OnGetBoostOrb(getPosition);
	}

	public void OnGetInvincibleOrb(Vector3 getPosition)
	{
		_currentBattleMode.OnGetInvincibleOrb(getPosition);
	}


	public void OnTryActiveExitArea()
	{
		_currentBattleMode.OnTryActiveExitArea();
	}

	public void On10SecondAgoActiveExitArea()
	{
		_currentBattleMode.On10SecondAgoActiveExitArea();
	}

	public void OnSuccessExitArea()
	{
		_currentBattleMode.OnSuccessExitArea();
	}
	#endregion
}
