using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;

public class BattleTestTool : EditorWindow
{
	[MenuItem("Window/Open Battle Test Window _F3")]
	static void Init()
	{
		EditorWindow.GetWindow<BattleTestTool>();
	}

	GUIContent guiContentTitle = new GUIContent("Battle Tool");
	void OnEnable()
	{
		titleContent = guiContentTitle;
		minSize = new Vector2(200, 400);
	}

	Color m_DefaultToolColor = new Color(0.8f, 0.8f, 0.8f);
	Color m_DefaultToolBackgroundColor = Color.white;
	
	string _notificationMsg = "Runs in Play mode!";
	GUIContent _notification = new GUIContent("Runs in Play mode!");
	void OnGUI()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
		}

		if (!Application.isPlaying)
		{
			//EditorApplication.isPlaying = true;
			EditorGUILayout.HelpBox(_notificationMsg, MessageType.Info);
			ShowNotification(_notification);
			return;
		}

		OnGUI_Player();
	}

	PlayerAI _playerAI = null;
	bool usePlayerAI = true;
	void OnGUI_Player()
	{
		GUILayout.BeginVertical("box");
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.cyan;
			string szDesc = string.Format("Player");
			EditorGUILayout.LabelField(szDesc, EditorStyles.textField);
			GUI.color = defaultColor;

			usePlayerAI = EditorGUILayout.Toggle("Toggle Player AI :", usePlayerAI);
			if (_playerAI == null)
				_playerAI = GameObject.FindObjectOfType<PlayerAI>();
			if (_playerAI != null)
				_playerAI.enabled = usePlayerAI;
		}
		GUILayout.EndVertical();	
	}
}
