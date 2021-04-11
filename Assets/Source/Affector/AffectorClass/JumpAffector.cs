using UnityEngine;
using System.Collections;
using ActorStatusDefine;
using UnityEngine.AI;
using DG.Tweening;

public class JumpAffector : AffectorBase
{
	enum eJumpType
	{
		TargetPosition = 1,
		RandomPosition = 2,
	}

	float _endTime;

	Vector3 _diff;
	Vector3 _targetPosition;
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

		// find target
		if (_actor.targetingProcessor.GetTargetCount() > 0)
			_targetPosition = _actor.targetingProcessor.GetTargetPosition();
		else
			_targetPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);

		// check exit condition
		switch ((eJumpType)affectorValueLevelTableData.iValue1)
		{
			case eJumpType.TargetPosition:
				Vector2 randomOffset = Random.insideUnitCircle * affectorValueLevelTableData.fValue4;
				_targetPosition.x += randomOffset.x;
				_targetPosition.z += randomOffset.y;
				break;
			case eJumpType.RandomPosition:
				_targetPosition = GetRandomPosition(affectorValueLevelTableData.fValue4);
				break;
		}

		HitObject.EnableRigidbodyAndCollider(false, _actor.GetRigidbody(), _actor.GetCollider());

		// lifeTime
		_diff = _targetPosition - _actor.cachedTransform.position;
		float jumpTime = _diff.magnitude / affectorValueLevelTableData.fValue1;
		_endTime = CalcEndTime(jumpTime);

		// jump
		if (affectorValueLevelTableData.fValue2 > 0.0f)
			_actor.actionController.cachedAnimatorTransform.DOLocalJump(Vector3.zero, affectorValueLevelTableData.fValue2, 1, jumpTime).SetEase(Ease.Linear);

		_diff.y = 0.0f;
		_diff = _diff.normalized;

		// 점프 시간은 줄어들면 안되기 때문에 _endTime 계산해놓고나서 _targetPosition과 _diff를 새로 계산해야한다.
		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
		{
			_diff = _diff.normalized * 0.01f;
			_targetPosition = _actor.cachedTransform.position + _diff;
		}
		else
		{
			float moveSpeedAddRate = _actor.actorStatus.GetValue(eActorStatus.MoveSpeedAddRate);
			if (moveSpeedAddRate < 0.0f)
			{
				_diff = _diff.normalized * _diff.magnitude * (1.0f + moveSpeedAddRate);
				_targetPosition = _actor.cachedTransform.position + _diff;
			}
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		// 원래라면 절대 들어오지 말아야하는데 점프중에 끝나지도 않았는데 점프가 또 온거다.
		//Debug.Break();
		Debug.LogError("Invalid call. Duplicated Jump Affector.");
	}

	int _agentTypeID = -1;
	Vector3 GetRandomPosition(float minimumDistance)
	{
		Vector3 randomPosition = Vector3.zero;
		Vector3 result = Vector3.zero;
		float maxDistance = 1.0f;
		int tryCount = 0;
		int tryBreakCount = 0;
		if (_agentTypeID == -1) _agentTypeID = MeLookAt.GetAgentTypeID(_actor);
		while (true)
		{
			// RushAffector와 마찬가지로 방향은 lookAt 시그널에서 처리하고 어펙터 안에서는 위치만 구하면 된다.
			randomPosition = _actor.cachedTransform.position + _actor.cachedTransform.forward * minimumDistance;

			// AI쪽 코드에서 가져와본다.
			randomPosition.y = 0.0f;

			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = _agentTypeID;
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			{
				result = randomPosition;
				break;
			}
			if (NavMesh.SamplePosition(randomPosition, out hit, maxDistance, navMeshQueryFilter))
			{
				result = hit.position;
				break;
			}

			// exception handling
			++tryCount;
			if (tryCount > 20)
			{
				tryCount = 0;
				maxDistance += 1.0f;
			}

			++tryBreakCount;
			if (tryBreakCount > 400)
			{
				Debug.LogError("JumpAffector RandomPosition Error. Not found valid random position.");
				return randomPosition;
			}
		}
		return result;
	}

	public override void UpdateAffector()
	{
		// 점프 도중엔 CannotAction이나 CannotMove걸리는게 더 이상할거라 처리하지 않는다.
		//if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
		//{
		//}
		//if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
		//{
		//}

		if (CheckEndTime(_endTime) == false)
			return;

		// 점프중에 DontDie 시그널로 죽게 할거 같진 않지만..
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// x,z 평면에서는 기존과 달리 트랜스폼으로 이동시킨다.
		_actor.cachedTransform.Translate(new Vector3(_diff.x, 0.0f, _diff.z) * _affectorValueLevelTableData.fValue1 * Time.deltaTime, Space.World);
	}

	public override void FinalizeAffector()
	{
		if (_actor.actorStatus.IsDie())
			return;

		HitObject.EnableRigidbodyAndCollider(true, _actor.GetRigidbody(), _actor.GetCollider());

		if (string.IsNullOrEmpty(_affectorValueLevelTableData.sValue1))
			return;
		_actor.actionController.animator.CrossFade(_affectorValueLevelTableData.sValue1, 0.05f);
	}

	public static bool CheckJump(AffectorProcessor affectorProcessor)
	{
		if (affectorProcessor.IsContinuousAffectorType(eAffectorType.Jump))
			return true;

		// 액션에다가 MecanimState로 걸어놨을수도 있으니 체크.
		// 이러면 JumpAffector없이 MovePositionCurve로 제어하면서 State시그널로 처리하는 경우에도 체크할 수 있다.
		if (affectorProcessor.actor.actionController.mecanimState.IsState((int)MecanimStateDefine.eMecanimState.Jump))
			return true;

		return false;
	}
}