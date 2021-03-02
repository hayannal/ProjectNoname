using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using MEC;

public class ChildTransformGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("ChildTransformGenerator")]
	public Transform[] childTransformList;
	public bool followParentTransform;

	int _remainCreateCount;

	List<HitObject> _listHitObject = new List<HitObject>();

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = childTransformList.Length;

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);

		if (followParentTransform)
			FollowTransform.Follow(cachedTransform, parentActor.cachedTransform, Vector3.zero);
	}

	// Update is called once per frame
	void Update()
	{
#if UNITY_EDITOR
		// 배틀씬에서는 ParentActor가 삭제될 수 있으니 이렇게 예외처리 해준다.
		if (_parentActor == null)
		{
			FinalizeHitObject();
			return;
		}
#endif

		if (CheckChangeState())
		{
			FinalizeHitObject();
			return;
		}

		if (moveType == eMoveType.None)
		{
			// 이 제네레이터만 부활 도중에 생성해야해서 이렇게 처리한다.
			if (_parentActor.actorStatus.IsDie() && ResurrectAffector.IsProcessingResurrect(_parentActor.affectorProcessor) == false)
			{
				FinalizeHitObject();
				return;
			}

			//if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			//	return;
		}
		else
		{
			if (UpdateMove())
			{
				FinalizeHitObject();
				return;
			}
		}

		if (_remainCreateCount == 0 && _listHitObject.Count > 0)
		{
			bool removed = false;
			for (int i = _listHitObject.Count - 1; i >= 0; --i)
			{
				if (_listHitObject[i].gameObject.activeSelf)
					continue;

				_listHitObject.Remove(_listHitObject[i]);
				removed = true;
			}
			if (removed && _listHitObject.Count == 0)
				gameObject.SetActive(false);
		}

		if (_remainCreateCount > 0)
		{
			for (int i = 0; i < childTransformList.Length; ++i)
			{
				HitObject hitObject = Generate(childTransformList[i].position, childTransformList[i].rotation, true);
				hitObject.cachedTransform.parent = childTransformList[i];
				_listHitObject.Add(hitObject);
				_remainCreateCount -= 1;
			}
		}
	}

	void FinalizeHitObject()
	{
		// 히트오브젝트들이 없어지기 전에 상위 부모인 제네레이터를 그냥 꺼버리면 HitObject를 재사용할 수 없게되서 만들어진게 있다면 먼저 삭제 처리를 해줘야한다.
		for (int i = 0; i < _listHitObject.Count; ++i)
		{
			if (_listHitObject[i] == null)
				continue;

			_listHitObject[i].FinalizeHitObject(true);
		}
		_listHitObject.Clear();
		gameObject.SetActive(false);
	}
}