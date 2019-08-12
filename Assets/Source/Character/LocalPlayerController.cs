using ECM.Common;
using ECM.Controllers;
using UnityEngine;
using MecanimStateDefine;

public sealed class LocalPlayerController : BaseCharacterController
{
	#region EDITOR EXPOSED FIELDS

	[Header("CUSTOM CONTROLLER")]
	[Tooltip("The character's follow camera.")]
	public Transform playerCamera;

	[Tooltip("Layers to be considered as ground (picking). Used by ground click detection.")]
	[SerializeField]
	public LayerMask groundMask = 1;            // Default layer

	#endregion

	#region FIELDS

	Actor _actor;
	ActionController _actionController;

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

	#endregion

	#region METHODS

	/// <summary>
	/// Overrides 'BaseCharacterController' Animate method.
	/// 
	/// This shows how to handle your characters' animation states using the Animate method.
	/// The use of this method is optional, for example you can use a separate script to manage your
	/// animations completely separate of movement controller.
	/// 
	/// </summary>

	protected override void Animate()
	{
		// If no animator, return

		if (animator == null)
			return;

		// Compute move vector in local space - not needed

		//var move = transform.InverseTransformDirection(moveDirection);

		// Update the animator parameters

		var moveAmount = moveDirection.sqrMagnitude;
		//animator.SetFloat("Move", moveAmount, 0.1f, Time.deltaTime);

		if (moveAmount > 0.0f)
			actionController.PlayActionByActionName("Move");
		else
		{
			if (actionController.mecanimState.IsState((int)eMecanimState.Move))
				actionController.PlayActionByActionName("Idle");
		}

		if (ScreenJoystick.instance.CheckInput(Control.eInputType.Tab))
		{
			Ray ray = Camera.main.ScreenPointToRay(ScreenJoystick.instance.tabPosition);
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, groundMask.value))
			{
				Vector3 targetPosition = hitInfo.point;

				if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
				{
					if (hitInfo.collider != null && hitInfo.collider.gameObject == GatePillar.instance.meshColliderObject)
						targetPosition = GatePillar.instance.cachedTransform.position;
				}

				if (actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Tab))
				{
					actor.targetingProcessor.SetCustomTargetPosition(targetPosition);
					RotateTowards(targetPosition - transform.position);
				}
			}
		}
	}

	/// <summary>
	/// Overrides 'BaseCharacterController' HandleInput,
	/// to perform custom controller input.
	/// </summary>

	protected override void HandleInput()
	{
		// Handle your custom input here...

		moveDirection = new Vector3
		{
			x = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Horizontal"),
			y = 0.0f,
			z = UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GetAxisRaw("Vertical")
		};

		// Transform moveDirection vector to be relative to camera view direction

		moveDirection = moveDirection.relativeTo(playerCamera);
	}

	#endregion
}