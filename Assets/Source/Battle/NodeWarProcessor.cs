using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MecanimStateDefine;

public class NodeWarProcessor : BattleModeProcessorBase
{
	public static float SpawnDistance = 16.0f;
	public static int DefaultMonsterMaxCount = 50;
	public static int SoulCountMax = 10;

	// 몹이나 아이템 둘다 이 거리를 넘어서면 강제로 삭제한다.
	public static float ValidDistance = 30.0f;

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
		UpdateSpawnSoul();
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

		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("AtkBetter", false, 0);
		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("AtkBetter", false, 0);
		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("AtkSpeedBetter", false, 0);
		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("AtkSpeedBetter", false, 0);
		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("CritBetter", false, 0);
		BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack("CritBetter", false, 0);
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
			for (int i = 0; i < TableDataManager.instance.nodeWarSpawnTable.dataArray.Length; ++i)
				_listSpawnRemainTime.Add(0.0f);
			_totalAliveMonsterCount = 0;
		}
		_phase = ePhase.FindSoul;
		_phaseStartTime = Time.time;
		_soulSpawnRemainTime = SoulSpawnDelay;
		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarRule1Mind"), 3.5f);
	}

	public override NodeWarTableData GetSelectedNodeWarTableData()
	{
		return _selectedNodeWarTableData;
	}

	List<float> _listSpawnRemainTime = new List<float>();
	Dictionary<string, int> _dicAliveMonsterCount = new Dictionary<string, int>();
	int _totalAliveMonsterCount;
	void UpdateSpawnMonster()
	{
		if (_phase == ePhase.Success)
			return;

		float diffTime = Time.time - _phaseStartTime;
		for (int i = 0; i < TableDataManager.instance.nodeWarSpawnTable.dataArray.Length; ++i)
		{
			if (_selectedNodeWarTableData.level < TableDataManager.instance.nodeWarSpawnTable.dataArray[i].minLevel)
				continue;
			if ((int)_phase < TableDataManager.instance.nodeWarSpawnTable.dataArray[i].minStep)
				continue;
			if (diffTime < TableDataManager.instance.nodeWarSpawnTable.dataArray[i].firstWaiting)
				continue;

			if (_listSpawnRemainTime[i] > 0.0f)
			{
				_listSpawnRemainTime[i] -= Time.deltaTime;
				if (_listSpawnRemainTime[i] <= 0.0f)
					_listSpawnRemainTime[i] = 0.0f;
				continue;
			}
			else
			{
				_listSpawnRemainTime[i] += TableDataManager.instance.nodeWarSpawnTable.dataArray[i].spawnPeriod;
			}
			if (Random.value > TableDataManager.instance.nodeWarSpawnTable.dataArray[i].spawnChance)
				continue;

			if (TableDataManager.instance.nodeWarSpawnTable.dataArray[i].totalMax)
			{
				if (_totalAliveMonsterCount >= DefaultMonsterMaxCount)
					continue;

				++_totalAliveMonsterCount;
			}
			else
			{
				string key = TableDataManager.instance.nodeWarSpawnTable.dataArray[i].monsterId;
				if (_dicAliveMonsterCount.ContainsKey(key) && _dicAliveMonsterCount[key] >= TableDataManager.instance.nodeWarSpawnTable.dataArray[i].maxCount)
					continue;

				// totalMax를 안쓰는 몬스터는 각자 개별로 체크한다.
				if (_dicAliveMonsterCount.ContainsKey(key))
					_dicAliveMonsterCount[key] += 1;
				else
					_dicAliveMonsterCount.Add(key, 1);
			}

			SpawnMonster(TableDataManager.instance.nodeWarSpawnTable.dataArray[i].monsterId);
		}
	}

	void SpawnMonster(string monsterId)
	{
		GameObject monsterPrefab = NodeWarGround.instance.GetMonsterPrefab(monsterId);
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
			if (diff.x * diff.x + diff.y * diff.y > ValidDistance * ValidDistance)
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

	int _soulCount;
	float _soulSpawnRemainTime;
	// 2분동안 10개를 모아야하니 개당 대략 12초인데 뒤에 생성되서 못얻을때도 있을거 대비해서 조금 줄여둔다.
	const float SoulSpawnDelay = 10.0f;
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
			BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarCollectingSoul", _listSoulGetPosition.Count), 3.5f);
		}
		else if (_listSoulGetPosition.Count == SoulCountMax)
		{
			// 임의의 위치에 포탈을 생성
			float distance = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MoveSpeed) * 15.0f;
			distance = Random.Range(distance * 0.8f, distance * 1.2f);
			Vector2 normalizedOffset = Random.insideUnitCircle.normalized;
			Vector2 randomOffset = normalizedOffset * Random.Range(1.0f, 1.1f) * distance;
			Vector3 desirePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);
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

	public override void OnTryActiveExitPortal()
	{
		if (_phase == ePhase.FindPortal)
		{
			_phase = ePhase.WaitActivePortal;
			_phaseStartTime = Time.time;
			BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarStartActivating"), 3.5f);
		}
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

			// 도착지점 프리팹도 만들어낸다.
			BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.nodeWarEndSafeAreaPrefab, EndSafeAreaPosition, Quaternion.identity);
		}
	}

	float _activeExitPortalRemainTime;
	const float ExitFirstWarningTime = 15.0f;
	bool _exitFirstWarning;
	float[] ExitLastWarningTimeList = { 6.0f, 4.0f, 2.0f };
	bool[] _exitLastWarningList = { false, false, false };
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