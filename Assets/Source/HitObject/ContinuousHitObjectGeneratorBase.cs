﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class ContinuousHitObjectGeneratorBase : MonoBehaviour
{
	public enum eMoveType
	{
		None,
		Forward,
	}

	public bool disableOnChangeState = true;
	public bool ignoreMainHitObject = true;
	public bool attachChild = true;
	public int createCount;
	public eMoveType moveType = eMoveType.None;
	public float moveSpeed;
	public bool checkQuadBound = true;
	protected int _initializedCreateCount;

	int _fullPathHash;

	protected MeHitObject _signal;
	protected Actor _parentActor;
	protected StatusBase _statusBase;
	protected int _hitSignalIndexInAction;
	int _repeatIndex;
	int _repeatAddCountByLevelPack;

	public virtual void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		_signal = meHit;
		_parentActor = parentActor;
		_statusBase = statusBase;
		_hitSignalIndexInAction = hitSignalIndexInAction;
		_repeatIndex = repeatIndex;
		_repeatAddCountByLevelPack = repeatAddCountByLevelPack;
		_initializedCreateCount = createCount + AddGeneratorCreateCountAffector.GetAddCount(parentActor.affectorProcessor);

		_fullPathHash = 0;
		if (disableOnChangeState)
		{
			if (parentActor.actionController.animator != null)
				_fullPathHash = parentActor.actionController.animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
		}
		cachedTransform.parent = attachChild ? spawnTransform : null;
	}

	
	protected HitObject DuplicateHitObject()
	{
		// 완벽한 복제를 위해선 spawnTransform 및 parentTransform까지 가져와야한다.
		//Vector3 targetPosition = HitObject.GetTargetPosition(_signal, _parentActor, _hitSignalIndexInAction);
		//Vector3 defaultPosition = HitObject.GetSpawnPosition(_spawnTransform, _signal, _parentTransform);
		//Quaternion defaultRotation = Quaternion.LookRotation(HitObject.GetSpawnDirection(defaultPosition, _signal, _parentTransform, targetPosition));
		//
		// 위 코드가 제일 정확하긴 한데
		// 사실 발생기는 발생을 위임받은거라 저렇게 발사하는 액터의 트랜스폼을 갖는게 구조적으로 이상해보인다.
		// 그래서 원래 의미대로 발생기의 포지션에서 발생하며
		// 각도만 현재 타겟에 따라 달라질 수 있게 바꿔본다.
		// AttachChild해놨으면 알아서 밀려나갈때 발생기도 밀려날거고 그렇지 않다면 생성된 자리에서 만들어낼거다.
		Vector3 targetPosition = HitObject.GetTargetPosition(_signal, _parentActor, _hitSignalIndexInAction);
		Vector3 position = cachedTransform.position;
		Vector3 spawnDirection = HitObject.GetSpawnDirection(position, cachedTransform, _signal, cachedTransform, targetPosition, _parentActor.targetingProcessor);
		if (attachChild && _signal.createPositionType == HitObject.eCreatePositionType.Bone)
		{
			// attachChild 켜고 Bone 켜면 특정 본 포지션에다 넣어두고 쭉 생성하는건데 이땐 몇몇 예외처리들이 있어야 제대로 동작이 된다.
			// 안그러면 땅 아래로 보낸다거나 하늘로 보낸다거나 하는 현상이 나타난다.
			if (_signal.fixedWorldPositionY)
			{
				position.y = _signal.offset.y;
			}
			if (spawnDirection.y != 0.0f)
			{
				spawnDirection.y = 0.0f;
				spawnDirection = spawnDirection.normalized;
			}
		}
		Quaternion rotation = Quaternion.LookRotation(spawnDirection);
		return Generate(position, rotation);
	}

	// 스파이럴이나 페인트 제네레이터는 새로운 position과 rotation을 계산해서 히트오브젝트를 만들어낸다. 시그널값 무시.
	protected HitObject Generate(Vector3 position, Quaternion rotation, bool useInitializedStatusBase = false)
	{
		HitObject hitObject = HitObject.GetCachedHitObject(_signal, position, rotation);
		if (hitObject == null)
			return null;

		StatusBase statusBase = null;
		if (useInitializedStatusBase)
		{
			// 제네레이터는 기본적으로 시간을 두고 발사하는 것들이라 디폴트는 parentActor에서 항상 새로 스탯을 뽑아내는거고,
			// 간혹 일부 동시간에 여러개 발사하는거만 초기화때 전달받은 스탯을 사용한다.
			statusBase = _statusBase;
		}
		else
		{
			statusBase = new StatusBase();
			_parentActor.actorStatus.CopyStatusBase(ref statusBase);
		}
		hitObject.InitializeHitObject(_signal, _parentActor, statusBase, 0.0f, _hitSignalIndexInAction, _repeatIndex, _repeatAddCountByLevelPack);
		return hitObject;
	}

	protected bool CheckChangeState()
	{
		if (disableOnChangeState == false)
			return false;
		if (_parentActor.actionController.animator == null)
			return false;
		if (_parentActor.actionController.animator.IsInTransition(0))
			return true;
		if (_parentActor.actionController.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == _fullPathHash)
			return false;

		return true;
	}

	protected bool UpdateMove()
	{
		if (moveType == eMoveType.None)
			return false;

		switch (moveType)
		{
			case eMoveType.Forward:
				cachedTransform.position += cachedTransform.forward * moveSpeed * Time.deltaTime;
				break;
		}

		// 이동 후 맵 밖으로 나갔는지 확인해야한다.
		if (checkQuadBound)
		{
			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.IsInQuadBound(cachedTransform.position) == false)
				return true;
		}

		return false;
	}





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
