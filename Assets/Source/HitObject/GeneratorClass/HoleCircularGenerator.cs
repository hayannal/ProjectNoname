using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class HoleCircularGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("HoleCircularGenerator")]
	public int circularSectorCount = 1;
	public float interval = 0.1f;

	public float betweenAngle;
	public bool useWorldSpaceDirection;
	public float worldSpaceCenterAngleY;
	public bool useHole;
	public float holeCenterAngle;
	public float holeSize;
	public bool useRandomSequenceHole;
	public float[] holeCenterAngleList;

	int _remainCreateCount;
	float _remainIntervalTime;
	float _actorRadius;
	List<float> _listRandomSequenceHole;
	int _sequenceIndex;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainIntervalTime = 0.0f;
		Collider collider = parentActor.GetCollider();
		_actorRadius = ColliderUtil.GetRadius(collider);

		if (useRandomSequenceHole)
		{
			if (_listRandomSequenceHole == null)
			{
				_listRandomSequenceHole = new List<float>();
				for (int i = 0; i < holeCenterAngleList.Length; ++i)
					_listRandomSequenceHole.Add(holeCenterAngleList[i]);
			}
			ObjectUtil.Shuffle<float>(_listRandomSequenceHole);
			_sequenceIndex = 0;
		}

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

		if (_parentActor.actorStatus.IsDie())
		{
			gameObject.SetActive(false);
			return;
		}

		if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

		_remainIntervalTime -= Time.deltaTime;
		if (_remainIntervalTime < 0.0f)
		{
			_remainCreateCount -= 1;
			_remainIntervalTime += interval;

			float holeStartAngle = 0.0f;
			float holeEndAngle = 0.0f;
			if (useHole)
			{
				holeStartAngle = holeCenterAngle - (holeSize * 0.5f);
				holeEndAngle = holeCenterAngle + (holeSize * 0.5f);
			}
			if (useRandomSequenceHole)
			{
				float randomHoleCenterAngle = _listRandomSequenceHole[_sequenceIndex++];
				if (_sequenceIndex >= _listRandomSequenceHole.Count) _sequenceIndex = 0;
				holeStartAngle = randomHoleCenterAngle - (holeSize * 0.5f);
				holeEndAngle = randomHoleCenterAngle + (holeSize * 0.5f);
			}
			if (useHole || useRandomSequenceHole)
			{
				if (useWorldSpaceDirection == false)
				{
					holeStartAngle += cachedTransform.rotation.eulerAngles.y;
					holeEndAngle += cachedTransform.rotation.eulerAngles.y;
				}
			}
			for (int i = 0; i < circularSectorCount; ++i)
			{
				float centerAngleY = useWorldSpaceDirection ? worldSpaceCenterAngleY : cachedTransform.rotation.eulerAngles.y;
				float baseAngle = circularSectorCount % 2 == 0 ? centerAngleY - (betweenAngle / 2f) : centerAngleY;
				float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);

				if ((useHole || useRandomSequenceHole) && holeStartAngle <= angle && angle <= holeEndAngle)
					continue;

				Generate(cachedTransform.position, Quaternion.Euler(0.0f, angle, 0.0f), true);
			}

			_remainCreateCount -= circularSectorCount;
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}