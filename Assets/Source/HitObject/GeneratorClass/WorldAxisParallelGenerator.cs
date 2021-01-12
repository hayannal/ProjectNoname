using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class WorldAxisParallelGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("WorldAxisParallelGenerator")]
	public bool xOrZ = false;
	public float betweenDistance = 0.5f;
	public int randomAdditionalCount;

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

		int totalCount = _initializedCreateCount + Random.Range(0, randomAdditionalCount + 1);
		for (int i = 0; i < totalCount; ++i)
		{
			Vector3 parallelOffset = Vector3.zero;
			float offset = ((totalCount - 1) * 0.5f * betweenDistance) * -1.0f + betweenDistance * i;
			if (xOrZ)
				parallelOffset.x = offset;
			else
				parallelOffset.z = offset;

			Vector3 offsetPosition = cachedTransform.TransformPoint(parallelOffset);
			Generate(offsetPosition, cachedTransform.rotation, true);
		}
		gameObject.SetActive(false);
	}
}