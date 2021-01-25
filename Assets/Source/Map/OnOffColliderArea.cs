using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnOffColliderArea : MonoBehaviour
{
	public float activeDuration = 10.0f;
	public float cooltimeDuration = 3.0f;
	public GameObject activeGroundEffectObject;
	public GameObject activeOrbEffectObject;

	float _activeRemainTime;
	float _cooltimeRemainTime;

	void OnDisable()
	{
		activeGroundEffectObject.SetActive(false);
		activeOrbEffectObject.SetActive(false);
	}

	void Update()
	{
		if (_activeRemainTime > 0.0f)
		{
			_activeRemainTime -= Time.deltaTime;
			if (_activeRemainTime <= 0.0f)
			{
				_activeRemainTime = 0.0f;
				activeGroundEffectObject.SetActive(false);
				activeOrbEffectObject.SetActive(false);
			}
		}


	}

	void OnTriggerEnter(Collider other)
	{
		// 활성화 중에는 처리하지 않는다.
		if (_activeRemainTime > 0.0f)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		_activeRemainTime = activeDuration;

		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = 0; i < listMonsterActor.Count; ++i)
		{
			OnOffColliderAffector.OnLight(listMonsterActor[i].affectorProcessor, activeDuration);
		}

		activeGroundEffectObject.SetActive(true);
		activeOrbEffectObject.SetActive(true);
	}







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