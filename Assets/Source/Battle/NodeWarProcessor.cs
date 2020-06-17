using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MecanimStateDefine;

public class NodeWarProcessor : BattleModeProcessorBase
{
	public static float SpawnDistance = 16.0f;
	public static int DefaultMonsterMaxCount = 47;
	public static int LastMonsterMaxCount = 55;
	public static int SoulCountMax = 10;

	// 유효 거리
	public static float ItemValidDistance = 30.0f;
	// 몬스터의 경우 항상 나에게 다가오고있기 때문에 스폰위치에서 어느정도 멀어지기만 해도 탈락했다고 보는게 낫다.
	// 이래야 뛰는 방향에서 새로운 몹들이 다시 스폰될 수 있다.
	public static float MonsterValidDistance = 22.0f;

	// 안전지대 위치는 10, 0, 10이다.
	public static Vector3 EndSafeAreaPosition = new Vector3(10.0f, 0.0f, 10.0f);

	enum ePhase
	{
		FindSoul = 1,
		FindPortal = 2,
		WaitActivePortal = 3,
		Exit = 4,
		Success = 5,
	}

	NodeWarTableData _selectedNodeWarTableData;
	ePhase _phase;
	float _phaseStartTime;

	public override void Update()
	{
		if (_selectedNodeWarTableData == null)
			return;

		UpdateSpawnMonster();
		UpdateMonsterDistance();
		UpdateTrap();
		UpdateSpawnSoul();
		UpdateSpawnHealOrb();
		UpdateSpawnBoostOrb();
		UpdateExit();
	}

	public override void OnStartBattle()
	{
		base.OnStartBattle();

		BattleInstanceManager.instance.playerActor.cachedTransform.rotation = Quaternion.identity;
		BattleInstanceManager.instance.playerActor.cachedTransform.position = Vector3.zero;
		CustomFollowCamera.instance.checkPlaneLeftRightQuad = false;
		CustomFollowCamera.instance.distanceToTarget += 8.0f;
		CustomFollowCamera.instance.followSpeed = 5.0f;
		CustomFollowCamera.instance.immediatelyUpdate = true;

		// 이렇게 강제로 셋팅하는 부분은 여기 하나뿐이다.
		StageManager.instance.playerLevel = StageManager.instance.GetMaxStageLevel();
		ApplyNodeWarLevelPack(BattleInstanceManager.instance.playerActor);
	}

