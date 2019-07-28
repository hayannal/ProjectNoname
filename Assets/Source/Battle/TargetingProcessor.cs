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

	public List<Collider> GetTargetList()
	{
		return _targetList;
	}

	public Collider GetTarget(int index = 0)
	{
		if (index < _targetList.Count)
			return _targetList[index];	
		return null;
	}

	public Vector3 GetTargetPosition(int index = 0)
	{
		Collider collider = GetTarget(index);
		if (collider == null)
			return Vector3.zero;
		return BattleInstanceManager.instance.GetTransformFromCollider(collider).position;
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


	#region Custom Position
	public bool IsRegisteredCustomTargetPosition()
	{
		return _registeredCustomTargetPositionCount > 0;
	}

	int _registeredCustomTargetPositionCount = 0;
	List<Vector3> _listCustomTargetPosition;
	public Vector3 GetCustomTargetPosition(int index)
	{
		if (_listCustomTargetPosition == null)
			return Vector3.zero;

		if (!IsRegisteredCustomTargetPosition())
			return Vector3.zero;

		_registeredCustomTargetPositionCount -= 1;
		if (index < _listCustomTargetPosition.Count)
			return _listCustomTargetPosition[index];
		return Vector3.zero;
	}

	public void SetCustomTargetPosition(Vector3 position)
	{
		if (_listCustomTargetPosition == null)
			_listCustomTargetPosition = new List<Vector3>();
		_listCustomTargetPosition.Clear();
		_listCustomTargetPosition.Add(position);
		_registeredCustomTargetPositionCount = 1;
	}

	public void SetCustomTargetPosition(List<Collider> listTarget)
	{
		if (_listCustomTargetPosition == null)
			_listCustomTargetPosition = new List<Vector3>();
		_listCustomTargetPosition.Clear();
		for (int i = 0; i < listTarget.Count; ++i)
			_listCustomTargetPosition.Add(BattleInstanceManager.instance.GetTransformFromCollider(listTarget[i]).position);
		_registeredCustomTargetPositionCount = listTarget.Count;
	}
	#endregion
}
