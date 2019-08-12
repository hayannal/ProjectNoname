using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SubjectNerd.Utilities;

[CustomEditor(typeof(SpawnFlag))]
public class SpawnFlagEditor : ReorderableArrayInspector
{
	SerializedProperty playerStartSpawnTransformProperty;
	SerializedProperty playerClearSpawnTransformProperty;
	SerializedProperty gatePillarSpawnTransformProperty;
	SerializedProperty powerSourceSpawnTransformProperty;

	protected override void InitInspector()
	{
		base.InitInspector();

		alwaysDrawInspector = true;

		playerStartSpawnTransformProperty = serializedObject.FindProperty("playerStartSpawnTransform");
		playerClearSpawnTransformProperty = serializedObject.FindProperty("playerClearSpawnTransform");
		gatePillarSpawnTransformProperty = serializedObject.FindProperty("gatePillarSpawnTransform");
		powerSourceSpawnTransformProperty = serializedObject.FindProperty("powerSourceSpawnTransform");
	}

	protected override void DrawInspector()
	{
		base.DrawInspector();

		if (GUILayout.Button("Save Spawn Flag Data"))
		{
			SpawnFlag targetComponent = (SpawnFlag)target;
			GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource<GameObject>(targetComponent.gameObject);
			if (prefab == null)
			{
				EditorUtility.DisplayDialog("Error", "Can't find original Prefab", "Ok");
				return;
			}

			string assetPath = AssetDatabase.GetAssetPath(prefab);
			if (string.IsNullOrEmpty(assetPath))
			{
				EditorUtility.DisplayDialog("Error", "Path not found", "Ok");
				return;
			}

			SpawnFlag spawnFlagPrefabComponent = prefab.GetComponent<SpawnFlag>();
			if (spawnFlagPrefabComponent._listSpawnInfo == null)
				spawnFlagPrefabComponent._listSpawnInfo = new List<SpawnFlag.SpawnInfo>();
			spawnFlagPrefabComponent._listSpawnInfo.Clear();

			int childCount = targetComponent.cachedTransform.childCount;
			for (int i = 0; i < childCount; ++i)
			{
				Transform childTransform = targetComponent.cachedTransform.GetChild(i);
				if (childTransform == targetComponent.playerStartSpawnTransform || childTransform == targetComponent.playerClearSpawnTransform)
					continue;
				if (childTransform == targetComponent.gatePillarSpawnTransform || childTransform == targetComponent.powerSourceSpawnTransform)
					continue;

				GameObject monsterPrefab = PrefabUtility.GetCorrespondingObjectFromSource<GameObject>(childTransform.gameObject);
				if (monsterPrefab == null)
					continue;
				string monsterPrefabPath = AssetDatabase.GetAssetPath(monsterPrefab);
				if (string.IsNullOrEmpty(monsterPrefabPath))
					continue;
				if (monsterPrefabPath.Contains("Assets/Resource/Monster") == false)
					continue;

				SpawnFlag.SpawnInfo info = new SpawnFlag.SpawnInfo();
				info.prefab = monsterPrefab;
				info.localPosition = childTransform.localPosition;
				info.localRotation = childTransform.localRotation.eulerAngles;
				info.localScale = childTransform.localScale;
				spawnFlagPrefabComponent._listSpawnInfo.Add(info);
			}

			if (targetComponent.playerStartSpawnTransform == null)
			{
				EditorUtility.DisplayDialog("Error", "Not found PlayerStartSpawnTransform in the memory", "Ok");
			}
			if (targetComponent.playerStartSpawnTransform != null)
			{
				if (spawnFlagPrefabComponent.playerStartSpawnTransform == null)
				{
					EditorUtility.DisplayDialog("Error", "Not found PlayerStartSpawnTransform in the prefab", "Ok");
				}
				else
					spawnFlagPrefabComponent.playerStartSpawnTransform.localPosition = targetComponent.playerStartSpawnTransform.localPosition;
			}

			if (targetComponent.playerClearSpawnTransform == null)
			{
				EditorUtility.DisplayDialog("Error", "Not found PlayerClearSpawnTransform in the memory", "Ok");
			}
			if (targetComponent.playerClearSpawnTransform != null)
			{
				if (spawnFlagPrefabComponent.playerClearSpawnTransform == null)
				{
					EditorUtility.DisplayDialog("Error", "Not found PlayerClearSpawnTransform in the prefab", "Ok");
				}
				else
					spawnFlagPrefabComponent.playerClearSpawnTransform.localPosition = targetComponent.playerClearSpawnTransform.localPosition;
			}

			if (targetComponent.gatePillarSpawnTransform == null)
			{
				EditorUtility.DisplayDialog("Error", "Not found GatePillarSpawnTransform in the memory", "Ok");
			}
			if (targetComponent.gatePillarSpawnTransform != null)
			{
				if (spawnFlagPrefabComponent.gatePillarSpawnTransform == null)
				{
					EditorUtility.DisplayDialog("Error", "Not found GatePillarSpawnTransform in the prefab", "Ok");
				}
				else
					spawnFlagPrefabComponent.gatePillarSpawnTransform.localPosition = targetComponent.gatePillarSpawnTransform.localPosition;
			}

			if (targetComponent.powerSourceSpawnTransform == null)
			{
				EditorUtility.DisplayDialog("Error", "Not found PowerSourceSpawnTransform in the memory", "Ok");
			}
			if (targetComponent.powerSourceSpawnTransform != null)
			{
				if (spawnFlagPrefabComponent.powerSourceSpawnTransform == null)
				{
					EditorUtility.DisplayDialog("Error", "Not found PowerSourceSpawnTransform in the prefab", "Ok");
				}
				else
					spawnFlagPrefabComponent.powerSourceSpawnTransform.localPosition = targetComponent.powerSourceSpawnTransform.localPosition;
			}

			PrefabUtility.SavePrefabAsset(prefab);
		}
	}
}
