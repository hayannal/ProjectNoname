using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinuousHitObjectGeneratorBase : MonoBehaviour
{
	public bool disableOnChangeState = true;
	public bool ignoreMainHitObject = true;
	public bool attachChild = true;
	public int createCount;
	protected int _initializedCreateCount;

	int _fullPathHash;

	protected MeHitObject _signal;
	protected Actor _parentActor;
	protected int _hitSignalIndexInAction;
	int _repeatIndex;
	int _repeatAddCountByLevelPack;

	public virtual void InitializeGenerator(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		_signal = meHit;
		_parentActor = parentActor;
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
		Quaternion rotation = Quaternion.LookRotation(HitObject.GetSpawnDirection(position, _signal, cachedTransform, targetPosition, _parentActor.targetingProcessor));
		return Generate(position, rotation);
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

		hitObject.InitializeHitObject(_signal, _parentActor, _hitSignalIndexInAction, _repeatIndex, _repeatAddCountByLevelPack);
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
