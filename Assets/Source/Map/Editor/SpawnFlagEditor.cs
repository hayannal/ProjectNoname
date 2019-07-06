using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnFlag))]
public class SpawnFlagEditor : Editor
{
	SerializedProperty playerSpawnTransformProperty;

	void OnEnable()
	{
		playerSpawnTransformProperty = serializedObject.FindProperty("playerSpawnTransform");
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

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

			PrefabUtility.SavePrefabAsset(prefab);
		}
	}
}
