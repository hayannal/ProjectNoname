//#define HUDDPS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using CodeStage.AntiCheat.ObscuredTypes;

public class BattleModeProcessorBase
{
	bool _mapLoaded = false;
	bool _monsterSpawned = false;
	bool _summonMonsterSpawned = false;
	int _monsterSpawnCount = 0;
	int _damageCountInStage = 0;
	float _spawnFlagStartTime = 0.0f;
	ObscuredInt _clearPoint;
	ObscuredInt _appliedChallengeRetryBonusClearPoint;

	public virtual void Update()
	{
		UpdateSummonMonsterSpawn();
		UpdateEndProcess();
		UpdateTrap();
	}

	DateTime _startDateTime;
	public virtual void OnStartBattle()
	{
		_startDateTime = DateTime.Now;

		// 초반 플레이의 자연스러움을 위해 들어가는 예외처리. 빅뱃으로 2챕터를 시작할때 빅뱃 파워레벨이 1이라면 공격력 보정을 해준다.
		if (PlayerData.instance.selectedChapter == (int)ContentsManager.eOpenContentsByChapter.Chapter && BattleInstanceManager.instance.playerActor.actorStatus.powerLevel == 1 && BattleInstanceManager.instance.playerActor.actorId == "Actor2103")
		{
			//Debug.Log("BigBat Bonus");
			// 독립적으로 유지되어야한다. managed On
			AffectorValueLevelTableData changeStatusAffectorValue = new AffectorValueLevelTableData();
			changeStatusAffectorValue.fValue1 = -1.0f; // duration
			changeStatusAffectorValue.fValue2 = 0.2f;
			changeStatusAffectorValue.iValue1 = (int)ActorStatusDefine.eActorStatus.AttackAddRate;
			BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.ChangeActorStatus, changeStatusAffectorValue, BattleInstanceManager.instance.playerActor, true);
		}
		if (PlayerData.instance.selectedChapter == (int)ContentsManager.eOpenContentsByChapter.Chaos && BattleInstanceManager.instance.playerActor.actorStatus.powerLevel <= 2 && BattleInstanceManager.instance.playerActor.actorId == "Actor1005")
		{
			// 독립적으로 유지되어야한다. managed On
			AffectorValueLevelTableData changeStatusAffectorValue = new AffectorValueLevelTableData();
			changeStatusAffectorValue.fValue1 = -1.0f; // duration
			changeStatusAffectorValue.fValue2 = 0.2f;
			changeStatusAffectorValue.iValue1 = (int)ActorStatusDefine.eActorStatus.AttackAddRate;
			BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.ChangeActorStatus, changeStatusAffectorValue, BattleInstanceManager.instance.playerActor, true);
		}

		// 다른 곳과 달리 PlayerData.instance.currentChallengeMode를 사용하면 안되는 곳이다.
		// 카오스가 열리기 전 챕터도 포함시켜야하므로 직접 검사하기로 한다.
		if (ContentsManager.IsTutorialChapter() == false && PlayerData.instance.selectedChapter == PlayerData.instance.highestPlayChapter && PlayerData.instance.chaosMode == false && PlayerData.instance.highestClearStage > 0)
		{
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar() == false)
				_clearPoint = _appliedChallengeRetryBonusClearPoint = PlayerData.instance.highestClearStage;
		}
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
		PositionFlag.OnPreInstantiateMap();
		BattleInstanceManager.instance.FinalizeAllPositionBuffAffector(true);
		BattleInstanceManager.instance.DisableAllHitObjectMoving();
		BattleInstanceManager.instance.FinalizeAllHitObject();
		BattleInstanceManager.instance.FinalizeAllSummonObject();
		BattleInstanceManager.instance.FinalizeAllManagedEffectObject();

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.buildTutorialScene && TutorialLinkAccountCanvas.instance != null && TutorialLinkAccountCanvas.instance.gameObject.activeSelf)
		{
			if (StageManager.instance.playStage > 5)
				TutorialLinkAccountCanvas.instance.gameObject.SetActive(false);
		}

		// 항상 꺼두는게 기본이고 보스몹이 스폰될때부터 처리해야한다.
		_enableTrap = false;
	}

	public virtual void OnLoadedMap()
	{
		_mapLoaded = true;
		_monsterSpawned = false;
		_summonMonsterSpawned = false;
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
		_spawnFlagStartTime = Time.time;

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

		// 호출 순서상 InitializeInProgressGame 되기전에 PowerSource는 스폰해야한다.
		bool callOnStartStage = true;
		if (StageManager.instance.spawnPowerSourcePrefab)
		{
			_powerSourceObject = BattleInstanceManager.instance.GetCachedObject(StageManager.instance.GetPreparedPowerSourcePrefab(), StageManager.instance.currentPowerSourceSpawnPosition, Quaternion.identity);

			// PowerSource 나오는 스테이지에서는 onStartStage가 호출되지 않는다.
			callOnStartStage = false;
		}
		else
		{
			if (ClientSaveData.instance.IsLoadingInProgressGame() && ClientSaveData.instance.GetCachedMonsterAllKill())
				callOnStartStage = false;
		}

		// 호출 순서상 CallAffectorValueAffector.eEventType.OnStartStage 보다는 앞에 호출되어야해서 위로 올려둔다.
		InitializeInProgressGame();
		if (callOnStartStage && BattleInstanceManager.instance.playerActor != null)
		{
			CallAffectorValueAffector.OnEvent(BattleInstanceManager.instance.playerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnStartStage);
			ChangeAttackStateAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			ChangeAttackStateByTimeAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			AutoSideAttackAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			PaybackSpFullAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
		}

#if HUDDPS
#if UNITY_EDITOR
		HUDDPS.instance.OnStartStage(StageManager.instance.playChapter, StageManager.instance.playStage, StageManager.instance.bossStage);
#endif
#endif
	}

	void InitializeInProgressGame()
	{
		// OnSpawnFlag의 마지막 부분이 플레이어를 복구하기 가장 적절한 타이밍이다.
		bool useCachedData = false;
		if (ClientSaveData.instance.IsLoadingInProgressGame())
			useCachedData = true;
		if (MainSceneBuilder.s_buildReturnScrollUsedScene)
			useCachedData = true;
		if (useCachedData == false)
			return;

		int exp = ClientSaveData.instance.GetCachedExp();
		StageManager.instance.SetLevelExpForInProgressGame(exp);
		StageManager.instance.SetReturnScrollForInProgressGame();

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
				else
				{
					// 사실 여기로 들어오면 안되는게 몹은 다 죽었고 레벨업창이 뜬것도 아닌데 GatePillar마저 없으면 아무것도 진행되지 않게 된다.
					// 에러 로그라도 남겨두고 게이트 필라라도 띄워두는게 낫지 않을까.
					Debug.LogError("Invalid Client Save Data. Force call OnClearStage.");
					OnClearStage();
				}
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
		int legendItemCount = 0;
		List<string> listDropItemId = ClientSaveData.instance.GetCachedDropItemList();
		for (int i = 0; i < listDropItemId.Count; ++i)
		{
			DropManager.instance.AddDropItem(listDropItemId[i]);
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listDropItemId[i]);
			if (equipTableData == null)
				continue;
			if (EquipData.IsUseLegendKey(equipTableData))
				++legendItemCount;
		}
		DropManager.instance.droppedStageItemCount = listDropItemId.Count;
		DropManager.instance.droppedLengendItemCount = legendItemCount;

		// 골드와 인장은 마지막 축적된 값을 기억해놨다가 가져오면 된다. 위의 템과 달리 중요도가 낮아서 DropObject의 획득 시점에 기록하는거로 되어있다.
		float dropGold = ClientSaveData.instance.GetCachedDropGold();
		DropManager.instance.AddDropGold(dropGold);
		int dropSeal = ClientSaveData.instance.GetCachedDropSeal();
		DropManager.instance.AddDropSeal(dropSeal);

		// 레벨팩 리프레쉬에 쓰이는 배틀 클리어 포인트도 로드
		_clearPoint = ClientSaveData.instance.GetCachedClearPoint();

		// 퀘스트 관련 정보도 복구
		QuestData.instance.SetQuestInfoForInProgressGame();
		// 가이드 퀘스트 역시 복구
		GuideQuestData.instance.SetGuideQuestInfoForInProgressGame();

		// 기타 정보들도 불러줘야한다.
		BattleInstanceManager.instance.allyContinuousKillCount = ClientSaveData.instance.GetCachedAllyContinuousKillCount();

		// 여기까지 셋팅했으면 귀환 주문서 씬 전환은 끝난거다. 플래그를 초기화해둔다.
		if (MainSceneBuilder.s_buildReturnScrollUsedScene)
		{
			MainSceneBuilder.s_buildReturnScrollUsedScene = false;
			return;
		}

		// 끝나면 ClientSaveData에 로드 완료를 알린다.
		ClientSaveData.instance.OnFinishLoadGame();
	}

	int _lastCheckedStageForTrap = -1;
	public virtual void OnSpawnMonster(MonsterActor monsterActor)
	{
		if (monsterActor.team.teamId != (int)Team.eTeamID.DefaultMonster || monsterActor.excludeMonsterCount)
			return;

		_monsterSpawned = true;
		++_monsterSpawnCount;

		if (monsterActor.summonMonster)
			_summonMonsterSpawned = true;

		#region ChapterTrap
		if (monsterActor.bossMonster)
		{
			// 보스의 스폰때마다 호출될테니 스테이지 당 1회만 호출될 수 있도록 체크를 한다.
			if (_lastCheckedStageForTrap != StageManager.instance.playStage)
			{
				InitializeTrap();
				_lastCheckedStageForTrap = StageManager.instance.playStage;
			}
		}
		#endregion
	}

	public virtual void OnDiePlayer(PlayerActor playerActor)
	{
		// 여기서 인풋은 막되
		LobbyCanvas.instance.battlePauseButton.interactable = false;

		#region Return Scroll
		// 정산전에 한번 부활 가능한 상태인지 판단해야한다.
		if (StageManager.instance.IsUsableReturnScroll() && StageManager.instance.IsSavedReturnScrollPoint())
		{
			PrepareReturnScroll();
			return;
		}
		#endregion

		// 바로 정산처리 하면 안되고
		// 드랍 아이템 존재하는지 확인 후 없으면 바로 1초 타이머를 센다.
		// 획득할 수 있는 드랍 아이템이 존재하면 다 획득 후 1초 타이머를 센다.
		// 이건 마지막 보스 클리어 후에도 동일하게 적용해야한다.
		_endProcess = true;
		_endProcessWaitRemainTime = 2.0f;
	}

	#region Return Scroll
	void PrepareReturnScroll()
	{
		// 귀환할 준비를 한다.
		// 이미 입력은 안통하는 상태일테니 시간 멈추고
		Time.timeScale = 0.0f;

		// 네트워크 대기화면 켜두고
		WaitingNetworkCanvas.Show(true);

		// 네트워크가 가능한 상태인지 확인
		PlayFabApiManager.instance.RequestNetwork(OnResponse);

		// 이후 통신이 제대로 되고나면 진행할 이펙트와 교체할 캐릭터를 미리 로딩걸어둔다.
		StageManager.instance.PrepareReturnScroll();
	}

	void OnResponse()
	{
		// 네트워크에 문제가 없다면 스크롤 차감 패킷을 보내서 확인
		PlayFabApiManager.instance.RequestUseReturnScroll(OnRecvUseReturnScroll);
	}

	void OnRecvUseReturnScroll()
	{
		// 서버에서 ok 떨어지면 귀환처리를 진행
		StageManager.instance.ReturnLastPowerSourcePoint();
	}
	#endregion

	public virtual void OnDieMonster(MonsterActor monsterActor)
	{
		if (monsterActor.team.teamId != (int)Team.eTeamID.DefaultMonster || monsterActor.excludeMonsterCount)
			return;

		// ChaDragon의 Summon 시그널에 또 문제가 생겼다.
		// 간혹가다 ChaDragon 잡고나서 게이트필라가 안뜨는 문제였는데 이번엔 EvilLich때의 드랍 프로세서가 문제가 아니었고
		// LevelUpIndicatorCanvas 닫힐때 OnClearStage를 호출하지 못하는게 문제였다.
		//
		// 발생원인은 다음과 같다.
		// ChaDragon 잡자마자 하단의 OnDieMonsterList 함수가 호출되면서 LevelUpIndicatorCanvas.SetTargetLevelUpCount 이 함수로 레벨업 카운트를 기억시켜두는데
		// 이때 하필 Summon중이던 Fungee가 이후에 나온 것이다.
		// 이 Fungee를 잡고나면 또 다시 OnDieMonsterList 함수가 호출되는데 이때 LevelUpIndicatorCanvas.SetTargetLevelUpCount 값을 0으로 밀어버린 것이다.
		// 이랬더니 LevelUpIndicatorCanvas 끝나는 시점에서 selectCount와 비교해서 OnClearStage를 호출하는 로직이 있는데 이게 호출되지 않은 것이다.
		//
		// 가장 문제는 요 아래 _monsterSpawnCount == 0 비교하는 곳에서 Summon중인 몬스터가 있으면 OnDieMonsterList를 호출하지 않게 처리해야하는데 이걸 빼먹은거다.
		// 그런데 더 큰 문제는 Summon시그널로 소환중인 몹이 있는지 검사하는게 더 힘들다는거다. (그래서 체크 안한 것도 있다.)
		// Summon시그널 자체가 아무 GameObject를 소환할 수 있도록 만들어둔거라 소환중인게 몹이 아닐수도 있고 소환자가 플레이어일수도 있기 때문.
		// 그러나 이걸 그냥 두기엔 레벨업창이 떠있는데 추가 몬스터가 나온다는 얘기니 사실 고쳐야하는게 맞아보인다.
		// 그래서 추가한게 BattleInstanceManager의 delayedSummonMonsterRefCount 프로퍼티다.
		//
		// OnDieMonsterList 함수가 중복호출 되었을때 위험한지에 대한 것도 고민이긴 한게
		// BattleManager.instance.OnClearStage(); 라인이 두번 호출되면 게이트필라가 두개 생겨버리게 된다.
		// 그러니 안전하게 하기 위해서라도 중복호출 대비는 해두는게 좋을거 같아서 예외처리는 해둔다.
		--_monsterSpawnCount;
		if (_mapLoaded && _monsterSpawned && _monsterSpawnCount == 0 && BattleInstanceManager.instance.CheckFinishSequentialMonster() && BattleInstanceManager.instance.delayedSummonMonsterRefCount == 0)
		{
			OnDieMonsterList();
		}
	}

	void OnDieMonsterList()
	{
		// all kill monster
		DropManager.instance.GetStackedDropExp();

		// 한 층이 끝날땐 관련 정보들을 저장해놔야한다. 레벨업을 했다면 hp회복 역시 반영되서 저장될 것이다.
		ClientSaveData.instance.OnChangedExp(StageManager.instance.playerExp);
		ClientSaveData.instance.OnChangedHpRatio(BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio());
		ClientSaveData.instance.OnChangedSpRatio(BattleInstanceManager.instance.playerActor.actorStatus.GetSPRatio());

		// 몬스터 킬에는 한가지 예외 상황이 있는데 막보를 잡자마자 강종하면 다음에 들어올때 클리어된 마지막층에 들어와서 정산만 하는 이상한 상황이 발생하게 된다.
		// 이렇게 해도 되긴한데 뭔가 어색해서 그냥 차라리 보스를 다시 잡는게 나을거 같아서 예외처리 해둔다.(이런식으로 재진입 악용하는 유저도 막기 위하여)
		if (StageManager.instance.playStage < StageManager.instance.GetCurrentMaxStage())
		{
			ClientSaveData.instance.OnChangedMonsterAllKill(true);
			ClientSaveData.instance.ClearEliteMonsterIndexList();
		}

		// 레벨팩 처리하기 전에 먼저 클리어 보너스를 체크해야한다. 튜토에서는 하지 않는다.
		bool noHitClear = (_damageCountInStage == 0);
		bool fastClear = false;
		float stageClearTime = Time.time - _spawnFlagStartTime;
		if (StageManager.instance.currentStageTableData != null && stageClearTime < StageManager.instance.currentStageTableData.fastClearLimit)
			fastClear = true;
		if (ContentsManager.IsTutorialChapter())
			fastClear = noHitClear = false;
		if (noHitClear)
		{
			_clearPoint += 1;
			QuestData.instance.OnQuestEvent(QuestData.eQuestClearType.NoHitClear);
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.NoHitClear);
		}
		if (fastClear)
		{
			_clearPoint += 1;
			QuestData.instance.OnQuestEvent(QuestData.eQuestClearType.FastClear);
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.FastClear);
		}
		ClientSaveData.instance.OnChangedClearPoint(_clearPoint);
		LobbyCanvas.instance.ShowClearPointInfo(fastClear, noHitClear);

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

		_enableTrap = false;

		if (StageManager.instance.bossStage)
		{
			QuestData.instance.OnQuestEvent(QuestData.eQuestClearType.ClearBossStage);
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ClearBossStage);
		}
		if (GuideQuestData.instance.CheckChapterStage(StageManager.instance.playChapter, StageManager.instance.playStage))
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ChapterStage);

