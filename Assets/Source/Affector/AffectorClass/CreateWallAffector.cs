using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CreateWallAffector : AffectorBase
{
	float _endTime;

	const float OffsetY = 1.0f;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_nextCreateTime = Time.time + affectorValueLevelTableData.fValue2;
	}

	int _directionIndex = 0;
	float _nextCreateTime = 0.0f;
	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		UpdateWallPosition();

		if (Time.time < _nextCreateTime)
			return;

		CreateWall();

		++_directionIndex;
		if (_directionIndex >= 4)
			_directionIndex = 0;

		_nextCreateTime += _affectorValueLevelTableData.fValue2;
	}

	GameObject _wallPrefab;
	Transform[] _wallTransformList;
	float[] _wallEndTimeList;
	void CreateWall()
	{
		if (_wallPrefab == null)
			_wallPrefab = FindPreloadObject(_affectorValueLevelTableData.sValue1);
		if (_wallPrefab == null)
			return;

		if (_wallTransformList == null)
		{
			_wallTransformList = new Transform[4];
			_wallEndTimeList = new float[4];
		}

		if (_wallTransformList[_directionIndex] != null && _wallTransformList[_directionIndex].gameObject.activeSelf)
			_wallTransformList[_directionIndex].gameObject.SetActive(false);

		Vector3 direction = GetDirection(_directionIndex);
		Vector3 position = _actor.cachedTransform.position + direction + new Vector3(0.0f, OffsetY, 0.0f);
		Quaternion rotation = Quaternion.LookRotation(direction);
		Transform newTransform = BattleInstanceManager.instance.GetCachedObject(_wallPrefab, position, rotation).transform;
		_wallTransformList[_directionIndex] = newTransform;
		_wallEndTimeList[_directionIndex] = Time.time + _affectorValueLevelTableData.fValue3;
	}

	Vector3 GetDirection(int index)
	{
		Vector3 direction = Vector3.forward;
		switch (index)
		{
			case 0: direction = Vector3.forward; break;
			case 1: direction = Vector3.right; break;
			case 2: direction = Vector3.back; break;
			case 3: direction = Vector3.left; break;
		}
		return direction;
	}

	void UpdateWallPosition()
	{
		if (_wallTransformList == null)
			return;

		for (int i = 0; i < _wallTransformList.Length; ++i)
		{
			if (_wallTransformList[i] == null)
				continue;
			if (_wallTransformList[i].gameObject.activeSelf == false)
				continue;
			if (_wallEndTimeList[i] == 0.0f)
				continue;

			_wallTransformList[i].position = _actor.cachedTransform.position + GetDirection(i) + new Vector3(0.0f, OffsetY, 0.0f);

			if (Time.time > _wallEndTimeList[i])
			{
				_wallTransformList[i].gameObject.SetActive(false);
				_wallEndTimeList[i] = 0.0f;
			}
		}
	}
}