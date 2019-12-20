using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class BackNwayGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("BackNwayGenerator")]
	public float betweenAngle = 5.0f;

	public static float BackNwayLocalRadiusScale = 1.2f;
	Vector3 _spawnLocalPosition = Vector3.zero;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		Collider collider = parentActor.GetCollider();
		_spawnLocalPosition.y = 1.0f;
		_spawnLocalPosition.z = ColliderUtil.GetRadius(collider) * -BackNwayLocalRadiusScale;

		if (_initializedCreateCount == 0)
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

		// lock spawn position
		Vector3 spawnPosition = _parentActor.cachedTransform.TransformPoint(_spawnLocalPosition);
		for (int i = 0; i < _initializedCreateCount; ++i)
		{
			// only local back
			float centerAngleY = cachedTransform.rotation.eulerAngles.y + 180.0f;
			float baseAngle = _initializedCreateCount % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

			Generate(spawnPosition, Quaternion.Euler(0.0f, angle, 0.0f), true);
		}
		gameObject.SetActive(false);
	}
}