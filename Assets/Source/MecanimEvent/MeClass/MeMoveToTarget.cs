using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.AI;

public class MeMoveToTarget : MecanimEventBase
{
	override public bool RangeSignal { get { return true; } }

	public float distanceOffset;
	public Vector2 randomPositionRadiusRange;
	public float maxDistance;
	//public bool useChase;
	public bool useTransform;
	public bool transformMoveUseDelta;
	public bool updateTargetPosition;
	public bool useRegisterdCustomTargetPosition;
	public bool checkNavPosition;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		distanceOffset = EditorGUILayout.FloatField("Distance Offset :", distanceOffset);
		randomPositionRadiusRange = EditorGUILayout.Vector2Field("Random Position Radius Range :", randomPositionRadiusRange);
		maxDistance = EditorGUILayout.FloatField("Max Distance :", maxDistance);
		//useChase = EditorGUILayout.Toggle("Use Chase :", useChase);

		useTransform = EditorGUILayout.Toggle("Use Transform :", useTransform);
		if (useTransform)
		{
			transformMoveUseDelta = EditorGUILayout.Toggle("Use Delta :", transformMoveUseDelta);
			if (transformMoveUseDelta == false)
				updateTargetPosition = EditorGUILayout.Toggle("Update Target Position :", updateTargetPosition);
			checkNavPosition = EditorGUILayout.Toggle("Check Nav Position :", checkNavPosition);
		}
	}
