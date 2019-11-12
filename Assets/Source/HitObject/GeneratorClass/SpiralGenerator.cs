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

	void OnEnable()
	{
		_remainCreateCount = createCount;
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

		if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;
			int index = (createCount - _remainCreateCount) - 1;

			if (useWorldSpaceDirection)
				Generate(cachedTransform.position, Quaternion.Euler(0.0f, startAngleY + index * shiftAngleY, 0.0f));
			else
				Generate(cachedTransform.position, cachedTransform.rotation * Quaternion.Euler(0.0f, startAngleY + index * shiftAngleY, 0.0f));
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}