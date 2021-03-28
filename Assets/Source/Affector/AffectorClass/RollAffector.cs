using UnityEngine;
using System.Collections;
using ActorStatusDefine;
using ECM.Common;

public class RollAffector : AffectorBase
{
	float _endTime;

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

		_moveDirection = _actor.cachedTransform.forward;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 원래라면 절대 들어오지 말아야하는데 러쉬중에 끝나지도 않았는데 러쉬가 또 온거다.
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated Roll Affector.");
	}

	Vector3 _moveDirection = Vector3.forward;
	public override void UpdateAffector()
	{
		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		{
			if (_endTime > 0.0f)
				_endTime += Time.deltaTime;
			return;
		}

		if (CheckEndTime(_endTime) == false)
			return;

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		Vector3 moveDirection = new Vector3
		{
			x = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Horizontal"),
			y = 0.0f,
			z = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Vertical")
		};

		if (moveDirection.sqrMagnitude > 0.0f)
			_moveDirection = moveDirection;

		_moveDirection = _moveDirection.relativeTo(cameraTransform);
		_actor.GetRigidbody().MoveRotation(Quaternion.LookRotation(_moveDirection));
	}

	public override void FixedUpdateAffector()
	{
		if (_actor.GetRigidbody() == null)
			return;

		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction) || _actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
		{
			_actor.GetRigidbody().velocity = Vector3.zero;
			return;
		}

		_actor.GetRigidbody().velocity = _actor.cachedTransform.forward * _affectorValueLevelTableData.fValue2;
	}

	public override void FinalizeAffector()
	{
		if (_actor.GetRigidbody() != null)
			_actor.GetRigidbody().velocity = Vector3.zero;
		if (_actor.actorStatus.IsDie())
			return;
		if (string.IsNullOrEmpty(_affectorValueLevelTableData.sValue1))
			return;
		_actor.actionController.animator.CrossFade(_affectorValueLevelTableData.sValue1, 0.05f);
	}


	// From LocalPlayerController
	Transform _cameraTransform;
	public Transform cameraTransform
	{
		get
		{
			if (_cameraTransform != null)
				return _cameraTransform;
			Camera mainCamera = UIInstanceManager.instance.GetCachedCameraMain();
			if (mainCamera != null)
				_cameraTransform = mainCamera.transform;
			return _cameraTransform;
		}
	}
}