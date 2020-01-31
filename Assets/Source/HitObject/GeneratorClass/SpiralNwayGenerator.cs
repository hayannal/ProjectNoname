using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class SpiralNwayGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("SpiralNwayGenerator")]
	public bool useWorldSpaceDirection;
	public int wayNum = 1;
	public float startAngleY;
	public float shiftAngleY;
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
				int index = _initializedCreateCount - _remainCreateCount;
				float centerAngle = startAngleY + (shiftAngleY * Mathf.Floor(index / wayNum));
				float baseAngle = wayNum % 2 == 0 ? centerAngle - (betweenAngle / 2f) : centerAngle;
				float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

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
}