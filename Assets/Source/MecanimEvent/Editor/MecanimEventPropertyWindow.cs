using UnityEngine;
using UnityEditor;
using System.Collections;

public class MecanimEventPropertyWindow : EditorWindow
{
	MecanimEventBase m_MecanimEventBase;

	GUIContent guiContentTitle = new GUIContent("Event Property");
	void OnEnable()
	{
		titleContent = guiContentTitle;
		minSize = new Vector2(400, 400);
	}

	public void SetMecanimEvent(MecanimEventBase eventBase)
	{
		if (m_MecanimEventBase != eventBase)
		{
			m_MecanimEventBase = eventBase;
			Repaint();
		}
	}

	void OnGUI()
	{
		if (m_MecanimEventBase == null) return;

		GUILayout.BeginVertical("box");
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.green;
			string szDesc = string.Format("{0} Property", m_MecanimEventBase.GetType().ToString());
			EditorGUILayout.LabelField(szDesc, EditorStyles.textField, GUILayout.Width(200));

			GUI.color = (Color.gray + Color.white) * 0.5f;
			if (m_MecanimEventBase.RangeSignal)
			{
				EditorGUILayout.LabelField("Event Start Time :", m_MecanimEventBase.StartTime.ToString(), EditorStyles.textField);
				EditorGUILayout.LabelField("Event End Time :", m_MecanimEventBase.EndTime.ToString(), EditorStyles.textField);
			}
			else
			{
				EditorGUILayout.LabelField("Event Time :", m_MecanimEventBase.StartTime.ToString(), EditorStyles.textField);
			}
			GUI.color = defaultColor;
		}
		GUILayout.EndVertical();

		m_MecanimEventBase.OnGUI_PropertyWindow();
	}
}
