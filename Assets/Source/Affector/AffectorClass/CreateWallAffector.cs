using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CreateWallAffector : AffectorBase
{
	float _endTime;

	const float OffsetY = 1.0f;

	public static int TEAM0_BARRIER_LAYER;

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

		// 지금은 플레이어만 이 어펙터를 사용하기 때문에 Tema0꺼만 처리해둔다.
		// 혹시 몬스터가 쓰게되면 이거 확장해야한다.
		if (TEAM0_BARRIER_LAYER == 0) TEAM0_BARRIER_LAYER = LayerMask.NameToLayer("Team0Barrier");
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
		if (_nextCreateTime < Time.time)
		{
			// 스왑 전에 얻은 어펙터가 다시 켜진거다. 예외처리 해준다.
			while (true)
			{
				_nextCreateTime += _affectorValueLevelTableData.fValue2;
				if (_nextCreateTime > Time.time)
					break;
			}
		}
	}

	public override void FinalizeAffector()
	{
		if (_wallTransformList == null)
			return;

		for (int i = 0; i < _wallTransformList.Length; ++i)
		{
			if (_wallTransformList[i] == null)
				continue;
			if (_wallTransformList[i].gameObject == null)
				continue;
			if (_wallTransformList[i].gameObject.activeSelf == false)
				continue;
			_wallTransformList[i].gameObject.SetActive(false);
			_wallTransformList[i] = null;
			_wallEndTimeList[i] = 0.0f;
		}
	}

	public override void DisableAffector()
	{
		// 현재 보여지고 있는 것들만 꺼두면 다음에 다시 켜질때 알아서 켜질거다.
		FinalizeAffector();
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

		if (newTransform.gameObject.layer != TEAM0_BARRIER_LAYER)
			ObjectUtil.ChangeLayer(newTransform.gameObject, TEAM0_BARRIER_LAYER);
	}

	Vector3 GetDirection(int index)
	{
		Vector3 direction = Vector3.forward;
		switch (index)
		{
			case 0: direction = (Vector3.forward + Vector3.right).normalized; break;
			case 1: direction = (Vector3.right + Vector3.back).normalized; break;
			case 2: direction = (Vector3.back + Vector3.left).normalized; break;
			case 3: direction = (Vector3.left + Vector3.forward).normalized; break;
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

			_wallTransformList[i].position = _actor.cachedTransform.position + GetDirection(i) + new Vector3(0.0f, OffsetY, 0.0f);

			if (_wallEndTimeList[i] > 0.0f && Time.time > _wallEndTimeList[i])
			{
				_wallTransformList[i].gameObject.SetActive(false);
				_wallTransformList[i] = null;
				_wallEndTimeList[i] = 0.0f;
			}
		}
	}
}