﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
#endif
using SubjectNerd.Utilities;

[ExecuteInEditMode]
public class SpawnFlag : MonoBehaviour
{
	public Transform playerStartSpawnTransform;
	public Transform playerClearSpawnTransform;
	public Transform gatePillarSpawnTransform;
	public Transform powerSourceSpawnTransform;
	public Transform returnScrollSpawnTransform;

	[Serializable]
	public class SpawnInfo
	{
		public GameObject prefab;
		public Vector3 localPosition;
		public Vector3 localRotation;
		public Vector3 localScale;
	}
	[Reorderable]
	public List<SpawnInfo> _listSpawnInfo;

#if UNITY_EDITOR
	bool _editorSpawned = false;
#endif
	void Awake()
	{
#if UNITY_EDITOR
		if (PrefabStageUtility.GetCurrentPrefabStage() != null)
			return;

		if (Application.isPlaying == false)
		{
			Spawn(true);
			_editorSpawned = true;
		}
#endif
	}

	// Start is called before the first frame update
	void Start()
    {
#if UNITY_EDITOR
		if (PrefabStageUtility.GetCurrentPrefabStage() != null)
			return;

		if (_editorSpawned)
			return;
#endif
		Spawn();
	}

	void Spawn(bool editorSpawn = false)
	{
#if UNITY_EDITOR
		for (int i = cachedTransform.childCount - 1; i >= 0; --i)
		{
			Transform childTransform = cachedTransform.GetChild(i);
			if (childTransform == playerStartSpawnTransform || childTransform == playerClearSpawnTransform)
				continue;
			if (childTransform == gatePillarSpawnTransform || childTransform == powerSourceSpawnTransform || childTransform == returnScrollSpawnTransform)
				continue;
			DestroyImmediate(childTransform.gameObject);
		}
#endif

		if (editorSpawn == false && ClientSaveData.instance.GetCachedMonsterAllKill())
		{
		}
		else
		{
			if (editorSpawn == false)
			{
				// 몬스터의 인덱스를 관리하기 위해 호출
				BattleInstanceManager.instance.OnPreInitializeMonster();
			}

			// 여기는 평소엔 절대 건너뛰면 안되는 곳이다.
			for (int i = 0; i < _listSpawnInfo.Count; ++i)
			{
#if UNITY_EDITOR
				GameObject newObject = null;
				if (editorSpawn)
				{
					newObject = PrefabUtility.InstantiatePrefab((UnityEngine.Object)_listSpawnInfo[i].prefab, cachedTransform) as GameObject;
					newObject.transform.localPosition = _listSpawnInfo[i].localPosition;
					newObject.transform.localRotation = Quaternion.Euler(_listSpawnInfo[i].localRotation);
				}
				else
					newObject = BattleInstanceManager.instance.GetCachedObject(_listSpawnInfo[i].prefab, _listSpawnInfo[i].localPosition, Quaternion.Euler(_listSpawnInfo[i].localRotation), cachedTransform);
#else
				GameObject newObject = BattleInstanceManager.instance.GetCachedObject(_listSpawnInfo[i].prefab, _listSpawnInfo[i].localPosition, Quaternion.Euler(_listSpawnInfo[i].localRotation), cachedTransform);
#endif
				newObject.transform.localScale = _listSpawnInfo[i].localScale;
			}
		}

		if (editorSpawn == false)
		{
			Transform spawnTransform = playerStartSpawnTransform;
			if (ClientSaveData.instance.GetCachedMonsterAllKill() || ClientSaveData.instance.GetCachedGatePillar())
				spawnTransform = playerClearSpawnTransform;

			if (MainSceneBuilder.instance == null || MainSceneBuilder.instance.mainSceneBuilding == false)
				BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerSpawnEffectPrefab, spawnTransform.position, Quaternion.identity);

			if (BattleInstanceManager.instance.playerActor != null)
				BattleInstanceManager.instance.playerActor.cachedTransform.position = spawnTransform.position;
			CustomFollowCamera.instance.immediatelyUpdate = true;
			StageManager.instance.currentGatePillarSpawnPosition = gatePillarSpawnTransform.position;
			StageManager.instance.spawnPowerSourcePrefab = powerSourceSpawnTransform.gameObject.activeSelf;
			if (StageManager.instance.spawnPowerSourcePrefab)
				StageManager.instance.currentPowerSourceSpawnPosition = powerSourceSpawnTransform.position;
			if (returnScrollSpawnTransform != null)
				StageManager.instance.currentReturnScrollSpawnPosition = returnScrollSpawnTransform.position;

			if (BattleManager.instance != null)
				BattleManager.instance.OnSpawnFlag();
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.mainSceneBuilding)
				MainSceneBuilder.instance.waitSpawnFlag = true;
		}
	}






	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
