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

	public virtual void Update()
	{
		UpdateEndProcess();
	}

	DateTime _startDateTime;
	public virtual void OnStartBattle()
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

	public virtual void OnLoadedMap()
	{
		_mapLoaded = true;
		_monsterSpawned = false;
		_monsterSpawnCount = 0;

		// 사실 로비에서는 BattleManager가 만들어지기 전이라 호출되지 않는다.
		if (ClientSaveData.instance.IsLoadingInProgressGame() == false)
		{
			// 다른 것도 저장할수는 있는데 제일 필수인거만 골라서 저장해둔다. 괜히 많아져봤자 느려질까봐 필수 3개만 고른거다.
			ClientSaveData.instance.OnChangedStage(StageManager.instance.playStage);
			ClientSaveData.instance.OnChangedMonsterAllKill(false);
			ClientSaveData.instance.OnChangedGatePillar(false);

			// 골드는 DropObject 개별로 하는거보다 한번에 하는게 나아서 여기서 하기로 한다.
			ClientSaveData.instance.OnChangedDropGold(DropManager.instance.GetStackedFloatDropGold());
		}
	}

	GameObject _powerSourceObject;
	public void OnSpawnFlag()
	{
		_damageCountInStage = 0;

		if (BattleInstanceManager.instance.playerActor != null)
		{	
			string stagePenaltyId = "";

			// 먼저 재진입중인지를 판단해서 저장된 값을 구해오고
			if (ClientSaveData.instance.IsLoadingInProgressGame())
				stagePenaltyId = ClientSaveData.instance.GetCachedStagePenalty();

			// 저장된 값이 없거나 재진입중이 아니라면 기존 루틴대로 진행한다.
			if (string.IsNullOrEmpty(stagePenaltyId))
			{
				if (StageManager.instance.currentStageTableData != null && StageManager.instance.currentStageTableData.stagePenaltyId.Length > 0)
					stagePenaltyId = StageManager.instance.currentStageTableData.stagePenaltyId[UnityEngine.Random.Range(0, StageManager.instance.currentStageTableData.stagePenaltyId.Length)];
			}

			if (string.IsNullOrEmpty(stagePenaltyId) == false)
				BattleInstanceManager.instance.playerActor.RefreshStagePenaltyAffector(stagePenaltyId, true);
		}

		if (StageManager.instance.spawnPowerSourcePrefab)
			_powerSourceObject = BattleInstanceManager.instance.GetCachedObject(StageManager.instance.GetPreparedPowerSourcePrefab(), StageManager.instance.currentPowerSourceSpawnPosition, Quaternion.identity);
		else
		{
			if (ClientSaveData.instance.IsLoadingInProgressGame() && ClientSaveData.instance.GetCachedMonsterAllKill())
			{
			}
			else
			{
				if (BattleInstanceManager.instance.playerActor != null)
					CallAffectorValueAffector.OnEvent(BattleInstanceManager.instance.playerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnStartStage);
			}
		}

#if HUDDPS
#if UNITY_EDITOR
		HUDDPS.instance.OnStartStage(StageManager.instance.playChapter, StageManager.instance.playStage, StageManager.instance.bossStage);
#endif
#endif

		InitializeInProgressGame();
	}

	void InitializeInProgressGame()
	{
		// OnSpawnFlag의 마지막 부분이 플레이어를 복구하기 가장 적절한 타이밍이다.
		if (ClientSaveData.instance.IsLoadingInProgressGame() == false)
			return;

		int exp = ClientSaveData.instance.GetCachedExp();
		StageManager.instance.SetLevelExpForInProgressGame(exp);

		float hpRatio = ClientSaveData.instance.GetCachedHpRatio();
		float spRatio = ClientSaveData.instance.GetCachedSpRatio();
		BattleInstanceManager.instance.playerActor.actorStatus.SetHpRatio(hpRatio);
		BattleInstanceManager.instance.playerActor.actorStatus.SetSpRatio(spRatio);
		PlayerGaugeCanvas.instance.InitializeGauge(BattleInstanceManager.instance.playerActor);
		SkillSlotCanvas.instance.OnChangedSP(BattleInstanceManager.instance.playerActor);

		// 이미 획득해둔 레벨팩들을 복구.
		// 근데 여기서 이미 발동되었거나 하는 것들 고려해서 복구해야한다.
		// 캐릭터 고유 스킬과 달리 레벨팩은 추가로 저장할 요소가 없기 때문에 리스트만 가지고 복구하면 된다.
		string jsonCachedLevelPackData = ClientSaveData.instance.GetCachedLevelPackData();
		if (string.IsNullOrEmpty(jsonCachedLevelPackData) == false)
			LevelPackDataManager.instance.SetInProgressLevelPackData(jsonCachedLevelPackData);

		// 캐릭터 다 했으면 스테이지 상태도 복구
		if (ClientSaveData.instance.GetCachedMonsterAllKill())
		{
			// 그 다음엔 레벨팩 획득창을 복구. 선택하지 않은채로 종료됐었다면 복구하는데 1회 선택했었다면 그건 제외하고 남은 카운트만큼만 복구해주면 된다.
			int remainLevelUpCount = ClientSaveData.instance.GetCachedRemainLevelUpCount();
			int remainLevelPackCount = ClientSaveData.instance.GetCachedRemainLevelPackCount();
			int remainNoHitLevelPackCount = ClientSaveData.instance.GetCachedRemainNoHitLevelPackCount();
			int targetLevelUpCount = remainLevelUpCount + remainLevelPackCount + remainNoHitLevelPackCount;
			LevelUpIndicatorCanvas.SetTargetLevelUpCount(targetLevelUpCount);
			if (targetLevelUpCount > 0)
			{
				if (remainLevelUpCount > 0)
					LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, remainLevelUpCount, 0, 0);
				if (remainLevelPackCount > 0)
					LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, 0, remainLevelPackCount, 0);
				if (remainNoHitLevelPackCount > 0)
					LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, 0, 0, remainNoHitLevelPackCount);
			}
			else
			{
				if (ClientSaveData.instance.GetCachedGatePillar())
					OnClearStage();
			}
		}
		else
		{
			// 파워소스 나오는 층에선 MonsterAllKill이 꺼져있지만 게이트필라만 처리될 수 있다.
			if (ClientSaveData.instance.GetCachedGatePillar())
				OnClearStage();
		}

		// 파워소스는 파워소스쪽에서 캐싱된 정보 읽어서 처리한다.

		// 남은건 획득 아이템 리스트다.
		List<string> listDropItemId = ClientSaveData.instance.GetCachedDropItemList();
		for (int i = 0; i < listDropItemId.Count; ++i)
			DropManager.instance.AddDropItem(listDropItemId[i]);

		// 골드와 인장은 마지막 축적된 값을 기억해놨다가 가져오면 된다. 위의 템과 달리 중요도가 낮아서 DropObject의 획득 시점에 기록하는거로 되어있다.
		float dropGold = ClientSaveData.instance.GetCachedDropGold();
		DropManager.instance.AddDropGold(dropGold);
		int dropSeal = ClientSaveData.instance.GetCachedDropSeal();
		DropManager.instance.AddDropSeal(dropSeal);

		// 끝나면 ClientSaveData에 로드 완료를 알린다.
		ClientSaveData.instance.OnFinishLoadGame();
	}

	public virtual void OnSpawnMonster(MonsterActor monsterActor)
	{
		_monsterSpawned = true;
		++_monsterSpawnCount;
	}

	public virtual void OnDiePlayer(PlayerActor playerActor)
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

	public virtual void OnDieMonster(MonsterActor monsterActor)
	{
		--_monsterSpawnCount;
		if (_mapLoaded && _monsterSpawned && _monsterSpawnCount == 0 && BattleInstanceManager.instance.CheckFinishSequentialMonster())
		{
			// all kill monster
			DropManager.instance.GetStackedDropExp();

			// 한 층이 끝날땐 관련 정보들을 저장해놔야한다. 레벨업을 했다면 hp회복 역시 반영되서 저장될 것이다.
			ClientSaveData.instance.OnChangedExp(StageManager.instance.playerExp);
			ClientSaveData.instance.OnChangedMonsterAllKill(true);
			ClientSaveData.instance.OnChangedHpRatio(BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio());
			ClientSaveData.instance.OnChangedSpRatio(BattleInstanceManager.instance.playerActor.actorStatus.GetSPRatio());

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
		{
			showPlayerIndicator = true;

			if (ClientSaveData.instance.IsLoadingInProgressGame())
			{
				if (ClientSaveData.instance.GetCachedCloseSwap())
					showPlayerIndicator = false;
			}
			else
				ClientSaveData.instance.OnChangedCloseSwap(false);
		}

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

		if (CheatingListener.detectedCheatTable)
		{
			_endProcess = false;
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
		if (clear && ClientSaveData.instance.inProgressGame)  // && IsRetryByCrash == false
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

	public virtual bool IsAutoPlay()
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
		ClientSaveData.instance.OnChangedGatePillar(true);
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




	public virtual void OnSelectedNodeWarLevel(int level)
	{
	}

	public virtual NodeWarTableData GetSelectedNodeWarTableData()
	{
		return null;
	}

	public virtual void OnGetSoul(Vector3 getPosition)
	{
	}

	public virtual void OnGetHealOrb(Vector3 getPosition)
	{
	}

	public virtual void OnGetBoostOrb(Vector3 getPosition)
	{
	}


	public virtual void OnTryActiveExitArea()
	{
	}

	public virtual void On10SecondAgoActiveExitArea()
	{
	}

	public virtual void OnSuccessExitArea()
	{
	}
}
