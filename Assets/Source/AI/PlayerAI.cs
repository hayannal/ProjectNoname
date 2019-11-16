using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MecanimStateDefine;
using ECM.Controllers;

public class PlayerAI : MonoBehaviour
{
	public const float FindTargetRange = 50.0f;
	const float TargetFindDelay = 0.1f;
	const float TargetChangeThreshold = 2.0f;

	public Collider targetCollider { get; private set; }
	float _targetColliderRadius;

	Actor actor { get; set; }
	TargetingProcessor targetingProcessor { get; set; }
	BaseCharacterController baseCharacterController { get; set; }

	void OnDisable()
	{
		if (_cachedTargetingObjectTransform != null)
			_cachedTargetingObjectTransform.gameObject.SetActive(false);
	}

	// Start is called before the first frame update
	void Start()
    {
		actor = GetComponent<Actor>();
		targetingProcessor = GetComponent<TargetingProcessor>();
		baseCharacterController = GetComponent<BaseCharacterController>();

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actor.actorId);
		if (actorTableData != null)
			_actorTableAttackRange = actorTableData.attackRange;
	}

    // Update is called once per frame
    void Update()
    {
		UpdateTargeting();
		UpdateTargetingObject();
		UpdateAttack();
		UpdateAttackRange();
	}

	float _currentFindDelay;
	Transform _cachedTargetingObjectTransform = null;
	//List<GameObject> _listCachedTargetingObject = null;
	void UpdateTargeting()
	{
		if (targetingProcessor == null)
			return;

		// 공격중일땐 findDelay만 0에 가깝게 줄여놓고 대기타다가 Idle로 진입하면 바로 find한다.
		if (actor.actionController.mecanimState.IsState((int)eMecanimState.Attack) || actor.actionController.mecanimState.IsState((int)eMecanimState.Ultimate))
		{
			if (_currentFindDelay > 0.0f)
				_currentFindDelay -= Time.deltaTime;
			return;
		}

		_currentFindDelay -= Time.deltaTime;
		if (_currentFindDelay <= 0.0f)
		{
			_currentFindDelay += TargetFindDelay;
			if (targetingProcessor.FindNearestMonster(FindTargetRange, actor.actionController.mecanimState.IsState((int)eMecanimState.Move) ? 0.0f : TargetChangeThreshold))
			{
				targetCollider = targetingProcessor.GetTarget();
				_targetColliderRadius = ColliderUtil.GetRadius(targetCollider);
			}
			else
				targetCollider = null;
		}

		if (targetCollider != null)
		{
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
			if (affectorProcessor != null && affectorProcessor.actor != null && affectorProcessor.actor.actorStatus.IsDie())
			{
				_currentFindDelay = 0.0f;
				targetCollider = null;
				targetingProcessor.ClearTarget();
			}
		}
	}

	void UpdateTargetingObject()
	{
		if (targetCollider == null)
		{
			if (_cachedTargetingObjectTransform != null)
				_cachedTargetingObjectTransform.gameObject.SetActive(false);
			return;
		}

		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		if (_cachedTargetingObjectTransform == null)
		{
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.targetCircleObject, null);
			_cachedTargetingObjectTransform = newObject.transform;
		}
		if (_cachedTargetingObjectTransform == null)
			return;

		_cachedTargetingObjectTransform.gameObject.SetActive(true);
		if (targetTransform.position.y < 0.0f)
		{
			Vector3 newPos = targetTransform.position;
			newPos.y = 0.0f;
			_cachedTargetingObjectTransform.position = newPos;
		}
		else
			_cachedTargetingObjectTransform.position = targetTransform.position;
	}

	float _actorTableAttackRange;
	float _currentAttackDelay;
	string NormalAttackName = "Attack";
	void UpdateAttack()
	{
		// Attack Delay?
		// 이미 쿨타임 프로세서가 알아서 처리하고 있지 않나.
		// 연산 최적화를 위해? 혹은 처리 위치에 따라 스턴중에 딜레이 안흐르게 할수도 있다. CannotAction보다 아래.
		if (_currentAttackDelay > 0.0f)
		{
			_currentAttackDelay -= Time.deltaTime;
			if (_currentAttackDelay > 0.0f)
				return;
		}

		// ContinuousAffector 검사
		if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

		// 시즈탱크 퉁퉁포처럼 플레이어가 이동하는 동안에도 포탑은 알아서 쏘는 거까지 커버하려면
		// 인풋이 없는거나 Move가 아닌거로 체크해선 안된다.
		// Idle 혹은 Attackable 같은 뭔가가 필요해보인다.
		bool autoAttackable = false;
		if (actor.actionController.mecanimState.IsState((int)eMecanimState.Idle))
			autoAttackable = true;

		// no target
		if (targetCollider == null)
			autoAttackable = false;

		if (!autoAttackable)
			return;

		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		Vector3 diff = targetTransform.position - actor.cachedTransform.position;
		diff.y = 0.0f;
		if (IsInAttackRange(diff) == false)
			return;

		baseCharacterController.movement.rotation = Quaternion.LookRotation(diff);
		if (actor.actionController.PlayActionByActionName(NormalAttackName))
		{
			Cooltime cooltime = actor.cooltimeProcessor.GetCooltime(NormalAttackName);
			if (cooltime != null)
				_currentAttackDelay = cooltime.cooltime;
		}
	}

	bool IsInAttackRange(Vector3 diff)
	{
		if (_actorTableAttackRange == 0.0f)
			return true;

		if (diff.sqrMagnitude - (_targetColliderRadius * _targetColliderRadius) > _actorTableAttackRange * _actorTableAttackRange)
			return false;

		return true;
	}

	void UpdateAttackRange()
	{
		if (targetCollider == null)
			return;
		if (_actorTableAttackRange == 0.0f)
			return;

		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		Vector3 diff = targetTransform.position - actor.cachedTransform.position;
		diff.y = 0.0f;
		RangeIndicator.instance.ShowIndicator(_actorTableAttackRange, !IsInAttackRange(diff), actor.cachedTransform, false);
	}
}
