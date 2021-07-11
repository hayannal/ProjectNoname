﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using DigitalRuby.ThunderAndLightning;
using PlayFab;
using CodeStage.AntiCheat.ObscuredTypes;

public class BattleInstanceManager : MonoBehaviour
{
	public static BattleInstanceManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("BattleInstanceManager")).AddComponent<BattleInstanceManager>();
			return _instance;
		}
	}
	static BattleInstanceManager _instance = null;

	#region Common
	public Collider targetColliderOfMonster { get; set; }
	public Actor targetOfMonster { get; set; }
	public Ground currentGround { get; set; }
	public Collider planeCollider { get; set; }
	#endregion


	#region EffectObject

	#endregion

	#region Object Pool
	Dictionary<GameObject, List<GameObject>> _dicInstancePool = new Dictionary<GameObject, List<GameObject>>();
	public GameObject GetCachedObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<GameObject> listCachedGameObject = null;
		if (_dicInstancePool.ContainsKey(prefab))
			listCachedGameObject = _dicInstancePool[prefab];
		else
		{
			listCachedGameObject = new List<GameObject>();
			_dicInstancePool.Add(prefab, listCachedGameObject);
		}

		for (int i = 0; i < listCachedGameObject.Count; ++i)
		{
			if (!listCachedGameObject[i].activeSelf)
			{
				listCachedGameObject[i].transform.parent = parentTransform;
				//listCachedGameObject[i].transform.position = position;
				//listCachedGameObject[i].transform.rotation = rotation;
				listCachedGameObject[i].transform.SetPositionAndRotation(position, rotation);
				listCachedGameObject[i].SetActive(true);
				return listCachedGameObject[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		listCachedGameObject.Add(newObject);
		return newObject;
	}

	public GameObject GetCachedObject(GameObject prefab, Transform parentTransform)
	{
		List<GameObject> listCachedGameObject = null;
		if (_dicInstancePool.ContainsKey(prefab))
			listCachedGameObject = _dicInstancePool[prefab];
		else
		{
			listCachedGameObject = new List<GameObject>();
			_dicInstancePool.Add(prefab, listCachedGameObject);
		}

		for (int i = 0; i < listCachedGameObject.Count; ++i)
		{
			if (!listCachedGameObject[i].activeSelf)
			{
				listCachedGameObject[i].transform.parent = parentTransform;
				listCachedGameObject[i].SetActive(true);
				return listCachedGameObject[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		listCachedGameObject.Add(newObject);
		return newObject;
	}

	public void DisableAllCachedObject()
	{
		Dictionary<GameObject, List<GameObject>>.Enumerator e = _dicInstancePool.GetEnumerator();
		while (e.MoveNext())
		{
			List<GameObject> listCachedGameObject = e.Current.Value;
			for (int i = 0; i < listCachedGameObject.Count; ++i)
				listCachedGameObject[i].SetActive(false);
		}
	}
	#endregion

	#region AffectorProcessor
	int _wallColliderLayer = 0;
	Dictionary<Collider, AffectorProcessor> _dicAffectorProcessorByCollider = new Dictionary<Collider, AffectorProcessor>();
	public AffectorProcessor GetAffectorProcessorFromCollider(Collider collider)
	{
		if (collider == null)
			return null;

		if (_wallColliderLayer == 0)
			_wallColliderLayer = LayerMask.NameToLayer("Team0WallCollider");
		if (collider.gameObject.layer == _wallColliderLayer)
			return null;

		if (_dicAffectorProcessorByCollider.ContainsKey(collider))
			return _dicAffectorProcessorByCollider[collider];

		AffectorProcessor affectorProcessor = collider.GetComponent<AffectorProcessor>();
		if (affectorProcessor == null)
			affectorProcessor = collider.GetComponentInParent<AffectorProcessor>();
		_dicAffectorProcessorByCollider.Add(collider, affectorProcessor);
		return affectorProcessor;
	}
	#endregion

	#region Team
	Dictionary<Collider, Team> _dicTeamByCollider = new Dictionary<Collider, Team>();
	public Team GetTeamFromCollider(Collider collider)
	{
		if (collider == null)
			return null;

		if (_dicTeamByCollider.ContainsKey(collider))
			return _dicTeamByCollider[collider];

		Team team = collider.GetComponent<Team>();
		if (team == null)
			team = collider.GetComponentInParent<Team>();
		_dicTeamByCollider.Add(collider, team);
		return team;
	}
	#endregion

	#region HitObject
	Dictionary<GameObject, List<HitObject>> _dicHitObjectInstancePool = new Dictionary<GameObject, List<HitObject>>();
	public HitObject GetCachedHitObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<HitObject> listCachedHitObject = null;
		if (_dicHitObjectInstancePool.ContainsKey(prefab))
			listCachedHitObject = _dicHitObjectInstancePool[prefab];
		else
		{
			listCachedHitObject = new List<HitObject>();
			_dicHitObjectInstancePool.Add(prefab, listCachedHitObject);
		}

		for (int i = 0; i < listCachedHitObject.Count; ++i)
		{
			if (!listCachedHitObject[i].gameObject.activeSelf)
			{
				listCachedHitObject[i].cachedTransform.parent = parentTransform;
				//listCachedHitObject[i].cachedTransform.position = position;
				//listCachedHitObject[i].cachedTransform.rotation = rotation;
				listCachedHitObject[i].cachedTransform.SetPositionAndRotation(position, rotation);
				listCachedHitObject[i].gameObject.SetActive(true);
				return listCachedHitObject[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		HitObject hitObject = newObject.GetComponent<HitObject>();
		if (hitObject == null) hitObject = newObject.AddComponent<HitObject>();
		listCachedHitObject.Add(hitObject);
		return hitObject;
	}

	Dictionary<Collider, HitObject> _dicHitObjectByCollider = new Dictionary<Collider, HitObject>();
	public HitObject GetHitObjectFromCollider(Collider collider)
	{
		if (collider == null)
			return null;

		if (_dicHitObjectByCollider.ContainsKey(collider))
			return _dicHitObjectByCollider[collider];

		//AffectorProcessor affectorProcessor = collider.GetComponent<AffectorProcessor>();
		//_dicAffectorProcessorByCollider.Add(collider, affectorProcessor);
		//return affectorProcessor;
		return null;
	}

	List<HitObject> _listInitializedHitObject = new List<HitObject>();
	public void OnInitializeHitObject(HitObject hitObject, Collider collider)
	{
		if (_listInitializedHitObject.Contains(hitObject) == false)
			_listInitializedHitObject.Add(hitObject);

		if (collider == null)
			return;

		if (_dicHitObjectByCollider.ContainsKey(collider) == false)
			_dicHitObjectByCollider.Add(collider, hitObject);
	}

	public void OnFinalizeHitObject(HitObject hitObject, Collider collider)
	{
		if (_readyForFinalizeAll == false)
			_listInitializedHitObject.Remove(hitObject);

		if (collider == null)
			return;

		_dicHitObjectByCollider.Remove(collider);
	}

	bool _readyForFinalizeAll = false;
	public void FinalizeAllHitObject()
	{
		_readyForFinalizeAll = true;
		for (int i = 0; i < _listInitializedHitObject.Count; ++i)
		{
			if (_listInitializedHitObject[i] == null)
				continue;

			// mine 말고는 activeSelf false이면서 들어있는 경우가 없을거라.. 체크하지 않는거로 해본다. 해도 상관은 없을듯
			//if (_listInitializedHitObject[i].gameObject != null && _listInitializedHitObject[i].gameObject.activeSelf == false)
			//	continue;
			_listInitializedHitObject[i].FinalizeHitObject(true);
		}
		_listInitializedHitObject.Clear();
		_readyForFinalizeAll = false;
	}

	List<HitObject> _listCachedEmptyHitObject = new List<HitObject>();
	public HitObject GetEmptyHitObject(Vector3 position, Quaternion rotation)
	{
		for (int i = 0; i < _listCachedEmptyHitObject.Count; ++i)
		{
			if (!_listCachedEmptyHitObject[i].gameObject.activeSelf)
			{
				//_listCachedEmptyHitObject[i].cachedTransform.position = position;
				//_listCachedEmptyHitObject[i].cachedTransform.rotation = rotation;
				_listCachedEmptyHitObject[i].cachedTransform.SetPositionAndRotation(position, rotation);
				//_listCachedEmptyHitObject[i].cachedTransform.localScale = localScale;
				_listCachedEmptyHitObject[i].gameObject.SetActive(true);
				return _listCachedEmptyHitObject[i];
			}
		}

		GameObject newObject = new GameObject();
		newObject.name = "EmptyHitObject";
		Transform newTransform = newObject.transform;
		//newTransform.position = position;
		//newTransform.rotation = rotation;
		newTransform.SetPositionAndRotation(position, rotation);
		//newTransform.localScale = localScale;
		HitObject hitObject = newObject.AddComponent<HitObject>();
		_listCachedEmptyHitObject.Add(hitObject);
		return hitObject;
	}
	#endregion

	#region HitObject Generator
	Dictionary<GameObject, List<ContinuousHitObjectGeneratorBase>> _dicContinuousHitObjectGeneratorePool = new Dictionary<GameObject, List<ContinuousHitObjectGeneratorBase>>();
	public ContinuousHitObjectGeneratorBase GetContinuousHitObjectGenerator(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<ContinuousHitObjectGeneratorBase> listCachedContinuousHitObjectGenerator = null;
		if (_dicContinuousHitObjectGeneratorePool.ContainsKey(prefab))
			listCachedContinuousHitObjectGenerator = _dicContinuousHitObjectGeneratorePool[prefab];
		else
		{
			listCachedContinuousHitObjectGenerator = new List<ContinuousHitObjectGeneratorBase>();
			_dicContinuousHitObjectGeneratorePool.Add(prefab, listCachedContinuousHitObjectGenerator);
		}

		for (int i = 0; i < listCachedContinuousHitObjectGenerator.Count; ++i)
		{
			if (!listCachedContinuousHitObjectGenerator[i].gameObject.activeSelf)
			{
				listCachedContinuousHitObjectGenerator[i].cachedTransform.parent = parentTransform;
				//listCachedContinuousHitObjectGenerator[i].cachedTransform.position = position;
				//listCachedContinuousHitObjectGenerator[i].cachedTransform.rotation = rotation;
				listCachedContinuousHitObjectGenerator[i].cachedTransform.SetPositionAndRotation(position, rotation);
				listCachedContinuousHitObjectGenerator[i].gameObject.SetActive(true);
				return listCachedContinuousHitObjectGenerator[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		ContinuousHitObjectGeneratorBase continuousHitObjectGeneratorBase = newObject.GetComponent<ContinuousHitObjectGeneratorBase>();
		listCachedContinuousHitObjectGenerator.Add(continuousHitObjectGeneratorBase);
		return continuousHitObjectGeneratorBase;
	}
	#endregion

	#region for EmptyObject
	List<Transform> _listCachedTransform = new List<Transform>();
	public Transform GetEmptyTransform(Vector3 position, Quaternion rotation)
	{
		for (int i = 0; i < _listCachedTransform.Count; ++i)
		{
			if (!_listCachedTransform[i].gameObject.activeSelf)
			{
				//_listCachedTransform[i].position = position;
				//_listCachedTransform[i].rotation = rotation;
				_listCachedTransform[i].SetPositionAndRotation(position, rotation);
				//_listCachedTransform[i].localScale = localScale;
				_listCachedTransform[i].gameObject.SetActive(true);
				return _listCachedTransform[i];
			}
		}

		GameObject newObject = new GameObject();
		newObject.name = "DuplicatedObject";
		Transform duplicatedTransform = newObject.transform;
		//duplicatedTransform.position = position;
		//duplicatedTransform.rotation = rotation;
		duplicatedTransform.SetPositionAndRotation(position, rotation);
		//duplicatedTransform.localScale = localScale;
		_listCachedTransform.Add(duplicatedTransform);
		return duplicatedTransform;
	}
	#endregion

	#region LineRenderer
	Dictionary<GameObject, List<LineRenderer>> _dicLineRendererInstancePool = new Dictionary<GameObject, List<LineRenderer>>();
	public LineRenderer GetCachedLineRenderer(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<LineRenderer> listCachedLineRenderer = null;
		if (_dicLineRendererInstancePool.ContainsKey(prefab))
			listCachedLineRenderer = _dicLineRendererInstancePool[prefab];
		else
		{
			listCachedLineRenderer = new List<LineRenderer>();
			_dicLineRendererInstancePool.Add(prefab, listCachedLineRenderer);
		}

		for (int i = 0; i < listCachedLineRenderer.Count; ++i)
		{
			if (!listCachedLineRenderer[i].gameObject.activeSelf)
			{
				listCachedLineRenderer[i].transform.parent = parentTransform;
				//listCachedLineRenderer[i].transform.position = position;
				//listCachedLineRenderer[i].transform.rotation = rotation;
				listCachedLineRenderer[i].transform.SetPositionAndRotation(position, rotation);
				listCachedLineRenderer[i].gameObject.SetActive(true);
				return listCachedLineRenderer[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		LineRenderer lineRenderer = newObject.GetComponent<LineRenderer>();
		listCachedLineRenderer.Add(lineRenderer);
		return lineRenderer;
	}

	Dictionary<GameObject, List<RayDesigner>> _dicRayDesignerInstancePool = new Dictionary<GameObject, List<RayDesigner>>();
	public RayDesigner GetCachedRayDesigner(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<RayDesigner> listCachedRayDesigner = null;
		if (_dicRayDesignerInstancePool.ContainsKey(prefab))
			listCachedRayDesigner = _dicRayDesignerInstancePool[prefab];
		else
		{
			listCachedRayDesigner = new List<RayDesigner>();
			_dicRayDesignerInstancePool.Add(prefab, listCachedRayDesigner);
		}

		for (int i = 0; i < listCachedRayDesigner.Count; ++i)
		{
			if (!listCachedRayDesigner[i].gameObject.activeSelf)
			{
				listCachedRayDesigner[i].transform.parent = parentTransform;
				//listCachedRayDesigner[i].transform.position = position;
				//listCachedRayDesigner[i].transform.rotation = rotation;
				listCachedRayDesigner[i].transform.SetPositionAndRotation(position, rotation);
				listCachedRayDesigner[i].gameObject.SetActive(true);
				return listCachedRayDesigner[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		RayDesigner rayDesigner = newObject.GetComponent<RayDesigner>();
		listCachedRayDesigner.Add(rayDesigner);
		return rayDesigner;
	}

	Dictionary<GameObject, List<LightningBoltPrefabScript>> _dicProceduralLightningInstancePool = new Dictionary<GameObject, List<LightningBoltPrefabScript>>();
	public LightningBoltPrefabScript GetCachedLightningBoltPrefabScript(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<LightningBoltPrefabScript> listCachedProceduralLightning = null;
		if (_dicProceduralLightningInstancePool.ContainsKey(prefab))
			listCachedProceduralLightning = _dicProceduralLightningInstancePool[prefab];
		else
		{
			listCachedProceduralLightning = new List<LightningBoltPrefabScript>();
			_dicProceduralLightningInstancePool.Add(prefab, listCachedProceduralLightning);
		}

		for (int i = 0; i < listCachedProceduralLightning.Count; ++i)
		{
			if (!listCachedProceduralLightning[i].gameObject.activeSelf)
			{
				listCachedProceduralLightning[i].transform.parent = parentTransform;
				//listCachedProceduralLightning[i].transform.position = position;
				//listCachedProceduralLightning[i].transform.rotation = rotation;
				listCachedProceduralLightning[i].transform.SetPositionAndRotation(position, rotation);
				listCachedProceduralLightning[i].gameObject.SetActive(true);
				return listCachedProceduralLightning[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		LightningBoltPrefabScript lightningBoltPrefabScript = newObject.GetComponent<LightningBoltPrefabScript>();
		listCachedProceduralLightning.Add(lightningBoltPrefabScript);
		return lightningBoltPrefabScript;
	}
	#endregion

	#region Collider Transform
	Dictionary<Collider, Transform> _dicTransformByCollider = new Dictionary<Collider, Transform>();
	public Transform GetTransformFromCollider(Collider collider)
	{
		if (collider == null)
			return null;

		if (_dicTransformByCollider.ContainsKey(collider))
			return _dicTransformByCollider[collider];

		Transform _transform = collider.transform;
		_dicTransformByCollider.Add(collider, _transform);
		return _transform;
	}
	#endregion


	#region HitParameter
	#endregion

	#region SkillLevel2AffectorLevel
	Dictionary<string, Dictionary<int, int>> _dicSkillLevel2AffectorLevelInfo = new Dictionary<string, Dictionary<int, int>>();
	public Dictionary<int, int> GetCachedSkillLevel2AffectorLevelData(string skillLevel2AffectorLevel)
	{
		if (_dicSkillLevel2AffectorLevelInfo.ContainsKey(skillLevel2AffectorLevel))
			return _dicSkillLevel2AffectorLevelInfo[skillLevel2AffectorLevel];

		Dictionary<int, int> dicConvertData = new Dictionary<int, int>();
		string[] split = skillLevel2AffectorLevel.Split(',');
		for (int i = 0; i < split.Length; ++i)
		{
			string[] matchInfo = split[i].Split(':');
			if (matchInfo.Length != 2)
				continue;
			int key = int.Parse(matchInfo[0]);
			int value = int.Parse(matchInfo[1]);
			if (!dicConvertData.ContainsKey(key))
				dicConvertData.Add(key, value);
		}
		_dicSkillLevel2AffectorLevelInfo.Add(skillLevel2AffectorLevel, dicConvertData);
		return dicConvertData;
	}
	#endregion

	#region String2AffectorValueList
	Dictionary<string, string[]> _dicStringList = new Dictionary<string, string[]>();
	public string[] GetCachedString2StringList(string sValue2)
	{
		if (_dicStringList.ContainsKey(sValue2))
			return _dicStringList[sValue2];

		string[] split = sValue2.Split(',');
		for (int i = 0; i < split.Length; ++i)
			split[i] = split[i].Replace(" ", "");
		_dicStringList.Add(sValue2, split);
		return split;
	}
	#endregion

	#region MultiHit DamageRatioList
	Dictionary<string, float[]> _dicMultiHitDamageRatioList = new Dictionary<string, float[]>();
	public float[] GetCachedMultiHitDamageRatioList(string sValue1)
	{
		if (_dicMultiHitDamageRatioList.ContainsKey(sValue1))
			return _dicMultiHitDamageRatioList[sValue1];
		
		string[] split = sValue1.Split(',');
		float[] damageRatioList = new float[split.Length];
		for (int i = 0; i < split.Length; ++i)
			damageRatioList[i] = float.Parse(split[i]);
		_dicMultiHitDamageRatioList.Add(sValue1, damageRatioList);
		return damageRatioList;
	}
	#endregion


	#region Live Monster List
	List<MonsterActor> _listLiveMonsterActor = new List<MonsterActor>();
	public void OnInitializeMonster(MonsterActor monsterActor)
	{
		if (_listLiveMonsterActor.Contains(monsterActor))
		{
#if UNITY_EDITOR
			Debug.LogErrorFormat("Invalid Data : LiveMonsterActorList already contains the monster : {0}", monsterActor.actorId);
#endif
			return;
		}
		_listLiveMonsterActor.Add(monsterActor);
	}

	public void OnDieMonster(MonsterActor monsterActor)
	{
		if (_listLiveMonsterActor.Contains(monsterActor) == false)
		{
#if UNITY_EDITOR
			Debug.LogErrorFormat("Invalid Data : LiveMonsterActorList does not contain the monster : {0}", monsterActor.actorId);
#endif
			return;
		}
		_listLiveMonsterActor.Remove(monsterActor);
	}

	public List<MonsterActor> GetLiveMonsterList()
	{
		return _listLiveMonsterActor;
	}
	#endregion

	#region Monster Index
	public int monsterIndex { get; set; }
	public bool useCachedEliteInfo { get; set; }
	public void OnPreInitializeMonster()
	{
		monsterIndex = 0;

		// 이 타이밍에 저장해놔야 MonsterActor의 Start때에도 제대로 판단할 수 있게 된다.
		useCachedEliteInfo = ClientSaveData.instance.IsLoadingInProgressGame();
	}
	#endregion

	#region PathFinder Agent
	Dictionary<int, int> _dicPathFinderAgentRefCount = new Dictionary<int, int>();
	public void OnInitializePathFinderAgent(int agentTypeID)
	{
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			return;

		if (_dicPathFinderAgentRefCount.ContainsKey(agentTypeID) == false)
		{
			_dicPathFinderAgentRefCount.Add(agentTypeID, 1);
			currentGround.BakeNavMesh(agentTypeID, true);
			AddTotalPathFinderAgentRefCount(1);
			return;
		}

		_dicPathFinderAgentRefCount[agentTypeID] += 1;
		AddTotalPathFinderAgentRefCount(1);
	}

	// 어차피 NavMeshSurface는 컴포넌트라서 맵 지워질때 날아갈텐데 바닥판 안바뀔걸 대비해서 지워두는게 나으려나
	public void OnFinalizePathFinderAgent(int agentTypeID)
	{
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			return;

		if (_dicPathFinderAgentRefCount.ContainsKey(agentTypeID) == false)
			return;

		_dicPathFinderAgentRefCount[agentTypeID] -= 1;
		if (_dicPathFinderAgentRefCount[agentTypeID] <= 0)
		{
			_dicPathFinderAgentRefCount.Remove(agentTypeID);

			// 마지막 몹 사라질때 지우니 웨이브 넘어갈땐 굳이 안지워도 되는데 지워진다. 웨이브는 거의 없으니 상관없으려나
			currentGround.ClearNavMesh(agentTypeID);
		}
		AddTotalPathFinderAgentRefCount(-1);
	}

	#region BulletFlying AgentType
	// 현재 타겟이 닫혀있는 맵 밖에 있는지 판단해서 자동 공격을 취소하는 로직이 있는데, 이게 제대로 동작하려면
	// 매우 작은 radius를 가진 NavAgent, 그것도 Trap을 무시하기 위해 Flying으로 된 NavAgent에 맞는 네비를 구워놔야한다.
	// 구워놓는 시점은 첫번째 몹의 네비가 구워질때 굽고 삭제는 모든 네비가 사라지는 시점이다.
	// 이 과정을 위해 괜히 쓰지도 않는 NavMeshAgent를 붙이는거보다 아래처럼 직접 AgentID전달해서 굽는 방식으로 가는게 깔끔해서 이렇게 해본다.
	// 참고로 이 AgentType아이디는 변경될리 없으니 NavMeshAreas.asset에서 가져왔다.
	int _totalPathFinderAgentRefCount = 0;
	int _bulletFlyingAgentTypeID = -562324683;
	public int bulletFlyingAgentTypeID { get { return _bulletFlyingAgentTypeID; } }
	void AddTotalPathFinderAgentRefCount(int addCount)
	{
		if (addCount > 0)
		{
			_totalPathFinderAgentRefCount += addCount;
			if (_totalPathFinderAgentRefCount == 1)
				currentGround.BakeNavMesh(_bulletFlyingAgentTypeID, false);
		}
		else
		{
			if (_totalPathFinderAgentRefCount <= 0)
				return;
			_totalPathFinderAgentRefCount += addCount;
			if (_totalPathFinderAgentRefCount <= 0)
				currentGround.ClearNavMesh(_bulletFlyingAgentTypeID);
		}
	}
	#endregion

	public void BakeNavMesh()
	{
		//queue navmesh
		Dictionary<int, int>.Enumerator e = _dicPathFinderAgentRefCount.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current.Value <= 0)
				continue;

			currentGround.BakeNavMesh(e.Current.Key, true);
		}
	}
	#endregion

	#region ActionNameHash
	Dictionary<string, int> _dicActionNameHash = new Dictionary<string, int>();
	public int GetActionNameHash(string actionName)
	{
		if (_dicActionNameHash.ContainsKey(actionName))
			return _dicActionNameHash[actionName];

		int hash = Animator.StringToHash(actionName);
		_dicActionNameHash.Add(actionName, hash);
		return hash;
	}

	Dictionary<string, int> _dicShaderPropertyId = new Dictionary<string, int>();
	public int GetShaderPropertyId(string propertyName)
	{
		if (_dicShaderPropertyId.ContainsKey(propertyName))
			return _dicShaderPropertyId[propertyName];

		int id = Shader.PropertyToID(propertyName);
		_dicShaderPropertyId.Add(propertyName, id);
		return id;
	}
	#endregion

	#region Global Constant
	Dictionary<string, float> _dicGlobalConstantFloat = new Dictionary<string, float>();
	public float GetCachedGlobalConstantFloat(string id)
	{
		if (_dicGlobalConstantFloat.ContainsKey(id))
			return _dicGlobalConstantFloat[id];

		float value = TableDataManager.instance.GetGlobalConstantFloat(id);
		_dicGlobalConstantFloat.Add(id, value);
		return value;
	}

	Dictionary<string, int> _dicGlobalConstantInt = new Dictionary<string, int>();
	public int GetCachedGlobalConstantInt(string id)
	{
		if (_dicGlobalConstantInt.ContainsKey(id))
			return _dicGlobalConstantInt[id];

		int value = TableDataManager.instance.GetGlobalConstantInt(id);
		_dicGlobalConstantInt.Add(id, value);
		return value;
	}

	Dictionary<string, string> _dicGlobalConstantString = new Dictionary<string, string>();
	public string GetCachedGlobalConstantString(string id)
	{
		if (_dicGlobalConstantString.ContainsKey(id))
			return _dicGlobalConstantString[id];

		string value = TableDataManager.instance.GetGlobalConstantString(id);
		_dicGlobalConstantString.Add(id, value);
		return value;
	}
	#endregion

	#region PlayerActor
	public PlayerActor playerActor { get; set; }
	public bool standbySwapPlayerActor { get; set; }
	#endregion

	#region PlayerActor List
	Dictionary<string, PlayerActor> _dicCachedPlayerActor = new Dictionary<string, PlayerActor>();
	public void OnInitializePlayerActor(PlayerActor playerActor, string actorId)
	{
		if (_dicCachedPlayerActor.ContainsKey(actorId))
			return;

		_dicCachedPlayerActor.Add(actorId, playerActor);
	}

	public PlayerActor GetCachedPlayerActor(string actorId)
	{
		if (_dicCachedPlayerActor.ContainsKey(actorId))
			return _dicCachedPlayerActor[actorId];
		return null;
	}
	#endregion

	#region Player List
	List<string> _listBattlePlayerActorIdList = new List<string>();
	public void AddBattlePlayer(string actorId)
	{
		ClientSaveData.instance.OnChangedBattleActor(actorId);
		if (_listBattlePlayerActorIdList.Contains(actorId))
			return;
		_listBattlePlayerActorIdList.Add(actorId);

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		ClientSaveData.instance.OnChangedBattleActorData(serializer.SerializeObject(_listBattlePlayerActorIdList));
	}

	public bool IsInBattlePlayerList(string actorId)
	{
		return _listBattlePlayerActorIdList.Contains(actorId);
	}

	public void SetInProgressBattlePlayerData(string jsonBattleActorData)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listBattlePlayerActorIdList = serializer.DeserializeObject<List<string>>(jsonBattleActorData);
	}
	#endregion

	#region Instance Id
	public Actor FindActorByInstanceId(int instanceId)
	{
		if (playerActor.GetInstanceID() == instanceId)
			return playerActor;

		if (ExperienceCanvas.instance != null && ExperienceCanvas.instance.gameObject.activeSelf && CharacterListCanvas.instance.selectedPlayerActor.GetInstanceID() == instanceId)
			return CharacterListCanvas.instance.selectedPlayerActor;

		for (int i = 0; i < _listLiveMonsterActor.Count; ++i)
		{
			if (_listLiveMonsterActor[i].GetInstanceID() == instanceId)
				return _listLiveMonsterActor[i];
		}
		return null;
	}
	#endregion

	#region Drop
	List<DropProcessor> _listCachedDropProcessor = new List<DropProcessor>();
	public DropProcessor GetCachedDropProcessor(Vector3 position)
	{
		for (int i = 0; i < _listCachedDropProcessor.Count; ++i)
		{
			if (!_listCachedDropProcessor[i].gameObject.activeSelf)
			{
				_listCachedDropProcessor[i].cachedTransform.position = position;
				_listCachedDropProcessor[i].gameObject.SetActive(true);
				return _listCachedDropProcessor[i];
			}
		}

		GameObject newObject = new GameObject();
		newObject.name = "DropProcessor";
		DropProcessor dropProcessor = newObject.AddComponent<DropProcessor>();
		dropProcessor.cachedTransform.position = position;
		_listCachedDropProcessor.Add(dropProcessor);
		return dropProcessor;
	}

	Dictionary<GameObject, List<DropObject>> _dicDropObjectPool = new Dictionary<GameObject, List<DropObject>>();
	public DropObject GetCachedDropObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		List<DropObject> listCachedDropObject = null;
		if (_dicDropObjectPool.ContainsKey(prefab))
			listCachedDropObject = _dicDropObjectPool[prefab];
		else
		{
			listCachedDropObject = new List<DropObject>();
			_dicDropObjectPool.Add(prefab, listCachedDropObject);
		}

		for (int i = 0; i < listCachedDropObject.Count; ++i)
		{
			if (!listCachedDropObject[i].gameObject.activeSelf)
			{
				//listCachedDropObject[i].cachedTransform.position = position;
				//listCachedDropObject[i].cachedTransform.rotation = rotation;
				listCachedDropObject[i].cachedTransform.SetPositionAndRotation(position, rotation);
				listCachedDropObject[i].gameObject.SetActive(true);
				return listCachedDropObject[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		DropObject dropObject = newObject.GetComponent<DropObject>();
		listCachedDropObject.Add(dropObject);
		return dropObject;
	}

	public void OnDropLastMonsterInStage()
	{
		for (int i = 0; i < _listCachedDropProcessor.Count; ++i)
		{
			if (!_listCachedDropProcessor[i].gameObject.activeSelf)
				continue;
			if (_listCachedDropProcessor[i].onAfterBattle)
				continue;

			// 사실은 여기서 클라이언트 세이브 작업을 해야 정상인데 해당 드랍 프로세서가 가지고 있는 리스트 전부 기록해두면 안된다.
			// 아마 드랍을 하고있는 중일텐데 일부는 이미 DropObject로 만들어져있을테니
			// DropManager.instance.OnDropLastMonsterInStage 함수 호출될때 클라이언트 세이브 목록에 추가됐을거다.
			// 그러니 이미 만들어진건 빼고 드랍 순서상 뒤쪽에 있어서 아직 드랍을 못한 것들만 추가해야 정확한건데
			// 이럴라면 DropProcessor의 DropProcess() 코루틴 함수 안에서 돌고있는 인덱스의 값을 얻어와서 그 이후의 항목들만 등록해야한다.
			// 너무 할게 많아진다.
			// 루프 돌면서 저 인덱스 뒤의 항목에 대해서만 ClientSaveData.instance.OnAddedDropItemId(stringValue); 호출해야한다.
			//
			// 그러다가 차라리 onAfterBattle이 true인채로 생성되는 DropObject들을 등록하는게 더 나을거 같아서
			// 여기서 처리하지 않기로 한다.
			_listCachedDropProcessor[i].onAfterBattle = true;
		}
	}

	public bool IsLastDropProcessorInStage(DropProcessor dropProcessor)
	{
		// 해당 시점에서 활성화 중인 DropProcessor 중 유일하게 onAfterBattle가 켜져있다면 스테이지 내 마지막 드랍 프로세서로 판단한다.
		int aliveCount = 0;
		bool exist = false;
		for (int i = 0; i < _listCachedDropProcessor.Count; ++i)
		{
			if (!_listCachedDropProcessor[i].gameObject.activeSelf)
				continue;
			if (_listCachedDropProcessor[i].onAfterBattle == false)
				return false;
			++aliveCount;
			if (aliveCount > 1)
				return false;

			if (_listCachedDropProcessor[i] == dropProcessor)
			{
				exist = true;
				continue;
			}
		}
		return exist;
	}

	public bool IsAliveAnyDropProcessor()
	{
		for (int i = 0; i < _listCachedDropProcessor.Count; ++i)
		{
			if (_listCachedDropProcessor[i].gameObject.activeSelf)
				return true;
		}
		return false;
	}
	#endregion

	#region Stage Table
	Dictionary<string, StageTableData> _dicStageTableData = new Dictionary<string, StageTableData>();
	public StageTableData GetCachedStageTableData(int chapter, int stage, bool chaos)
	{
		string key = string.Format("{0}_{1}_{2}", chapter, stage, chaos ? 1 : 0);
		if (_dicStageTableData.ContainsKey(key))
			return _dicStageTableData[key];

		StageTableData stageTableData = TableDataManager.instance.FindStageTableData(chapter, stage, chaos);
		_dicStageTableData.Add(key, stageTableData);
		return stageTableData;
	}

	Dictionary<string, MapTableData> _dicMapTableData = new Dictionary<string, MapTableData>();
	public MapTableData GetCachedMapTableData(string mapId)
	{
		if (_dicMapTableData.ContainsKey(mapId))
			return _dicMapTableData[mapId];

		MapTableData mapTableData = TableDataManager.instance.FindMapTableData(mapId);
		_dicMapTableData.Add(mapId, mapTableData);
		return mapTableData;
	}
	#endregion

	#region CommonPoolPreloadObjectList
	// 액션툴에 넣을 수 없는 패시브용 이펙트들을 위해 만든 구조다.
	// 패시브용 이펙트도 크게 둘로 분류할 수 있는데,
	// 방어자한테 붙는건 자기 액터쪽에 붙어있을테니 어펙터 처리할때 그거 읽어오면 되니 로드하기가 편하다.
	// 하지만 화상이나 빙결처럼 다른 상대한테 거는 이펙트들은 방어자가 미리 들고있을 수 없기때문에(모든걸 다 넣어야한다.) 로드하기가 불편하다.
	// 그렇다고 공용 피격 이펙트들을 한데모아 다 로드하는건 낭비이기 때문에(메모리도 로딩속도도 다 손해다.)
	// 결국 공격자가 들고있게 해놔야하고 해당 액터를 로드시 이렇게 공용 풀에 등록해서 꺼내쓰기로 한다.
	//
	// 꼭 지켜야할건 여러 캐릭터가 같은 이펙트를 사용할 수 있기 때문에 디펜던시 걸리도록 별도의 번들로 묶어야 한다는거다.
	List<GameObject> _listCommonPoolPreloadObject = new List<GameObject>();
	public void AddCommonPoolPreloadObjectList(GameObject[] commonPoolPreloadObjectList)
	{
		for (int i = 0; i < commonPoolPreloadObjectList.Length; ++i)
		{
			GameObject newObject = commonPoolPreloadObjectList[i];
#if UNITY_EDITOR
			if (newObject == null)
			{
				Debug.LogError("Common Pool Error : Null Object!!!!!!");
				continue;
			}
#endif
			if (_listCommonPoolPreloadObject.Contains(newObject) == false)
				_listCommonPoolPreloadObject.Add(newObject);
		}
	}

	public void AddCommonPoolPreloadObjectList(GameObject commonPoolPreloadObject)
	{
#if UNITY_EDITOR
		if (commonPoolPreloadObject == null)
		{
			Debug.LogError("Common Pool Error : Null Object!!!!!!");
			return;
		}
#endif
		if (_listCommonPoolPreloadObject.Contains(commonPoolPreloadObject) == false)
			_listCommonPoolPreloadObject.Add(commonPoolPreloadObject);
	}

	public GameObject FindCommonPoolPreloadObject(string objectName)
	{
		for (int i = 0; i < _listCommonPoolPreloadObject.Count; ++i)
		{
			if (_listCommonPoolPreloadObject[i] == null)
				continue;

			if (_listCommonPoolPreloadObject[i].name == objectName)
				return _listCommonPoolPreloadObject[i];
		}
		return null;
	}
	#endregion

	#region Portal
	List<Portal> _listCachedPortal = new List<Portal>();
	public Portal GetCachedPortal(Vector3 position, Quaternion rotation)
	{
		for (int i = 0; i < _listCachedPortal.Count; ++i)
		{
			if (!_listCachedPortal[i].gameObject.activeSelf)
			{
				//_listCachedPortal[i].cachedTransform.position = position;
				//_listCachedPortal[i].cachedTransform.rotation = rotation;
				_listCachedPortal[i].cachedTransform.SetPositionAndRotation(position, rotation);
				_listCachedPortal[i].gameObject.SetActive(true);
				return _listCachedPortal[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(BattleManager.instance.portalPrefab, position, rotation, null);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		Portal portal = newObject.GetComponent<Portal>();
		_listCachedPortal.Add(portal);
		return portal;
	}

	// OnClosedPortal 이벤트를 받지 않기에 현재 오픈되어있는거와 완벽하게 일치하진 않지만(맵 넘어갈땐 Opened에 남아있는다.)
	// 저 리스트가 필요한게 아니라서 느슨하게 짜둔다.
	// 현재 오픈하려는 portal이면 ClosePortal호출하지 않는거로도 충분하다.
	List<Portal> _listOpenedPortal = new List<Portal>();
	public void OnOpenedPortal(Portal portal)
	{
		for (int i = 0; i < _listOpenedPortal.Count; ++i)
		{
			if (_listOpenedPortal[i] == portal)
				continue;
			_listOpenedPortal[i].ClosePortal();
		}
		_listOpenedPortal.Clear();
		_listOpenedPortal.Add(portal);
	}
	#endregion

	#region OnOffColliderArea
	List<OnOffColliderArea> _listCachedOnOffColliderArea = new List<OnOffColliderArea>();
	public OnOffColliderArea GetCachedOnOffColliderArea(Vector3 position, Quaternion rotation)
	{
		for (int i = 0; i < _listCachedOnOffColliderArea.Count; ++i)
		{
			if (!_listCachedOnOffColliderArea[i].gameObject.activeSelf)
			{
				//_listCachedPortal[i].cachedTransform.position = position;
				//_listCachedPortal[i].cachedTransform.rotation = rotation;
				_listCachedOnOffColliderArea[i].cachedTransform.SetPositionAndRotation(position, rotation);
				_listCachedOnOffColliderArea[i].gameObject.SetActive(true);
				return _listCachedOnOffColliderArea[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(BattleManager.instance.onOffColliderAreaPrefab, position, rotation, null);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		OnOffColliderArea onOffColliderArea = newObject.GetComponent<OnOffColliderArea>();
		_listCachedOnOffColliderArea.Add(onOffColliderArea);
		return onOffColliderArea;
	}
	#endregion

	#region Sequential Monster
	List<SequentialMonster> _listSequentialMonster;
	public SequentialMonster bossGaugeSequentialMonster { get; set; }
	public void OnInitializeSequentialMonster(SequentialMonster sequentialMonster)
	{
		if (_listSequentialMonster == null)
			_listSequentialMonster = new List<SequentialMonster>();

		if (_listSequentialMonster.Contains(sequentialMonster))
		{
			Debug.LogError("Invalid Data : listSequentialMonsters already contains the sequential monster");
			return;
		}

		_listSequentialMonster.Add(sequentialMonster);

		if (sequentialMonster.applyBossMonsterGauge)
		{
			if (bossGaugeSequentialMonster == null)
				bossGaugeSequentialMonster = sequentialMonster;
			else
				Debug.LogError("Invalid Data : bossGaugeSequentialMonster already exist");
		}
	}

	public void OnFinalizeSequentialMonster(SequentialMonster sequentialMonster)
	{
		if (_listSequentialMonster == null)
			return;

		if (_listSequentialMonster.Contains(sequentialMonster) == false)
		{
#if UNITY_EDITOR
			Debug.LogError("Invalid Data : listSequentialMonsters does not contain the sequential monster");
#endif
			return;
		}
		_listSequentialMonster.Remove(sequentialMonster);
	}

	public bool CheckFinishSequentialMonster()
	{
		if (_listSequentialMonster == null)
			return true;

		return _listSequentialMonster.Count == 0;
	}
	#endregion

	#region Attack Indicator
	Dictionary<GameObject, List<AttackIndicator>> _dicAttackIndicatorInstancePool = new Dictionary<GameObject, List<AttackIndicator>>();
	public AttackIndicator GetCachedAttackIndicator(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<AttackIndicator> listCachedAttackIndicator = null;
		if (_dicAttackIndicatorInstancePool.ContainsKey(prefab))
			listCachedAttackIndicator = _dicAttackIndicatorInstancePool[prefab];
		else
		{
			listCachedAttackIndicator = new List<AttackIndicator>();
			_dicAttackIndicatorInstancePool.Add(prefab, listCachedAttackIndicator);
		}

		for (int i = 0; i < listCachedAttackIndicator.Count; ++i)
		{
			if (!listCachedAttackIndicator[i].gameObject.activeSelf)
			{
				listCachedAttackIndicator[i].transform.parent = parentTransform;
				//listCachedAttackIndicator[i].transform.position = position;
				//listCachedAttackIndicator[i].transform.rotation = rotation;
				listCachedAttackIndicator[i].cachedTransform.SetPositionAndRotation(position, rotation);
				listCachedAttackIndicator[i].gameObject.SetActive(true);
				return listCachedAttackIndicator[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		AttackIndicator attackIndicator = newObject.GetComponent<AttackIndicator>();
		listCachedAttackIndicator.Add(attackIndicator);
		return attackIndicator;
	}
	#endregion

	#region Summon Object
	List<GameObject> _listSummonObject = new List<GameObject>();
	public void OnInitializeSummonObject(GameObject summonObject)
	{
		if (_listSummonObject.Contains(summonObject))
			return;
		_listSummonObject.Add(summonObject);
	}

	public void FinalizeAllSummonObject()
	{
		for (int i = 0; i < _listSummonObject.Count; ++i)
		{
			if (_listSummonObject[i] == null)
				continue;
			if (_listSummonObject[i].gameObject.activeSelf == false)
				continue;
			_listSummonObject[i].gameObject.SetActive(false);
		}
		_listSummonObject.Clear();
	}

	public int delayedSummonMonsterRefCount { get; private set; }
	public void AddDelayedSummonMonsterRefCount(int count)
	{
		delayedSummonMonsterRefCount += count;
	}
	#endregion

	#region Managed Effect Object
	List<GameObject> _listManagedEffectObject = new List<GameObject>();
	public void OnInitializeManagedEffectObject(GameObject effectObject)
	{
		if (_listManagedEffectObject.Contains(effectObject))
			return;
		_listManagedEffectObject.Add(effectObject);
	}

	public void FinalizeAllManagedEffectObject()
	{
		for (int i = 0; i < _listManagedEffectObject.Count; ++i)
		{
			if (_listManagedEffectObject[i] == null)
				continue;
			if (_listManagedEffectObject[i].gameObject.activeSelf == false)
				continue;
			_listManagedEffectObject[i].gameObject.SetActive(false);
		}
		_listManagedEffectObject.Clear();
	}
	#endregion

	#region Equip Object
	Dictionary<GameObject, List<EquipPrefabInfo>> _dicEquipObjectInstancePool = new Dictionary<GameObject, List<EquipPrefabInfo>>();
	public EquipPrefabInfo GetCachedEquipObject(GameObject prefab, Transform parentTransform)
	{
		List<EquipPrefabInfo> listCachedEquipPrefabInfo = null;
		if (_dicEquipObjectInstancePool.ContainsKey(prefab))
			listCachedEquipPrefabInfo = _dicEquipObjectInstancePool[prefab];
		else
		{
			listCachedEquipPrefabInfo = new List<EquipPrefabInfo>();
			_dicEquipObjectInstancePool.Add(prefab, listCachedEquipPrefabInfo);
		}

		for (int i = 0; i < listCachedEquipPrefabInfo.Count; ++i)
		{
			if (!listCachedEquipPrefabInfo[i].gameObject.activeSelf)
			{
				listCachedEquipPrefabInfo[i].transform.parent = parentTransform;
				listCachedEquipPrefabInfo[i].gameObject.SetActive(true);
				return listCachedEquipPrefabInfo[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		EquipPrefabInfo equipPrefabInfo = newObject.GetComponent<EquipPrefabInfo>();
		listCachedEquipPrefabInfo.Add(equipPrefabInfo);
		return equipPrefabInfo;
	}
	#endregion

	#region Equip Object
	Dictionary<GameObject, List<RandomBoxAnimator>> _dicRandomBoxAnimatorInstancePool = new Dictionary<GameObject, List<RandomBoxAnimator>>();
	public RandomBoxAnimator GetCachedRandomBoxAnimator(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentTransform = null)
	{
		List<RandomBoxAnimator> listCachedRandomBoxAnimator = null;
		if (_dicRandomBoxAnimatorInstancePool.ContainsKey(prefab))
			listCachedRandomBoxAnimator = _dicRandomBoxAnimatorInstancePool[prefab];
		else
		{
			listCachedRandomBoxAnimator = new List<RandomBoxAnimator>();
			_dicRandomBoxAnimatorInstancePool.Add(prefab, listCachedRandomBoxAnimator);
		}

		for (int i = 0; i < listCachedRandomBoxAnimator.Count; ++i)
		{
			if (!listCachedRandomBoxAnimator[i].gameObject.activeSelf)
			{
				listCachedRandomBoxAnimator[i].transform.parent = parentTransform;
				//listCachedRandomBoxAnimator[i].transform.position = position;
				//listCachedRandomBoxAnimator[i].transform.rotation = rotation;
				listCachedRandomBoxAnimator[i].cachedTransform.SetPositionAndRotation(position, rotation);
				listCachedRandomBoxAnimator[i].gameObject.SetActive(true);
				return listCachedRandomBoxAnimator[i];
			}
		}

		GameObject newObject = Instantiate<GameObject>(prefab, position, rotation, parentTransform);
#if UNITY_EDITOR
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#endif
		RandomBoxAnimator randomBoxAnimator = newObject.GetComponent<RandomBoxAnimator>();
		listCachedRandomBoxAnimator.Add(randomBoxAnimator);
		return randomBoxAnimator;
	}
	#endregion








	#region CreateHitObjectMoving List
	// RailMonster는 스스로 OnDestroy를 체크하기가 편하기때문에 static으로 관리해도 리스트에서 삭제가 용이했었는데,
	// 이 CreateHitObjectMovingAffector꺼는 히트오브젝트가 꺼지는 타임마다 체크를 해야해서
	// 이벤트를 받기가 상대적으로 어려웠다.
	// 그러다보니 static리스트를 클리어하기가 어려워서 차라리 BattleInstanceManager에가 맡기기로 했다.
	// 이러면 어차피 맵이동시 알아서 지워지기 때문에 삭제에 대한걸 처리하지 않아도 괜찮아진다.
	//
	// 그렇지만 이 방법은 웬만하면 쓰지 않기로 한다.
	// 이미 HitObject를 따로 관리하고 있는 마당에 괜히 중복해서 관리하는거라 static으로 처리하기 어려워서 예외적으로 해주는거 뿐이다.
	List<HitObject> _listHitObjectMoving;
	public void AddHitObjectMoving(HitObject hitObject)
	{
		if (_listHitObjectMoving == null)
			_listHitObjectMoving = new List<HitObject>();

		if (_listHitObjectMoving.Contains(hitObject) == false)
			_listHitObjectMoving.Add(hitObject);
	}

	public void DisableAllHitObjectMoving()
	{
		if (_listHitObjectMoving == null)
			return;

		for (int i = 0; i < _listHitObjectMoving.Count; ++i)
		{
			if (_listHitObjectMoving[i] == null)
				continue;
			if (_listHitObjectMoving[i].gameObject == null || _listHitObjectMoving[i].gameObject.activeSelf == false)
				continue;
			_listHitObjectMoving[i].gameObject.SetActive(false);
		}
	}
	#endregion

	#region Teleported Affector List
	// 결국 Teleported Affector도 static으로는 관리하기가 어려워져서 여기에 추가하기로 했다.
	List<TeleportedAffector> _listTeleportedAffector;
	public void AddTeleportedAffector(TeleportedAffector teleportedAffector)
	{
		if (_listTeleportedAffector == null)
			_listTeleportedAffector = new List<TeleportedAffector>();

		_listTeleportedAffector.Add(teleportedAffector);
	}

	public void RemoveTeleportedAffector(TeleportedAffector teleportedAffector)
	{
		if (_listTeleportedAffector == null)
			return;

		_listTeleportedAffector.Remove(teleportedAffector);
	}

	public int GetActiveTeleportedCount()
	{
		if (_listTeleportedAffector == null)
			return 0;
		return _listTeleportedAffector.Count;
	}

	public int GetActiveTeleportedCountByType(bool bossMonster)
	{
		if (_listTeleportedAffector == null)
			return 0;
		int count = 0;
		for (int i = 0; i < _listTeleportedAffector.Count; ++i)
		{
			if (bossMonster != _listTeleportedAffector[i].bossMonster)
				continue;
			++count;
		}
		return count;
	}

	public void RestoreFirstTeleportedObject()
	{
		if (_listTeleportedAffector == null)
			return;

		if (_listTeleportedAffector.Count > 0)
			_listTeleportedAffector[0].finalized = true;
	}
	#endregion

	#region Position Buff Affector List
	List<PositionBuffAffector> _listPositionBuffAffector;
	public void AddPositionBuffAffector(PositionBuffAffector positionBuffAffector)
	{
		if (_listPositionBuffAffector == null)
			_listPositionBuffAffector = new List<PositionBuffAffector>();

		_listPositionBuffAffector.Add(positionBuffAffector);
	}

	public void RemovePositionBuffAffector(PositionBuffAffector positionBuffAffector)
	{
		if (_listPositionBuffAffector == null)
			return;

		_listPositionBuffAffector.Remove(positionBuffAffector);
	}

	public void FinalizeAllPositionBuffAffector(bool ignoreFinalizeEffect)
	{
		if (_listPositionBuffAffector == null)
			return;

		for (int i = 0; i < _listPositionBuffAffector.Count; ++i)
		{
			if (_listPositionBuffAffector[i] == null)
				continue;
			_listPositionBuffAffector[i].ignoreFinalizeEffect = ignoreFinalizeEffect;
			_listPositionBuffAffector[i].finalized = true;
		}
		_listPositionBuffAffector.Clear();
	}
	#endregion




	#region AddAttackByContinuousKillAffector Argument
	ObscuredInt _allyContinuousKillCount;
	public int allyContinuousKillCount
	{
		get
		{
			return _allyContinuousKillCount;
		}
		set
		{
			_allyContinuousKillCount = value;
			ClientSaveData.instance.OnChangedAllyContinuousKillCount(_allyContinuousKillCount);
		}
	}
	#endregion





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
