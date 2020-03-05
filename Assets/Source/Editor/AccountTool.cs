using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;
using ECM.Controllers;
using UnityEngine.SceneManagement;
using ActorStatusDefine;

public class AccountTool : EditorWindow
{
	[MenuItem("Window/Open Account Tool")]
	static void Init()
	{
		EditorWindow.GetWindow<AccountTool>();
	}

	GUIContent guiContentTitle = new GUIContent("Account Tool");
	void OnEnable()
	{
		titleContent = guiContentTitle;
		minSize = new Vector2(200, 200);
	}

	Color m_DefaultToolColor = new Color(0.8f, 0.8f, 0.8f);
	Color m_DefaultToolBackgroundColor = Color.white;

	string _notificationMsg = "Runs in Edit mode!";
	GUIContent _notification = new GUIContent("Runs in Edit mode!");
	void OnGUI()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
		}

		if (Application.isPlaying)
		{
			//EditorApplication.isPlaying = true;
			EditorGUILayout.HelpBox(_notificationMsg, MessageType.Info);
			ShowNotification(_notification);
			return;
		}

		OnGUI_Guest();
	}

	string _guestCustomId;
	void OnGUI_Guest()
	{
		GUILayout.BeginVertical("box");
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.cyan;
			string szDesc = string.Format("Guest");
			EditorGUILayout.LabelField(szDesc, EditorStyles.textField);
			GUI.color = defaultColor;

			_guestCustomId = EditorGUILayout.TextField("Guest CustomId :", _guestCustomId);

			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Get Cached Custom Id"))
				{
					_guestCustomId = AuthManager.GetLastGuestCustomId();
				}
				if (GUILayout.Button("Set Custom Id"))
				{
					AuthManager.SetGuestCustomId(_guestCustomId);
				}
			}
			GUILayout.EndHorizontal();

			if (GUILayout.Button("Delete Cached Login Info"))
			{
				AuthManager.DeleteCachedLastLoginInfo();
				_guestCustomId = "";
			}
		}
		GUILayout.EndVertical();
	}
}