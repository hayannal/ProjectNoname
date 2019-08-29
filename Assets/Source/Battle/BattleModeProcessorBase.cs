using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleModeProcessorBase
{
	bool _mapLoaded = false;
	bool _monsterSpawned = false;
	int _monsterSpawnCount = 0;

	public void OnPreInstantiateMap()
	{
		if (_playerIndicatorCanvas != null)
		{
			_playerIndicatorCanvas.gameObject.SetActive(false);
			_playerIndicatorCanvas = null;
		}

		if (_powerSourceObject != null)
		{
			_powerSourceObject.gameObject.SetActive(false);
			_powerSourceObject = null;
		}
	}

	public void OnLoadedMap()
	{
		_mapLoaded = true;
		_monsterSpawned = false;
		_monsterSpawnCount = 0;
	}

	GameObject _powerSourceObject;
	public void OnSpawnFlag()
	{
		if (StageManager.instance.spawnPowerSourcePrefab)
			_powerSourceObject = BattleInstanceManager.instance.GetCachedObject(StageManager.instance.GetCurrentPowerSourcePrefab(), StageManager.instance.currentPowerSourceSpawnPosition, Quaternion.identity);
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
			BattleManager.instance.GetStackedDropExp();
			// 경험치 얻는 처리를 한다.
			// 이펙트가 먼저 나오고 곧바로 렙업창이 뜬다. 두번 이상 렙업 되는걸 처리하기 위해 업데이트 돌면서 스택에 쌓아둔채 꺼내쓰는 방법으로 해야할거다.

			// all kill monster
			BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);

			if (StageManager.instance.currentStageTableData != null && StageManager.instance.currentStageTableData.swap)
			{
				_playerIndicatorCanvas = (PlayerIndicatorCanvas)UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(StageManager.instance.playerIndicatorPrefab);
				_playerIndicatorCanvas.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
			}
		}
	}

	public int GetSpawnedMonsterCount()
	{
		return _monsterSpawnCount;
	}
}
