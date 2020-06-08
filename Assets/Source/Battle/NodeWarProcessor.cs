using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeWarProcessor : BattleModeProcessorBase
{
	public static float SpawnDistance = 16.0f;

	NodeWarTableData _selectedNodeWarTableData;

	public override void Update()
	{
		if (_selectedNodeWarTableData == null)
			return;

		UpdatePhase1();
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
		_selectedNodeWarTableData = TableDataManager.instance.FindNodeWarTableData(level);
	}

	public override NodeWarTableData GetSelectedNodeWarTableData()
	{
		return _selectedNodeWarTableData;
	}

	float DefaultSpawnDelay = 1.0f;
	float _spawnRemainTime;
	void UpdatePhase1()
	{
		// 일정 마리수를 채울때까지 계속 스폰할 것인가.
		// 아니면 레벨별로 스폰 딜레이가 정해져있어서 정해진 주기대로 스폰할 것인가.
		// 그래도 한프레임에 통째로 만드는거보단 나눠서 만드는게 나을거 같기도 하다.
		// wave 형태가 아니다보니 

		_spawnRemainTime -= Time.deltaTime;
		if (_spawnRemainTime <= 0.0f)
		{
			Vector2 normalizedOffset = UnityEngine.Random.insideUnitCircle.normalized;
			Vector2 randomOffset = normalizedOffset * UnityEngine.Random.Range(1.0f, 1.1f) * SpawnDistance;
			Vector3 desirePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(randomOffset.x, 0.0f, randomOffset.y);

#if UNITY_EDITOR
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.monsterPrefabList[0], desirePosition, Quaternion.identity);
#else
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(NodeWarGround.instance.monsterPrefabList[0], cachedTransform);
#endif
			_spawnRemainTime += DefaultSpawnDelay;
		}
	}
}
