using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleModeProcessorBase
{
	bool _mapLoaded = false;
	bool _monsterSpawned = false;
	int _monsterSpawnCount = 0;

	public void OnLoadedMap()
	{
		_mapLoaded = true;
		_monsterSpawned = false;
		_monsterSpawnCount = 0;
	}

	public void OnSpawnPlayer(PlayerActor playerActor)
	{
	}

	public void OnSpawnMonster(MonsterActor monsterActor)
	{
		_monsterSpawned = true;
		++_monsterSpawnCount;
	}

	public void OnDiePlayer(PlayerActor playerActor)
	{
	}

	public void OnDieMonster(MonsterActor monsterActor)
	{
		--_monsterSpawnCount;
		if (_mapLoaded && _monsterSpawned && _monsterSpawnCount == 0)
		{
			// all kill monster
		}
	}
}
