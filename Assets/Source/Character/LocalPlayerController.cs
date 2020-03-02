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

	bool _waitAttackState = false;
	bool _standbyClearCustomTarget = false;
	RaycastHit[] _raycastHitList = null;
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

			#region Invalid Move
			// 정말 두기 싫은 코드인데 이동중에 MoveState가 없어지는 경우가 생겼다.
			// 이러면 위 코드에서 true가 되지않아 Idle로도 가지 않고 계속 Move애니가 돌게 된다.
			// 그러다가 다음번 Move(루프니까)가 올때 Idle로 가는데
			// 하필 root motion 쓰는 캐릭터-간파울의 경우에는 진짜로 앞으로 이동해버린다.
			// 애니만 나오는거면 몰라도 이러면 너무 치명적이라 원인을 찾기 전까진 아래 코드로 검사해본다.
			if (actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			{
				// 진행중인걸 얻어와야하므로 Transition중이라면 Next를 구해온다.
				AnimatorStateInfo animatorStateInfo = actionController.animator.GetCurrentAnimatorStateInfo(0);
				if (actionController.animator.IsInTransition(0))
					animatorStateInfo = actionController.animator.GetNextAnimatorStateInfo(0);
				if (animatorStateInfo.loop && animatorStateInfo.IsName("Move"))
				{
#if UNITY_EDITOR
					Debug.LogError("Invalid Move!!!!!");
#endif
					actionController.PlayActionByActionName("Idle");
				}
			}
			#endregion
		}

		if (ScreenJoystick.instance.CheckInput(Control.eInputType.Tab) && IsAutoPlay() == false)
		{
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null)
				TitleCanvas.instance.FadeTitle();

			if (_raycastHitList == null)
				_raycastHitList = new RaycastHit[20];

			Ray ray = UIInstanceManager.instance.GetCachedCameraMain().ScreenPointToRay(ScreenJoystick.instance.tabPosition);
			Vector3 targetPosition = Vector3.zero;
			Collider targetCollider = null;
			Vector3 subHitPoint = Vector3.zero;
			Collider subCollider = null;
			bool groundHitted = false;
			bool raycastHitted = false;
			int resultCount = Physics.RaycastNonAlloc(ray, _raycastHitList, Mathf.Infinity, groundMask);
			if (resultCount > 0)
			{
				// first gate pillar
				if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
				{
					for (int i = 0; i < resultCount; ++i)
					{
						if (i >= _raycastHitList.Length)
							break;
						if (_raycastHitList[i].collider == null || _raycastHitList[i].collider.isTrigger)
							continue;

						if (_raycastHitList[i].collider.gameObject == GatePillar.instance.meshColliderObject)
						{
							targetPosition = GatePillar.instance.cachedTransform.position;
							targetCollider = _raycastHitList[i].collider;
							groundHitted = true;
							break;
						}
					}
				}

				if (groundHitted == false)
				{
					for (int i = 0; i < resultCount; ++i)
					{
						if (i >= _raycastHitList.Length)
							break;
						if (_raycastHitList[i].collider == null || _raycastHitList[i].collider.isTrigger)
							continue;

						if (_raycastHitList[i].collider == BattleInstanceManager.instance.planeCollider)
						{
							targetPosition = _raycastHitList[i].point;
							targetCollider = _raycastHitList[i].collider;
							groundHitted = true;
							break;
						}

						subHitPoint = _raycastHitList[i].point;
						subCollider = _raycastHitList[i].collider;
						raycastHitted = true;
					}
				}
			}
			if (groundHitted == false && raycastHitted)
			{
				targetPosition = subHitPoint;
				targetCollider = subCollider;
			}
			
			if ((groundHitted || raycastHitted) && actionController.PlayActionByControl(Control.eControllerType.ScreenController, Control.eInputType.Tab))
			{
				_waitAttackState = true;
				RotateTowards(targetPosition - cachedTransform.position);
				targetPosition = CheckAttackRange(targetPosition, targetCollider);
				actor.targetingProcessor.SetCustomTargetPosition(targetPosition);
				if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
					++GatePillar.instance.raycastCount;
			}
		}

		if (OptionManager.instance.useDoubleTab == 1 && ScreenJoystick.instance.CheckInput(Control.eInputType.DoubleTab) && IsAutoPlay() && actor.actorStatus.GetSPRatio() == 1.0f)
		{
			actionController.PlayActionByControl(Control.eControllerType.UltimateSkillSlot, Control.eInputType.Tab);
		}

		if (_waitAttackState)
		{
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Attack))
			{
				_waitAttackState = false;
				_standbyClearCustomTarget = true;
				return;
			}
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.Move))
			{
				// for attack cancel
				// 그렇지만 Attack State가 0부터 시작되어있을 경우 클릭한 바로 다음 프레임에 움직여야 들어오는거라 일반적인 경우라면 불가능할거다. 안전장치 겸 해둔다.
				_waitAttackState = false;
			}
		}

		if (_standbyClearCustomTarget && actionController.mecanimState.IsState((int)eMecanimState.Attack) == false && actionController.mecanimState.IsState((int)eMecanimState.Idle))
		{
			actor.targetingProcessor.ClearCustomTargetPosition();
			_standbyClearCustomTarget = false;
		}
	}

	Vector3 CheckAttackRange(Vector3 targetPosition, Collider targetCollider)
	{
		if (_actorTableAttackRange == 0.0f)
			return targetPosition;

		float targetRadius = 0.0f;
		if (targetCollider != null) targetRadius = ColliderUtil.GetRadius(targetCollider);
		if (targetRadius < 0.0f) targetRadius = 0.0f;

		Vector3 diff = targetPosition - actor.cachedTransform.position;
		diff.y = 0.0f;
		if (diff.sqrMagnitude - (targetRadius * targetRadius) > _actorTableAttackRange * _actorTableAttackRange)
		{
			RangeIndicator.instance.ShowIndicator(_actorTableAttackRange, true, cachedTransform, true);
			targetPosition = actor.cachedTransform.position + diff.normalized * _actorTableAttackRange;
		}
		return targetPosition;
	}

	bool IsAutoPlay()
	{
#if UNITY_EDITOR
		if (BattleInstanceManager.instance.playerActor != null && BattleInstanceManager.instance.playerActor.playerAI.enabled == false)
			return false;
#endif
		if (BattleManager.instance != null && BattleManager.instance.IsAutoPlay())
			return true;
		return false;
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

		// 레이저 필살기 시전 중에 회전되는 문제가 있었는데 그게 이 moveDirection에 input으로 발생된 값을 그대로 넣었기 때문에 발생하던 거였다.
		// 이동이 가능한 상태에서만 아래 코드를 호출해야하는거라 Die체크말고 조금 더 추가하기로 한다.
		// - 언제나 디폴트 상태는 movable true이며 일부 특정한 상황에서만 불가능하다.
		bool movable = true;
		if (actor.actionController.mecanimState.IsState((int)eMecanimState.Ultimate))
			movable = false;
		if (movable == false)
		{
			moveDirection = Vector3.zero;
			return;
		}


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