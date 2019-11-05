using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftRightNwayGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("LeftRightNwayGenerator")]
	public float betweenAngle = 5.0f;

	Vector3 _spawnLocalLeftPosition = Vector3.zero;
	Vector3 _spawnLocalRightPosition = Vector3.zero;

	void OnEnable()
	{
		if (createCount == 0)
			gameObject.SetActive(false);
	}

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		Collider collider = parentActor.GetCollider();
		_spawnLocalLeftPosition.x = ColliderUtil.GetRadius(collider) * -BackNwayGenerator.BackNwayLocalRadiusScale;
		_spawnLocalLeftPosition.y = 1.0f;
		_spawnLocalRightPosition.x = ColliderUtil.GetRadius(collider) * BackNwayGenerator.BackNwayLocalRadiusScale;
		_spawnLocalRightPosition.y = 1.0f;
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
		Vector3 spawnPosition = _parentActor.cachedTransform.TransformPoint(_spawnLocalLeftPosition);
		for (int i = 0; i < createCount; ++i)
		{
			// only local back
			float centerAngleY = cachedTransform.rotation.eulerAngles.y - 90.0f;
			float baseAngle = createCount % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

			Generate(spawnPosition, Quaternion.Euler(0.0f, angle, 0.0f));
		}

		spawnPosition = _parentActor.cachedTransform.TransformPoint(_spawnLocalRightPosition);
		for (int i = 0; i < createCount; ++i)
		{
			// only local back
			float centerAngleY = cachedTransform.rotation.eulerAngles.y + 90.0f;
			float baseAngle = createCount % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

			Generate(spawnPosition, Quaternion.Euler(0.0f, angle, 0.0f));
		}
		gameObject.SetActive(false);
	}
}