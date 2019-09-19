using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomCameraShake : MonoBehaviour
{
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
			Shake();
	}

	bool _started = false;
	void Start()
    {
		if (_cameraShake == null)
			_cameraShake = UIInstanceManager.instance.GetCachedCameraMain().GetComponent<Thinksquirrel.CShake.CameraShake>();
		if (_cameraShake == null)
			_cameraShake = UIInstanceManager.instance.GetCachedCameraMain().gameObject.AddComponent<Thinksquirrel.CShake.CameraShake>();
		_started = true;

		Shake();
	}

    void Shake()
	{
		_cameraShake.Shake(Thinksquirrel.CShake.CameraShake.ShakeType.CameraMatrix, numberOfShakes,
			new Vector3(Random.Range(shakeAmountMin.x, shakeAmountMax.x), Random.Range(shakeAmountMin.y, shakeAmountMax.y), Random.Range(shakeAmountMin.z, shakeAmountMax.z)),
			new Vector3(Random.Range(rotationAmountMin.x, rotationAmountMax.x), Random.Range(rotationAmountMin.y, rotationAmountMax.y), Random.Range(rotationAmountMin.z, rotationAmountMax.z)),
			Random.Range(distanceMin, distanceMax), Random.Range(speedMin, speedMax), decay, 1.0f, true);
	}
}
