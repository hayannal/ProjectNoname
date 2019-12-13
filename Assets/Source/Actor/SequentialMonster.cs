using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SubjectNerd.Utilities;
using MEC;

public class SequentialMonster : MonoBehaviour
{
	public GameObject spawnPrefab;
	public GameObject spawnEffectPrefab;
	public int totalCount;
	public float startDelay;
	public float defaultSpawnInterval;
	public float defaultSpawnLoopInterval;
	public int maxAliveMonsterCount = 50;
	public bool useRandomSpawnIndex;
	public bool applyBossMonsterGauge;

	[Reorderable]
	public List<Transform> listSpawnPositionInfo;

	private void OnEnable()
	{
		_remainSpawnCount = totalCount;
		_remainDelay = startDelay;
		_listAliveMonsterActor.Clear();
		_spawnPositionIndex = 0;
		_spawnLooped = false;
		_firstSpawn = true;

		BattleInstanceManager.instance.OnInitializeSequentialMonster(this);
	}

	public int GetRemainSpawnCount()
	{
		return _remainSpawnCount;
	}

	int _remainSpawnCount = 0;
	float _remainDelay = 0.0f;
	void Update()
	{
		if (_remainDelay > 0.0f)
		{
			_remainDelay -= Time.deltaTime;
			return;
		}

		if (_listAliveMonsterActor.Count >= maxAliveMonsterCount)
		{
			_remainDelay += defaultSpawnLoopInterval;
			return;
		}

		SpawnMonster();
		_remainSpawnCount -= 1;
		if (_remainSpawnCount <= 0)
		{
			BattleInstanceManager.instance.OnFinalizeSequentialMonster(this);
			gameObject.SetActive(false);
			return;
		}

		_remainDelay += _spawnLooped ? defaultSpawnLoopInterval : defaultSpawnInterval;
		_spawnLooped = false;
	}

	List<MonsterActor> _listAliveMonsterActor = new List<MonsterActor>();
	int _spawnPositionIndex;
	bool _spawnLooped = false;
	void SpawnMonster()
	{
		Vector3 spawnPosition = Vector3.zero;
		Quaternion spawnRotation = Quaternion.identity;
		if (useRandomSpawnIndex)
		{
			Transform spawnTransform = listSpawnPositionInfo[UnityEngine.Random.Range(0, listSpawnPositionInfo.Count)];
			spawnPosition = spawnTransform.position;
			spawnRotation = spawnTransform.rotation;				
		}
		else
		{
			Transform spawnTransform = listSpawnPositionInfo[_spawnPositionIndex];
			spawnPosition = spawnTransform.position;
			spawnRotation = spawnTransform.rotation;

			++_spawnPositionIndex;
			if (_spawnPositionIndex >= listSpawnPositionInfo.Count)
			{
				_spawnPositionIndex = 0;
				_spawnLooped = true;
			}
		}

		Timing.RunCoroutine(DelayedSpawnMonster(spawnPosition, spawnRotation));
	}

	bool _firstSpawn;
	IEnumerator<float> DelayedSpawnMonster(Vector3 spawnPosition, Quaternion spawnRotation)
	{
		// 겹쳐서 스폰될때 다른 액터의 컬리더 위로 올라가는 현상을 줄이기 위해 랜덤 오차를 넣어본다.
		// 이거에 더해서 100프레임동안 y값이 높아지는지도 확인하고 있는데
		// 만약 이거로도 처리되지 않으면 정석대로 생성된 오브젝트 확인하면서 포지션이 겹치지 않게 검사해야할거다.
		Vector3 randomAdjustPosition = new Vector3(UnityEngine.Random.value * 0.01f - 0.005f, 0.0f, UnityEngine.Random.value * 0.01f - 0.005f);
		BattleInstanceManager.instance.GetCachedObject(spawnEffectPrefab, spawnPosition + randomAdjustPosition, spawnRotation);

		yield return Timing.WaitForSeconds(1.0f);

		// avoid gc
		if (this == null)
			yield break;

		GameObject newObject = BattleInstanceManager.instance.GetCachedObject(spawnPrefab, spawnPosition + randomAdjustPosition, spawnRotation);
		GroupMonster groupMonster = newObject.GetComponent<GroupMonster>();
		if (groupMonster != null)
		{
			for (int i = 0; i < groupMonster.listMonsterActor.Count; ++i)
			{
				groupMonster.listMonsterActor[i].sequentialMonster = this;
				groupMonster.listMonsterActor[i].checkOverlapPositionFrameCount = 100;
				_listAliveMonsterActor.Add(groupMonster.listMonsterActor[i]);
			}
		}

		if (groupMonster == null)
		{
			MonsterActor monsterActor = newObject.GetComponent<MonsterActor>();
			if (monsterActor != null)
			{
				monsterActor.sequentialMonster = this;
				monsterActor.checkOverlapPositionFrameCount = 100;
				_listAliveMonsterActor.Add(monsterActor);
			}
		}

		// 몹을 처음 스폰할땐 상관이 없는데 재활용해서 얻어올때의 MonsterActor.InitializeMonster 함수에서
		// sequentialMonster 셋팅이 되기 전에 미리 BossMonsterGaugeCanvas.instance.InitializeGauge 함수가 호출되면서
		// 계산을 할 수 없는 경우가 생겼다.
		// 그래서 차라리 안전하게 최초로 몬스터 스폰한 다음 프레임에 초기화 하기로 한다.
		yield return Timing.WaitForOneFrame;

		// avoid gc
		if (this == null)
			yield break;

		if (_firstSpawn && applyBossMonsterGauge)
		{
			// 여기서 한 그룹당 피가 얼마인지를 계산해둔다.
			_sumHpPerSpawnSequence = 0.0f;
			for (int i = 0; i < _listAliveMonsterActor.Count; ++i)
			{
				if (_listAliveMonsterActor[i].bossMonster == false)
					continue;
				_sumHpPerSpawnSequence += _listAliveMonsterActor[i].actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxHp);
			}
			BossMonsterGaugeCanvas.instance.InitializeSequentialGauge(this);
		}
		_firstSpawn = false;
	}

	float _sumHpPerSpawnSequence;
	public float GetCurrentHp()
	{
		float sumHp = 0.0f;
		for (int i = 0; i < _listAliveMonsterActor.Count; ++i)
			sumHp += _listAliveMonsterActor[i].actorStatus.GetHP();
		sumHp += (_remainSpawnCount * _sumHpPerSpawnSequence);
		return sumHp;
	}

	public void OnDieMonster(MonsterActor monsterActor)
	{
		if (_listAliveMonsterActor.Contains(monsterActor) == false)
			return;

		_listAliveMonsterActor.Remove(monsterActor);
		if (_listAliveMonsterActor.Count == 0 && _remainDelay > 0.1f)
			_remainDelay = 0.1f;
	}

	public bool IsLastAliveMonster(MonsterActor monsterActor)
	{
		if (_remainSpawnCount > 0)
			return false;

		bool allDie = true;
		for (int i = 0; i < _listAliveMonsterActor.Count; ++i)
		{
			if (_listAliveMonsterActor[i] == monsterActor)
				continue;

			if (_listAliveMonsterActor[i].actorStatus.IsDie() == false)
			{
				allDie = false;
				break;
			}
		}
		return allDie;
	}
}
