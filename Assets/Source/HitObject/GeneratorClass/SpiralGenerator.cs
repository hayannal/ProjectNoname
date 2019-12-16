using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiralGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("SpiralGenerator")]
	public bool useWorldSpaceDirection;
	public float startAngleY;
	public float shiftAngleY;
	public float interval;

	int _remainCreateCount;
	float _remainIntervalTime;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

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
			int index = (_initializedCreateCount - _remainCreateCount) - 1;

			if (useWorldSpaceDirection)
				Generate(cachedTransform.position, Quaternion.Euler(0.0f, startAngleY + index * shiftAngleY, 0.0f));
			else
				Generate(cachedTransform.position, cachedTransform.rotation * Quaternion.Euler(0.0f, startAngleY + index * shiftAngleY, 0.0f));
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}