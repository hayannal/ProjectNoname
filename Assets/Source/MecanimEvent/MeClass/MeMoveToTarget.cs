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
	public bool useChase;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		distanceOffset = EditorGUILayout.FloatField("Distance Offset :", distanceOffset);
		useChase = EditorGUILayout.Toggle("Use Chase :", useChase);
	}
#endif

	// 기본적으로 MovePositionCurve와 같이 Rigidbody사용해서 전진한다.
	Actor _actor = null;
	float _velocityZ = 0.0f;
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

		_velocityZ = 0.0f;
		Vector3 diff = Vector3.zero;
		float durationTime = (EndTime - StartTime) * stateInfo.length;
		if (_actor.targetingProcessor.GetTarget() == null)
		{
			Vector3 targetPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);
			diff = targetPosition - _actor.cachedTransform.position;
			diff.y = 0.0f;
		}
		else
		{
			Collider targetCollider = _actor.targetingProcessor.GetTarget();
			Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
			if (targetTransform != null)
			{
				diff = targetTransform.position - _actor.cachedTransform.position;
				diff.y = 0.0f;
			}
		}
		if (diff.magnitude + distanceOffset > 0.0f)
			_velocityZ = (diff.magnitude + distanceOffset) / durationTime;

		if (_velocityZ != 0.0f)
		{
			// MovePositionCurve에서 했던거처럼 FixedUpdate가 필요하니 VelocityAffector를 호출한다.
			AffectorValueLevelTableData velocityAffectorValue = new AffectorValueLevelTableData();
			velocityAffectorValue.fValue1 = durationTime;
			velocityAffectorValue.fValue2 = 0.0f;
			velocityAffectorValue.fValue3 = _velocityZ;
			_actor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Velocity, velocityAffectorValue, _actor, false);
		}
	}

	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif
		if (_actor == null)
			return;

		if (useChase)
		{

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

		_actor.GetRigidbody().velocity = Vector3.zero;
	}
}