	public static void ApplyNodeWarLevelPack(PlayerActor playerActor)
	{
		playerActor.skillProcessor.CheckAllExclusiveLevelPack();
		playerActor.skillProcessor.AddLevelPack("AtkBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkSpeedBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkSpeedBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkSpeedBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("CritBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("CritBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("CritBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("MoveSpeedUpOnKill", false, 0);
		playerActor.skillProcessor.AddLevelPack("MoveSpeedUpOnKill", false, 0);
		playerActor.skillProcessor.AddLevelPack("MoveSpeedUpOnKill", false, 0);
		playerActor.skillProcessor.AddLevelPack("MoveSpeedUpOnAttacked", false, 0);
		playerActor.skillProcessor.AddLevelPack("MoveSpeedUpOnAttacked", false, 0);
		playerActor.skillProcessor.AddLevelPack("MoveSpeedUpOnAttacked", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkUpOnLowerHpBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkUpOnLowerHpBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("AtkUpOnLowerHpBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("HealSpOnAttackBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("HealSpOnAttackBetter", false, 0);
		playerActor.skillProcessor.AddLevelPack("HealSpOnAttackBetter", false, 0);
	}

	public override void OnLoadedMap()
	{
		//base.OnLoadedMap();
	}

	public override void OnSelectedNodeWarLevel(int level)
	{
		Debug.LogFormat("Select Level = {0}", level);
		
		if (_selectedNodeWarTableData == null)
		{
			_selectedNodeWarTableData = TableDataManager.instance.FindNodeWarTableData(level);

			// SpawnTable은 그냥 쓰면 안되고 여기서 현재 레벨에 필요한 몬스터들만 추려서 따로 가지고 있어야한다.
			// 먼저 루프 한번 돌면서 fixedLevel이 같은 것들을 먼저 리스트에 담고
			for (int i = 0; i < TableDataManager.instance.nodeWarSpawnTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.nodeWarSpawnTable.dataArray[i].fixedLevel != level)
					continue;
				_listCurrentNodeWarSpawnTableData.Add(TableDataManager.instance.nodeWarSpawnTable.dataArray[i]);
			}
			// 만약 fixedLevel로 설정된게 하나도 없다면 일의 자리로 판단해서 가져오기로 한다.
			if (_listCurrentNodeWarSpawnTableData.Count == 0)
			{
				int oneLevel = level % 10;
				for (int i = 0; i < TableDataManager.instance.nodeWarSpawnTable.dataArray.Length; ++i)
				{
					if (TableDataManager.instance.nodeWarSpawnTable.dataArray[i].fixedLevel != 0)
						continue;
					if (TableDataManager.instance.nodeWarSpawnTable.dataArray[i].oneLevel != oneLevel)
						continue;
					_listCurrentNodeWarSpawnTableData.Add(TableDataManager.instance.nodeWarSpawnTable.dataArray[i]);
				}
			}
			// 사용할 리스트가 정해지면 이 리스트에 맞춰서 RemainTime리스트도 만들어낸다.
			for (int i = 0; i < _listCurrentNodeWarSpawnTableData.Count; ++i)
				_listCurrentSpawnRemainTime.Add(0.0f);
			_totalAliveMonsterCount = 0;
		}
		_phase = ePhase.FindSoul;
		_phaseStartTime = Time.time;
		_trapSpawnRemainTime = Random.Range(TrapSpawnDelayMin, TrapSpawnDelayMax);
		_soulSpawnRemainTime = SoulSpawnDelay;
		_healOrbSpawnRemainTime = HealOrbSpawnDelay;
		_boostOrbSpawnRemainTime = BoostOrbSpawnDelay;
		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarRule1Mind"), 3.5f);
	}

	public override NodeWarTableData GetSelectedNodeWarTableData()
	{
		return _selectedNodeWarTableData;
	}

	#region Monster
	List<NodeWarSpawnTableData> _listCurrentNodeWarSpawnTableData = new List<NodeWarSpawnTableData>();
	List<float> _listCurrentSpawnRemainTime = new List<float>();
	Dictionary<string, int> _dicAliveMonsterCount = new Dictionary<string, int>();
	int _totalAliveMonsterCount;
	bool _monsterSpawnBoosted = false;
	void UpdateSpawnMonster()
	{
		if (_phase == ePhase.Success)
			return;

		float diffTime = Time.time - _phaseStartTime;
		for (int i = 0; i < _listCurrentNodeWarSpawnTableData.Count; ++i)
		{
			if ((int)_phase < _listCurrentNodeWarSpawnTableData[i].minStep)
				continue;
			if (diffTime < _listCurrentNodeWarSpawnTableData[i].firstWaiting)
				continue;

			if (_listCurrentSpawnRemainTime[i] > 0.0f)
			{
				_listCurrentSpawnRemainTime[i] -= Time.deltaTime;
				if (_listCurrentSpawnRemainTime[i] <= 0.0f)
					_listCurrentSpawnRemainTime[i] = 0.0f;
				continue;
			}
			else
			{
				_listCurrentSpawnRemainTime[i] += (_monsterSpawnBoosted ? _listCurrentNodeWarSpawnTableData[i].lastSpawnPeriod : _listCurrentNodeWarSpawnTableData[i].spawnPeriod);
			}
			if (Random.value > _listCurrentNodeWarSpawnTableData[i].spawnChance)
				continue;

			if (_listCurrentNodeWarSpawnTableData[i].totalMax)
			{
				if (_totalAliveMonsterCount >= (_monsterSpawnBoosted ? LastMonsterMaxCount : DefaultMonsterMaxCount))
					continue;

				++_totalAliveMonsterCount;
			}
			else
			{
				string key = _listCurrentNodeWarSpawnTableData[i].monsterId;
				if (_dicAliveMonsterCount.ContainsKey(key) && _dicAliveMonsterCount[key] >= (_monsterSpawnBoosted ? _listCurrentNodeWarSpawnTableData[i].lastMaxCount : _listCurrentNodeWarSpawnTableData[i].maxCount))
					continue;

				// totalMax를 안쓰는 몬스터는 각자 개별로 체크한다.
				if (_dicAliveMonsterCount.ContainsKey(key))
					_dicAliveMonsterCount[key] += 1;
				else
					_dicAliveMonsterCount.Add(key, 1);
			}

			SpawnMonster(_listCurrentNodeWarSpawnTableData[i].monsterId);
		}
	}

	void SpawnMonster(string monsterId)
	{
		GameObject monsterPrefab = NodeWarGround.instance.GetMonsterPrefab(monsterId);
		if (monsterPrefab == null)
			return;
		Vector2 normalizedOffset = Random.insideUnitCircle.normalized;
		Vector2 randomOffset = normalizedOffset * Random.Range(1.0f, 1.1f) * SpawnDistance;
		Vector3 desirePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
		BattleInstanceManager.instance.GetCachedObject(monsterPrefab, desirePosition, Quaternion.identity);
	}

	void UpdateMonsterDistance()
	{
		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;

		// 엄청나게 멀어질 경우 삭제하지 않으면 몬스터 총량 제한때문에 생성이 안되어버린다.
		// 그러니 이땐 강제로 죽는처리를 해줘야한다.
		MonsterActor findMonsterActor = null;
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = 0; i < listMonsterActor.Count; ++i)
		{
			Vector3 position = listMonsterActor[i].cachedTransform.position;
			Vector2 diff;
			diff.x = playerPosition.x - position.x;
			diff.y = playerPosition.z - position.z;
			if (diff.x * diff.x + diff.y * diff.y > MonsterValidDistance * MonsterValidDistance)
			{
				findMonsterActor = listMonsterActor[i];
				break;
			}
		}
		if (findMonsterActor != null)
		{
			findMonsterActor.actorStatus.SetHpRatio(0.0f);
			findMonsterActor.DisableForNodeWar();
			findMonsterActor.gameObject.SetActive(false);
		}
	}

	public override void OnSpawnMonster(MonsterActor monsterActor)
	{
		// 위 함수에서 다 처리해서 여기서 할게 없긴 한데 NavMesh가 없는 곳이라 Warning뜨지 않게 처리 하나 해둔다.
		monsterActor.pathFinderController.agent.enabled = false;
	}
	#endregion

	#region Trap
	float _trapSpawnRemainTime;
	const float TrapSpawnDelayMin = 3.0f;
	const float TrapSpawnDelayMax = 7.0f;
	void UpdateTrap()
	{
		if (_phase == ePhase.Success)
			return;

		_trapSpawnRemainTime -= Time.deltaTime;
		if (_trapSpawnRemainTime < 0.0f)
		{
			Vector3 resultPosition = Vector3.zero;
			if (GetTrapSpawnPosition(ref resultPosition))
			{
				BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.trapPrefab, resultPosition, Quaternion.identity);
				_trapSpawnRemainTime += Random.Range(TrapSpawnDelayMin, TrapSpawnDelayMax);
			}
			else
			{
				// 이 자리에서 만들 수 없다고 판단되면 잠시 딜레이를 줘서 조금 후에 다시 체크하도록 한다.
				_trapSpawnRemainTime += 1.0f;
			}
		}
	}

	public float TrapSpawnDistance = 24.0f;
	const float TrapNoSpawnRange = 12.0f;
	bool GetTrapSpawnPosition(ref Vector3 resultPosition)
	{
		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		playerPosition.y = 0.0f;
		for (int i = 0; i < 5; ++i)
		{
			// 먼저 플레이어 앞쪽에 랜덤으로 구해본다. 직선상에서만 구하면 너무 티나니 구해놓고 랜덤 오프셋을 한번 더 준다.
			Vector3 desirePosition = playerPosition + BattleInstanceManager.instance.playerActor.cachedTransform.forward * Random.Range(0.5f, 10.0f);
			Vector2 offset = Random.insideUnitCircle;
			desirePosition.x += offset.x * 2.0f;
			desirePosition.y = 0.0f;
			desirePosition.z += offset.y * 2.0f;

			// 랜덤으로 나온 포지션 근처에 이미 트랩이 존재한다면 다시 시도.
			if (NodeWarTrap.IsExistInRange(desirePosition, TrapNoSpawnRange))
				continue;
			if (_phase != ePhase.FindSoul && NodeWarExitPortal.instance != null)
			{
				Vector3 diff = desirePosition - NodeWarExitPortal.instance.cachedTransform.position;
				diff.y = 0.0f;
				if (diff.sqrMagnitude < (NodeWarExitPortal.instance.lastHealAreaRange + TrapNoSpawnRange * 0.5f) * (NodeWarExitPortal.instance.lastHealAreaRange + TrapNoSpawnRange * 0.5f))
					continue;
			}
			resultPosition = desirePosition;
			return true;
		}

		for (int i = 0; i < 20; ++i)
		{
			// 찾을 수 없다면 플레이어 주변으로 구해본다.
			Vector3 desirePosition = playerPosition;
			Vector2 offset = Random.insideUnitCircle;
			desirePosition.x += offset.x * TrapSpawnDistance;
			desirePosition.y = 0.0f;
			desirePosition.z += offset.y * TrapSpawnDistance;

			if (NodeWarTrap.IsExistInRange(desirePosition, TrapNoSpawnRange))
				continue;
			if (_phase != ePhase.FindSoul && NodeWarExitPortal.instance != null)
			{
				Vector3 diff = desirePosition - NodeWarExitPortal.instance.cachedTransform.position;
				diff.y = 0.0f;
				if (diff.sqrMagnitude < (NodeWarExitPortal.instance.lastHealAreaRange + TrapNoSpawnRange * 0.5f) * (NodeWarExitPortal.instance.lastHealAreaRange + TrapNoSpawnRange * 0.5f))
					continue;
			}
			resultPosition = desirePosition;
			return true;
		}
		return false;
	}
	#endregion

	#region Soul
	int _soulCount;
	float _soulSpawnRemainTime;
	const float SoulSpawnDelay = 4.0f;
	void UpdateSpawnSoul()
	{
		if (_phase != ePhase.FindSoul)
			return;

		// 페이즈1에서는 수집품목 아이템을 찾아야하므로 몬스터와 비슷하게 주변위치에 계속 생성해야하는데
		// 몬스터와 달리 이동중에만 생성해야한다.
		if (BattleInstanceManager.instance.playerActor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			return;

		_soulSpawnRemainTime -= Time.deltaTime;
		if (_soulSpawnRemainTime < 0.0f)
		{
			Vector3 resultPosition = Vector3.zero;
			if (GetSoulSpawnPosition(ref resultPosition))
			{
				BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.soulPrefab, resultPosition, Quaternion.identity);
				_soulSpawnRemainTime += SoulSpawnDelay;
			}
			else
			{
				// 이 자리에서 만들 수 없다고 판단되면 잠시 딜레이를 줘서 조금 후에 다시 체크하도록 한다.
				_soulSpawnRemainTime += 1.0f;
			}
		}
	}

	const float SoulNoDropRange = 24.0f;
	List<Vector3> _listSoulGetPosition = new List<Vector3>();
	bool GetSoulSpawnPosition(ref Vector3 resultPosition)
	{
		// 10회 돌려보고 안되면 못구하는거로 판정. 다음에 다시 시도하도록 한다.
		for (int i = 0; i < 10; ++i)
		{
			Vector2 normalizedOffset = Random.insideUnitCircle.normalized;
			Vector2 randomOffset = normalizedOffset * Random.Range(1.0f, 1.1f) * SpawnDistance;
			Vector3 desirePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);

			// 한번 획득한 자리 근처에서는 더이상 드랍되지 않게 해서 이동하면서 찾게 해야한다.
			bool inNoDropRange = false;
			for (int j = 0; j < _listSoulGetPosition.Count; ++j)
			{
				Vector2 diff;
				diff.x = _listSoulGetPosition[j].x - desirePosition.x;
				diff.y = _listSoulGetPosition[j].z - desirePosition.z;
				if (diff.x * diff.x + diff.y * diff.y < SoulNoDropRange * SoulNoDropRange)
				{
					inNoDropRange = true;
					break;
				}
			}
			if (inNoDropRange)
				continue;
			resultPosition = desirePosition;
			return true;
		}
		return false;
	}

