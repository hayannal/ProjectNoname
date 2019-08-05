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
	private bool m_bLoop = true;


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
		_standbyLoadLastController = true;
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

		m_bPlay = false;
	}

	Color m_DefaultToolColor = new Color(0.8f, 0.8f, 0.8f);
	Color m_DefaultToolBackgroundColor = Color.white;

	bool _selectedController = false;
	bool _selectedMeshData = false;
	string _notificationMsg = "Runs in Play mode!";
	GUIContent _notification = new GUIContent("Runs in Play mode!");
	GUIStyle _centeredTextStyle = new GUIStyle();
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

		_centeredTextStyle.alignment = TextAnchor.MiddleCenter;

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

	bool _standbyLoadLastController = false;
	void OnGUI_SelectController()
	{
		GUILayout.BeginVertical("box", GUILayout.Width(300));
		{
			GUI.color = Color.cyan;
			EditorGUILayout.LabelField("Select Controller", EditorStyles.toolbarButton);
			GUI.color = m_DefaultToolColor;

			AnimatorController oldController = m_AC;
			LoadLastController();
			m_AC = EditorGUILayout.ObjectField (m_AC, typeof(AnimatorController), false) as AnimatorController;
			_selectedController = (m_AC != null);

			if (m_AC != null && m_AC != oldController)
			{
				SaveLastController();
				_standbyLoadLastMeshData = true;
			}
		}
		GUILayout.EndVertical();
	}

	bool _standbyLoadLastMeshData;
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

			if (m_OrigMeshObject != oldGameObject || m_ToolGameObject == null)
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

					PlayerAI playerAI = m_ToolGameObject.GetComponent<PlayerAI>();
					if (playerAI != null) playerAI.enabled = false;
					MonsterAI monsterAI = m_ToolGameObject.GetComponent<MonsterAI>();
					if (monsterAI != null) monsterAI.enabled = false;
				}
			}
		}
	}

	void LoadLastController()
	{
		if (!_standbyLoadLastController) return;

		string szKey = "_MecanimEventTool_LastController";
		if (PlayerPrefs.HasKey(szKey))
		{
			string szValue = PlayerPrefs.GetString(szKey);
			m_AC = (AnimatorController)AssetDatabase.LoadAssetAtPath(szValue, typeof(AnimatorController));
		}
		else
		{
			m_AC = null;
		}
		_standbyLoadLastController = false;
	}

	void SaveLastController()
	{
		string szValue = AssetDatabase.GetAssetPath(m_AC);
		PlayerPrefs.SetString("_MecanimEventTool_LastController", szValue);
	}

	void LoadLastMeshData()
	{
		if (!_standbyLoadLastMeshData) return;

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
		_standbyLoadLastMeshData = false;
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
						RecursiveCopy(m_AC.layers[i].stateMachine.stateMachines[j], m_ToolAnimatorController.layers[i].stateMachine.stateMachines[j]);

						// 안전할때까지 삭제하지 않는다.
						/*
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
						*/
					}
				}
				EditorUtility.SetDirty(m_AC);
				AssetDatabase.SaveAssets();
			}
			GUI.color = m_DefaultToolColor;
		}
	}

	void RecursiveCopy(ChildAnimatorStateMachine childAnimatorStateMachine, ChildAnimatorStateMachine toolChildAnimatorStateMachine)
	{
		for (int k = 0; k < childAnimatorStateMachine.stateMachine.states.Length; ++k)
		{
			for (int l = 0; l < childAnimatorStateMachine.stateMachine.states[k].state.behaviours.Length; ++l)
			{
				Object.DestroyImmediate(childAnimatorStateMachine.stateMachine.states[k].state.behaviours[l], true);
			}
			for (int l = 0; l < toolChildAnimatorStateMachine.stateMachine.states[k].state.behaviours.Length; ++l)
			{
				MecanimEventBase eventBase = toolChildAnimatorStateMachine.stateMachine.states[k].state.behaviours[l] as MecanimEventBase;
				if (eventBase == null) continue;
				MecanimEventBase copyEventBase = MecanimEventCustomCreator.CreateMecanimEvent(childAnimatorStateMachine.stateMachine.states[k].state, MecanimEventCustomCreator.GetMecanimEventType(eventBase));
				EditorUtility.CopySerialized(toolChildAnimatorStateMachine.stateMachine.states[k].state.behaviours[l], copyEventBase);
			}
		}

		for (int i = 0; i < childAnimatorStateMachine.stateMachine.stateMachines.Length; ++i)
			RecursiveCopy(childAnimatorStateMachine.stateMachine.stateMachines[i], toolChildAnimatorStateMachine.stateMachine.stateMachines[i]);
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
				RecursiveDraw(m_TargetStateMachine.stateMachines[i]);

			GUI.color = m_DefaultToolColor;

			if (m_TargetState != oldAnimatorState && m_ToolAnimator != null)
			{
				AnimatorController.SetAnimatorController(m_ToolAnimator, m_ToolAnimatorController);
				m_ToolAnimator.speed = 0.0f;
				_playSliderValue = 0.0f;
				_forcePlayAnimator = true;
				_selectedEventBase = null;
				m_bPlay = false;
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

	void RecursiveDraw(ChildAnimatorStateMachine childAnimatorStateMachine)
	{
		if (childAnimatorStateMachine.stateMachine.states.Length > 0)
		{
			GUI.color = Color.gray;
			GUILayout.Label(string.Format("<{0}>", childAnimatorStateMachine.stateMachine.name), _centeredTextStyle);
			for (int i = 0; i < childAnimatorStateMachine.stateMachine.states.Length; ++i)
			{
				GUI.color = Color.white;
				if (m_TargetState == childAnimatorStateMachine.stateMachine.states[i].state) GUI.color = Color.gray;
				if (GUILayout.Button(string.Format("ㄴ{0}", childAnimatorStateMachine.stateMachine.states[i].state.name)))
					m_TargetState = childAnimatorStateMachine.stateMachine.states[i].state;
			}
		}

		for (int i = 0; i < childAnimatorStateMachine.stateMachine.stateMachines.Length; ++i)
			RecursiveDraw(childAnimatorStateMachine.stateMachine.stateMachines[i]);
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
			GUILayout.Space(4);
			GUI.color = (_selectedEventBase != null) ? Color.white : Color.gray;
			if (GUILayout.Button("Dupl", GUILayout.Width(100), GUILayout.Height(24)))
			{
				StateMachineBehaviour[] behaviours = m_TargetState.behaviours;
				int findIndex = -1;
				for (int i = 0; i < behaviours.Length; ++i)
				{
					if (behaviours[i] == _selectedEventBase)
					{
						findIndex = i;
						break;
					}
				}
				if (findIndex != -1)
				{
					MecanimEventBase newEventBase = MecanimEventCustomCreator.CreateMecanimEvent(m_TargetState, MecanimEventCustomCreator.GetMecanimEventType(_selectedEventBase));
					EditorUtility.CopySerialized(_selectedEventBase, newEventBase);
				}
			}
			if (GUILayout.Button("Up", GUILayout.Width(100), GUILayout.Height(24)))
			{
				StateMachineBehaviour[] behaviours = m_TargetState.behaviours;
				int findIndex = -1;
				for (int i = 0; i < behaviours.Length; ++i)
				{
					if (behaviours[i] == _selectedEventBase)
					{
						findIndex = i;
						break;
					}
				}
				if (findIndex != -1 && findIndex != 0)
				{
					StateMachineBehaviour t = behaviours[findIndex - 1];
					behaviours[findIndex - 1] = _selectedEventBase;
					behaviours[findIndex] = t;
					m_TargetState.behaviours = behaviours;
				}
			}
			if (GUILayout.Button("Down", GUILayout.Width(100), GUILayout.Height(24)))
			{
				StateMachineBehaviour[] behaviours = m_TargetState.behaviours;
				int findIndex = -1;
				for (int i = 0; i < behaviours.Length; ++i)
				{
					if (behaviours[i] == _selectedEventBase)
					{
						findIndex = i;
						break;
					}
				}
				if (findIndex != -1 && findIndex != behaviours.Length - 1)
				{
					StateMachineBehaviour t = behaviours[findIndex + 1];
					behaviours[findIndex + 1] = _selectedEventBase;
					behaviours[findIndex] = t;
					m_TargetState.behaviours = behaviours;
				}
			}
			GUILayout.FlexibleSpace();
		}
		GUILayout.EndHorizontal();
		GUI.color = m_DefaultToolColor;
	}

	float _playSliderValue = 0.0f;
	bool _needSetPlaySlider = false;
	float _needSetPlaySliderValue = 0.0f;
	public float timelineSliderValue { get { return _playSliderValue; } set { _needSetPlaySlider = true; _needSetPlaySliderValue = value; } }
	bool _forcePlayAnimator = false;
	float _speedMultiplier = 1.0f;
	void OnGUI_DrawPlayPanel()
	{
		AnimatorStateInfo asi = m_ToolAnimator.GetCurrentAnimatorStateInfo(_selectedLayer);
		AnimatorClipInfo[] acis = m_ToolAnimator.GetCurrentAnimatorClipInfo(_selectedLayer);
		float clipLength = 0.0f;
		if (acis.Length > 0 && acis[0].clip != null) clipLength = acis[0].clip.length;
		float clipLengthWithSpeed = clipLength / asi.speed;
		GUILayout.BeginHorizontal();
		{
			GUI.color = Color.green;
			GUILayout.Space(50);
			bool needResetNormalizedTime = false;
			if (GUILayout.Button (m_bPlay?"Pause":"Play", EditorStyles.toolbarButton, GUILayout.Width(80)))
			{
				if (m_bPlay == false && m_bLoop == false && asi.normalizedTime == 1.0f)
					needResetNormalizedTime = true;

				m_bPlay = !m_bPlay;
				MecanimEventBase.s_bDisableMecanimEvent = !m_bPlay;
				MecanimEventBase.s_bForceCallUpdate = m_bPlay;
			}
			GUI.color = m_bLoop?Color.gray:Color.white;
			GUILayout.Space(4);
			if (GUILayout.Button("∞", EditorStyles.toolbarButton))
			{
				m_bLoop = !m_bLoop;
			}
			GUI.color = Color.white;
			GUILayout.Space(4);
			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100.0f;
			_speedMultiplier = EditorGUILayout.FloatField("Speed Multiplier", _speedMultiplier, GUILayout.Width(140));
			EditorGUIUtility.labelWidth = defaultLabelWidth;
			GUI.color = m_DefaultToolColor;

			float asiCurrentTime = asi.normalizedTime * clipLength;
			GUILayout.FlexibleSpace();
			GUI.backgroundColor = Color.white;
			GUILayout.Label(string.Format("Tool Speed : {0:0.00}", asi.speed), EditorStyles.textField, GUILayout.Width(110));
			GUI.backgroundColor = Color.yellow;
			string szPlayTime = string.Format("{0:0.00}s / {1:0.00}s ({2:000.0}%)", asiCurrentTime / asi.speed / _speedMultiplier, clipLength / asi.speed / _speedMultiplier, _playSliderValue * 100.0f);
			GUILayout.Label (szPlayTime, EditorStyles.textField, GUILayout.Width(140));
			GUI.backgroundColor = m_DefaultToolBackgroundColor;

			if (m_bPlay)
			{
				if (needResetNormalizedTime) asiCurrentTime = 0.0f;
				asiCurrentTime += Time.deltaTime * asi.speed * _speedMultiplier;
				if (asiCurrentTime >= clipLength)
				{
					if (m_bLoop)
						asiCurrentTime -= clipLength;
					else
					{
						asiCurrentTime = clipLength;
						m_bPlay = false;
					}
				}
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
			if (_needSetPlaySlider)
			{
				_playSliderValue = _needSetPlaySliderValue;
				_needSetPlaySliderValue = 0.0f;
				_needSetPlaySlider = false;
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
			m_PropertyWindow.SetMecanimEvent(this, _selectedEventBase);

		m_Gizmos.SetMecanimEvent(_selectedEventBase);
	}
}
