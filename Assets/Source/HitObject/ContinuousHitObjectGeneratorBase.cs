using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousHitObjectGeneratorBase : MonoBehaviour
{
	public bool disableOnChangeState;
	public bool ignoreMainHitObject;
	public bool attachChild;
	public int createCount;

	int _fullPathHash;

	MeHitObject _signal;
	Actor _parentActor;
	int _hitSignalIndexInAction;
	int _repeatIndex;
	Transform _spawnTransform;
	Transform _parentTransform;

	public virtual void InitializeGenerator(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction, int repeatIndex, Transform spawnTransform, Transform parentTransform)
	{
		_signal = meHit;
		_parentActor = parentActor;
		_hitSignalIndexInAction = hitSignalIndexInAction;
		_repeatIndex = repeatIndex;
		_spawnTransform = spawnTransform;
		_parentTransform = parentTransform;

		_fullPathHash = 0;
		if (disableOnChangeState)
		{
			if (parentActor.actionController.animator != null)
				_fullPathHash = parentActor.actionController.animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
		}

		if (attachChild)
		{

		}
	}

	// 완벽한 복제를 위해선 spawnTransform 및 parentTransform까지 가져와야한다.
	protected HitObject DuplicateHitObject()
	{
		Vector3 targetPosition = HitObject.GetTargetPosition(_signal, _parentActor, _hitSignalIndexInAction);
		Vector3 defaultPosition = HitObject.GetSpawnPosition(_spawnTransform, _signal, _parentTransform);
		Quaternion defaultRotation = Quaternion.LookRotation(HitObject.GetSpawnDirection(defaultPosition, _signal, _parentTransform, targetPosition));
		return Generate(defaultPosition, defaultRotation);
	}

	// 본체 기준에서 만들어내는 Generate
	protected HitObject Generate()
	{
		return Generate(cachedTransform.position, cachedTransform.rotation);
	}

	// 스파이럴이나 페인트 제네레이터는 새로운 position과 rotation을 계산해서 히트오브젝트를 만들어낸다. 시그널값 무시.
	protected HitObject Generate(Vector3 position, Quaternion rotation)
	{
		HitObject hitObject = HitObject.GetCachedHitObject(_signal, position, rotation);
		if (hitObject == null)
			return null;

		hitObject.InitializeHitObject(_signal, _parentActor, _hitSignalIndexInAction, _repeatIndex);
		return hitObject;
	}

	protected bool CheckChangeState()
	{
		if (disableOnChangeState == false)
			return false;
		if (_parentActor.actionController.animator == null)
			return false;
		if (_parentActor.actionController.animator.GetCurrentAnimatorStateInfo(0).fullPathHash == _fullPathHash)
			return false;

		return true;
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
