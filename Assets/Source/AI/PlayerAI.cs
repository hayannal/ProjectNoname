using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAI : MonoBehaviour
{
	Collider targetCollider;

	const float TargetFindDelay = 0.1f;
	float _currentFindDelay = 0.0f;

	public TargetingProcessor targetingProcessor { get; private set; }

	// Start is called before the first frame update
	void Start()
    {
		targetingProcessor = GetComponent<TargetingProcessor>();
	}

    // Update is called once per frame
    void Update()
    {
		UpdateTargeting();
    }

	Transform _cachedTargetingObjectTransform = null;
	//List<GameObject> _listCachedTargetingObject = null;
	void UpdateTargeting()
	{
		if (targetingProcessor == null)
			return;

		_currentFindDelay -= Time.deltaTime;
		if (_currentFindDelay <= 0.0f)
		{
			_currentFindDelay += TargetFindDelay;
			if (targetingProcessor.FindNearestTarget(Team.eTeamCheckFilter.Enemy, 50.0f))
				targetCollider = targetingProcessor.GetTarget();
			else
				targetCollider = null;
		}

		if (targetCollider != null)
		{
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
			if (affectorProcessor != null && affectorProcessor.actor != null && affectorProcessor.actor.actorStatus.IsDie())
			{
				_currentFindDelay = 0.0f;
				targetCollider = null;
			}
		}

		if (targetCollider == null)
		{
			if (_cachedTargetingObjectTransform != null)
				_cachedTargetingObjectTransform.gameObject.SetActive(false);
			return;
		}

		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		if (_cachedTargetingObjectTransform == null)
		{
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.targetCircleObject, null);
			_cachedTargetingObjectTransform = newObject.transform;
		}
		if (_cachedTargetingObjectTransform == null)
			return;

		_cachedTargetingObjectTransform.gameObject.SetActive(true);
		_cachedTargetingObjectTransform.position = targetTransform.position;
	}
}
