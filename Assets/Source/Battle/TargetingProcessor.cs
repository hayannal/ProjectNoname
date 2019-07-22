using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingProcessor : MonoBehaviour {

	void OnEnable()
	{
		_targetList.Clear();
	}

	public int GetTargetCount()
	{
		return _targetList.Count;
	}

	public Collider GetTarget()
	{
		if (_targetList.Count > 1)
		{
		}
		else if (_targetList.Count == 1)
			return _targetList[0];
		return null;
	}

	public Vector3 GetTargetPosition()
	{
		Collider collider = GetTarget();
		if (collider != null)
			return collider.transform.position;
		return Vector3.zero;
	}

	public List<Collider> GetTargetList()
	{
		return _targetList;
	}

	List<Collider> _targetList = new List<Collider>();

	Transform _transform = null;
	Team _teamComponent = null;
	public bool FindNearestTarget(Team.eTeamCheckFilter teamFilter, float range)
	{
		if (_transform == null)
			_transform = GetComponent<Transform>();
		if (_teamComponent == null)
			_teamComponent = GetComponent<Team>();

		Vector3 position = _transform.position;
		Collider[] result = Physics.OverlapSphere(position, range); // range * _transform.localScale.x
		float nearestDistance = float.MaxValue;
		Collider nearestCollider = null;
		for (int i = 0; i < result.Length; ++i)
		{
			// affector processor
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(result[i]);
			if (affectorProcessor == null)
				continue;

			// team check
			if (_teamComponent != null)
			{
				if (!Team.CheckTeamFilter(_teamComponent.teamID, result[i], teamFilter, false))
					continue;
			}

			// hp
			Actor actor = affectorProcessor.actor;
			if (actor != null)
			{
				if (actor.actorStatus.IsDie())
					continue;
			}

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(result[i]);
			if (colliderRadius == -1.0f) continue;

			// distance
			Vector3 diff = result[i].transform.position - position;
			diff.y = 0.0f;
			float distance = diff.magnitude - colliderRadius;
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestCollider = result[i];
			}
		}

		_targetList.Clear();
		if (nearestDistance != float.MaxValue && nearestCollider != null)
		{
			_targetList.Add(nearestCollider);
			return true;
		}
		return false;
	}
}
