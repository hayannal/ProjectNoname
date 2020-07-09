using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class ArcFormGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("ArcFormGenerator")]
	public float betweenAngle = 15.0f;
	public int delayedAdditionalCreateCount;
	public float delay;

	bool _createdInitialArcForm;
	int _additionalCreateCount;
	float _remainDelayTime;
	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_createdInitialArcForm = false;
		_additionalCreateCount = 0;
		_remainDelayTime = 0.0f;

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

		if (_remainDelayTime > 0.0f)
		{
			if (_parentActor.actorStatus.IsDie())
			{
				gameObject.SetActive(false);
				return;
			}

			if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
				return;

			_remainDelayTime -= Time.deltaTime;
			if (_remainDelayTime <= 0.0f)
			{
				CreateArcForm(_additionalCreateCount, betweenAngle);
				gameObject.SetActive(false);
			}
			return;
		}

		if (_createdInitialArcForm == false)
		{
			CreateArcForm(_initializedCreateCount, betweenAngle);
			_createdInitialArcForm = true;
		}

		int count = delayedAdditionalCreateCount;
		float delay = this.delay;
		ArcFormHitObjectAffector.GetInfo(_parentActor.affectorProcessor, ref count, ref delay);
		if (count > 0 && delay > 0.0f)
		{
			_additionalCreateCount = count;
			_remainDelayTime = delay;
		}
		else
			gameObject.SetActive(false);
	}

	void CreateArcForm(int count, float betweenAngle)
	{
		for (int i = 0; i < count; ++i)
		{
			float centerAngleY = cachedTransform.eulerAngles.y;
			float baseAngle = count % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

			HitObject hitObject = Generate(cachedTransform.position, Quaternion.Euler(0.0f, angle, 0.0f), true);
			if (hitObject != null)
				hitObject.cachedTransform.Translate(0.0f, 0.0f, 0.5f, Space.Self);
		}
	}
}