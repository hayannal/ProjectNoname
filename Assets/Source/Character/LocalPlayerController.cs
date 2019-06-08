using ECM.Common;
using ECM.Controllers;
using UnityEngine;

public sealed class LocalPlayerController : BaseCharacterController
{
	#region EDITOR EXPOSED FIELDS

	[Header("CUSTOM CONTROLLER")]
	[Tooltip("The character's follow camera.")]
	public Transform playerCamera;

	#endregion

	#region METHODS

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