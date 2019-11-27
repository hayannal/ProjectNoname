using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class BattleModeProcessorBase
{
	bool _mapLoaded = false;
	bool _monsterSpawned = false;
	int _monsterSpawnCount = 0;
	int _damageCountInStage = 0;

	public void OnPreInstantiateMap()
	{
		PlayerIndicatorCanvas.Show(false, null);

		if (_powerSourceObject != null)
		{
			_powerSourceObject.gameObject.SetActive(false);
			_powerSourceObject = null;
		}

		RailMonster.OnPreInstantiateMap();
		BattleInstanceManager.instance.DisableHitObjectMoving();
		BattleInstanceManager.instance.FinalizeAllPositionBuffAffector(true);
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
		_damageCountInStage = 0;

		// environmentSetting처럼 있으면 랜덤하게 골라서 적용하고 없을땐 그냥 냅둬서 유지시킨다.
		if (StageManager.instance.currentStageTableData != null && StageManager.instance.currentStageTableData.stagePenaltyId.Length > 0)
		{
			string stagePenaltyId = StageManager.instance.currentStageTableData.stagePenaltyId[Random.Range(0, StageManager.instance.currentStageTableData.stagePenaltyId.Length)];
			if (BattleInstanceManager.instance.playerActor != null)
				BattleInstanceManager.instance.playerActor.RefreshStagePenaltyAffector(stagePenaltyId, true);
		}

		if (StageManager.instance.spawnPowerSourcePrefab)
			_powerSourceObject = BattleInstanceManager.instance.GetCachedObject(StageManager.instance.GetPreparedPowerSourcePrefab(), StageManager.instance.currentPowerSourceSpawnPosition, Quaternion.identity);
		else
		{
			if (BattleInstanceManager.instance.playerActor != null)
				CallAffectorValueAffector.OnEvent(BattleInstanceManager.instance.playerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnStartStage);
		}
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
			BattleManager.instance.GetStackedDropExp();
			if (LevelUpIndicatorCanvas.IsShow() || BattleManager.instance.reservedLevelPackCount > 0)
			{
				// 게이트 필라 생성하는 타이밍이 카운트를 지정하기에 가장 적당한 곳이다.
				LevelUpIndicatorCanvas.SetTargetLevelUpCount(StageManager.instance.needLevelUpCount + BattleManager.instance.reservedLevelPackCount);
				StageManager.instance.needLevelUpCount = BattleManager.instance.reservedLevelPackCount = 0;
			}
			else
			{
				BattleManager.instance.OnClearStage();
			}
		}
	}

	public void OnClearStage()
	{
		bool showPlayerIndicator = false;
		if (StageManager.instance.currentStageTableData != null && StageManager.instance.currentStageTableData.swap && PlayerData.instance.swappable)
			showPlayerIndicator = true;

		if (showPlayerIndicator == false)
		{
			ShowGatePillar();
			return;
		}

		PlayerIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform);

		Timing.RunCoroutine(DelayedShowGatePillar(1.3f));
	}

	IEnumerator<float> DelayedShowGatePillar(float delayTime)
	{
		yield return Timing.WaitForSeconds(delayTime);

		// avoid gc
		if (this == null)
			yield break;

		ShowGatePillar();
	}

	void ShowGatePillar()
	{
		BattleInstanceManager.instance.GetCachedObject(string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossPreviewAddress) ? StageManager.instance.gatePillarPrefab : StageManager.instance.bossGatePillarPrefab,
			StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
	}



	public int GetSpawnedMonsterCount()
	{
		return _monsterSpawnCount;
	}

	public void AddDamageCountOnStage()
	{
		++_damageCountInStage;
	}

	public int GetDamageCountOnStage()
	{
		return _damageCountInStage;
	}
}
