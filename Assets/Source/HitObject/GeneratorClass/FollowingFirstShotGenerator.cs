using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class FollowingFirstShotGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("FollowingFirstShotGenerator")]
	public float interval;

	Quaternion _firstShotRotation;
	int _remainCreateCount;
	float _remainIntervalTime;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainIntervalTime = 0.0f;

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		if (CheckChangeState())
		{
			gameObject.SetActive(false);
			return;
		}

		if (_parentActor.actorStatus.IsDie())
		{
			gameObject.SetActive(false);
			return;
		}

		if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;

			if ((_remainCreateCount + 1) == _initializedCreateCount)
			{
				HitObject hitObject = DuplicateHitObject();
				if (hitObject != null)
					_firstShotRotation = hitObject.cachedTransform.rotation;
			}
			else
			{
				Generate(cachedTransform.position, _firstShotRotation);
			}
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}