using ECM.Common;
using ECM.Controllers;
using UnityEngine;
using MecanimStateDefine;

public class PathFinderController : BaseAgentController
{
	#region FIELDS

	ActionController _actionController;

	#endregion

	#region PROPERTIES

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
	}

	/// <summary>
	/// Overrides 'BaseAgentController' HandleInput,
	/// to perform custom controller input.
	/// </summary>

	protected override void HandleInput()
	{
		// Handle mouse input

		//if (!Input.GetButton("Fire2"))
		//	return;

		// If mouse right click,
		// found click position in the world

		//var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		//RaycastHit hitInfo;
		//if (!Physics.Raycast(ray, out hitInfo, Mathf.Infinity, groundMask.value))
		//	return;

		// Set agent destination to ground hit point

		//agent.SetDestination(hitInfo.point);
	}

	#endregion
}