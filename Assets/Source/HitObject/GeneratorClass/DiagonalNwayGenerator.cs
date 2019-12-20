using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class DiagonalNwayGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("DiagonalNwayGenerator")]
	public float betweenAngle = 5.0f;

	Vector3 _spawnLocalLeftDiagonalPosition = Vector3.zero;
	Vector3 _spawnLocalRightDiagonalPosition = Vector3.zero;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		Collider collider = parentActor.GetCollider();
		Vector3 diagonal = new Vector3(-1.0f, 0.0f, 1.0f);
		diagonal = diagonal.normalized * ColliderUtil.GetRadius(collider);
		_spawnLocalLeftDiagonalPosition.x = diagonal.x * -BackNwayGenerator.BackNwayLocalRadiusScale;
		_spawnLocalLeftDiagonalPosition.y = 1.0f;
		_spawnLocalLeftDiagonalPosition.z = diagonal.z * BackNwayGenerator.BackNwayLocalRadiusScale;
		diagonal = new Vector3(1.0f, 0.0f, 1.0f);
		diagonal = diagonal.normalized * ColliderUtil.GetRadius(collider);
		_spawnLocalRightDiagonalPosition.x = diagonal.x * BackNwayGenerator.BackNwayLocalRadiusScale;
		_spawnLocalRightDiagonalPosition.y = 1.0f;
		_spawnLocalRightDiagonalPosition.z = diagonal.z * BackNwayGenerator.BackNwayLocalRadiusScale;

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
		Vector3 spawnPosition = _parentActor.cachedTransform.TransformPoint(_spawnLocalLeftDiagonalPosition);
		for (int i = 0; i < _initializedCreateCount; ++i)
		{
			// only local back
			float centerAngleY = cachedTransform.rotation.eulerAngles.y - 45.0f;
			float baseAngle = _initializedCreateCount % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

			Generate(spawnPosition, Quaternion.Euler(0.0f, angle, 0.0f), true);
		}

		spawnPosition = _parentActor.cachedTransform.TransformPoint(_spawnLocalRightDiagonalPosition);
		for (int i = 0; i < _initializedCreateCount; ++i)
		{
			// only local back
			float centerAngleY = cachedTransform.rotation.eulerAngles.y + 45.0f;
			float baseAngle = _initializedCreateCount % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

			Generate(spawnPosition, Quaternion.Euler(0.0f, angle, 0.0f), true);
		}
		gameObject.SetActive(false);
	}
}