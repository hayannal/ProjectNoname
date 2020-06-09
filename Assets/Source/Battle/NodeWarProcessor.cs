using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarProcessor : BattleModeProcessorBase
{
	public static float SpawnDistance = 16.0f;
	public static int DefaultMonsterMaxCount = 50;

	enum ePhase
	{
		FindKey = 1,
		Node,
		Exit,
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
		_phase = ePhase.FindKey;
		_phaseStartTime = Time.time;
		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarRule1Mind"), 3.5f);

		// 페이즈1 몬스터를 만들어야한다. 인디케이터도 만들어야한다.
	}

	public override NodeWarTableData GetSelectedNodeWarTableData()
	{
		return _selectedNodeWarTableData;
	}

	List<float> _listSpawnRemainTime = new List<float>();
	Dictionary<string, int> _listAliveMonsterCount = new Dictionary<string, int>();
	int _totalAliveMonsterCount;
	void UpdateSpawnMonster()
	{
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
				if (_listAliveMonsterCount.ContainsKey(key) && _listAliveMonsterCount[key] >= TableDataManager.instance.nodeWarSpawnTable.dataArray[i].maxCount)
					continue;

				// totalMax를 안쓰는 몬스터는 각자 개별로 체크한다.
				if (_listAliveMonsterCount.ContainsKey(key))
					_listAliveMonsterCount[key] += 1;
				else
					_listAliveMonsterCount.Add(key, 1);
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
		// 엄청나게 멀어질 경우 삭제하지 않으면 몬스터 총량 제한때문에 생성이 안되어버린다.
		// 그러니 이땐 강제로 죽는처리를 해줘야한다.
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = 0; i < listMonsterActor.Count; ++i)
		{

		}
	}

	public override void OnSpawnMonster(MonsterActor monsterActor)
	{
		// 위 함수에서 다 처리해서 여기서 할게 없긴 한다.
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
			if (_listAliveMonsterCount.ContainsKey(monsterActor.actorId))
				_listAliveMonsterCount[monsterActor.actorId] -= 1;
		}
	}
}