	public override void OnGetSoul(Vector3 getPosition)
	{
		_listSoulGetPosition.Add(getPosition);
		BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.soulGetEffectPrefab, getPosition, Quaternion.identity);

		if (_listSoulGetPosition.Count < SoulCountMax)
		{
			BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarCollectingSoul", _listSoulGetPosition.Count, SoulCountMax), 3.5f);
		}
		else if (_listSoulGetPosition.Count == SoulCountMax)
		{
			// 임의의 위치에 포탈을 생성
			float distance = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MoveSpeed) * 15.0f;
			distance = Random.Range(distance * 0.8f, distance * 1.2f);
			Vector2 normalizedOffset = Random.insideUnitCircle.normalized;
			Vector2 randomOffset = normalizedOffset * Random.Range(1.0f, 1.1f) * distance;
			Vector3 desirePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
			desirePosition.y = 0.0f;
			BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.nodeWarExitPortalPrefab, desirePosition, Quaternion.identity);
			
			_phase = ePhase.FindPortal;
			_phaseStartTime = Time.time;
			BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarActivateMovePannel"), 3.5f);
		}
		else
		{
			// 초과해서 얻는거에 한해서는 무시
		}
	}
	#endregion

	#region Heal Orb
	float _healOrbSpawnRemainTime;
	// 마나에 비해선 천처히 나와야한다.
	const float HealOrbSpawnDelay = 7.0f;
	void UpdateSpawnHealOrb()
	{
		if (_phase == ePhase.Success)
			return;

		if (BattleInstanceManager.instance.playerActor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			return;

		if (_listSoulGetPosition.Count == 0)
			return;

		_healOrbSpawnRemainTime -= Time.deltaTime;
		if (_healOrbSpawnRemainTime < 0.0f)
		{
			Vector2 normalizedOffset = Random.insideUnitCircle.normalized;
			Vector2 randomOffset = normalizedOffset * Random.Range(1.0f, 1.1f) * SpawnDistance;
			Vector3 desirePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
			BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.healOrbPrefab, desirePosition, Quaternion.identity);
			_healOrbSpawnRemainTime += HealOrbSpawnDelay;
		}
	}

	public override void OnGetHealOrb(Vector3 getPosition)
	{
		AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
		healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("NodeWarHeal");
		BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, BattleInstanceManager.instance.playerActor, false);
		BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.healOrbGetEffectPrefab, getPosition, Quaternion.identity);
	}
	#endregion

	#region Boost Orb
	float _boostOrbSpawnRemainTime;
	const float BoostOrbSpawnDelay = 5.0f;
	void UpdateSpawnBoostOrb()
	{
		if (_phase == ePhase.WaitActivePortal || _phase == ePhase.Exit || _phase == ePhase.Success)
			return;

		if (BattleInstanceManager.instance.playerActor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			return;

		if (_listSoulGetPosition.Count == 0)
			return;

		_boostOrbSpawnRemainTime -= Time.deltaTime;
		if (_boostOrbSpawnRemainTime < 0.0f)
		{
			Vector2 normalizedOffset = Random.insideUnitCircle.normalized;
			Vector2 randomOffset = normalizedOffset * Random.Range(1.0f, 1.1f) * SpawnDistance;
			Vector3 desirePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
			BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.boostOrbPrefab, desirePosition, Quaternion.identity);
			_boostOrbSpawnRemainTime += BoostOrbSpawnDelay;
		}
	}

	static string s_generatedBoostId = "_generatedId_NodeWarBoostItem";
	public override void OnGetBoostOrb(Vector3 getPosition)
	{
		AffectorValueLevelTableData boostAffectorValue = new AffectorValueLevelTableData();
		// OverrideAffector가 제대로 호출되기 위해서 임시 아이디를 지정해줘야한다.
		boostAffectorValue.affectorValueId = s_generatedBoostId;
		boostAffectorValue.fValue1 = 5.0f; // duration
		boostAffectorValue.fValue2 = 3.0f;
		boostAffectorValue.iValue1 = (int)ActorStatusDefine.eActorStatus.MoveSpeed;
		BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.ChangeActorStatus, boostAffectorValue, BattleInstanceManager.instance.playerActor, false);
		BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.boostOrbGetEffectPrefab, getPosition, Quaternion.identity);
	}
	#endregion

	public override void OnTryActiveExitPortal()
	{
		if (_phase == ePhase.FindPortal)
		{
			_phase = ePhase.WaitActivePortal;
			_phaseStartTime = Time.time;
			BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarHealSpread"), 3.5f);
		}
	}

	public override void On5SecondAgoActiveExitPortal()
	{
		// 포탈을 활성화 하기 5초전부터 몬스터 스폰 부스트가 시작된다.
		_monsterSpawnBoosted = true;
	}

	public override void OnActiveExitPortal()
	{
		if (_phase == ePhase.WaitActivePortal)
		{
			_phase = ePhase.Exit;
			_phaseStartTime = Time.time;
			_activeExitPortalRemainTime = NodeWarExitPortal.ActivePortalTime;
			BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarPortalOpen"), 3.5f);
		}
	}

	public override void OnSuccessExitPortal()
	{
		if (_phase == ePhase.Exit)
		{
			_phase = ePhase.Success;
			_phaseStartTime = Time.time;

			// 모든 몬스터를 삭제해야한다. 뒤에서부터 루프 돈다.
			List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
			for (int i = listMonsterActor.Count - 1; i >= 0; --i)
			{
				MonsterActor monsterActor = listMonsterActor[i];
				monsterActor.actorStatus.SetHpRatio(0.0f);
				monsterActor.DisableForNodeWar();
				monsterActor.gameObject.SetActive(false);
			}

			// 모든 트랩도 삭제해야한다.
			NodeWarTrap.DisableAllTrap();

			// 도착지점 프리팹도 만들어낸다.
			BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.nodeWarEndSafeAreaPrefab, EndSafeAreaPosition, Quaternion.identity);
		}
	}

	float _activeExitPortalRemainTime;
	const float ExitFirstWarningTime = 15.0f;
	bool _exitFirstWarning;
	float[] ExitLastWarningTimeList = { 7.0f, 5.0f };
	bool[] _exitLastWarningList = { false, false };
	void UpdateExit()
	{
		if (_phase != ePhase.Exit)
			return;

		if (_activeExitPortalRemainTime > 0.0f)
		{
			_activeExitPortalRemainTime -= Time.deltaTime;
			if (_exitFirstWarning == false && _activeExitPortalRemainTime <= ExitFirstWarningTime)
			{
				BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarPortalLosingPower"), 3.5f);
				_exitFirstWarning = true;
			}

			for (int i = 0; i < _exitLastWarningList.Length; ++i)
			{
				if(_exitLastWarningList[i] == false && _activeExitPortalRemainTime <= ExitLastWarningTimeList[i])
				{
					BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarPortalLosingAlmost"), 1.5f);
					_exitLastWarningList[i] = true;
				}
			}

			if (_activeExitPortalRemainTime <= 0.0f)
			{
				_activeExitPortalRemainTime = 0.0f;
				BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarFailedToEscape"), 3.5f);
			}
		}
	}

	public override void OnDiePlayer(PlayerActor playerActor)
	{
		// 여기서 인풋은 막되
		LobbyCanvas.instance.battlePauseButton.interactable = false;

		// 드랍템이 없기 때문에 바로 endProcess를 진행하면 된다.
		//_endProcess = true;
		//_endProcessWaitRemainTime = 2.0f;
	}

	public override void OnDieMonster(MonsterActor monsterActor)
	{
		NodeWarSpawnTableData nodeWarSpawnTableData = TableDataManager.instance.FindNodeWarSpawnTableData(monsterActor.actorId);
		if (nodeWarSpawnTableData == null)
			return;

		if (nodeWarSpawnTableData.totalMax)
		{
			--_totalAliveMonsterCount;
		}
		else
		{
			// totalMax를 안쓰는 몬스터는 각자 개별로 체크한다.
			if (_dicAliveMonsterCount.ContainsKey(monsterActor.actorId))
				_dicAliveMonsterCount[monsterActor.actorId] -= 1;
		}
	}

	public override bool IsAutoPlay()
	{
		if (_totalAliveMonsterCount > 0)
			return true;
		Dictionary<string, int>.Enumerator e = _dicAliveMonsterCount.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current.Value > 0)
				return true;
		}
		return false;
	}
}