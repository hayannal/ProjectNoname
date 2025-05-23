﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class TripleAreaGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("TripleAreaGenerator")]
	public float distance;
	public float interval;

	int _remainCreateCount;
	float _remainIntervalTime;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainIntervalTime = 0.0f;
		_startAngleY = Random.Range(0.0f, 360.0f);

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	float _startAngleY;
	void Update()
	{
		// Area라서 Die체크 하지 않는다. 이미 발사되었다고 판단하는거다.
		//if (CheckChangeState())
		//{
		//	gameObject.SetActive(false);
		//	return;
		//}
		//if (_parentActor.actorStatus.IsDie())
		//{
		//	gameObject.SetActive(false);
		//	return;
		//}
		//if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		//	return;

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;
			int index = (_initializedCreateCount - _remainCreateCount) - 1;

			HitObject hitObject = Generate(cachedTransform.position, cachedTransform.rotation * Quaternion.Euler(0.0f, _startAngleY + index * 120.0f, 0.0f));
			if (hitObject != null)
				hitObject.cachedTransform.Translate(0.0f, 0.0f, distance, Space.Self);
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}