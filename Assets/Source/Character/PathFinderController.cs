using ECM.Common;
using ECM.Controllers;
using UnityEngine;
using MecanimStateDefine;

public class PathFinderController : BaseAgentController
{
	#region FIELDS

	Actor _actor;
	ActionController _actionController;
	MonsterAI _monsterAI;

	#endregion

	#region PROPERTIES

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

	public MonsterAI monsterAI
	{
		get
		{
			if (_monsterAI != null)
				return _monsterAI;
			_monsterAI = GetComponent<MonsterAI>();
			return _monsterAI;
		}
	}

	#endregion

	#region METHODS

	/// <summary>
	/// Calculate the desired movement velocity.
	/// Eg: Convert the input (moveDirection) to movement velocity vector,
	///     use navmesh agent desired velocity, etc.
	/// </summary>

	bool _dontMove = false;
	public bool dontMove
	{
		get
		{
			return _dontMove;
		}
		set
		{
			_dontMove = value;
			if (agent != null)
				agent.speed = _dontMove ? 0.0f : speed;
		}
	}

	private void Start()
	{
		// temp code
		agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
	}

	protected override Vector3 CalcDesiredVelocity()
	{
		if (_dontMove)
			return Vector3.zero;

		var desiredVelocity = Vector3.zero;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
		{
			if (monsterAI.nodeWarDestinationState)
				moveDirection = (monsterAI.nodeWarDestinationPosition - actor.cachedTransform.position).normalized;
			else
				moveDirection = Vector3.zero;

			desiredVelocity = moveDirection * speed;
		}
		else
		{
			SetMoveDirection();

			desiredVelocity = base.CalcDesiredVelocity();

			// Attack 애니에 들어있는 루트모션은 쓰고싶으면 useRootMotion을 켜고 RootMotionController를 Animator에 붙여두면 되는데
			// 이 상태에서 Move애니에 이동값이 없으면 아예 desiredVelocity가 0으로 되서 제자리에서 못움직이게 된다.
			// 그래서 아래 두줄로 예외처리를 해본다.
			// 지금껏 몬스터한테 이걸 켜는 경우가 없었는데 처음으로 StoneMonster에게 적용해본다.
			if (useRootMotion && applyRootMotion && desiredVelocity == Vector3.zero)
				desiredVelocity = moveDirection * speed;
		}

		return autoBraking ? desiredVelocity * brakingRatio : desiredVelocity;
	}

	/// <summary>
	/// Overrides 'BaseCharacterController' Animate method.
	/// 
	/// This shows how to handle your characters' animation states using the Animate method.
	/// The use of this method is optional, for example you can use a separate script to manage your
	/// animations completely separate of movement controller.
	/// 
	/// </summary>

	public bool diableAnimate { get; set; }
	protected override void Animate()
	{
		// If no animator, return

		if (animator == null)
			return;
		if (actor.actorStatus.IsDie())
			return;

		// MonsterAI 가 꺼있을때는 컨티뉴어스 어펙터 등에서 뭔가 특이한 액션을 처리할때다.
		// 이땐 아래 액션 체인지 코드를 수행하지 않는다.
		if (monsterAI.enabled == false)
			return;

		// AI를 끄지 않아도 아래 액션 체인지 코드를 수행하지 않게 하고싶을 때가 있다.
		// 대표적으로 이동을 AI가 제어하는 StraightMove다.
		// 이럴때를 대비해서 플래그 하나 추가해둔다.
		if (diableAnimate)
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