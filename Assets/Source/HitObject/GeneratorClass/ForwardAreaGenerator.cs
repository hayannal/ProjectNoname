using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class ForwardAreaGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("ForwardAreaGenerator")]
	public float betweenDistance;
	public float interval;
	public float startDelay;

	int _remainCreateCount;
	float _remainIntervalTime;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainIntervalTime = startDelay;

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		// Area라서 Die체크 하지 않는다. 이미 발사되었다고 판단하는거다.
		//if (CheckChangeState())
		//{
		//	gameObject.SetActive(false);
		//	return;
		//}
		//if (_parentActor.actorStatus.IsDie())
		//{
		//	gameObject.SetActive(false);
		//	return;
		//}
		//if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		//	return;

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;
			int index = (_initializedCreateCount - _remainCreateCount) - 1;

			Vector3 position = cachedTransform.position;
			if (index > 0)
				position += cachedTransform.forward * (betweenDistance * index);

			// check Quad Position
			bool ignoreCreate = false;
			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.IsInQuadBound(position) == false)
				ignoreCreate = true;

			if (ignoreCreate == false)
				Generate(position, cachedTransform.rotation, true);
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}