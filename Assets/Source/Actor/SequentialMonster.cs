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
		_createdInstanceCount = 0;
		_firstSpawn = true;

		BattleInstanceManager.instance.OnInitializeSequentialMonster(this);
	}

	int _remainSpawnCount = 0;
	float _remainDelay = 0.0f;
	// 위의 스폰은 스폰 타이밍을 결정하는 변수라서 현재 생성되어있는 몬스터 수를 계산하기엔 부적합하다. 실제로 생성은 아래 createdInstanceCount를 사용해야한다.
	int _createdInstanceCount;
	void Update()
	{
		if (_remainSpawnCount <= 0)
			return;

		if (_remainDelay > 0.0f)
		{
			_remainDelay -= Time.deltaTime;
			return;
		}

		if (_firstSpawn == false && GetAliveMonsterCount() >= maxAliveMonsterCount)
		{
			_remainDelay += defaultSpawnLoopInterval;
			return;
		}

		SpawnMonster();
		_remainSpawnCount -= 1;
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
	int _monsterCountPerSpawnSequence;
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
			}
		}

		MonsterActor monsterActor = null;
		if (groupMonster == null)
		{
			monsterActor = newObject.GetComponent<MonsterActor>();
			if (monsterActor != null)
			{
				monsterActor.sequentialMonster = this;
				monsterActor.checkOverlapPositionFrameCount = 100;
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

		// 스탯 초기화가 끝난 후에 AliveMonster에 넣는다. (이래야 hp계산 타이밍이 언제가 되든 문제없이 계산된다.)
		// 이때 실제로 생성한 카운트도 증가시켜서 동기를 맞춰둔다.
		// 한가지 주의해야할 점이 있는데 하필 위에서 한프레임 기다리는동안 같은 프레임에 원샷으로 맞아서 죽어버리면
		// OnDieMonster 함수가 먼저 호출되면서 Remove를 못하게 된다.
		// 그래서 차라리 Hp가 0이하라면 이미 죽었다고 판단해서 넣지 않기로 해본다.
		// (사실 이게 다 저 yield return Timing.WaitForOneFrame 때문이다. 지금 구조상 어쩔 수 없어서 이렇게 만든거니..)
		++_createdInstanceCount;
		if (groupMonster != null)
		{
			for (int i = 0; i < groupMonster.listMonsterActor.Count; ++i)
			{
				if (groupMonster.listMonsterActor[i].actorStatus.IsDie())
					continue;
				_listAliveMonsterActor.Add(groupMonster.listMonsterActor[i]);
			}
			if (_firstSpawn) _monsterCountPerSpawnSequence = groupMonster.listMonsterActor.Count;
		}
		if (groupMonster == null && monsterActor != null)
		{
			if (monsterActor.actorStatus.IsDie() == false)
				_listAliveMonsterActor.Add(monsterActor);
			if (_firstSpawn) _monsterCountPerSpawnSequence = 1;
		}

		if (_firstSpawn)
		{
			_firstSpawn = false;
			if (applyBossMonsterGauge)
			{
				// 여기서 한 그룹당 피가 얼마인지를 계산해둔다.
				_sumBossHpPerSpawnSequence = 0.0f;
				for (int i = 0; i < _listAliveMonsterActor.Count; ++i)
				{
					if (_listAliveMonsterActor[i].bossMonster == false)
						continue;
					_sumBossHpPerSpawnSequence += _listAliveMonsterActor[i].actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxHp);
				}
				BossMonsterGaugeCanvas.instance.InitializeSequentialGauge(this);
			}
		}
	}

	public int GetAliveMonsterCount()
	{
		// _listAliveMonsterActor 리스트만 가지고서는 제대로 계산하면 안된다. 만들고있는 몬스터들까지 포함시켜줘야한다.
		int count = _listAliveMonsterActor.Count;
		int creatingCount = (totalCount - _createdInstanceCount) - _remainSpawnCount;
		count += creatingCount * _monsterCountPerSpawnSequence;
		return count;
	}

	float _sumBossHpPerSpawnSequence;
	public float GetSumBossCurrentHp()
	{
		if (_firstSpawn == true)
		{
			// 아직 첫번째 스폰이 수행되기 전이라서 정보가 캐싱되어있지 않다. 호출되면 안되는 타이밍이다.
			Debug.LogError("Invalid call. This can be called after the first spawn.");
			return 0.0f;
		}

		float sumHp = 0.0f;
		for (int i = 0; i < _listAliveMonsterActor.Count; ++i)
			sumHp += _listAliveMonsterActor[i].actorStatus.GetHP();
		sumHp += (totalCount - _createdInstanceCount) * _sumBossHpPerSpawnSequence;
		return sumHp;
	}

	public void OnDieMonster(MonsterActor monsterActor)
	{
		if (_listAliveMonsterActor.Contains(monsterActor) == false)
		{
			Debug.LogError("Invalid call. monsterActor not contains.");
			return;
		}

		_listAliveMonsterActor.Remove(monsterActor);
		if (GetAliveMonsterCount() == 0)
		{
			if (totalCount == _createdInstanceCount)
			{
				BattleInstanceManager.instance.OnFinalizeSequentialMonster(this);
				gameObject.SetActive(false);
				return;
			}
			
			if (_remainSpawnCount > 0 && _remainDelay > 0.1f)
				_remainDelay = 0.1f;
		}
	}

	public bool IsLastAliveMonster(MonsterActor monsterActor)
	{
		if (totalCount - _createdInstanceCount > 0)
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
