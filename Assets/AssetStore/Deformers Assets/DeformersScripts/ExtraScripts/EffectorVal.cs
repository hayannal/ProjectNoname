using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EffectorVal : MonoBehaviour 
{
	public float EffectorDistance = 3.0f;
	public float EffectorRecoverySpeed = 10.0f;
	public bool Inverted = false;
	public AnimationCurve FallOffCurve;

	void Start()
	{
		if (FallOffCurve.length == 0) {
			FallOffCurve = new AnimationCurve (new Keyframe (0, 1), new Keyframe (1, 1));
		}
	}

	float _prevDistance;
	float _lastDuration;
	public void TweenDistance(float targetDistance, float duration)
	{
		_prevDistance = EffectorDistance;
		DOTween.To(() => EffectorDistance, x => EffectorDistance = x, targetDistance, duration).SetEase(Ease.Linear);
		_lastDuration = duration;
	}

	public void ResetTweenDistance()
	{
		DOTween.To(() => EffectorDistance, x => EffectorDistance = x, _prevDistance, _lastDuration).SetEase(Ease.Linear);
	}
}
