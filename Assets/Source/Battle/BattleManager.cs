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

	public GameObject monsterHPGaugeRootCanvasPrefab;
	public GameObject monsterHPGaugePrefab;
	public GameObject bossMonsterHPGaugePrefab;
	public GameObject playerHPGaugePrefab;
	public GameObject skillSlotCanvasPrefab;

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

	public void OnSpawnPlayer(PlayerActor playerActor)
	{
		_currentBattleMode.OnSpawnPlayer(playerActor);
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

	public int GetSpawnedMonsterCount()
	{
		return _currentBattleMode.GetSpawnedMonsterCount();
	}
}
