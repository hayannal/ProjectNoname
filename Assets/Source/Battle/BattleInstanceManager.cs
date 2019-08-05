using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	public Actor targetOfMonster { get; set; }
	public MapObject.Plane currentPlane { get; set; }
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
		listCachedGameObject.Add(newObject);
		return newObject;
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
		_dicHitObjectByCollider.Remove(collider);
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


	#region PathFinder Agent
	Dictionary<int, int> _dicPathFinderAgentRefCount = new Dictionary<int, int>();
	public void OnInitializePathFinderAgent(int agentTypeID)
	{
		if (_dicPathFinderAgentRefCount.ContainsKey(agentTypeID) == false)
		{
			_dicPathFinderAgentRefCount.Add(agentTypeID, 1);
			currentPlane.BakeNavMesh(agentTypeID);
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
			currentPlane.ClearNavMesh(agentTypeID);
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

			currentPlane.BakeNavMesh(e.Current.Key);
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
