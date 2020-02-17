using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(HUDDPS))]
public class HUDDPSEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("Copy Data"))
		{
			HUDDPS targetComponent = (HUDDPS)target;
			targetComponent.CopyData();
		}
	}
}
