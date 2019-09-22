using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowingFirstShotGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("FollowingFirstShotGenerator")]
	public float interval;

	Vector3 _firstShotPosition;
	Quaternion _firstShotRotation;
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

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;

			if ((_remainCreateCount + 1) == createCount)
			{
				HitObject hitObject = DuplicateHitObject();
				if (hitObject != null)
				{
					_firstShotPosition = hitObject.cachedTransform.position;
					_firstShotRotation = hitObject.cachedTransform.rotation;
				}
			}
			else
			{
				Generate(_firstShotPosition, _firstShotRotation);
			}
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}