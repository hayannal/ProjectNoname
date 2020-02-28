using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using MEC;

public class RandomAreaGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("RandomAreaGenerator")]
	public float randomDistance;
	public float interval;
	public bool firstGenNoRandom;

	public MeAttackIndicator meAttackIndicatorReference;
	public float showIndicatorTimeAdjust = 0.05f;

	int _remainCreateCount;
	float _remainIntervalTime;
	bool _useAttackIndicator;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainIntervalTime = 0.0f;
		_useAttackIndicator = (meAttackIndicatorReference != null && meAttackIndicatorReference.overrideLifeTime > 0.0f);
		_generatedCount = 0;

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
		if (_remainIntervalTime < 0.0f && _remainCreateCount > 0)
		{
			_remainIntervalTime += interval;
			int index = (_initializedCreateCount - _remainCreateCount);

			Vector3 position = cachedTransform.position;
			if (index > (firstGenNoRandom ? 0 : -1))
			{
				Vector2 offset = Random.insideUnitCircle * randomDistance;
				position.x += offset.x;
				position.z += offset.y;
			}

			// check Quad Position
			bool ignoreCreate = false;
			//if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.IsInQuadBound(position) == false)
			//	ignoreCreate = true;

			if (ignoreCreate == false)
			{
				if (_useAttackIndicator)
				{
					meAttackIndicatorReference.InitializeForGenerator(position, cachedTransform.rotation, cachedTransform);
					Timing.RunCoroutine(DelayedGenerate(meAttackIndicatorReference.overrideLifeTime + showIndicatorTimeAdjust, position));
				}
				else
					Generate(position);
			}

			--_remainCreateCount;
		}

		//if (_remainCreateCount <= 0)
		//	gameObject.SetActive(false);
	}

	IEnumerator<float> DelayedGenerate(float delayTime, Vector3 position)
	{
		yield return Timing.WaitForSeconds(delayTime);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		Generate(position);
	}

	int _generatedCount;
	void Generate(Vector3 position)
	{
		Generate(position, cachedTransform.rotation, true);
		++_generatedCount;
		if (_initializedCreateCount == _generatedCount)
			gameObject.SetActive(false);
	}
}