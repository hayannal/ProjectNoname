using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using DigitalRuby.ThunderAndLightning;

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
				listCachedGameObject[i].transform.position = position;
				listCachedGameObject[i].transform.rotation = rotation;
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
	Dictionary<Collider, AffectorProcessor> _dicAffectorProcessorByCollider = new Dictionary<Collider, AffectorProcessor>();
	public AffectorProcessor GetAffectorProcessorFromCollider(Collider collider)
	{
		if (collider == null)
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
				listCachedHitObject[i].cachedTransform.position = position;
				listCachedHitObject[i].cachedTransform.rotation = rotation;
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

	public void OnInitializeHitObject(HitObject hitObject, Collider collider)
	{
		if (collider == null)
			return;

		if (_dicHitObjectByCollider.ContainsKey(collider) == false)
		{
			_dicHitObjectByCollider.Add(collider, hitObject);
		}
	}

	public void OnFinalizeHitObject(Collider collider)
	{
		if (collider == null)
			return;

		_dicHitObjectByCollider.Remove(collider);
	}

	List<HitObject> _listCachedEmptyHitObject = new List<HitObject>();
	public HitObject GetEmptyHitObject(Vector3 position, Quaternion rotation)
	{
		for (int i = 0; i < _listCachedEmptyHitObject.Count; ++i)
		{
			if (!_listCachedEmptyHitObject[i].gameObject.activeSelf)
			{
				_listCachedEmptyHitObject[i].cachedTransform.position = position;
				_listCachedEmptyHitObject[i].cachedTransform.rotation = rotation;
				//_listCachedEmptyHitObject[i].cachedTransform.localScale = localScale;
				_listCachedEmptyHitObject[i].gameObject.SetActive(true);
				return _listCachedEmptyHitObject[i];
			}
		}

		GameObject newObject = new GameObject();
		newObject.name = "EmptyHitObject";
		Transform newTransform = newObject.transform;
		newTransform.position = position;
		newTransform.rotation = rotation;
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
				listCachedContinuousHitObjectGenerator[i].cachedTransform.position = position;
				listCachedContinuousHitObjectGenerator[i].cachedTransform.rotation = rotation;
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
				_listCachedTransform[i].position = position;
				_listCachedTransform[i].rotation = rotation;
				//_listCachedTransform[i].localScale = localScale;
				_listCachedTransform[i].gameObject.SetActive(true);
				return _listCachedTransform[i];
			}
		}

		GameObject newObject = new GameObject();
		newObject.name = "DuplicatedObject";
		Transform duplicatedTransform = newObject.transform;
		duplicatedTransform.position = position;
		duplicatedTransform.rotation = rotation;
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
				listCachedLineRenderer[i].transform.position = position;
				listCachedLineRenderer[i].transform.rotation = rotation;
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
				listCachedRayDesigner[i].transform.position = position;
				listCachedRayDesigner[i].transform.rotation = rotation;
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
				listCachedProceduralLightning[i].transform.position = position;
				listCachedProceduralLightning[i].transform.rotation = rotation;
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
			Debug.LogError("Invalid Data : LiveMonsterActorList already contains the monster");
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
			Debug.LogError("Invalid Data : LiveMonsterActorList does not contain the monster");
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

	#region PathFinder Agent
	Dictionary<int, int> _dicPathFinderAgentRefCount = new Dictionary<int, int>();
	public void OnInitializePathFinderAgent(int agentTypeID)
	{
		if (_dicPathFinderAgentRefCount.ContainsKey(agentTypeID) == false)
		{
			_dicPathFinderAgentRefCount.Add(agentTypeID, 1);
			currentGround.BakeNavMesh(agentTypeID);
			return;
		}

		_dicPathFinderAgentRefCount[agentTypeID] += 1;
	}

	// 어차피 NavMeshSurface는 컴포넌트라서 맵 지워질때 날아갈텐데 바닥판 안바뀔걸 대비해서 지워두는게 나으려나
	public void OnFinalizePathFinderAgent(int agentTypeID)
	{
		if (_dicPathFinderAgentRefCount.ContainsKey(agentTypeID) == false)
			return;

		_dicPathFinderAgentRefCount[agentTypeID] -= 1;
		if (_dicPathFinderAgentRefCount[agentTypeID] <= 0)
		{
			_dicPathFinderAgentRefCount.Remove(agentTypeID);

			// 마지막 몹 사라질때 지우니 웨이브 넘어갈땐 굳이 안지워도 되는데 지워진다. 웨이브는 거의 없으니 상관없으려나
			currentGround.ClearNavMesh(agentTypeID);
		}
	}

	public void BakeNavMesh()
	{
		//queue navmesh
		Dictionary<int, int>.Enumerator e = _dicPathFinderAgentRefCount.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current.Value <= 0)
				continue;

			currentGround.BakeNavMesh(e.Current.Key);
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
	#endregion

	#region PlayerActor
	public PlayerActor playerActor { get; set; }
	#endregion

	#region Instance Id
	public Actor FindActorByInstanceId(int instanceId)
	{
		if (playerActor.GetInstanceID() == instanceId)
			return playerActor;

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
				listCachedDropObject[i].cachedTransform.position = position;
				listCachedDropObject[i].cachedTransform.rotation = rotation;
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

	List<DropObject> _listDropObject = new List<DropObject>();
	public void OnInitializeDropObject(DropObject dropObject)
	{
		_listDropObject.Add(dropObject);
	}

	public void OnFinalizeDropObject(DropObject dropObject)
	{
		_listDropObject.Remove(dropObject);
	}

	public void OnDropLastMonsterInStage()
	{
		// 막타 이전에 죽은 몬스터의 DropProcessor에서 아직 스폰되지 않은 아이템이 남아있을 수 있으니
		// 스폰된 드랍템에 적용 후 DropProcessor에도 적용해야한다.
		for (int i = 0; i < _listDropObject.Count; ++i)
			_listDropObject[i].OnAfterBattle();

		for (int i = 0; i < _listCachedDropProcessor.Count; ++i)
		{
			if (!_listCachedDropProcessor[i].gameObject.activeSelf)
				continue;
			_listCachedDropProcessor[i].onAfterBattle = true;
		}
	}

	DropObject _reservedLastDropObject;
	public void ReserveLastDropObject(DropObject dropObject)
	{
		_reservedLastDropObject = dropObject;
	}

	public void ApplyLastDropObject()
	{
		if (_reservedLastDropObject != null)
		{
			_reservedLastDropObject.ApplyLastDropObject();
			_reservedLastDropObject = null;
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

	public void OnFinishLastDropAnimation()
	{
		for (int i = 0; i < _listDropObject.Count; ++i)
		{
			// 다음 스테이지에 드랍된 템들은 켜져있지 않을거다. 패스.
			if (_listDropObject[i].onAfterBattle == false)
				continue;

			_listDropObject[i].OnAfterAllDropAnimation();
		}
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
			if (_listCommonPoolPreloadObject.Contains(newObject) == false)
				_listCommonPoolPreloadObject.Add(newObject);
		}
	}

	public GameObject FindCommonPoolPreloadObject(string objectName)
	{
		for (int i = 0; i < _listCommonPoolPreloadObject.Count; ++i)
		{
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
				_listCachedPortal[i].cachedTransform.position = position;
				_listCachedPortal[i].cachedTransform.rotation = rotation;
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
