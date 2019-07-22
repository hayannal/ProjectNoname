using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
	public static BattleManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("BattleManager")).AddComponent<BattleManager>();
			return _instance;
		}
	}
	static BattleManager _instance = null;

	public GameObject targetCircleObject;

	void Awake()
	{
		_instance = this;
	}

	public enum eBattleMode
	{
		DefaultMode,
	}

	BattleModeProcessorBase _currentBattleMode = null;

	void Start()
	{
		if (_currentBattleMode == null)
			Initialize(eBattleMode.DefaultMode);
	}

	public void Initialize(eBattleMode battleMode = eBattleMode.DefaultMode)
	{
		switch (battleMode)
		{
			case eBattleMode.DefaultMode:
				_currentBattleMode = new BattleModeProcessorBase();
				break;
		}
	}

	public void OnLoadedMap()
	{
		_currentBattleMode.OnLoadedMap();
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
}