#endif

	// 기본적으로 MovePositionCurve와 같이 Rigidbody사용해서 전진한다.
	Actor _actor = null;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		// 돌진과 달리 애니도중에 Area로 공격하는데 주로 사용되기 때문에
		// Radius 검사는 하지 않고 포지션끼리 체크해서 거리를 계산한다.
		// 그러니 -3미터 설정하면 타겟으로부터 3미터 가까운 지점에 멈추게 된다.
		float velocityZ = 0.0f;
		float durationTime = (EndTime - StartTime) * stateInfo.length;
		Vector3 diff = Vector3.zero;
		Vector3 targetPosition = GetTargetPosition(ref diff);

		// SeaPrincess처럼 공중에 올라서 찍는걸 사용할때 컬리더를 끄고 점프 상태 및 DontDie상태로 바꾸는 경우엔 하단의 Velocity어펙터가 먹히질 않게된다.
		// 이럴땐 Transform이동으로 처리해서 이동시켜야한다. 장애물이 있으면 어색할테니 주의해서 사용해야한다.
		if (useTransform)
		{
			// Transform을 안쓸때는 컬리더가 꺼있지도 않을테고 rigidbody를 밀어서 옮기는거라 네비메시 검사를 아예 할필요가 없는데
			// Transform 써서 이동하는 경우에는 갑자기 벽 위로 올라갈 수가 있다.
			// 이걸 대비헤서 checkNavPosition이 켜있다면 내비 검사를 해서 새로운 위치를 구해야한다.
			if (checkNavPosition) targetPosition = GetNavPosition(targetPosition);

			// SeaPrincess처럼 장애물이 없는 곳은 Nav검사 할필요 없이 그냥 하면 된다.
			_targetPosition = targetPosition;

			_startPosition = _actor.cachedTransform.position;
			_speed = (_targetPosition - _startPosition) / durationTime;
			_durationTime = durationTime;
			_t = 0.0f;
			return;
		}

		if (diff.magnitude + distanceOffset > 0.0f)
			velocityZ = (diff.magnitude + distanceOffset) / durationTime;

		if (velocityZ != 0.0f)
		{
			// MovePositionCurve에서 했던거처럼 FixedUpdate가 필요하니 VelocityAffector를 호출한다.
			AffectorValueLevelTableData velocityAffectorValue = new AffectorValueLevelTableData();
			velocityAffectorValue.fValue1 = durationTime;
			velocityAffectorValue.fValue2 = 0.0f;
			velocityAffectorValue.fValue3 = velocityZ;
			_actor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Velocity, velocityAffectorValue, _actor, false);
		}
	}

	Vector3 GetTargetPosition(ref Vector3 diff)
	{
		Vector2 randomRadius = Random.insideUnitCircle * randomPositionRadiusRange;
		Vector3 targetPosition = Vector3.zero;
		
		// 다른 시그널에서 등록한 CustomTargetPosition을 사용할때 이렇게 가져와서 쓴다.
		if (useRegisterdCustomTargetPosition && _actor.targetingProcessor.IsRegisteredCustomTargetPosition())
		{
			targetPosition = _actor.targetingProcessor.GetCustomTargetPosition(0);
			targetPosition += new Vector3(randomRadius.x, 0.0f, randomRadius.y);
			diff = targetPosition - _actor.cachedTransform.position;
			diff.y = 0.0f;
			targetPosition += -diff.normalized * distanceOffset;
		}
		else
		{
			if (_actor.targetingProcessor.GetTarget() == null)
			{
				targetPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);
				targetPosition += new Vector3(randomRadius.x, 0.0f, randomRadius.y);
				diff = targetPosition - _actor.cachedTransform.position;
				diff.y = 0.0f;
				targetPosition += -diff.normalized * distanceOffset;
			}
			else
			{
				Collider targetCollider = _actor.targetingProcessor.GetTarget();
				Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
				if (targetTransform != null)
				{
					targetPosition = targetTransform.position;
					targetPosition += new Vector3(randomRadius.x, 0.0f, randomRadius.y);
					diff = targetPosition - _actor.cachedTransform.position;
					diff.y = 0.0f;
					targetPosition += -diff.normalized * distanceOffset;
				}
			}
		}

		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
		{
			diff = diff.normalized * 0.01f;
			targetPosition = _actor.cachedTransform.position + diff;
			return targetPosition;
		}

		if (maxDistance > 0.0f && diff.magnitude > maxDistance)
		{
			diff = diff.normalized * maxDistance;
			targetPosition = _actor.cachedTransform.position + diff;
		}

		float moveSpeedAddRate = _actor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MoveSpeedAddRate);
		if (moveSpeedAddRate < 0.0f)
		{
			diff = diff.normalized * diff.magnitude * (1.0f + moveSpeedAddRate);
			targetPosition = _actor.cachedTransform.position + diff;
		}

		return targetPosition;
	}

	// Summon Signal에서 가져와 쓴다.
	int _agentTypeID = -1;
	Vector3 GetNavPosition(Vector3 desirePosition)
	{
		Vector3 result = Vector3.zero;
		float maxDistance = 1.0f;
		int tryBreakCount = 0;
		desirePosition.y = 0.0f;
		if (_agentTypeID == -1) _agentTypeID = MeLookAt.GetAgentTypeID(_actor);
		while (true)
		{
			// AI쪽 코드에서 가져와서 변형
			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = _agentTypeID;
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			{
				result = desirePosition;
				break;
			}
			if (NavMesh.SamplePosition(desirePosition, out hit, maxDistance, navMeshQueryFilter))
			{
				result = hit.position;
				break;
			}
			maxDistance += 1.0f;

			// exception handling
			++tryBreakCount;
			if (tryBreakCount > 50)
			{
				Debug.LogError("MeMoveToTarget NavPosition Error. Not found valid nav position.");
				return desirePosition;
			}
		}
		return result;
	}

	Vector3 _startPosition;
	Vector3 _targetPosition;
	Vector3 _speed;
	float _durationTime;
	float _t;
	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif
		if (_actor == null)
			return;

		// 추적기능이 이론상으로도 조금 애매하긴 하다.
		// RushAffector와 달리 시간이 고정이기 때문에 추적이 되려면 속도를 바꿔야하는데
		// 속도는 FixedUpdate에서 처리해야해서 여기서 직접 못하고 VelocityAffector에다가 남은 시간이랑 변경된 속도를 전달해야한다.
		// 근데 이게 약간의 오차가 생길 수 있어서 - 시간은 deltaTime으로 계산하고 물리는 fixedDeltaTime으로 처리되기 때문.
		// 추적시 완벽하게 계산해서 따라가기 어렵긴 하다.
		//
		// 그리고 같은 직선상 거리에서 멀어지는게 아니라 옆으로 뛸 경우가 더 문제인데
		// 룩엣을 해서 쫓아가는건 체공 중에 몸을 회전하는거라 이상해보일테고
		// 룩엣 없이 슬라이드로 횡이동 하는건 바라보는 방향대로 나아가는게 아니라서 역시 이상해보인다.
		// 어차피 이 MoveToTarget은 하나의 애니 안에서 적에게 다가가는거라 직선으로 가야 제일 자연스러운데
		// 이걸 깨면서까지 추적기능이 있어야하나 싶어서
		// 우선은 구현하지 않고 나중에 필요한 순간이 오면 그때 구현하기로 한다.(플레이어용 궁극기..)
		//if (useChase)
		//{
		//}

		if (useTransform)
		{
			// 트랜스폼을 사용해 이동시키는거도 크게 둘로 나눌 수 있다.
			if (transformMoveUseDelta)
			{
				// 하나는 캐릭터 방향을 lookAt시그널에 맡기고 총 이동량만큼만 이동시키는 방법이다.
				// Velocity로 이동하는 것과 비슷한 방식이다. lookAt은 건드리지 않게 때문에 장애물에 의해 막히거나 비스듬히 되면 목적 위치가 틀어질 수 있다.
				_actor.cachedTransform.position += _speed * Time.deltaTime;
			}
			else
			{
				// CactusBoss때문에 기능이 하나 추가되었는데 타겟 위치를 매프레임 갱신하는 기능이다.
				// 이게 있어야 멀어지는 플레이어를 추적하면서 다가가서 점프공격을 할 수 있다.
				if (updateTargetPosition)
				{
					Vector3 diff = Vector3.zero;
					Vector3 targetPosition = GetTargetPosition(ref diff);
					if (checkNavPosition) targetPosition = GetNavPosition(targetPosition);
					_targetPosition = targetPosition;
				}

				// 하나는 진짜 포지션 비교해서 목적지에 다다르게 하는 방식이다.(SeaPrincess처럼 AttackIndicator까지 그려놓은 경우라면 이렇게 찍어야 정확해진다.)
				_t += Time.deltaTime;
				_actor.cachedTransform.position = _startPosition + (_targetPosition - _startPosition) * (_t / _durationTime);
			}
		}
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (_actor == null)
			return;

		if (useTransform)
		{
			if (transformMoveUseDelta)
			{
			}
			else
			{
				// 시그널 끝날때 포지션 보정까지 해준다.
				_actor.cachedTransform.position = _targetPosition;
			}
			return;
		}

		_actor.GetRigidbody().velocity = Vector3.zero;
	}
}