using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MecanimStateDefine;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;
using DG.Tweening;

public class BossBattleProcessor : BattleModeProcessorBase
{
	public override void Update()
	{
		UpdateTimer();
		UpdateSummonMonsterSpawn();
		UpdateEndProcess();
	}

	ObscuredBool _firstClear;
	ObscuredInt _selectedDifficulty;
	ObscuredInt _clearDifficulty;
	public override void OnStartBattle()
	{
		// base꺼 호출할 필요 없다. startDateTime도 안쓰고 빅뱃 예외처리도 필요없다.
		//base.OnStartBattle();

		// 노드워때처럼 강제로 셋팅.
		StageManager.instance.playerLevel = StageManager.instance.GetMaxStageLevel();
		ApplyBossBattleLevelPack(BattleInstanceManager.instance.playerActor);

		// Enter 패킷에서 저장해놨으니 불러올 수 있다.
		int currentBossId = PlayerData.instance.bossBattleId;
		if (currentBossId == 0)
			currentBossId = 1;
		_selectedDifficulty = PlayerData.instance.GetBossBattleSelectedDifficulty(currentBossId.ToString());
		_clearDifficulty = PlayerData.instance.GetBossBattleClearDifficulty(currentBossId.ToString());
		_firstClear = false;
		if (_clearDifficulty == 0)
			_firstClear = true;
		if (_selectedDifficulty == (_clearDifficulty + 1))
			_firstClear = true;

		// 초반 플레이 보정. 
		if (currentBossId <= 5 && _selectedDifficulty == 1)
		{
			AffectorValueLevelTableData changeStatusAffectorValue = new AffectorValueLevelTableData();
			changeStatusAffectorValue.fValue1 = -1.0f; // duration
			changeStatusAffectorValue.fValue2 = 0.2f;
			changeStatusAffectorValue.iValue1 = (int)ActorStatusDefine.eActorStatus.AttackAddRate;
			BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.ChangeActorStatus, changeStatusAffectorValue, BattleInstanceManager.instance.playerActor, true);
		}
	}

	public static void ApplyBossBattleLevelPack(PlayerActor playerActor)
	{
		playerActor.skillProcessor.CheckAllExclusiveLevelPack();
		//playerActor.skillProcessor.AddLevelPack("Atk", false, 0);
		//playerActor.skillProcessor.AddLevelPack("AtkSpeed", false, 0);
		//playerActor.skillProcessor.AddLevelPack("AtkSpeed", false, 0);

		LobbyCanvas.instance.RefreshExpPercent(1.0f, 5);
		LobbyCanvas.instance.RefreshLevelText(StageManager.instance.GetMaxStageLevel());

		// 이거 하나만큼은 BattleEnterCanvas로부터 가져오기로 한다.
		int bossOpenChapter = BossBattleEnterCanvas.instance.GetBossBattleTableData().chapter;
		int packCount = BossBattleEnterCanvas.instance.GetXpLevel();
		for (int i = 0; i < packCount; ++i)
		{
			// 팩의 등장은 현재 선택된 difficulty에 따라 나오는게 아니라 보스의 원래 등장 챕터 기반으로 나와야한다.
			List<LevelPackDataManager.RandomLevelPackInfo> listRandomLevelPackInfo = LevelPackDataManager.instance.GetRandomLevelPackTableDataList(BattleInstanceManager.instance.playerActor, false, bossOpenChapter);
			int index = LevelUpIndicatorCanvas.FindIndex(listRandomLevelPackInfo, Random.value);
			if (index == -1)
				continue;

			playerActor.skillProcessor.AddLevelPack(listRandomLevelPackInfo[index].levelPackTableData.levelPackId, false, 0);
		}
	}

	public override void OnLoadedMap()
	{
		//base.OnLoadedMap();

		_monsterSpawned = false;
		_summonMonsterSpawned = false;
		_monsterSpawnCount = 0;
	}

	GameObject _powerSourceObject;
	public override void OnSpawnFlag()
	{
		if (BattleInstanceManager.instance.playerActor != null)
		{
			// 보스전에서의 패널티는 같은 스테이지 테이블의 1층꺼를 구해와서 적용해야 제대로 된다.
			StageTableData penaltyStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(StageManager.instance.currentStageTableData.chapter, 1, false);

			string stagePenaltyId = "";
			if (penaltyStageTableData != null && penaltyStageTableData.stagePenaltyId.Length > 0)
				stagePenaltyId = penaltyStageTableData.stagePenaltyId[Random.Range(0, StageManager.instance.currentStageTableData.stagePenaltyId.Length)];

			if (string.IsNullOrEmpty(stagePenaltyId) == false)
				BattleInstanceManager.instance.playerActor.RefreshStagePenaltyAffector(stagePenaltyId, true);
		}

		if (BattleInstanceManager.instance.playerActor != null)
		{
			// NodeWar 했던거처럼.
			CallAffectorValueAffector.OnEvent(BattleInstanceManager.instance.playerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnStartStage);
			ChangeAttackStateAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			ChangeAttackStateByTimeAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			//AutoSideAttackAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			PaybackSpFullAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
		}
	}

	public override void OnSpawnMonster(MonsterActor monsterActor)
	{
		if (monsterActor.team.teamId != (int)Team.eTeamID.DefaultMonster || monsterActor.excludeMonsterCount)
			return;

		_monsterSpawned = true;
		++_monsterSpawnCount;

		if (monsterActor.summonMonster)
			_summonMonsterSpawned = true;
	}

	public override void OnDiePlayer(PlayerActor playerActor)
	{
		// 여기서 인풋은 막되
		LobbyCanvas.instance.battlePauseButton.interactable = false;

		// 챕터에서 했을때와 비슷하게 처리. 패킷 전달시간이 없다보니 1초 더 늘려둔다.
		_endProcess = true;
		_endProcessWaitRemainTime = 2.0f;
	}

	public override void OnDieMonster(MonsterActor monsterActor)
	{
		if (monsterActor.team.teamId != (int)Team.eTeamID.DefaultMonster || monsterActor.excludeMonsterCount)
			return;

		--_monsterSpawnCount;
		if (_monsterSpawned && _monsterSpawnCount == 0 && BattleInstanceManager.instance.CheckFinishSequentialMonster() && BattleInstanceManager.instance.delayedSummonMonsterRefCount == 0)
		{
			OnDieMonsterList();
		}
	}

	void OnDieMonsterList()
	{
		BattleManager.instance.OnClearStage();
	}

	public override void OnClearStage()
	{
		// boss clear
		LobbyCanvas.instance.battlePauseButton.interactable = false;
		_endProcess = true;
		_endProcessWaitRemainTime = 3.0f;
	}

	#region Timer
	bool _timerStarted = false;
	float _timerRemainTime;
	bool _timeOut = false;
	public float remainTime { get { return _timerRemainTime; } }
	void UpdateTimer()
	{
		if (_timerStarted == false)
		{
			_timerStarted = true;
			_timerRemainTime = 90.0f;
			UIInstanceManager.instance.ShowCanvasAsync("BossBattleTimerCanvas", () =>
			{
				BossBattleTimerCanvas.instance.Initialize(this);
			}, false);
			return;
		}

		if (_timerStarted == false)
			return;

		if (_endProcess)
			return;

		if (_timerRemainTime <= 0.0f)
			return;

		_timerRemainTime -= Time.deltaTime;
		if (_timerRemainTime <= 0.0f)
		{
			_timeOut = true;
			_endProcess = true;
			_endProcessWaitRemainTime = 1.5f;
		}
	}
	#endregion

	#region EndGame
	bool _endProcess = false;
	float _endProcessWaitRemainTime = 0.0f; // 최소 대기타임
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

		if (CheatingListener.detectedCheatTable)
		{
			_endProcess = false;
			return;
		}

		bool clear = false;
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie() == false && _timeOut == false)
		{
			HitObject.EnableRigidbodyAndCollider(false, null, BattleInstanceManager.instance.playerActor.GetCollider());
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

		if (clear)
		{
			// 클리어 했다면 다음번 보스가 누구일지 미리 굴려서 End패킷에 보내야한다.
			int nextBossId = PlayerData.instance.GetNextRandomBossId();

			PlayFabApiManager.instance.RequestEndBossBattle(clear, nextBossId, _selectedDifficulty, DropManager.instance.GetStackedDropEquipList(), (result, nextId, itemGrantString) =>
			{
				// 정보를 갱신하기 전에 먼저 BattleResult를 보여준다.
				UIInstanceManager.instance.ShowCanvasAsync("BossBattleResultCanvas", () =>
				{
					BossBattleResultCanvas.instance.RefreshInfo(true, _selectedDifficulty, _firstClear, itemGrantString);
					OnRecvEndBossBattle(result, nextId, itemGrantString);
				});
			});
		}
		else
		{
			PlayFabApiManager.instance.RequestCancelBossBattle();
			UIInstanceManager.instance.ShowCanvasAsync("BossBattleResultCanvas", () =>
			{
				BossBattleResultCanvas.instance.RefreshInfo(false, _selectedDifficulty, _firstClear, "");
			});
		}

		_endProcess = false;
	}

	void OnRecvEndBossBattle(bool clear, int nextBossId, string itemGrantString)
	{
		// 반복클리어냐 아니냐에 따라 결과를 나누면 된다.
		int addDia = 0;
		int addGold = 0;
		if (clear)
		{
			PlayerData.instance.OnClearBossBattle(_selectedDifficulty, _clearDifficulty, nextBossId);

			if (_firstClear)
			{
				//PlayerData.instance.nodeWarClearLevel = _selectedNodeWarTableData.level;
				//addDia += _selectedNodeWarTableData.firstRewardDiamond;
				//addGold += _selectedNodeWarTableData.firstRewardGold;
			}

			/*
			PlayerData.instance.nodeWarCurrentLevel = _selectedNodeWarTableData.level;
			int rate = 1;
			if (PlayerData.instance.nodeWarBoostRemainCount > 0)
			{
				PlayerData.instance.nodeWarBoostRemainCount -= 1;
				rate = 3;
			}
			addGold += (_selectedNodeWarTableData.repeatRewardGold * rate);
			*/
		}

		CurrencyData.instance.gold += addGold;
		CurrencyData.instance.dia += addDia;

		if (itemGrantString != "")
			TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString);
	}
	#endregion
}