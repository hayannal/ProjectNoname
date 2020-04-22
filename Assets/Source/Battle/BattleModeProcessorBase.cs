//#define HUDDPS

using System;
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

	public void Update()
	{
		UpdateEndProcess();
	}

	DateTime _startDateTime;
	public void OnStartBattle()
	{
		_startDateTime = DateTime.Now;
	}

	public void OnPreInstantiateMap()
	{
		PlayerIndicatorCanvas.Show(false, null);

		if (_powerSourceObject != null)
		{
			_powerSourceObject.gameObject.SetActive(false);
			_powerSourceObject = null;
		}

		RailMonster.OnPreInstantiateMap();
		BattleInstanceManager.instance.FinalizeAllPositionBuffAffector(true);
		BattleInstanceManager.instance.DisableAllHitObjectMoving();
		BattleInstanceManager.instance.FinalizeAllHitObject();
		BattleInstanceManager.instance.FinalizeAllSummonObject();
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
			string stagePenaltyId = StageManager.instance.currentStageTableData.stagePenaltyId[UnityEngine.Random.Range(0, StageManager.instance.currentStageTableData.stagePenaltyId.Length)];
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

#if HUDDPS
#if UNITY_EDITOR
		HUDDPS.instance.OnStartStage(StageManager.instance.playChapter, StageManager.instance.playStage, StageManager.instance.bossStage);
#endif
#endif
	}

	public void OnSpawnMonster(MonsterActor monsterActor)
	{
		_monsterSpawned = true;
		++_monsterSpawnCount;
	}

	public void OnDiePlayer(PlayerActor playerActor)
	{
		// 여기서 인풋은 막되
		LobbyCanvas.instance.battlePauseButton.interactable = false;

		// 바로 정산처리 하면 안되고
		// 드랍 아이템 존재하는지 확인 후 없으면 바로 1초 타이머를 센다.
		// 획득할 수 있는 드랍 아이템이 존재하면 다 획득 후 1초 타이머를 센다.
		// 이건 마지막 보스 클리어 후에도 동일하게 적용해야한다.
		_endProcess = true;
		_endProcessWaitRemainTime = 2.0f;
	}

	public void OnDieMonster(MonsterActor monsterActor)
	{
		--_monsterSpawnCount;
		if (_mapLoaded && _monsterSpawned && _monsterSpawnCount == 0 && BattleInstanceManager.instance.CheckFinishSequentialMonster())
		{
			// all kill monster
			DropManager.instance.GetStackedDropExp();
			if (LevelUpIndicatorCanvas.IsShow() || DropManager.instance.reservedLevelPackCount > 0)
			{
				// 게이트 필라 생성하는 타이밍이 카운트를 지정하기에 가장 적당한 곳이다.
				LevelUpIndicatorCanvas.SetTargetLevelUpCount(StageManager.instance.needLevelUpCount + DropManager.instance.reservedLevelPackCount);
				StageManager.instance.needLevelUpCount = DropManager.instance.reservedLevelPackCount = 0;
			}
			else
			{
				BattleManager.instance.OnClearStage();
			}

#if HUDDPS
#if UNITY_EDITOR
			HUDDPS.instance.OnClearStage();
#endif
#endif
		}
	}

	public void OnClearStage()
	{
		// last stage
		if (StageManager.instance.playStage == StageManager.instance.GetMaxStage(StageManager.instance.playChapter))
		{
			LobbyCanvas.instance.battlePauseButton.interactable = false;
			_endProcess = true;
			_endProcessWaitRemainTime = 3.0f;
			return;
		}

		bool showPlayerIndicator = false;
		if (StageManager.instance.currentStageTableData != null && StageManager.instance.currentStageTableData.swap && PlayerData.instance.swappable)
			showPlayerIndicator = true;

		if (showPlayerIndicator == false)
		{
			ShowGatePillar();
			return;
		}

		PlayerIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform);

		Timing.RunCoroutine(DelayedShowGatePillar(0.1f));
	}

	#region EndGame
	bool _endProcess = false;
	float _endProcessWaitRemainTime = 0.0f;	// 최소 대기타임
	void UpdateEndProcess()
	{
		if (_endProcess == false)
			return;

		if (_endProcessWaitRemainTime > 0.0f)
		{
			_endProcessWaitRemainTime -= Time.deltaTime;
			if (_endProcessWaitRemainTime <= 0.0f)
				_endProcessWaitRemainTime = 0.0f;
			return;
		}

		// 현재 드랍템 동기화 구조는 템을 먹을때마다 미리 패킷을 보내뒀다가 정산때 쓰는 방식이 아니라
		// 먹었던걸 기억하고 있다가 마지막 패킷 날릴때 몰아서 보내는 구조다보니
		// 모든 드랍을 먹고나서 정산 패킷을 보내야만 한다. 안그러면 템을 저장할 수 없게 된다.
		if (DropManager.instance.IsExistAcquirableDropObject())
		{
			// 하나라도 존재하면 waitRemainTime을 늘려서 대기시켰다가 체크한다.
			_endProcessWaitRemainTime = 0.333f;
			return;
		}

		if (PlayerData.instance.clientOnly)
		{
			_endProcess = false;
			BattleResultCanvas.instance.gameObject.SetActive(true);
			return;
		}

		bool clear = false;
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie() == false)
		{
			HitObject.EnableRigidbodyAndCollider(false, null, BattleInstanceManager.instance.playerActor.GetCollider());
			if (StageManager.instance.playStage == StageManager.instance.GetMaxStage(StageManager.instance.playChapter))
				clear = true;
		}

		PlayFabApiManager.instance.RequestEndGame(clear, PlayerData.instance.currentChaosMode, StageManager.instance.playChapter, StageManager.instance.playStage - 1,
			DropManager.instance.GetStackedDropGold(), DropManager.instance.GetStackedDropSeal(), DropManager.instance.GetStackedDropEquipList(), (result, newCharacterId, itemGrantString) =>
		{
			// 정보를 갱신하기 전에 먼저 BattleResult를 보여준다.
			BattleResultCanvas.instance.RefreshChapterInfo(itemGrantString);
			OnRecvEndGame(result, newCharacterId, itemGrantString);
		});

		_endProcess = false;
	}

	void OnRecvEndGame(bool clear, string newCharacterId, string itemGrantString)
	{
		if (PlayerData.instance.currentChaosMode)
		{
			// 클리어 여부에 상관없이 purify를 채워야한다. 어차피 최대가 되면 로비에서 알아서 최대치 표시로 넘어갈테니 여기선 수량증가만 해둔다.
			++PlayerData.instance.purifyCount;
		}
		if (PlayerData.instance.currentChaosMode == false && PlayerData.instance.highestPlayChapter == PlayerData.instance.selectedChapter)
		{
			int prevHighestStage = PlayerData.instance.highestClearStage;
			if (clear)
			{
				PlayerData.instance.highestPlayChapter += 1;
				PlayerData.instance.highestClearStage = 0;
				PlayerData.instance.selectedChapter += 1;

				int chapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
				if (PlayerData.instance.highestPlayChapter >= chapterLimit)
					PlayerData.instance.chaosMode = true;

				EventManager.instance.OnEventPlayHighestStage(PlayerData.instance.highestPlayChapter - 1, prevHighestStage, 50);
				EventManager.instance.OnEventClearHighestChapter(PlayerData.instance.highestPlayChapter, newCharacterId);
			}
			else
			{
				if (PlayerData.instance.highestClearStage < StageManager.instance.playStage - 1)
				{
					PlayerData.instance.highestClearStage = StageManager.instance.playStage - 1;
					EventManager.instance.OnEventPlayHighestStage(PlayerData.instance.highestPlayChapter, prevHighestStage, PlayerData.instance.highestClearStage);
				}

				if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Chaos))
				{
					PlayerData.instance.chaosMode = true;
					PlayerData.instance.purifyCount = 0;
				}

				EventManager.instance.OnEventPlayHighestChapter(PlayerData.instance.highestPlayChapter);
			}
		}
		CurrencyData.instance.gold += DropManager.instance.GetStackedDropGold();
		PlayerData.instance.sealCount += DropManager.instance.GetStackedDropSeal();

		if (itemGrantString != "")
			TimeSpaceData.instance.OnRecvItemGrantResult(itemGrantString, true);

		// 클리어 했다면 시간 체크 한번 해본다.
		// 강종으로 인한 재접속때 안하는거 추가해야한다.
		if (clear)  // && IsRetryByCrash == false
		{
			TimeSpan timeSpan = DateTime.Now - _startDateTime;
			bool sus = false;
			if (timeSpan < TimeSpan.FromMinutes(10) && PlayerData.instance.highestPlayChapter == PlayerData.instance.selectedChapter)
				sus = true;
			if (sus == false && timeSpan < TimeSpan.FromSeconds(30))
				sus = true;
			if (sus)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.FastEndGame, true, (int)timeSpan.TotalSeconds);
		}
	}
	#endregion

	public bool IsAutoPlay()
	{
		if (_monsterSpawned)
		{
			if (_monsterSpawnCount > 0)
				return true;
		}
		return false;
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
		BattleInstanceManager.instance.GetCachedObject(string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossName) ? StageManager.instance.gatePillarPrefab : StageManager.instance.bossGatePillarPrefab,
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
