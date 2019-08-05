using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SubjectNerd.Utilities;

[CustomEditor(typeof(MonsterAI))]
public class MonsterAIEditor : Editor
{
	public override void OnInspectorGUI()
	{
		MonsterAI t = (MonsterAI)target;

		t.startDelay = EditorGUILayout.FloatField("Start Delay Time", t.startDelay);
		t.startState = (MonsterAI.eStateType)EditorGUILayout.EnumPopup("Start State Type", t.startState);

		if (t.useStateList[(int)t.startState] == false)
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.red;
			EditorGUILayout.LabelField("Start State is disabled", EditorStyles.whiteLabel);
			GUI.color = defaultColor;
		}

		DrawUILine(Color.grey);

		t.useStateList[0] = EditorGUILayout.Toggle("Use Random Move State", t.useStateList[0]);
		if (t.useStateList[0])
		{
			t.moveTimeRange = EditorGUILayout.Vector2Field("Move Total Time", t.moveTimeRange);
			t.refreshTickTimeRange = EditorGUILayout.Vector2Field("Refresh Tick Time", t.refreshTickTimeRange);
			t.desireDistance = EditorGUILayout.FloatField("Disire Distance", t.desireDistance);
		}

		DrawUILine(Color.grey);

		t.useStateList[1] = EditorGUILayout.Toggle("Use Custom Action State", t.useStateList[1]);
		if (t.useStateList[1])
		{
			t.useTableCustomActionName = EditorGUILayout.Toggle("Use Table Action Name", t.useTableCustomActionName);
			if (t.useTableCustomActionName)
			{
				t.customActionName = EditorGUILayout.TextField("Table Action Name", t.customActionName);
			}
			else
			{
				t.customActionName = EditorGUILayout.TextField("State Name", t.customActionName);
				t.customActionFadeDuration = EditorGUILayout.FloatField("Fade Duration", t.customActionFadeDuration);
			}
		}

		DrawUILine(Color.grey);

		t.useStateList[2] = EditorGUILayout.Toggle("Use Chase State", t.useStateList[2]);
		if (t.useStateList[2])
		{
			t.chaseDistance = EditorGUILayout.FloatField("Chase Distance", t.chaseDistance);
		}

		DrawUILine(Color.grey);

		t.useStateList[3] = EditorGUILayout.Toggle("Use Attack Action State", t.useStateList[3]);
		if (t.useStateList[3])
		{
			t.useTableAttackActionName = EditorGUILayout.Toggle("Use Table Action Name", t.useTableAttackActionName);
			if (t.useTableAttackActionName)
			{
				t.attackActionName = EditorGUILayout.TextField("Table Action Name", t.attackActionName);
			}
			else
			{
				t.attackActionName = EditorGUILayout.TextField("State Name", t.attackActionName);
				t.attackActionFadeDuration = EditorGUILayout.FloatField("Fade Duration", t.attackActionFadeDuration);
			}
			t.lookAtTargetBeforeAttack = EditorGUILayout.Toggle("Look At Target Before Attack", t.lookAtTargetBeforeAttack);
		}

		DrawUILine(Color.grey);

		t.useStateList[4] = EditorGUILayout.Toggle("Use Attack Delay State", t.useStateList[4]);
		if (t.useStateList[4])
		{
			t.attackDelayTimeRange = EditorGUILayout.Vector2Field("Attack Delay Time", t.attackDelayTimeRange);
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(t);
		}
	}

	public static void DrawUILine(Color color, int thickness = 1, int padding = 10)
	{
		Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
		rect.height = thickness;
		rect.y += padding / 2;
		EditorGUI.DrawRect(rect, color);
	}
}
