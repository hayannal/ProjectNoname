using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class InvasionProcessor : BattleModeProcessorBase
{
	public override void Update()
	{
		UpdatePortal();
		UpdateSummonMonsterSpawn();
		UpdateEndProcess();
	}

	public override void OnStartBattle()
	{
		// base꺼 호출할 필요 없다. startDateTime도 안쓰고 빅뱃 예외처리도 필요없다.
		//base.OnStartBattle();

		// 미리 클리어 포인트를 셋팅.
		if (InvasionEnterCanvas.instance != null)
			_clearPoint = _appliedChallengeRetryBonusClearPoint = InvasionEnterCanvas.instance.GetTodayClearPoint();

		// 노드워때처럼 강제로 셋팅.
		StageManager.instance.playerLevel = StageManager.instance.GetMaxStageLevel();
		ApplyInvasionLevelPack(BattleInstanceManager.instance.playerActor);
	}

	const int LevelPackCount = 5;
	public static void ApplyInvasionLevelPack(PlayerActor playerActor)
	{
		playerActor.skillProcessor.CheckAllExclusiveLevelPack();
		//playerActor.skillProcessor.AddLevelPack("Atk", false, 0);
		//playerActor.skillProcessor.AddLevelPack("AtkSpeed", false, 0);
		//playerActor.skillProcessor.AddLevelPack("AtkSpeed", false, 0);

		// 연출상으로는 5회 정도
		LobbyCanvas.instance.RefreshExpPercent(1.0f, 5);
		LobbyCanvas.instance.RefreshLevelText(StageManager.instance.GetMaxStageLevel());

		// 보스전과 달리 유저가 직접 5회의 레벨팩을 고르는 형태다.
		LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, LevelPackCount, 0, 0);
		LevelUpIndicatorCanvas.SetTargetLevelUpCount(LevelPackCount);
	}

	public override void OnLoadedMap()
	{
		//base.OnLoadedMap();

		_monsterSpawned = false;
		_summonMonsterSpawned = false;
		_monsterSpawnCount = 0;
	}

	public override void OnSpawnFlag()
	{
		// 맵이동처럼 처리해야하는거라 여기서 별도로 호출해준다.
		PositionFlag.OnPreInstantiateMap();
		BattleInstanceManager.instance.FinalizeAllPositionBuffAffector(true);
		BattleInstanceManager.instance.DisableAllHitObjectMoving();
		BattleInstanceManager.instance.FinalizeAllHitObject();
		BattleInstanceManager.instance.FinalizeAllSummonObject();
		BattleInstanceManager.instance.FinalizeAllManagedEffectObject();

		// 요일던전에서는 페널티가 없다.

		if (BattleInstanceManager.instance.playerActor != null)
		{
			// NodeWar 했던거처럼.
			CallAffectorValueAffector.OnEvent(BattleInstanceManager.instance.playerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnStartStage);
			ChangeAttackStateAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			ChangeAttackStateByTimeAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
			AutoSideAttackAffector.OnEventStartStage(BattleInstanceManager.instance.playerActor.affectorProcessor);
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

	bool _waitSelectLevelPack = true;
	public override void OnClearStage()
	{
		// 첫번째 클리어는 레벨팩 다 선택했을때 호출될거다.
		if (_waitSelectLevelPack)
		{
			_waitSelectLevelPack = false;
			OpenPortal();
			return;
		}

		// boss clear
		LobbyCanvas.instance.battlePauseButton.interactable = false;
		_endProcess = true;
		_endProcessWaitRemainTime = 3.0f;
	}

	Vector3 _portalPosition = Vector3.zero;
	GameObject _portalObject;
	void OpenPortal()
	{
		_portalPosition = Vector3.zero;
		if (BattleInstanceManager.instance.playerActor.cachedTransform.position.magnitude < 1.5f)
			_portalPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(0.0f, 0.0f, 2.0f);

		_portalObject = BattleInstanceManager.instance.GetCachedObject(InvasionEnterCanvas.instance.nodeWarEndPortalEffectPrefab, _portalPosition, Quaternion.identity);
	}

	bool _spawnFlag = false;
	void UpdatePortal()
	{
		if (_spawnFlag)
			return;
		if (_portalObject == null)
			return;

		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position - _portalPosition;
		if (diff.sqrMagnitude < 0.5f)
		{
			StageManager.instance.InstantiateInvasionSpawnFlag();
			_portalObject.SetActive(false);
			_spawnFlag = true;
		}
	}

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
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie() == false)
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
			// 성공시에는 패킷 보내고 통과해야 연출을 진행. 노드워와 동일하다.
			PlayFabApiManager.instance.RequestEndInvasion(BattleInstanceManager.instance.playerActor.actorId, 1, DropManager.instance.GetLobbyDropItemInfo(), (itemGrantString) =>
			{
				//Timing.RunCoroutine(ClearProcess(result, itemGrantString));
			});

			/*
			// 클리어 했다면 다음번 보스가 누구일지 미리 굴려서 End패킷에 보내야한다.
			int nextBossId = PlayerData.instance.GetNextRandomBossId();

			// 첫 클리어라면 첫클리어 드랍 보상도 굴려야한다. 드랍 정보만 필요하고 연출은 필요없기때문에 간단하게 처리한다.
			if (_firstClear)
			{
				DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _bossRewardTableData.firstDropId, "", true, true);
				if (CheatingListener.detectedCheatTable)
					return;
			}

			PlayFabApiManager.instance.RequestEndBossBattle(clear, nextBossId, _selectedDifficulty, DropManager.instance.GetLobbyDropItemInfo(), DropManager.instance.GetStackedDropEquipList(), (result, nextId, firstItemGrantString, itemGrantString) =>
			{
				// 정보를 갱신하기 전에 먼저 BattleResult를 보여준다.
				UIInstanceManager.instance.ShowCanvasAsync("BossBattleResultCanvas", () =>
				{
					BossBattleResultCanvas.instance.RefreshInfo(true, _selectedDifficulty, _firstClear, firstItemGrantString, itemGrantString);
					OnRecvEndBossBattle(result, _firstClear, nextId, firstItemGrantString, itemGrantString);
				});
			});
			*/
		}
		else
		{
			// 보스전과 달리 질때는 쌓이는게 없으니 End패킷 대신 캔슬로 처리
			PlayFabApiManager.instance.RequestCancelInvasion();
			//UIInstanceManager.instance.ShowCanvasAsync("InvasionResultCanvas", () =>
			//{
			//	NodeWarResultCanvas.instance.RefreshInfo(false, _selectedNodeWarTableData, _firstClear, "");
			//});
		}

		_endProcess = false;
	}

	void OnRecvEndBossBattle(bool clear, bool firstClear, int nextBossId, string firstItemGrantString, string itemGrantString)
	{
		/*
		// 반복클리어냐 아니냐에 따라 결과를 나누면 된다.
		CurrencyData.instance.gold += _bossRewardTableData.enterGold;
		PlayerData.instance.AddBossBattleCount();

		if (clear)
		{
			PlayerData.instance.OnClearBossBattle(_selectedDifficulty, _clearDifficulty, nextBossId);

			if (_firstClear)
			{
				if (_bossRewardTableData.firstEnergy > 0)
					CurrencyData.instance.OnRecvRefillEnergy(_bossRewardTableData.firstEnergy);

				// 확정보상으로 굴리는거라 로비에서 쓰던 함수로 쓴다.
				if (firstItemGrantString != "")
					TimeSpaceData.instance.OnRecvGrantEquip(firstItemGrantString, 0);
			}

			// 이건 레전드키까지 써서 굴리는 진짜 드랍이므로 OnRecvItemGrantResult를 쓴다.
			if (itemGrantString != "")
				TimeSpaceData.instance.OnRecvItemGrantResult(itemGrantString, true);
		}
		*/
	}
	#endregion
}