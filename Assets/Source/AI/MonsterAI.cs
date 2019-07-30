using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
	const float TargetFindDelay = 0.1f;

	Actor targetActor;
	float targetRadius;

	Actor actor { get; set; }
	float actorRadius;
	TargetingProcessor targetingProcessor { get; set; }
	PathFinderController pathFinderController { get; set; }

	// Start is called before the first frame update
	void Start()
    {
		actor = GetComponent<Actor>();
		actorRadius = ColliderUtil.GetRadius(GetComponent<Collider>());
		targetingProcessor = GetComponent<TargetingProcessor>();
		pathFinderController = GetComponent<PathFinderController>();
	}

    // Update is called once per frame
    void Update()
    {
		UpdateTargeting();
		UpdateChase();
	}

	float _currentFindDelay;
	void UpdateTargeting()
	{
		if (targetingProcessor == null)
			return;

		if (targetActor != null)
		{
			if (targetActor.actorStatus.IsDie())
			{
				if (BattleInstanceManager.instance.targetOfMonster == targetActor)
					BattleInstanceManager.instance.targetOfMonster = null;
				_currentFindDelay = 0.0f;
				targetActor = null;
			}
		}
		if (targetActor != null)
			return;

		_currentFindDelay -= Time.deltaTime;
		if (_currentFindDelay <= 0.0f)
		{
			_currentFindDelay += TargetFindDelay;

			if (BattleInstanceManager.instance.targetOfMonster == null)
			{
				if (targetingProcessor.FindNearestTarget(Team.eTeamCheckFilter.Enemy, PlayerAI.FindTargetRange))
				{
					Collider targetCollider = targetingProcessor.GetTarget();
					targetRadius = ColliderUtil.GetRadius(targetCollider);
					AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
					BattleInstanceManager.instance.targetOfMonster = affectorProcessor.actor;
					targetActor = affectorProcessor.actor;
				}
				else
					targetActor = null;
			}
			else
			{
				targetActor = BattleInstanceManager.instance.targetOfMonster;
			}
		}
	}

	Vector3 _lastGoalPosition = Vector3.up;
	void UpdateChase()
	{
		if (targetActor == null)
			return;

		Vector3 diff = actor.cachedTransform.position - targetActor.cachedTransform.position;
		float sqrDiff = diff.sqrMagnitude;
		float sqrRadius = (targetRadius + actorRadius) * (targetRadius + actorRadius) + 0.01f;
		if (sqrDiff <= sqrRadius)
			return;

		if (_lastGoalPosition != targetActor.cachedTransform.position)
		{
			pathFinderController.agent.SetGoalMoveHere(targetActor.cachedTransform.position, collectPathContent: true);//order path to current target and also collect path content
			_lastGoalPosition = targetActor.cachedTransform.position;
		}

		if (sqrDiff <= sqrRadius)
		{
			//pathFinderController.agent.RemoveNextNodeIfCloserSqrVector2(targetRadius * 1.2f);
			pathFinderController.agent.RemoveNextNode(false);
		}
	}
}
