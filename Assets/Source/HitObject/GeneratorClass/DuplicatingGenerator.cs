using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicatingGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("DuplicatingGenerator")]
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
			DuplicateHitObject();
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}