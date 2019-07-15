using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SubjectNerd.Utilities;

[CustomEditor(typeof(SpawnFlag))]
public class SpawnFlagEditor : ReorderableArrayInspector
{
	SerializedProperty playerSpawnTransformProperty;

	protected override void InitInspector()
	{
		base.InitInspector();

		alwaysDrawInspector = true;

		playerSpawnTransformProperty = serializedObject.FindProperty("playerSpawnTransform");
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
				if (childTransform == targetComponent.playerSpawnTransform)
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

			if (targetComponent.playerSpawnTransform == null)
			{
				EditorUtility.DisplayDialog("Error", "Not found PlayerSpawnTransform in the memory", "Ok");
			}
			if (targetComponent.playerSpawnTransform != null)
			{
				if (spawnFlagPrefabComponent.playerSpawnTransform == null)
				{
					EditorUtility.DisplayDialog("Error", "Not found PlayerSpawnTransform in the prefab", "Ok");
				}
				else
					spawnFlagPrefabComponent.playerSpawnTransform.localPosition = targetComponent.playerSpawnTransform.localPosition;
			}

			PrefabUtility.SavePrefabAsset(prefab);
		}
	}
}
