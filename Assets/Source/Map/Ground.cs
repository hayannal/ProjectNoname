using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Ground : MonoBehaviour
{
	public GameObject quadRootObject;
	public bool fakeGround;

	Bounds _bounds = new Bounds();

	void Awake()
	{
		Transform quadRootTransform = quadRootObject.transform;
		_bounds.max = new Vector3(quadRootTransform.Find("QuadRight").localPosition.x, 10.0f, quadRootTransform.Find("QuadUp").localPosition.z);
		_bounds.min = new Vector3(quadRootTransform.Find("QuadLeft").localPosition.x, -10.0f, quadRootTransform.Find("QuadDown").localPosition.z);
		_colliderList = quadRootTransform.GetComponentsInChildren<Collider>();

		if (fakeGround)
			_bounds.center = transform.position;
	}

	// Start is called before the first frame update
	bool _started = false;
	void Start()
	{
		if (fakeGround)
		{
			_started = true;
			return;
		}

		StaticBatchingUtility.Combine(gameObject);

		if (CustomFollowCamera.instance != null)
			CustomFollowCamera.instance.OnLoadPlaneObject(_bounds.max.z, _bounds.min.z, _bounds.min.x, _bounds.max.x);
		_started = true;
	}

	void OnEnable()
	{
		if (fakeGround)
			return;

		BattleInstanceManager.instance.currentGround = this;

		if (_started && CustomFollowCamera.instance != null)
			CustomFollowCamera.instance.OnLoadPlaneObject(_bounds.max.z, _bounds.min.z, _bounds.min.x, _bounds.max.x);
	}

	public bool IsInQuadBound(Vector3 position)
	{
		if (_bounds.size == Vector3.zero)
			return true;
		return _bounds.Contains(position);
	}

	public Vector3 SamplePositionInQuadBound(Vector3 position)
	{
		if (IsInQuadBound(position))
			return position;
		if (position.x < _bounds.min.x)
			position.x = _bounds.min.x;
		if (position.x > _bounds.max.x)
			position.x = _bounds.max.x;
		if (position.z < _bounds.min.z)
			position.z = _bounds.min.z;
		if (position.z > _bounds.max.z)
			position.z = _bounds.max.z;
		return position;
	}

	#region Runtime NavMesh
	Dictionary<int, NavMeshSurface> _dicNavMeshSurface = null;
	public void BakeNavMesh(int agentTypeID)
	{
		if (_dicNavMeshSurface == null)
			_dicNavMeshSurface = new Dictionary<int, NavMeshSurface>();

		NavMeshSurface navMeshSurface = null;
		if (_dicNavMeshSurface.ContainsKey(agentTypeID))
		{
			navMeshSurface = _dicNavMeshSurface[agentTypeID];
		}
		else
		{
			navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
			navMeshSurface.agentTypeID = agentTypeID;
			navMeshSurface.collectObjects = CollectObjects.Volume;
			navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
			navMeshSurface.center = _bounds.center;
			// 쿼드만큼 딱 볼륨 잡아서 구으면 경계면이 이쁘게 안나와서 0.5씩 더해서 뽑는다. 1더하니 네비가 Quad넘어서 미세하게 구워져서 0.5로 바꾼다.
			navMeshSurface.size = _bounds.size + Vector3.one * 0.5f;
			_dicNavMeshSurface.Add(agentTypeID, navMeshSurface);
		}
		navMeshSurface.BuildNavMesh();
	}

	public void ClearNavMesh(int agentTypeID)
	{
		if (_dicNavMeshSurface == null)
			return;
		if (_dicNavMeshSurface.ContainsKey(agentTypeID) == false)
			return;

		NavMeshSurface navMeshSurface = _dicNavMeshSurface[agentTypeID];
		if (navMeshSurface != null)
			navMeshSurface.RemoveData();
	}
	#endregion

	Collider[] _colliderList;
	public bool CheckQuadCollider(Collider col)
	{
		if (_colliderList == null)
			return false;
		for (int i = 0; i < _colliderList.Length; ++i)
		{
			if (_colliderList[i] == col)
				return true;
		}
		return false;
	}
}
