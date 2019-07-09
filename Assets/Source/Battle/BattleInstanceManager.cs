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

	#region EffectObject

	#endregion

	#region HitObject
	Dictionary<GameObject, List<GameObject>> _dicHitObjectPool = new Dictionary<GameObject, List<GameObject>>();
	public GameObject GetCachedHitObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		List<GameObject> listCachedGameObject = null;
		if (_dicHitObjectPool.ContainsKey(prefab))
			listCachedGameObject = _dicHitObjectPool[prefab];
		else
		{
			listCachedGameObject = new List<GameObject>();
			_dicHitObjectPool.Add(prefab, listCachedGameObject);
		}

		for (int i = 0; i < listCachedGameObject.Count; ++i)
		{
			if (!listCachedGameObject[i].activeSelf)
			{
				listCachedGameObject[i].transform.position = position;
				listCachedGameObject[i].transform.rotation = rotation;
				listCachedGameObject[i].SetActive(true);
				return listCachedGameObject[i];
			}
		}

		GameObject newHitObject = Instantiate(prefab, position, rotation) as GameObject;
		listCachedGameObject.Add(newHitObject);
		return newHitObject;
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

	public void OnFinalizeHitObject(HitObject hitObject, Collider collider)
	{

	}
	#endregion


	#region HitParameter
	#endregion

	#region AffectorValueId
	Dictionary<string, string[]> _dicAffectorValueIdList = new Dictionary<string, string[]>();
	public string[] GetAffectorValueIdList(string affectorValueIdList)
	{
		if (_dicAffectorValueIdList.ContainsKey(affectorValueIdList))
			return _dicAffectorValueIdList[affectorValueIdList];

		string[] splitList = affectorValueIdList.Split(',');
		_dicAffectorValueIdList.Add(affectorValueIdList, splitList);
		return splitList;
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
