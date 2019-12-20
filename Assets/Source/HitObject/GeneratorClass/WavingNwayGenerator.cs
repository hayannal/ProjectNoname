using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class WavingNwayGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("WavingNwayGenerator")]
	public bool useWorldSpaceDirection;
	public int wayNum = 1;
	public float waveCenterAngleY = 0.0f;
	public float waveRangeSize = 40.0f;
	public float waveSpeed = 5.0f;
	public float betweenAngle = 5.0f;
	public float lineInterval = 0.1f;

	int _remainCreateCount;
	float _remainLineIntervalTime;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainLineIntervalTime = 0.0f;

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

		_remainLineIntervalTime -= Time.deltaTime;
		if (_remainLineIntervalTime < 0.0f)
		{
			_remainLineIntervalTime += lineInterval;
			for (int i = 0; i < wayNum; ++i)
			{
				float centerAngle = waveCenterAngleY + (waveRangeSize / 2f * Mathf.Sin(Time.time * 60.0f * waveSpeed / 100f));
				float baseAngle = wayNum % 2 == 0 ? centerAngle - (betweenAngle / 2f) : centerAngle;
				float angle = GetShiftedAngle(i, baseAngle, betweenAngle);

				if (useWorldSpaceDirection)
					Generate(cachedTransform.position, Quaternion.Euler(0.0f, angle, 0.0f));
				else
					Generate(cachedTransform.position, cachedTransform.rotation * Quaternion.Euler(0.0f, angle, 0.0f));

				_remainCreateCount -= 1;
				if (_remainCreateCount <= 0)
				{
					gameObject.SetActive(false);
					break;
				}
			}
		}
	}

	public static float GetShiftedAngle(int wayIndex, float baseAngle, float betweenAngle)
	{
		return wayIndex % 2 == 0 ? baseAngle - (betweenAngle * (float)wayIndex / 2f) : baseAngle + (betweenAngle * Mathf.Ceil((float)wayIndex / 2f));
	}
}