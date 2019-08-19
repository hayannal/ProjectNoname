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

		if (_playerIndicatorCanvas != null)
		{
			_playerIndicatorCanvas.gameObject.SetActive(false);
			_playerIndicatorCanvas = null;
		}
	}

	public void OnSpawnFlag()
	{
		if (StageManager.instance.spawnPowerSourcePrefab)
			BattleInstanceManager.instance.GetCachedObject(StageManager.instance.GetCurrentPowerSourcePrefab(), StageManager.instance.currentPowerSourceSpawnPosition, Quaternion.identity);
	}

	public void OnSpawnPlayer(PlayerActor playerActor)
	{
		BattleInstanceManager.instance.playerActor = playerActor;
	}

	public void OnSpawnMonster(MonsterActor monsterActor)
	{
		_monsterSpawned = true;
		++_monsterSpawnCount;
	}

	public void OnDiePlayer(PlayerActor playerActor)
	{
	}

	PlayerIndicatorCanvas _playerIndicatorCanvas;
	public void OnDieMonster(MonsterActor monsterActor)
	{
		--_monsterSpawnCount;
		if (_mapLoaded && _monsterSpawned && _monsterSpawnCount == 0)
		{
			// all kill monster
			BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

			if (StageManager.instance.currentStageSwappable)
			{
				_playerIndicatorCanvas = (PlayerIndicatorCanvas)UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(StageManager.instance.playerIndicatorPrefab);
				_playerIndicatorCanvas.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
			}
		}
	}
}
