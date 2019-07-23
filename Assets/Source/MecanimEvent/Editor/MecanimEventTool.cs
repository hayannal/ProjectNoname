using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;

public class MecanimEventTool : EditorWindow
{
	private AnimatorController m_AC;					// root animator controller
	private AnimatorStateMachine m_TargetStateMachine;	// each layer control
	private AnimatorState m_TargetState;				// state

	private GameObject m_OrigMeshObject;
	private GameObject m_ToolGameObject;						// tool object
	private Animator m_ToolAnimator;
	private const string m_szToolAnimatorControllerPathname = "Assets/_tempAnimationController.controller";
	private AnimatorController m_ToolAnimatorController;

	private MecanimEventPropertyWindow m_PropertyWindow;
	private MecanimEventGizmos m_Gizmos;

	private bool m_bPlay = false;


	[MenuItem("Window/Open Mecanim Editor Window _F4")]
	static void Init()
	{
		EditorWindow.GetWindow<MecanimEventTool> ();
		MecanimEventBase.s_bDisableMecanimEvent = true;
	}

	GUIContent guiContentTitle = new GUIContent("Event Tool");
	void OnEnable()
	{
		MecanimEventBase.s_bDisableMecanimEvent = true;
		titleContent = guiContentTitle;
		minSize = new Vector2(1000, 400);
	}

	void OnDisable()
	{
		MecanimEventBase.s_bDisableMecanimEvent = false;
		if (m_ToolGameObject != null)
			GameObject.DestroyImmediate(m_ToolGameObject);
		if (m_ToolAnimatorController != null)
			AssetDatabase.DeleteAsset(m_szToolAnimatorControllerPathname);
		if (m_PropertyWindow != null)
			m_PropertyWindow.Close();
	}

	Color m_DefaultToolColor = new Color(0.8f, 0.8f, 0.8f);
	Color m_DefaultToolBackgroundColor = Color.white;

	bool _selectedController = false;
	bool _selectedMeshData = false;
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

		GUILayout.BeginHorizontal();
		{
			OnGUI_SelectController();
			OnGUI_MeshData();
			OnGUI_SaveButton();
		}
		GUILayout.EndHorizontal();

