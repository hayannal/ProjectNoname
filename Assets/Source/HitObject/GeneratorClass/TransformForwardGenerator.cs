using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class TransformForwardGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("TransformForwardGenerator")]
	public float interval;
	public bool applyAttackSpeedAddRate;

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
		if (CheckChangeState())
		{
			gameObject.SetActive(false);
			return;
		}

		if (moveType == eMoveType.None)
		{
			if (_parentActor.actorStatus.IsDie())
			{
				gameObject.SetActive(false);
				return;
			}

			if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
				return;
		}
		else
		{
			if (UpdateMove())
			{
				gameObject.SetActive(false);
				return;
			}
		}

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;

			float resultInterval = interval;
			if (applyAttackSpeedAddRate)
			{
				float addRate = _parentActor.actorStatus.GetValue(eActorStatus.AttackSpeedAddRate);
				if (addRate > 0.0f)
					resultInterval /= (1.0f + addRate);
			}
			_remainIntervalTime += resultInterval;

			Generate(cachedTransform.position, cachedTransform.rotation);
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}