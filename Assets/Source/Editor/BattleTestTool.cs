using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections;
using System.Collections.Generic;
using ECM.Controllers;
using UnityEngine.SceneManagement;

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

		Reinitialize();
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
		OnGUI_Map();
	}

	bool _foldoutAffectorGroup = false;
	PlayerAI _playerAI = null;
	bool usePlayerAI = true;
	PlayerActor _playerActor = null;
	bool spFull = false;
	bool hpFull = false;
	string _affectorValueId;
	int _affectorValueLevel;
	string _actorStateId;
	string _levelPackId;
	void OnGUI_Player()
	{
		GUILayout.BeginVertical("box");
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.cyan;
			string szDesc = string.Format("Player");
			EditorGUILayout.LabelField(szDesc, EditorStyles.textField);
			GUI.color = defaultColor;

			if (_playerActor == null)
				_playerActor = GameObject.FindObjectOfType<PlayerActor>();
			if (_playerActor != null && _playerActor != BattleInstanceManager.instance.playerActor)
			{
				_playerActor = BattleInstanceManager.instance.playerActor;
				_playerAI = _playerActor.playerAI;
			}
			usePlayerAI = EditorGUILayout.Toggle("Toggle Player AI :", usePlayerAI);
			if (_playerAI == null)
				_playerAI = GameObject.FindObjectOfType<PlayerAI>();
			if (_playerAI != null)
				_playerAI.enabled = usePlayerAI;

			spFull = EditorGUILayout.Toggle("SP Full :", spFull);
			if (spFull && _playerActor != null && _playerActor.actorStatus.GetSPRatio() != 1.0f)
				_playerActor.actorStatus.AddSP(_playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxSp));
			hpFull = EditorGUILayout.Toggle("HP Full :", hpFull);
			if (hpFull && _playerActor != null && _playerActor.actorStatus.GetHPRatio() != 1.0f && _playerActor.actorStatus.IsDie() == false)
				_playerActor.actorStatus.AddHP(_playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MaxHp));

			_foldoutAffectorGroup = EditorGUILayout.Foldout(_foldoutAffectorGroup, _foldoutAffectorGroup ? "[Hide Affector Group]" : "[Show Affector Group]");
			if (_foldoutAffectorGroup)
			{
				_affectorValueId = EditorGUILayout.TextField("Affector Value Id :", _affectorValueId);
				_affectorValueLevel = EditorGUILayout.IntField("Affector Value Level :", _affectorValueLevel);
				if (GUILayout.Button("Apply Affector Value"))
				{
					HitParameter hitParameter = new HitParameter();
					hitParameter.statusBase = _playerActor.actorStatus.statusBase;
					SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _playerActor);
					hitParameter.statusStructForHitObject.skillLevel = _affectorValueLevel;
					_playerActor.affectorProcessor.ApplyAffectorValue(_affectorValueId, hitParameter);
				}
				_actorStateId = EditorGUILayout.TextField("Actor State Id :", _actorStateId);
				if (GUILayout.Button("Add Actor State"))
				{
					HitParameter hitParameter = new HitParameter();
					hitParameter.statusBase = _playerActor.actorStatus.statusBase;
					SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _playerActor);
					_playerActor.affectorProcessor.AddActorState(_actorStateId, hitParameter);
				}
				_levelPackId = EditorGUILayout.TextField("Level Pack Id :", _levelPackId);
				if (GUILayout.Button("Add Level Pack"))
				{
					_playerActor.skillProcessor.AddLevelPack(_levelPackId);
				}
			}
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

			if (GUILayout.Button("Toggle Chaos Mode"))
			{
				// 카오스 모드는 스테이지 진행 도중에 바꿀 수 없어서 이렇게 버튼으로 구현한다.
				PlayerData.instance.chaosMode ^= true;
				SceneManager.LoadScene(0);
				return;
			}

			playChapter = EditorGUILayout.IntField("Chapter :", playChapter);
			playStage = EditorGUILayout.IntField("Stage :", playStage);

			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Get Stage"))
				{
					playChapter = StageManager.instance.playChapter;
					playStage = StageManager.instance.playStage;
				}
				if (GUILayout.Button("Set Next Stage"))
				{
					StageManager.instance.playChapter = playChapter;
					StageManager.instance.playStage = playStage - 1;
					StageManager.instance.GetNextStageInfo();
				}
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();
	}

	void Reinitialize()
	{
		_nextLoopActionTime = 0.0f;
	}

	GameObject monsterPrefab;
	GameObject monsterInstance;
	GameObject prevMonsterInstance;
	bool lookAtMe = false;
	BaseCharacterController monsterBaseCharacterController;
	TargetingProcessor monsterTargetingProcessor;
	MonsterAI monsterAI;
	GameObject monsterTargetObject;
	Animator monsterAnimator;
	AnimatorStateMachine targetStateMachine;
	AnimatorState loopTargetState;
	bool loopMonsterState = false;
	float loopStateDelay = 1.0f;
	float _nextLoopActionTime = 0.0f;
	bool standbyLoadAttackDelay = false;
	bool useMonsterAI = false;
	GroupMonster groupMonster;
	bool useGroupMonsterAI = false;
	RailMonster railMonster;

	void OnGUI_Monster()
	{
		bool needRepaint = false;
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
				StageTableData stageTableData = BattleInstanceManager.instance.GetCachedStageTableData(playChapter, playStage, PlayerData.instance.chaosMode);
				if (stageTableData != null)
					StageManager.instance.currentStageTableData = stageTableData;
				GameObject newObject = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, Vector3.forward, Quaternion.identity);
				monsterInstance = null;
				groupMonster = null;
				railMonster = null;
				MonsterActor newMonsterActor = newObject.GetComponent<MonsterActor>();
				if (newMonsterActor != null)
				{
					monsterInstance = newObject;
				}
				GroupMonster newGroupMonster = newObject.GetComponent<GroupMonster>();
				if (newGroupMonster != null)
				{
					groupMonster = newGroupMonster;
					useGroupMonsterAI = false;
				}
				RailMonster newRailMonster = newObject.GetComponent<RailMonster>();
				if (newRailMonster != null)
				{
					monsterInstance = newRailMonster.monsterActor.gameObject;
					railMonster = newRailMonster;
				}
			}
			if (monsterInstance != null)
			{
				if (!monsterInstance.activeSelf)
				{
					monsterBaseCharacterController = null;
					monsterTargetingProcessor = null;
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
							// component
							monsterBaseCharacterController = monsterInstance.GetComponent<BaseCharacterController>();
							monsterTargetingProcessor = monsterInstance.GetComponent<TargetingProcessor>();
							monsterAI = monsterInstance.GetComponent<MonsterAI>();

							// Load State Machine
							monsterAnimator = monsterInstance.GetComponentInChildren<Animator>();
							AnimatorController ac = monsterAnimator.runtimeAnimatorController as AnimatorController;
							if (ac != null)
								targetStateMachine = ac.layers[0].stateMachine;

							// for load status
							standbyLoadAttackDelay = true;
						}
						else
						{
							targetStateMachine = null;
						}
						useMonsterAI = false;
						monsterAI.enabled = false;
						if (railMonster != null)
							railMonster.enabled = false;

						prevMonsterInstance = monsterInstance;
					}

					if (monsterInstance != null && standbyLoadAttackDelay)
					{
						// Load Attack Delay
						MonsterActor monsterActor = monsterInstance.GetComponent<MonsterActor>();
						if (monsterActor != null && monsterActor.actorStatus != null && monsterActor.actorStatus.statusBase != null)
						{
							loopStateDelay = monsterActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.AttackDelay);
							standbyLoadAttackDelay = false;
						}
					}

					if (useMonsterAI == false)
					{
						if (monsterInstance != null)
						{
							lookAtMe = EditorGUILayout.Toggle("Look At Me:", lookAtMe);
							if (lookAtMe && _playerAI != null)
							{
								monsterBaseCharacterController.RotateTowards(_playerAI.transform.position - monsterInstance.transform.position);
								needRepaint = true;
							}

							GUI.color = Color.white;
							if (GUILayout.Button("Find Target"))
								monsterTargetingProcessor.FindNearestTarget(Team.eTeamCheckFilter.Enemy, PlayerAI.FindTargetRange);
							GUI.color = defaultColor;

							if (monsterTargetingProcessor.GetTarget() != null)
							{
								monsterTargetObject = monsterTargetingProcessor.GetTarget().gameObject;
								monsterTargetObject = (GameObject)EditorGUILayout.ObjectField("Target :", monsterTargetObject, typeof(GameObject), true);
							}
						}

						if (targetStateMachine != null)
						{
							loopMonsterState = EditorGUILayout.Toggle("Toggle Loop Attack:", loopMonsterState);
							if (loopMonsterState)
								loopStateDelay = EditorGUILayout.FloatField("Attack Delay :", loopStateDelay);

							if (loopMonsterState == false)
								loopTargetState = null;

							for (int i = 0; i < targetStateMachine.states.Length; ++i)
							{
								if (targetStateMachine.states[i].state.name == "Idle" || targetStateMachine.states[i].state.name == "Move" || targetStateMachine.states[i].state.name == "Die")
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

							for (int i = 0; i < targetStateMachine.stateMachines.Length; ++i)
								RecursiveDraw(targetStateMachine.stateMachines[i]);

							GUI.color = defaultColor;
						}
					}
					else
					{
						// ai?
					}

					if (monsterInstance != null)
					{
						EditorGUI.BeginChangeCheck();
						useMonsterAI = EditorGUILayout.Toggle("Toggle Monster AI :", useMonsterAI);
						if (EditorGUI.EndChangeCheck())
						{
							if (monsterAI != null)
								monsterAI.enabled = useMonsterAI;
							if (railMonster != null)
								railMonster.enabled = useMonsterAI;
						}
					}
				}
				GUILayout.EndVertical();
			}
			if (groupMonster != null)
			{
				if (!groupMonster.gameObject.activeSelf)
				{
					groupMonster = null;
				}

				GUILayout.BeginVertical("box");
				{
					if (groupMonster != null)
					{
						useGroupMonsterAI = EditorGUILayout.Toggle("Toggle GroupMonster AI :", useGroupMonsterAI);
						for (int i = 0; i < groupMonster.listMonsterActor.Count; ++i)
							groupMonster.listMonsterActor[i].monsterAI.enabled = useGroupMonsterAI;
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
				if (monsterTargetingProcessor.GetComponent<Actor>().actorStatus.IsDie() == false)
					monsterAnimator.CrossFade(loopTargetState.name, 0.05f);
			}
			needRepaint = true;
		}
		if (needRepaint)
			Repaint();
	}

	void RecursiveDraw(ChildAnimatorStateMachine childAnimatorStateMachine)
	{
		if (childAnimatorStateMachine.stateMachine.states.Length > 0)
		{
			GUI.color = Color.white;
			GUI.enabled = !loopMonsterState;
			if (GUILayout.Button(string.Format("<{0}>", childAnimatorStateMachine.stateMachine.name)))
				monsterAnimator.SetTrigger(childAnimatorStateMachine.stateMachine.name);

			GUI.enabled = true;
			for (int i = 0; i < childAnimatorStateMachine.stateMachine.states.Length; ++i)
			{
				GUI.color = Color.white;
				if (loopMonsterState && loopTargetState == childAnimatorStateMachine.stateMachine.states[i].state) GUI.color = Color.gray;
				if (GUILayout.Button(string.Format("ㄴ{0}", childAnimatorStateMachine.stateMachine.states[i].state.name)))
				{
					if (loopMonsterState)
					{
						if (loopTargetState == childAnimatorStateMachine.stateMachine.states[i].state)
							loopTargetState = null;
						else
						{
							loopTargetState = childAnimatorStateMachine.stateMachine.states[i].state;
							monsterAnimator.CrossFade(loopTargetState.name, 0.05f);
							_nextLoopActionTime = Time.time + loopStateDelay;
						}
					}
					else
						monsterAnimator.CrossFade(childAnimatorStateMachine.stateMachine.states[i].state.name, 0.05f);
				}
			}
		}

		for (int i = 0; i < childAnimatorStateMachine.stateMachine.stateMachines.Length; ++i)
			RecursiveDraw(childAnimatorStateMachine.stateMachine.stateMachines[i]);
	}

	void OnGUI_Map()
	{
		GUILayout.BeginVertical("box");
		{
			Color defaultColor = GUI.color;
			GUI.color = Color.cyan;
			string szDesc = string.Format("Map");
			EditorGUILayout.LabelField(szDesc, EditorStyles.textField);
			GUI.color = defaultColor;

			if (GUILayout.Button("Force Rebake NavMesh"))
			{
				BattleInstanceManager.instance.BakeNavMesh();
			}
		}
		GUILayout.EndVertical();
	}
}
