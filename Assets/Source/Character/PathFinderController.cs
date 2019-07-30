//#define DRAW_PATH_LINE_RENDERER

using ECM.Common;
using ECM.Controllers;
using UnityEngine;
using MecanimStateDefine;
using K_PathFinder;

public class PathFinderController : BaseCharacterController
{
#if DRAW_PATH_LINE_RENDERER
	public LineRenderer lineRenderer;
#endif

	#region FIELDS

	Actor _actor;
	ActionController _actionController;

	#endregion

	#region PROPERTIES

	/// <summary>
	/// Cached NavMeshAgent component.
	/// </summary>

	public PathFinderAgent agent { get; private set; }

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
	}

	/// <summary>
	/// Synchronize the NavMesh Agent simulation position with the character movement position,
	/// we control the agent.
	/// 
	/// NOTE: Must be called in LateUpdate method.
	/// </summary>

	protected void SyncAgent()
	{
		//agent.speed = speed;
		//agent.angularSpeed = angularSpeed;

		//agent.acceleration = acceleration;
		//agent.velocity = movement.velocity;

		//agent.nextPosition = transform.position;
	}

	/// <summary>
	/// Assign the character's desired move direction (input) based on agent's info.
	/// </summary>

	protected virtual void SetMoveDirection()
	{
		// If agent is not moving, return

		moveDirection = Vector3.zero;

		//execute path to current target
		if (agent.haveNextNode == false)
			return;

		//remove next node if closer than radius in top projection. there is other variants of this function
		agent.RemoveNextNodeIfCloserThanRadiusVector2();

		//if next point still exist then move towards it
		if (agent.haveNextNode)
		{
			Vector2 normalizedVector2 = agent.nextNodeDirectionVector2.normalized;
			moveDirection = new Vector3(normalizedVector2.x, 0.0f, normalizedVector2.y);
			//controler.SimpleMove(new Vector3(moveDirection.x, 0, moveDirection.y) * speed);
		}
	}

	/// <summary>
	/// Overrides 'BaseCharacterController' CalcDesiredVelocity method,
	/// adding auto braking support.
	/// </summary>

	protected override Vector3 CalcDesiredVelocity()
	{
		SetMoveDirection();

		var desiredVelocity = base.CalcDesiredVelocity();
		//return autoBraking ? desiredVelocity * brakingRatio : desiredVelocity;
		return desiredVelocity;
	}

	/// <summary>
	/// Overrides 'BaseCharacterController' HandleInput,
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

	#region MONOBEHAVIOUR

	/// <summary>
	/// Validate this editor exposed fields.
	/// </summary>

	public override void OnValidate()
	{
		// Calls the parent class' version of method.

		base.OnValidate();

		// This class validation

		//autoBraking = _autoBraking;

		//brakingDistance = _brakingDistance;
		//stoppingDistance = _stoppingDistance;
	}

	/// <summary>
	/// Initialize this.
	/// </summary>

	public override void Awake()
	{
		// Calls the parent class' version of method.

		base.Awake();

		// Cache and initialize components

		agent = GetComponent<PathFinderAgent>();
		if (agent != null)
		{
			//agent.autoBraking = autoBraking;
			//agent.stoppingDistance = stoppingDistance;

			// Turn-off NavMeshAgent control,
			// we control it, not the other way

			//agent.updatePosition = false;
			//agent.updateRotation = false;

			//agent.updateUpAxis = false;
		}
		else
		{
			Debug.LogError(
				string.Format(
					"PathFinderController: There is no 'PathFinderAgent' attached to the '{0}' game object.\n" +
					"Please add a 'PathFinderAgent' to the '{0}' game object.",
					name));
		}
	}

	public virtual void LateUpdate()
	{
		// Synchronize agent with character movement

		//SyncAgent();
	}

#if DRAW_PATH_LINE_RENDERER
	// 패스 찾는게 이상해서 보고 싶을땐 이 디파인 활성화 하고
	// K-PathFinder에 있는 아무거나 라인렌더러 가져다가 몹프리팹 안에 넣은 후 인스펙터에서 연결해주면 비쥬얼로 보일거다.
	void Start()
	{
		agent.SetRecievePathDelegate(RecivePathDelegate, AgentDelegateMode.ThreadSafe);
	}

	void RecivePathDelegate(Path path)
	{
		Debug.LogFormat("Path is {0}, it have {1} nodes", path.pathType, path.count);       //we ricieve that
		K_PathFinder.Samples.ExampleThings.PathToLineRenderer(agent.positionVector3, lineRenderer, path, 0.2f);//move line to path position
	}
#endif

	#endregion
}