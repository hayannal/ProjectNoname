using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using ECM.Components;

public class MeMoveToTarget : MecanimEventBase
{
	override public bool RangeSignal { get { return true; } }

	public float distanceOffset;
	public float maxDistance;
	//public bool useChase;
	public bool useTransform;
	public bool transformMoveUseDelta;
	public bool useRegisterdCustomTargetPosition;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		distanceOffset = EditorGUILayout.FloatField("Distance Offset :", distanceOffset);
		//useChase = EditorGUILayout.Toggle("Use Chase :", useChase);
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
		Vector3 targetPosition = Vector3.zero;
		Vector3 diff = Vector3.zero;
		float durationTime = (EndTime - StartTime) * stateInfo.length;
		// 다른 시그널에서 등록한 CustomTargetPosition을 사용할때 이렇게 가져와서 쓴다.
		if (useRegisterdCustomTargetPosition && _actor.targetingProcessor.IsRegisteredCustomTargetPosition())
		{
			targetPosition = _actor.targetingProcessor.GetCustomTargetPosition(0);
			diff = targetPosition - _actor.cachedTransform.position;
			diff.y = 0.0f;
		}
		else
		{
			if (_actor.targetingProcessor.GetTarget() == null)
			{
				targetPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);
				diff = targetPosition - _actor.cachedTransform.position;
				diff.y = 0.0f;
			}
			else
			{
				Collider targetCollider = _actor.targetingProcessor.GetTarget();
				Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
				if (targetTransform != null)
				{
					targetPosition = targetTransform.position;
					diff = targetPosition - _actor.cachedTransform.position;
					diff.y = 0.0f;
				}
			}
		}

		if (maxDistance > 0.0f && diff.magnitude > maxDistance)
		{
			diff = diff.normalized * maxDistance;
			targetPosition = _actor.cachedTransform.position + diff;
		}

		// SeaPrincess처럼 공중에 올라서 찍는걸 사용할때 컬리더를 끄고 점프 상태 및 DontDie상태로 바꾸는 경우엔 하단의 Velocity어펙터가 먹히질 않게된다.
		// 이럴땐 Transform이동으로 처리해서 이동시켜야한다. 장애물이 있으면 어색할테니 주의해서 사용해야한다.
		if (useTransform)
		{
			_targetPosition = targetPosition;
			_targetPosition += -diff.normalized * distanceOffset;
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