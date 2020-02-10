using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class RandomCameraShake : MonoBehaviour
{
	public float startDelay = 0.0f;

	public int numberOfShakes = 3;
	public Vector3 shakeAmountMin;
	public Vector3 shakeAmountMax;
	public Vector3 rotationAmountMin;
	public Vector3 rotationAmountMax;
	public float distanceMin;
	public float distanceMax;
	public float speedMin;
	public float speedMax;
	public float decay = 0.75f;

	Thinksquirrel.CShake.CameraShake _cameraShake;

	void OnEnable()
	{
		if (_started)
			PlayShake();
	}

	bool _started = false;
	void Start()
    {
		if (_cameraShake == null)
			_cameraShake = UIInstanceManager.instance.GetCachedCameraMain().GetComponent<Thinksquirrel.CShake.CameraShake>();
		if (_cameraShake == null)
			_cameraShake = UIInstanceManager.instance.GetCachedCameraMain().gameObject.AddComponent<Thinksquirrel.CShake.CameraShake>();
		_started = true;

		PlayShake();
	}

	void PlayShake()
	{
		if (startDelay == 0.0f)
			Shake();
		else
			Timing.RunCoroutine(DelayedShake(startDelay));
	}

	IEnumerator<float> DelayedShake(float delayTime)
	{
		yield return Timing.WaitForSeconds(delayTime);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		Shake();
	}

	void Shake()
	{
		_cameraShake.Shake(Thinksquirrel.CShake.CameraShake.ShakeType.CameraMatrix,
			numberOfShakes,
			new Vector3(
				RandomFromDistribution.RandomRangeExponential(shakeAmountMin.x, shakeAmountMax.x, 1.5f, RandomFromDistribution.Direction_e.Right),
				RandomFromDistribution.RandomRangeExponential(shakeAmountMin.y, shakeAmountMax.y, 1.5f, RandomFromDistribution.Direction_e.Right),
				RandomFromDistribution.RandomRangeExponential(shakeAmountMin.z, shakeAmountMax.z, 1.5f, RandomFromDistribution.Direction_e.Right)
				),
			new Vector3(Random.Range(rotationAmountMin.x, rotationAmountMax.x), Random.Range(rotationAmountMin.y, rotationAmountMax.y), Random.Range(rotationAmountMin.z, rotationAmountMax.z)),
			Random.Range(distanceMin, distanceMax),
			Random.Range(speedMin, speedMax),
			decay,
			1.0f, true);
	}
}
