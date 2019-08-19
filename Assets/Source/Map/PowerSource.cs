using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerSource : MonoBehaviour
{
	void OnEnable()
	{
		_spawnedGatePillar = false;
	}

	bool _spawnedGatePillar;
	void OnCollisionEnter(Collision collision)
	{
		if (_spawnedGatePillar)
			return;

		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;
			OnTriggerEnter(col);
			break;
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (_spawnedGatePillar)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamID == (int)Team.eTeamID.DefaultMonster)
			return;

		BattleInstanceManager.instance.GetCachedObject(StageManager.instance.gatePillarPrefab, StageManager.instance.currentGatePillarSpawnPosition, Quaternion.identity);
		_spawnedGatePillar = true;
	}
}
