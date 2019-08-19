using System;
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
			if (childTransform == gatePillarSpawnTransform || childTransform == powerSourceSpawnTransform)
				continue;
			DestroyImmediate(childTransform.gameObject);
		}
#endif

		if (_listSpawnInfo == null)
			return;

		for (int i = 0; i < _listSpawnInfo.Count; ++i)
		{
#if UNITY_EDITOR
			GameObject newObject = null;
			if (editorSpawn)
				newObject = PrefabUtility.InstantiatePrefab((UnityEngine.Object)_listSpawnInfo[i].prefab, cachedTransform) as GameObject;
			else
				newObject = BattleInstanceManager.instance.GetCachedObject(_listSpawnInfo[i].prefab, cachedTransform);
#else
			GameObject newObject = Instantiate(_listSpawnInfo[i].prefab, cachedTransform);
#endif
			newObject.transform.localPosition = _listSpawnInfo[i].localPosition;
			newObject.transform.localRotation = Quaternion.Euler(_listSpawnInfo[i].localRotation);
			newObject.transform.localScale = _listSpawnInfo[i].localScale;
		}

		if (editorSpawn == false)
		{
			BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerSpawnEffectPrefab, playerStartSpawnTransform.position, Quaternion.identity);
			BattleInstanceManager.instance.playerActor.cachedTransform.position = playerStartSpawnTransform.position;
			CustomFollowCamera.instance.immediatelyUpdate = true;
			StageManager.instance.currentGatePillarSpawnPosition = gatePillarSpawnTransform.position;
			StageManager.instance.spawnPowerSourcePrefab = powerSourceSpawnTransform.gameObject.activeSelf;
			if (StageManager.instance.spawnPowerSourcePrefab)
				StageManager.instance.currentPowerSourceSpawnPosition = powerSourceSpawnTransform.position;

			BattleManager.instance.OnSpawnFlag();
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
