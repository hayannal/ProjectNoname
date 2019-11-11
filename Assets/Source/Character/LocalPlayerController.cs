using ECM.Common;
using ECM.Controllers;
using UnityEngine;
using MecanimStateDefine;

public sealed class LocalPlayerController : BaseCharacterController
{
	#region EDITOR EXPOSED FIELDS

	[Tooltip("Layers to be considered as ground (picking). Used by ground click detection.")]
	[SerializeField]
	public LayerMask groundMask = 1;            // Default layer

	#endregion

	#region FIELDS

	Actor _actor;
	ActionController _actionController;
	Transform _cameraTransform;
	float _actorTableAttackRange;

	#endregion

	#region PROPERTIES

	/// <summary>
	/// The character's walk speed.
	/// </summary>
	/// 

	public Actor actor
	{
		get
		{
			if (_actor != null)
				return _actor;
			_actor = GetComponent<Actor>();
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actor.actorId);
			if (actorTableData != null)
				_actorTableAttackRange = actorTableData.attackRange;
			return _actor;
		}
	}

	public ActionController actionController
	{
		get
		{
			if (_actionController != null)
				return _actionController;
			_actionController = GetComponent<ActionController>();
			return _actionController;
		}
	}

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

	#endregion

	#region METHODS

	/// <summary>
	/// Calculate the desired movement velocity.
	/// Eg: Convert the input (moveDirection) to movement velocity vector,
	///     use navmesh agent desired velocity, etc.
	/// </summary>

	public bool dontMove { get; set; }
	protected override Vector3 CalcDesiredVelocity()
	{
		if (dontMove)
			return Vector3.zero;

		return base.CalcDesiredVelocity();
	}

	/// <summary>
	/// Overrides 'BaseCharacterController' Animate method.
	/// 
	/// This shows how to handle your characters' animation states using the Animate method.
	/// The use of this method is optional, for example you can use a separate script to manage your
	/// animations completely separate of movement controller.
	/// 
	/// </summary>

	int _clearCustomTargetWaitCount = 0;
	bool _standbyClearCustomTarget = false;
	protected override void Animate()
	{
		// If no animator, return

		if (animator == null)
			return;
		if (actor.actorStatus.IsDie())
			return;

		// Compute move vector in local space - not needed

		//var move = transform.InverseTransformDirection(moveDirection);

		// Update the animator parameters

		var moveAmount = moveDirection.sqrMagnitude;
		//animator.SetFloat("Move", moveAmount, 0.1f, Time.deltaTime);

		if (moveAmount > 0.0f)
		{
			actionController.PlayActionByActionName("Move");

			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null)
				TitleCanvas.instance.FadeTitle();
		}
		else
		{
			if (actionController.mecanimState.IsState((int)eMecanimState.Move))
				actionController.PlayActionByActionName("Idle");
		}

		if (ScreenJoystick.instance.CheckInput(Control.eInputType.Tab))
		{
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null)
				TitleCanvas.instance.FadeTitle();

			Ray ray = UIInstanceManager.instance.GetCachedCameraMain().ScreenPointToRay(ScreenJoystick.instance.tabPosition);
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, groundMask.value))
			{
				Vector3 targetPosition = hitInfo.point;
				Collider targetCollider = null;

				if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
				{
					if (hitInfo.collider != null && hitInfo.collider.gameObject == GatePillar.instance.meshColliderObject)
					{
						targetPosition = GatePillar.instance.cachedTransform.position;
						targetCollider = hitInfo.collider;
					}
				}

				if (actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Tab))
				{
					actor.targetingProcessor.SetCustomTargetPosition(targetPosition);
					_clearCustomTargetWaitCount = 10;
					RotateTowards(targetPosition - cachedTransform.position);
					CheckAttackRange(targetPosition, targetCollider);
				}
			}
		}

		if (_clearCustomTargetWaitCount > 0)
		{
			_clearCustomTargetWaitCount -= 1;
			if (_clearCustomTargetWaitCount == 0)
				_standbyClearCustomTarget = true;
		}

		if (_standbyClearCustomTarget && actionController.mecanimState.IsState((int)eMecanimState.Idle))
		{
			actor.targetingProcessor.ClearCustomTargetPosition();
			_standbyClearCustomTarget = false;
		}
	}

	void CheckAttackRange(Vector3 targetPosition, Collider targetCollider)
	{
		if (_actorTableAttackRange == 0.0f)
			return;

		float targetRadius = 0.0f;
		if (targetCollider != null) targetRadius = ColliderUtil.GetRadius(targetCollider);

		Vector3 diff = targetPosition - actor.cachedTransform.position;
		diff.y = 0.0f;
		if (diff.sqrMagnitude - (targetRadius * targetRadius) > _actorTableAttackRange * _actorTableAttackRange)
			RangeIndicator.instance.ShowIndicator(_actorTableAttackRange, true, cachedTransform, true);
	}

	/// <summary>
	/// Overrides 'BaseCharacterController' HandleInput,
	/// to perform custom controller input.
	/// </summary>

	protected override void HandleInput()
	{
		// moveDirection 도 막아야 회전을 안한다.
		if (actor.actorStatus.IsDie())
		{
			// 제자리 애니들(대표적으로 Run애니)을 가지고 있어서 useRootMotion 끄는 캐릭들이 있다.
			// 이런 캐릭들은 죽을때 이동중이었다면 미끄러지면서 죽는 모션이 나오게 된다.(useRootMotion이 꺼있기 때문에 마지막 velocity가 계속 적용되는 상태라 이렇다.)
			// 그래서 둘 중 하나로 처리해야하는데 useRootMotion를 바꾸는건 복구코드가 별도로 필요해지기 때문에
			// moveDirection을 초기화 하기로 한다.
			moveDirection = Vector3.zero;
			//useRootMotion = true;
			return;
		}

		// 여기서 zero로 바꾸면 Move액션이 안나가게 된다.
		//if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
		//{
		//	moveDirection = Vector3.zero;
		//	return;
		//}


		// Handle your custom input here...

		moveDirection = new Vector3
		{
			x = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Horizontal"),
			y = 0.0f,
			z = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Vertical")
		};

		// Transform moveDirection vector to be relative to camera view direction

		moveDirection = moveDirection.relativeTo(cameraTransform);
	}

	#endregion




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