		if (_selectedController && _selectedMeshData) {

			GUILayout.BeginHorizontal();
			{
				OnGUI_DrawLayerPanel();
				OnGUI_DrawStatePanel();

				GUI.color = Color.gray;
				GUILayout.Space(2);

				GUILayout.BeginVertical("Box", GUILayout.Width(4));
				GUILayout.FlexibleSpace();
				GUILayout.EndVertical();

				GUILayout.Space(2);
				GUI.color = m_DefaultToolColor;

				OnGUI_DrawEventPlayPanel();
			}
			GUILayout.EndHorizontal();
		}
	}

	void OnGUI_SelectController()
	{
		GUILayout.BeginVertical("box", GUILayout.Width(300));
		{
			GUI.color = Color.cyan;
			EditorGUILayout.LabelField("Select Controller", EditorStyles.toolbarButton);
			GUI.color = m_DefaultToolColor;

			AnimatorController oldController = m_AC;
			m_AC = EditorGUILayout.ObjectField (m_AC, typeof(AnimatorController), false) as AnimatorController;
			_selectedController = (m_AC != null);

			if (m_AC != null && m_AC != oldController)
				_loadLastMeshData = true;
		}
		GUILayout.EndVertical();
	}

	bool _loadLastMeshData;
	void OnGUI_MeshData()
	{
		if (_selectedController)
		{
			GameObject oldGameObject = m_OrigMeshObject;

			LoadLastMeshData();

			GUILayout.Space(14);
			GUILayout.BeginVertical("box", GUILayout.Width(300));
			GUI.backgroundColor = (Color.gray + Color.white) * 0.5f;
			EditorGUILayout.LabelField("Select Mesh Data :", EditorStyles.toolbarButton);
			GUI.backgroundColor = m_DefaultToolBackgroundColor;
			m_OrigMeshObject = EditorGUILayout.ObjectField (m_OrigMeshObject, typeof(GameObject), false) as GameObject;
			GUILayout.EndVertical();

			if (m_OrigMeshObject != null)
			{
				if (m_OrigMeshObject.GetComponentInChildren<Animator>() == null)
					m_OrigMeshObject = null;
			}
			_selectedMeshData = (m_OrigMeshObject != null);

			if (m_OrigMeshObject != oldGameObject)
			{
				if (m_ToolGameObject != null)
					GameObject.DestroyImmediate(m_ToolGameObject);
				if (m_ToolAnimatorController != null)
					AssetDatabase.DeleteAsset(m_szToolAnimatorControllerPathname);

				if (m_OrigMeshObject != null)
				{
					m_ToolGameObject = Instantiate(m_OrigMeshObject) as GameObject;
					m_ToolAnimator = m_ToolGameObject.GetComponentInChildren<Animator>();

					string szAC = AssetDatabase.GetAssetPath(m_AC);
					if (AssetDatabase.CopyAsset(szAC, m_szToolAnimatorControllerPathname))
					{
						AssetDatabase.ImportAsset(m_szToolAnimatorControllerPathname, ImportAssetOptions.Default);
						m_ToolAnimatorController = (AnimatorController)AssetDatabase.LoadAssetAtPath(m_szToolAnimatorControllerPathname, typeof(AnimatorController));
						AnimatorController.SetAnimatorController(m_ToolAnimator, m_ToolAnimatorController);
					}

					/*
					string szAC = AssetDatabase.GetAssetPath(m_AC);
					m_ToolAnimatorController = (AnimatorController)AssetDatabase.LoadAssetAtPath(szAC, typeof(AnimatorController));
					//m_ToolAnimatorController = AnimatorController.CreateAnimatorControllerAtPath(szAC);
					//m_ToolAnimatorController = new AnimatorController();
					//m_ToolAnimatorController.AddLayer("base layer");
					//m_ToolAnimatorController.layers[0].stateMachine.AddState("editing state");
					AnimatorController.SetAnimatorController(m_ToolAnimator, m_ToolAnimatorController);
					*/
					/*
					//m_ToolAnimatorController = new AnimatorController();
					//m_ToolAnimatorController.AddLayer("base layer");
					//m_ToolAnimatorController.layers[0].stateMachine.AddState("editing state");
					AnimatorController.SetAnimatorController(m_ToolAnimator, m_AC);
					*/
					m_ToolAnimator.speed = 0.0f;
					SaveLastMeshData();

					m_Gizmos = m_ToolGameObject.GetComponent<MecanimEventGizmos>();
					if (m_Gizmos == null) m_Gizmos = m_ToolGameObject.AddComponent<MecanimEventGizmos>();
					m_Gizmos.SetMecanimEventTransform(m_ToolGameObject.transform);
				}
			}
		}
	}

	void LoadLastMeshData()
	{
		if (!_loadLastMeshData) return;

		string szAC = AssetDatabase.GetAssetPath(m_AC);
		string szKey = string.Format("_MecanimEventTool_ToolData_{0}", szAC);
		if (PlayerPrefs.HasKey(szKey))
		{
			string szValue = PlayerPrefs.GetString(szKey);
			m_OrigMeshObject = (GameObject)AssetDatabase.LoadAssetAtPath(szValue, typeof(GameObject));
		}
		else
		{
			m_OrigMeshObject = null;
		}
		_loadLastMeshData = false;
	}

	void SaveLastMeshData()
	{
		string szAC = AssetDatabase.GetAssetPath(m_AC);
		string szMeshData = AssetDatabase.GetAssetPath(m_OrigMeshObject);

		string szKey = string.Format("_MecanimEventTool_ToolData_{0}", szAC);
		PlayerPrefs.SetString(szKey, szMeshData);
	}

	void OnGUI_SaveButton()
	{
		if (_selectedController && _selectedMeshData)
		{
			GUILayout.Space(60);
			GUI.color = Color.cyan;
			if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(40)))
			{
				//EditorUtility.CopySerialized(m_ToolAnimatorController, m_AC);

				for (int i = 0; i < m_AC.layers.Length; ++i)
				{
					for (int j = 0; j < m_AC.layers[i].stateMachine.states.Length; ++j)
					{
						//EditorUtility.CopySerialized(m_ToolAnimatorController.layers[i].stateMachine.states[j].state,
						//                             m_AC.layers[i].stateMachine.states[j].state);

						//m_AC.layers[i].stateMachine.states[j].state.behaviours = m_ToolAnimatorController.layers[i].stateMachine.states[j].state.behaviours;

						for (int k = 0; k < m_AC.layers[i].stateMachine.states[j].state.behaviours.Length; ++k)
						{
							Object.DestroyImmediate(m_AC.layers[i].stateMachine.states[j].state.behaviours[k], true);
						}
						for (int k = 0; k < m_ToolAnimatorController.layers[i].stateMachine.states[j].state.behaviours.Length; ++k)
						{
							//StateMachineBehaviour smb = m_ToolAnimatorController.layers[i].stateMachine.states[j].state.behaviours[k];
							//m_AC.layers[i].stateMachine.states[j].state.AddStateMachineBehaviour< smb.GetType( ) >();

							MecanimEventBase eventBase = m_ToolAnimatorController.layers[i].stateMachine.states[j].state.behaviours[k] as MecanimEventBase;
							if (eventBase == null) continue;
							MecanimEventBase copyEventBase = MecanimEventCustomCreator.CreateMecanimEvent(m_AC.layers[i].stateMachine.states[j].state, MecanimEventCustomCreator.GetMecanimEventType(eventBase));
							EditorUtility.CopySerialized(m_ToolAnimatorController.layers[i].stateMachine.states[j].state.behaviours[k], copyEventBase);
						}
					}

					for (int j = 0; j < m_AC.layers[i].stateMachine.stateMachines.Length; ++j)
					{
						for (int k = 0; k < m_AC.layers[i].stateMachine.stateMachines[j].stateMachine.states.Length; ++k)
						{
							for (int l = 0; l < m_AC.layers[i].stateMachine.stateMachines[j].stateMachine.states[k].state.behaviours.Length; ++l)
							{
								Object.DestroyImmediate(m_AC.layers[i].stateMachine.stateMachines[j].stateMachine.states[k].state.behaviours[l], true);
							}
							for (int l = 0; l < m_ToolAnimatorController.layers[i].stateMachine.stateMachines[j].stateMachine.states[k].state.behaviours.Length; ++l)
							{
								MecanimEventBase eventBase = m_ToolAnimatorController.layers[i].stateMachine.stateMachines[j].stateMachine.states[k].state.behaviours[l] as MecanimEventBase;
								if (eventBase == null) continue;
								MecanimEventBase copyEventBase = MecanimEventCustomCreator.CreateMecanimEvent(m_AC.layers[i].stateMachine.stateMachines[j].stateMachine.states[k].state, MecanimEventCustomCreator.GetMecanimEventType(eventBase));
								EditorUtility.CopySerialized(m_ToolAnimatorController.layers[i].stateMachine.stateMachines[j].stateMachine.states[k].state.behaviours[l], copyEventBase);
							}
						}
					}
				}
				EditorUtility.SetDirty(m_AC);
				AssetDatabase.SaveAssets();
			}
			GUI.color = m_DefaultToolColor;
		}
	}

	Vector2 _layerPanelScrollPos;
	int _selectedLayer = 0;
	AnimatorControllerLayer[] _layers;
	void OnGUI_DrawLayerPanel()
	{
		GUILayout.BeginVertical(GUILayout.Width(150));

		int layerCount = m_ToolAnimatorController.layers.Length;	
		GUILayout.Label(layerCount + " layer(s)", EditorStyles.helpBox);
		
		if (Event.current.type == EventType.Layout || _layers == null) {
			_layers = m_ToolAnimatorController.layers;
		}
		
		GUILayout.BeginVertical("Box");
		_layerPanelScrollPos = GUILayout.BeginScrollView(_layerPanelScrollPos);
		
		string[] layerNames = new string[layerCount];
		
		for (int layer = 0; layer < layerCount; layer++) {
			layerNames[layer] = "[" + layer.ToString() + "]" + _layers[layer].name;
		}

		GUI.color = Color.white;
		_selectedLayer = GUILayout.SelectionGrid(_selectedLayer, layerNames, 1);
		GUI.color = m_DefaultToolColor;
		
		if (_selectedLayer >= 0 && _selectedLayer < layerCount) {
			
			if (_layers[_selectedLayer].syncedLayerIndex != -1)
			{
				m_TargetStateMachine = _layers[_layers[_selectedLayer].syncedLayerIndex].stateMachine;
			}
			else
			{
				m_TargetStateMachine = _layers[_selectedLayer].stateMachine;
			}
		}
		else {
			m_TargetStateMachine = null;
			m_TargetState = null;
		}
		
		GUILayout.EndScrollView();
		GUILayout.EndVertical();

		GUILayout.Space(5);
		
		GUILayout.EndVertical();
	}

	Vector2 _statePanelScrollPos;
	void OnGUI_DrawStatePanel() {
		
		GUILayout.BeginVertical(GUILayout.Width(150));

		if (m_TargetStateMachine != null) {

			AnimatorState oldAnimatorState = m_TargetState;
			GUILayout.Label("Select state", EditorStyles.helpBox);
			
			GUILayout.BeginVertical("Box");
			_statePanelScrollPos = GUILayout.BeginScrollView(_statePanelScrollPos);

			// base states
			for (int i = 0; i < m_TargetStateMachine.states.Length; ++i)
			{
				GUI.color = Color.white;
				if (m_TargetState == m_TargetStateMachine.states[i].state) GUI.color = Color.gray;
				if (GUILayout.Button(m_TargetStateMachine.states[i].state.name))
					m_TargetState = m_TargetStateMachine.states[i].state;

				// default setting
				if (i == 0 && m_TargetState == null)
					m_TargetState = m_TargetStateMachine.states[i].state;
			}

			// sub stateMachine states
			for (int i = 0; i < m_TargetStateMachine.stateMachines.Length; ++i)
			{
				GUILayout.Label("▼" + m_TargetStateMachine.stateMachines[i].stateMachine.name);
				for (int j = 0; j < m_TargetStateMachine.stateMachines[i].stateMachine.states.Length; ++j)
				{
					GUI.color = Color.white;
					if (m_TargetState == m_TargetStateMachine.stateMachines[i].stateMachine.states[j].state) GUI.color = Color.gray;
					if (GUILayout.Button(m_TargetStateMachine.stateMachines[i].stateMachine.states[j].state.name))
						m_TargetState = m_TargetStateMachine.stateMachines[i].stateMachine.states[j].state;
				}
			}
			GUI.color = m_DefaultToolColor;

			if (m_TargetState != oldAnimatorState && m_ToolAnimator != null)
			{
				AnimatorController.SetAnimatorController(m_ToolAnimator, m_ToolAnimatorController);
				m_ToolAnimator.speed = 0.0f;
				_playSliderValue = 0.0f;
				_forcePlayAnimator = true;
				_selectedEventBase = null;
			}

			/*
			var layer = animatorControllerForEditor_.GetLayer(layerIndex_);
			var motion = editor_PlayStates_[i].GetMotion(layer);
			
			State editorState = null;
			if (Editor_GetState(i, out editorState))
			{
				var blendTree = motion as BlendTree;
				if (blendTree != null)
				{
					var motion2 = blendTree.GetMotion(0);
					editorState.SetAnimationClip(motion2 as AnimationClip);
				}
				else
				{
					editorState.SetAnimationClip(motion as AnimationClip);
				}
			}
			*/
			
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			
		}
		else {
			
			GUILayout.Label("No state machine available.");
		}

		GUILayout.Space(5);
		
		GUILayout.EndVertical();
	}

	void OnGUI_DrawEventPlayPanel() {
		
		GUILayout.BeginVertical();

		if (m_TargetState != null && m_ToolAnimator != null) {
			OnGUI_DrawControlPanel();
			OnGUI_DrawPlayPanel();

			GUI.color = GUI.backgroundColor = Color.gray;
			GUILayout.BeginHorizontal("Box", GUILayout.Height(0));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUI.color = m_DefaultToolColor;
			GUI.backgroundColor = m_DefaultToolBackgroundColor;

			OnGUI_DrawEventPanel();
			OnGUI_DrawEventPropertyWindow();
		}
		else {
			GUILayout.Label("Select model");
		}

		GUILayout.EndVertical();
	}

	eMecanimEventType _eEventType = eMecanimEventType.State;
	void OnGUI_DrawControlPanel()
	{
		GUI.color = Color.white;
		GUILayout.BeginHorizontal("box");
		{
			GUI.backgroundColor = Color.yellow;
			_eEventType = (eMecanimEventType)EditorGUILayout.EnumPopup(_eEventType, EditorStyles.toolbarPopup, GUILayout.Width(90));
			GUI.backgroundColor = m_DefaultToolBackgroundColor;

			if (GUILayout.Button("Add", GUILayout.Width(100), GUILayout.Height(24)))
			{
				_selectedEventBase = MecanimEventCustomCreator.CreateMecanimEvent(m_TargetState, _eEventType);
				_selectedEventBase.StartTime = _playSliderValue;
			}
			if (GUILayout.Button("Del", GUILayout.Width(100), GUILayout.Height(24)))
			{
				if (_selectedEventBase != null)
				{
					StateMachineBehaviour[] behaviours = m_TargetState.behaviours;
					for (int i = 0; i < behaviours.Length; ++i)
					{
						if (behaviours[i] == _selectedEventBase)
						{
							Object.DestroyImmediate(behaviours[i], true);
							_selectedEventBase = null;
							break;
						}
					}
				}
			}	
			GUILayout.FlexibleSpace();
		}
		GUILayout.EndHorizontal();
		GUI.color = m_DefaultToolColor;
	}

	float _playSliderValue = 0.0f;
	bool _forcePlayAnimator = false;
	void OnGUI_DrawPlayPanel()
	{
		AnimatorStateInfo asi = m_ToolAnimator.GetCurrentAnimatorStateInfo(_selectedLayer);
		AnimatorClipInfo[] acis = m_ToolAnimator.GetCurrentAnimatorClipInfo(_selectedLayer);
		float clipLength = 0.0f;
		if (acis.Length > 0 && acis[0].clip != null) clipLength = acis[0].clip.length;
		GUILayout.BeginHorizontal();
		{
			GUI.color = Color.green;
			GUILayout.Space(110);
			if (GUILayout.Button (m_bPlay?"Pause":"Play", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				m_bPlay = !m_bPlay;
				MecanimEventBase.s_bDisableMecanimEvent = !m_bPlay;
				MecanimEventBase.s_bForceCallUpdate = m_bPlay;
			}
			GUI.color = Color.white;
			GUILayout.Space(4);
			if (GUILayout.Button("<", EditorStyles.toolbarButton))
			{
			}
			GUILayout.Space(4);
			if (GUILayout.Button(">", EditorStyles.toolbarButton))
			{
			}
			GUI.color = m_DefaultToolColor;

			float asiCurrentTime = asi.normalizedTime * clipLength;
			GUI.backgroundColor = Color.yellow;
			GUILayout.FlexibleSpace();
			string szPlayTime = string.Format("{0:0.00}s / {1:0.00}s ({2:000.0}%)", asiCurrentTime, clipLength, _playSliderValue * 100.0f);
			GUILayout.Label (szPlayTime, EditorStyles.textField, GUILayout.Width(140));
			GUI.backgroundColor = m_DefaultToolBackgroundColor;

			if (m_bPlay)
			{
				asiCurrentTime += Time.deltaTime;
				if (asiCurrentTime >= clipLength) asiCurrentTime -= clipLength;
				_playSliderValue = asiCurrentTime / clipLength;
				Repaint();

				// force update for tool play
				for (int i = 0; i < m_TargetState.behaviours.Length; ++i)
				{
					if (m_TargetState.behaviours[i] is MecanimEventBase)
						m_TargetState.behaviours[i].OnStateUpdate(m_ToolAnimator, asi, _selectedLayer);
				}
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		{
			GUILayout.Label("   - Timeline -", EditorStyles.textArea, GUILayout.Width(_timelineItemNameTextWitdh+4));
			GUI.color = Color.white;
			_playSliderValue = GUILayout.HorizontalSlider(_playSliderValue, 0.0f, 1.0f, EditorStyles.textField, EditorStyles.objectFieldThumb);
			if (asi.normalizedTime != _playSliderValue || _forcePlayAnimator)
			{
				m_ToolAnimator.Play (m_TargetState.name, 0, _playSliderValue);
				_forcePlayAnimator = false;
			}
			GUI.color = Color.gray;
			GUILayout.Label("", EditorStyles.textArea, GUILayout.Width(_timelineItemTimeTextWitdh+4));
			GUI.color = m_DefaultToolColor;
		}
		GUILayout.EndHorizontal();
	}

	float _timelineItemNameTextWitdh = 100.0f;
	float _timelineItemTimeTextWitdh = 70.0f;
	int _EventCount = 0;
	MecanimEventBase _selectedEventBase = null;
	Vector2 _eventPanelScrollPos;
	void OnGUI_DrawEventPanel()
	{
		_EventCount = 0;
		StateMachineBehaviour[] behaviours = m_TargetState.behaviours;
		for (int i = 0; i < behaviours.Length; ++i)
		{
			if (behaviours[i] is MecanimEventBase)
				_EventCount++;
		}
		if (_EventCount == 0)
		{
			GUILayout.Label("No Mecanim Event!");
			return;
		}

		_eventPanelScrollPos = GUILayout.BeginScrollView(_eventPanelScrollPos);
		for (int i = 0; i < behaviours.Length; ++i)
		{
			if (behaviours[i] is MecanimEventBase == false)
				continue;

			MecanimEventBase eventBase = behaviours[i] as MecanimEventBase;

			if (_selectedEventBase == eventBase) GUI.color = (Color.yellow + Color.white + Color.white + Color.white) * 0.25f;
			GUILayout.BeginHorizontal("Box");
			{
				if (GUILayout.Button(eventBase.GetType().ToString(), EditorStyles.toolbarButton, GUILayout.Width(_timelineItemNameTextWitdh)))
				{
					_selectedEventBase = eventBase;
				}
				string szTimeText = "";
				if (eventBase.RangeSignal)
				{
					float oldStartTime = eventBase.StartTime;
					float oldEndTime = eventBase.EndTime;
					EditorGUILayout.MinMaxSlider(ref eventBase.StartTime, ref eventBase.EndTime, 0.0f, 1.0f);
					szTimeText = string.Format("{0:0.00} / {1:0.00}", eventBase.StartTime, eventBase.EndTime);
					if (eventBase.StartTime != oldStartTime || eventBase.EndTime != oldEndTime)
					{
						_selectedEventBase = eventBase;
						if (m_PropertyWindow != null) m_PropertyWindow.Repaint();
					}
				}
				else
				{
					float oldStartTime = eventBase.StartTime;
					eventBase.StartTime = GUILayout.HorizontalSlider(eventBase.StartTime, 0.0f, 1.0f);
					szTimeText = string.Format("{0:0.00} / {1:0.00}", eventBase.StartTime, 1.0f);
					if (eventBase.StartTime != oldStartTime)
					{
						_selectedEventBase = eventBase;
						if (m_PropertyWindow != null) m_PropertyWindow.Repaint();
					}
				}
				if (GUILayout.Button(szTimeText, EditorStyles.textArea, GUILayout.Width(_timelineItemTimeTextWitdh)))
				{
					_selectedEventBase = eventBase;
				}
			}
			GUILayout.EndHorizontal();
			GUI.color = m_DefaultToolColor;
		}

		GUILayout.EndScrollView();
	}

	void OnGUI_DrawEventPropertyWindow()
	{
		if (_selectedEventBase != null && m_PropertyWindow == null)
			m_PropertyWindow = EditorWindow.GetWindow<MecanimEventPropertyWindow>();

		if (m_PropertyWindow != null)
			m_PropertyWindow.SetMecanimEvent(_selectedEventBase);

		m_Gizmos.SetMecanimEvent(_selectedEventBase);
	}
}