#if HUDDPS
#if UNITY_EDITOR
		HUDDPS.instance.OnClearStage();
#endif
#endif
	}

	public void OnClearStage()
	{
		// last stage
		if (StageManager.instance.playStage == StageManager.instance.GetCurrentMaxStage())
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

	float SummonMonsterSpawnCheckDelay = 1.0f;
	float _checkMonsterCountRemainTime;
	int _monsterCountZeroStreakCount = 0;
	void UpdateSummonMonsterSpawn()
	{
		// 이상하게 EvilLich같이 몬스터를 소환하는 기능이 있는 판에서 간혹 판의 종료를 체크하지 못하는 경우가 나온다.
		// 몹이 생성되는 프레임에 마인이나 날아오던 볼에 맞아서 죽으면 몬스터 카운트가 꼬이는건가 해서 테스트도 해봤는데
		// 최초 생성이든 재활용이든 문제없이 OnSpawnMonster 호출 후에 OnDieMonster 호출되서 아무런 문제가 생기지 않는다.
		// 도대체 뭐가 문제인지 원인은 못찾았으나 무조건 고쳐야하는 이슈기 때문에
		// summonMonster가 포함된 전투에서는 현재 몬스터 카운트를 세서라도 판의 종료를 알리기로 한다.
		if (_summonMonsterSpawned == false)
			return;

		// endProcess로 진입했다면 처리하지 않는다.
		if (_endProcess)
			return;

		// 마지막층이 아니라면 endProcess로 가진 않을테고 게이트 필라가 나오거나 레벨업 창이 떴을텐데
		// 간단하게 stackedDropExp로 체크하기로 한다.(GetCachedMonsterAllKill 로 검사해도 될거 같긴 하다.)
		if (DropManager.instance.stackDropExp == 0)
			return;

		// 초당 한번씩 셀거고 연속해서 5초동안 체크해서 없으면 클리어로 간주한다.
		_checkMonsterCountRemainTime -= Time.deltaTime;
		if (_checkMonsterCountRemainTime < 0.0f)
		{
			_checkMonsterCountRemainTime = SummonMonsterSpawnCheckDelay;
			int monsterCount = BattleInstanceManager.instance.GetLiveMonsterList().Count;
			if (monsterCount == 0 && BattleInstanceManager.instance.delayedSummonMonsterRefCount == 0)
			{
				++_monsterCountZeroStreakCount;
				if (_monsterCountZeroStreakCount > 5)
				{
					OnDieMonsterList();
					_summonMonsterSpawned = false;
				}
			}
			else
				_monsterCountZeroStreakCount = 0;
		}
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
			if (StageManager.instance.playStage == StageManager.instance.GetCurrentMaxStage())
				clear = true;
		}

		// 패킷처리 완료 후 나올 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ObscuredString> listDropItemId = DropManager.instance.GetStackedDropEquipList();
		if (listDropItemId != null)
		{
			for (int i = 0; i < listDropItemId.Count; ++i)
			{
				EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listDropItemId[i]);
				if (equipTableData == null)
					continue;

				AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
			}
		}

		SoundManager.instance.StopBGM(3.0f);
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
					//PlayerData.instance.purifyCount = 0;
					PlayerData.instance.chaosModeOpened = true;
				}

				EventManager.instance.OnEventPlayHighestChapter(PlayerData.instance.highestPlayChapter);
			}
		}
		CurrencyData.instance.gold += DropManager.instance.GetStackedDropGold();
		PlayerData.instance.sealCount += DropManager.instance.GetStackedDropSeal();
		PlayerData.instance.sealGainCount = DropManager.instance.GetStackedDropSeal();

		if (itemGrantString != "")
			TimeSpaceData.instance.OnRecvItemGrantResult(itemGrantString, true);

		// 클리어 했다면 시간 체크 한번 해본다.
		// 강종으로 인한 재접속때 안하는거 추가해야한다.
		if (clear && ClientSaveData.instance.inProgressGame == false)  // && IsRetryByCrash == false
		{
			TimeSpan timeSpan = DateTime.Now - _startDateTime;
			bool sus = false;
			if (PlayerData.instance.highestPlayChapter == PlayerData.instance.selectedChapter)
			{
				if (StageManager.instance.playChapter == 0)
				{
					if (timeSpan < TimeSpan.FromSeconds(30))
						sus = true;
				}
				else if (StageManager.instance.playChapter == 7 || StageManager.instance.playChapter == 14)
				{
					if (timeSpan < TimeSpan.FromMinutes(3))
						sus = true;
				}
				else
				{
					if (timeSpan < TimeSpan.FromMinutes(10))
						sus = true;
				}
			}
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
		// 혹시 모를 중복호출에 대비해둔다. 사실 들어오면 안되는거니 로그는 남겨둔다.
		if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
		{
			Debug.LogError("Invalid call. GatePillar is already active.");
			return;
		}

		BattleInstanceManager.instance.GetCachedObject(string.IsNullOrEmpty(StageManager.instance.nextMapTableData.bossName) ? StageManager.instance.gatePillarPrefab : StageManager.instance.bossGatePillarPrefab,
			StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
		ClientSaveData.instance.OnChangedGatePillar(true);
	}



	#region ChapterTrap
	// 후반부 챕터의 보스전에서는 트랩이 생성된다.
	// NodeWar때랑 달리 스테이지 별로 
	bool _enableTrap = false;
	GameObject _trapPrefab;
	float _trapSpawnRemainTime;
	float _trapSpawnDelayMin = 3.0f;
	float _trapSpawnDelayMax = 7.0f;
	float _trapFirstWaitingRemainTime;
	float _trapNoSpawnRange = 12.0f;
	void InitializeTrap()
	{
		_enableTrap = false;
		bool lastStage = (StageManager.instance.playStage == StageManager.instance.GetCurrentMaxStage());
		ChapterTrapTableData chapterTrapTableData = TableDataManager.instance.FindChapterTrapTableData(StageManager.instance.playChapter, lastStage);
		if (chapterTrapTableData == null)
			return;

		_trapFirstWaitingRemainTime = chapterTrapTableData.firstWaiting;
		_trapSpawnDelayMin = chapterTrapTableData.minPeriod;
		_trapSpawnDelayMax = chapterTrapTableData.maxPeriod;
		_trapSpawnRemainTime = 0.0f;
		_trapNoSpawnRange = chapterTrapTableData.trapNoSpawnRange;
		_enableTrap = true;

		AddressableAssetLoadManager.GetAddressableGameObject(chapterTrapTableData.trapAddress, "Trap", (prefab) =>
		{
			_trapPrefab = prefab;
		});
	}

	void UpdateTrap()
	{
		// 보스를 상대할때만 나오고 죽이고 나서는 더이상 스폰되지 않는다.
		if (_enableTrap == false)
			return;

		if (_trapFirstWaitingRemainTime > 0.0f)
		{
			_trapFirstWaitingRemainTime -= Time.deltaTime;
			if (_trapFirstWaitingRemainTime <= 0.0f)
				_trapFirstWaitingRemainTime = 0.0f;
			return;
		}

		_trapSpawnRemainTime -= Time.deltaTime;
		if (_trapSpawnRemainTime < 0.0f)
		{
			// 거의 일어나지 않겠지만 혹시라도 아직 트립 프리팹이 로딩되지 않았다면 1초후에 다시 시도하게 한다.
			if (_trapPrefab == null)
			{
				_trapSpawnRemainTime += 1.0f;
				return;
			}

			Vector3 resultPosition = Vector3.zero;
			if (GetTrapSpawnPosition(ref resultPosition))
			{
				BattleInstanceManager.instance.GetCachedObject(_trapPrefab, resultPosition, Quaternion.identity);
				_trapSpawnRemainTime += UnityEngine.Random.Range(_trapSpawnDelayMin, _trapSpawnDelayMax);
			}
			else
			{
				// 이 자리에서 만들 수 없다고 판단되면 잠시 딜레이를 줘서 조금 후에 다시 체크하도록 한다.
				_trapSpawnRemainTime += 1.0f;
			}
		}
	}

	bool GetTrapSpawnPosition(ref Vector3 resultPosition)
	{
		for (int i = 0; i < 20; ++i)
		{
			// 맵의 크기 안에서 임의의 위치에 나오면 된다.
			Vector3 desirePosition = Vector3.zero;

			desirePosition.x = UnityEngine.Random.Range(CustomFollowCamera.instance.cachedQuadLeft, CustomFollowCamera.instance.cachedQuadRight);
			desirePosition.y = 0.0f;
			desirePosition.z = UnityEngine.Random.Range(CustomFollowCamera.instance.cachedQuadDown, CustomFollowCamera.instance.cachedQuadUp);

			if (NodeWarTrap.IsExistInRange(desirePosition, _trapNoSpawnRange))
				continue;
			resultPosition = desirePosition;
			return true;
		}
		return false;
	}
	#endregion



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

	public int GetClearPoint()
	{
		return _clearPoint;
	}

	public void AddClearPoint(int add)
	{
		_clearPoint += add;

		// 값이 변하고 나면 꼭 기록해놔야한다.
		ClientSaveData.instance.OnChangedClearPoint(_clearPoint);
	}

	public int GetAppliedChallengeRetryBonusClearPoint()
	{
		return _appliedChallengeRetryBonusClearPoint;
	}

	public void ResetAppliedChallengeRetryBonusClearPoint()
	{
		_appliedChallengeRetryBonusClearPoint = 0;
	}




	public virtual void OnSelectedNodeWarLevel(int level)
	{
	}

	public virtual NodeWarTableData GetSelectedNodeWarTableData()
	{
		return null;
	}

	public virtual bool IsFirstClear()
	{
		return false;
	}

	public virtual bool IsSacrificePhase()
	{
		return false;
	}

	public virtual float GetSpawnCountRate(string monsterId)
	{
		return 0.0f;
	}

	public virtual void OnGetSoul(Vector3 getPosition)
	{
	}

	public virtual void OnGetHealOrb(Vector3 getPosition)
	{
	}

	public virtual void OnGetSpHealOrb(Vector3 getPosition)
	{
	}

	public virtual void OnGetBoostOrb(Vector3 getPosition)
	{
	}

	public virtual void OnGetInvincibleOrb(Vector3 getPosition)
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
