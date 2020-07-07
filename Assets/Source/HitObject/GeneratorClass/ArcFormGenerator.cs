using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class ArcFormGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("ArcFormGenerator")]
	public float betweenAngle = 15.0f;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

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

		for (int i = 0; i < _initializedCreateCount; ++i)
		{
			float centerAngleY = cachedTransform.eulerAngles.y;
			float baseAngle = _initializedCreateCount % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

			HitObject hitObject = Generate(cachedTransform.position, Quaternion.Euler(0.0f, angle, 0.0f), true);
			if (hitObject != null)
				hitObject.cachedTransform.Translate(0.0f, 0.0f, 0.5f, Space.Self);
		}
		gameObject.SetActive(false);
	}
}