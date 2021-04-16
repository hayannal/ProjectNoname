using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using MEC;

public class AreaLineGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("AreaLineGenerator")]
	public float betweenDistance;

	public MeAttackIndicator meAttackIndicatorReference;
	public float showIndicatorTimeAdjust = 0.05f;

	int _remainCreateCount;
	bool _useAttackIndicator;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_useAttackIndicator = (meAttackIndicatorReference != null && meAttackIndicatorReference.overrideLifeTime > 0.0f);
		_wait = false;

		transform.eulerAngles = new Vector3(0.0f, Random.Range(0.0f, 360.0f), 0.0f);

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	bool _wait;
	void Update()
	{
		if (_wait)
			return;

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

		if (_useAttackIndicator)
		{
			for (int i = 0; i < _remainCreateCount; ++i)
			{
				Vector3 position = cachedTransform.TransformPoint(new Vector3(0.0f, 0.0f, (i - ((_remainCreateCount - 1) * 0.5f)) * betweenDistance));
				meAttackIndicatorReference.InitializeForGenerator(position, cachedTransform.rotation, null);	
			}
			Timing.RunCoroutine(DelayedGenerate(meAttackIndicatorReference.overrideLifeTime + showIndicatorTimeAdjust));
			_wait = true;
		}
		else
		{
			GenerateHitObjectList();
		}
	}

	IEnumerator<float> DelayedGenerate(float delayTime)
	{
		yield return Timing.WaitForSeconds(delayTime);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		GenerateHitObjectList();
	}

	void GenerateHitObjectList()
	{
		for (int i = 0; i < _remainCreateCount; ++i)
		{
			HitObject hitObject = Generate(cachedTransform.position, cachedTransform.rotation);
			if (hitObject != null)
				hitObject.cachedTransform.Translate(0.0f, 0.0f, (i - ((_remainCreateCount - 1) * 0.5f)) * betweenDistance, Space.Self);
		}
		gameObject.SetActive(false);
	}
}