using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;
using DG.Tweening;

public class InvasionProcessor : BattleModeProcessorBase
{
	float _spOnkillNormalMonster = 0.0f;

	public override void Update()
	{
		UpdatePortal();
		UpdateSummonMonsterSpawn();
		UpdateTimer();
		UpdateEndProcess();
	}

	public override void OnStartBattle()
	{
		// base꺼 호출할 필요 없다. startDateTime도 안쓰고 빅뱃 예외처리도 필요없다.
		//base.OnStartBattle();

		// 시작위치는 좌측이 레벨팩 선택하기 편하다.
		BattleInstanceManager.instance.playerActor.cachedTransform.position = new Vector3(-3.0f, 0.0f, -1.0f);
		CustomFollowCamera.instance.immediatelyUpdate = true;

		// 미리 클리어 포인트를 셋팅.
		if (InvasionEnterCanvas.instance != null)
			_clearPoint = _appliedChallengeRetryBonusClearPoint = InvasionEnterCanvas.instance.GetTodayClearPoint();

		_spOnkillNormalMonster = InvasionEnterCanvas.instance.GetInvasionTableData().killSp;

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

		if (monsterActor.bossMonster == false)
		{
			float dropSpValue = _spOnkillNormalMonster;
			PlayerActor playerActor = BattleInstanceManager.instance.playerActor;
			float spGainAddRate = playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.SpGainAddRate);
			if (spGainAddRate != 0.0f) dropSpValue *= (1.0f + spGainAddRate);
			playerActor.actorStatus.AddSP(dropSpValue);
		}

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

	DropProcessor _cachedDropProcessor;
	void PrepareDropProcessor()
	{
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, InvasionEnterCanvas.instance.GetInvasionTableData().dropId, "", true, true);
		_cachedDropProcessor.AdjustDropRange(3.2f);
		if (CheatingListener.detectedCheatTable)
			return;

		// 연출 끝나고 나올 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ObscuredString> listDropItemId = DropManager.instance.GetLobbyDropItemInfo();
		for (int i = 0; i < listDropItemId.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listDropItemId[i]);
			if (equipTableData == null)
				continue;

			AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
		}
	}

	#region Timer
	bool _timerStarted = false;
	DateTime _dailyResetTime;
	bool _timeOut = false;
	void UpdateTimer()
	{
		if (_timerStarted == false)
		{
			_timerStarted = true;
			_dailyResetTime = DailyShopData.instance.dailyShopSlotPurchasedResetTime;
			UIInstanceManager.instance.ShowCanvasAsync("InvasionTimerCanvas", null, false);
			return;
		}

		if (_timerStarted == false)
			return;

		if (_endProcess)
			return;

		if (ServerTime.UtcNow < _dailyResetTime)
		{
		}
		else
		{
			_timeOut = true;
			_endProcess = true;
			_endProcessWaitRemainTime = 0.5f;
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

		SoundManager.instance.StopBGM(3.0f);

		if (clear)
		{
			// 성공시에는 패킷 보내고 통과해야 연출을 진행. 노드워와 동일하다.
			// 그전에 드랍부터 굴려서 결과를 패킷으로 보내야 한다.
			PrepareDropProcessor();
			if (CheatingListener.detectedCheatTable)
				return;

			string playerActorId = BattleInstanceManager.instance.playerActor.actorId;
			int difficulty = InvasionEnterCanvas.instance.GetInvasionTableData().hard;
			int dayWeek = InvasionEnterCanvas.instance.GetInvasionTableData().dayWeek;
			PlayFabApiManager.instance.RequestEndInvasion(dayWeek, playerActorId, difficulty, (itemGrantString) =>
			{
				Timing.RunCoroutine(ClearProcess(itemGrantString));
			});
		}
		else
		{
			// 보스전과 달리 질때는 쌓이는게 없으니 End패킷 대신 캔슬로 처리
			PlayFabApiManager.instance.RequestCancelInvasion();
			UIInstanceManager.instance.ShowCanvasAsync("InvasionResultCanvas", () =>
			{
				InvasionResultCanvas.instance.RefreshInfo(false, InvasionEnterCanvas.instance.GetInvasionTableData().hard, "");
			});
		}

		_endProcess = false;
	}

	IEnumerator<float> ClearProcess(string itemGrantString)
	{
		// 인풋 막는처리 NodeWar때 쓰던거 가져와서 쓴다.
		LocalPlayerController localPlayerController = BattleInstanceManager.instance.playerActor.baseCharacterController as LocalPlayerController;
		localPlayerController.dontMove = true;
		localPlayerController.enabled = false;

		AffectorValueLevelTableData invincibleAffectorValue = new AffectorValueLevelTableData();
		invincibleAffectorValue.fValue1 = -1.0f;
		invincibleAffectorValue.iValue3 = 1;    // noText
		BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Invincible, invincibleAffectorValue, BattleInstanceManager.instance.playerActor, false);

		// avoid gc
		if (this == null)
			yield break;

		// 연출이 끝나면 결과창을 띄울건데 요일던전은 요일마다 나오는 템이 달라서 요일별로 나눠서 처리해야한다.
		bool usePp = false;
		bool useEquip = false;
		bool useCurrency = false;
		switch ((DayOfWeek)InvasionEnterCanvas.instance.GetInvasionTableData().dayWeek)
		{
			case DayOfWeek.Sunday: useCurrency = true; break;
			case DayOfWeek.Monday: useEquip = true; break;
			case DayOfWeek.Tuesday: usePp = true; break;
			case DayOfWeek.Wednesday: useCurrency = true; break;
			case DayOfWeek.Thursday: usePp = true; break;
			case DayOfWeek.Friday: useEquip = true; break;
			case DayOfWeek.Saturday: usePp = true; break;
		}

		// 선처리 할것들 해둔다.
		List<ItemInstance> listGrantItem = null;
		if (useEquip && itemGrantString != "")
		{
			listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
			for (int i = 0; i < listGrantItem.Count; ++i)
			{
				EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listGrantItem[i].ItemId);
				if (equipTableData == null)
					continue;

				AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
			}
		}

		// 이후 바로 뽑기 연출 진행
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.NodeWar, _cachedDropProcessor, 0, 0, () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("InvasionResultCanvas", () =>
				{
					InvasionResultCanvas.instance.RefreshInfo(true, InvasionEnterCanvas.instance.GetInvasionTableData().hard, itemGrantString);
					OnRecvEndInvasion(usePp, useEquip, useCurrency, itemGrantString);
				});
			});
		});
	}

	void OnRecvEndInvasion(bool usePp, bool useEquip, bool useCurrency, string itemGrantString)
	{
		// 요일에 따라 얻을거만 체크
		if (usePp)
		{

		}
		else if (useEquip)
		{
			if (itemGrantString != "")
				TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString, 0);
		}
		else if (useCurrency)
		{
			CurrencyData.instance.gold += DropManager.instance.GetLobbyGoldAmount();
			CurrencyData.instance.dia += DropManager.instance.GetLobbyDiaAmount();
		}
	}
	#endregion
}