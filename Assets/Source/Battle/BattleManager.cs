using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
	public static BattleManager instance;

	public GameObject targetCircleObject;
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

	public GameObject floatingDamageTextRootCanvasPrefab;
	public GameObject pauseCanvasPrefab;
	public GameObject battleResultPrefab;

	public GameObject[] dropObjectPrefabList;
	public GameObject portalPrefab;
	public GameObject portalMoveEffectPrefab;
	public GameObject portalGaugePrefab;

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
	}

	BattleModeProcessorBase _currentBattleMode = null;

	public void Initialize(eBattleMode battleMode = eBattleMode.DefaultMode)
	{
		switch (battleMode)
		{
			case eBattleMode.DefaultMode:
				_currentBattleMode = new BattleModeProcessorBase();
				break;
		}
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
}
