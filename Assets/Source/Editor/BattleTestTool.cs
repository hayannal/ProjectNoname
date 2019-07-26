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
		OnGUI_Stage();
		OnGUI_Monster();
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

	int playChapter = 1;
	int playStage = 1;
	void OnGUI_Stage()
	{
		GUILayout.BeginVertical("box");
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.cyan;
			string szDesc = string.Format("Stage");
			EditorGUILayout.LabelField(szDesc, EditorStyles.textField);
			GUI.color = defaultColor;

			playChapter = EditorGUILayout.IntField("Chapter :", playChapter);
			playStage = EditorGUILayout.IntField("Stage :", playStage);
		}
		GUILayout.EndVertical();
	}

	GameObject monsterPrefab;
	GameObject monsterInstance;
	GameObject prevMonsterInstance;
	Animator monsterAnimator;
	AnimatorStateMachine targetStateMachine;
	AnimatorState loopTargetState;
	bool loopMonsterState = false;
	float loopStateDelay = 1.0f;
	float _nextLoopActionTime = 0.0f;
	bool useMonsterAI = false;
	PlayerAI _monsterAI = null;
	void OnGUI_Monster()
	{
		GUILayout.BeginVertical("box");
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.cyan;
			string szDesc = string.Format("Monster");
			EditorGUILayout.LabelField(szDesc, EditorStyles.textField);
			GUI.color = defaultColor;

			monsterPrefab = (GameObject)EditorGUILayout.ObjectField("Monster Prefab :", monsterPrefab, typeof(GameObject), false);
			if (GUILayout.Button("Spawn"))
			{
				StageTableData currentStageTableData = TableDataManager.instance.FindStageTableData(playChapter, playStage);
				if (currentStageTableData != null)
				{
					StageManager.instance.currentMonstrStandardHp = currentStageTableData.standardHp;
					StageManager.instance.currentMonstrStandardAtk = currentStageTableData.standardAtk;
					StageManager.instance.currentMonstrStandardDef = currentStageTableData.standardDef;
				}
				monsterInstance = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, Vector3.forward, Quaternion.identity);
				return;
			}
			if (monsterInstance != null)
			{
				if (!monsterInstance.activeSelf)
				{
					loopTargetState = null;
					targetStateMachine = null;
					monsterAnimator = null;
					monsterInstance = null;
				}

				GUILayout.BeginVertical("box");
				{
					GUI.color = Color.gray;
					EditorGUILayout.LabelField("Current Spawned Monster", EditorStyles.textField);
					GUI.color = defaultColor;
					monsterInstance = (GameObject)EditorGUILayout.ObjectField("Instance :", monsterInstance, typeof(GameObject), true);

					if (prevMonsterInstance != monsterInstance)
					{
						if (monsterInstance != null)
						{
							// Load State Machine
							monsterAnimator = monsterInstance.GetComponentInChildren<Animator>();
							AnimatorController ac = monsterAnimator.runtimeAnimatorController as AnimatorController;
							if (ac != null)
								targetStateMachine = ac.layers[0].stateMachine;

							// Load Attack Delay
							MonsterActor monsterActor = monsterInstance.GetComponent<MonsterActor>();
							if (monsterActor != null)
								loopStateDelay = monsterActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.AttackDelay);
						}
						else
						{
							targetStateMachine = null;
						}
						useMonsterAI = false;

						prevMonsterInstance = monsterInstance;
					}

					if (useMonsterAI == false)
					{
						if (targetStateMachine != null)
						{
							loopMonsterState = EditorGUILayout.Toggle("Toggle Loop:", loopMonsterState);
							if (loopMonsterState)
								loopStateDelay = EditorGUILayout.FloatField("Attack Delay :", loopStateDelay);

							if (loopMonsterState == false)
								loopTargetState = null;

							for (int i = 0; i < targetStateMachine.states.Length; ++i)
							{
								if (targetStateMachine.states[i].state.name == "Idle" || targetStateMachine.states[i].state.name == "Die")
									continue;

								GUI.color = Color.white;
								if (loopMonsterState && loopTargetState == targetStateMachine.states[i].state) GUI.color = Color.gray;
								if (GUILayout.Button(targetStateMachine.states[i].state.name))
								{
									if (loopMonsterState)
									{
										if (loopTargetState == targetStateMachine.states[i].state)
											loopTargetState = null;
										else
										{
											loopTargetState = targetStateMachine.states[i].state;
											monsterAnimator.CrossFade(loopTargetState.name, 0.05f);
											_nextLoopActionTime = Time.time + loopStateDelay;
										}
									}
									else
										monsterAnimator.CrossFade(targetStateMachine.states[i].state.name, 0.05f);
								}
								GUI.color = defaultColor;
							}
						}
					}
					else
					{
						// ai?
					}

					if (monsterInstance != null)
					{
						useMonsterAI = EditorGUILayout.Toggle("Toggle Monster AI :", useMonsterAI);
						if (_monsterAI == null)
							_monsterAI = monsterInstance.GetComponent<PlayerAI>();
						if (_monsterAI != null)
							_monsterAI.enabled = useMonsterAI;
					}
				}
				GUILayout.EndVertical();
			}
		}
		GUILayout.EndVertical();

		if (useMonsterAI == false && loopTargetState != null && monsterInstance != null && targetStateMachine != null && monsterAnimator != null && loopMonsterState && loopStateDelay > 0.0f)
		{
			if (Time.time > _nextLoopActionTime)
			{
				_nextLoopActionTime = Time.time + loopStateDelay;
				monsterAnimator.CrossFade(loopTargetState.name, 0.05f);
			}
			Repaint();
		}
	}
}
