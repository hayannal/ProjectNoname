using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class RandomForwardAreaGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("RandomForwardAreaGenerator")]
	public float interval;

	int _remainCreateCount;
	float _remainIntervalTime;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainIntervalTime = 0.0f;

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
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

		// 엄청 단순한 랜덤 각도 제네레이터인데 Collider타입에서만 되던거라서 Area용으로 별도로 만들어서 쓰기로 한다.
		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;

			Quaternion randomRotation = Quaternion.Euler(0.0f, Random.Range(-180.0f, 180.0f), 0.0f) * cachedTransform.rotation;
			Generate(cachedTransform.position, randomRotation, true);
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}