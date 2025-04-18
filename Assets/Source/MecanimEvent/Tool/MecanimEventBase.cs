﻿//#define DEBUG_ON_DAMAGE_CHANGE_STATE

using UnityEngine;
using System.Collections;

public class MecanimEventBase : StateMachineBehaviour {

#if UNITY_EDITOR
	static public bool s_bDisableMecanimEvent = false;
	static public bool s_bForceCallUpdate = false;
	static public float s_fAnimatorSpeed = 1.0f;
#endif

	public virtual bool RangeSignal { get { return false; } }

	public float StartTime = 0.0f;
	public float EndTime = 1.0f;

	// OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
#if UNITY_EDITOR
		if (s_bForceCallUpdate) return;
#endif

#if DEBUG_ON_DAMAGE_CHANGE_STATE
		if (GameManager.Instance.testFlag)
		{
			if (GameManager.Instance.testValue == stateInfo.fullPathHash)
			{
				Debug.Log("OnStateEnter : " + Time.frameCount);
			}
		}
#endif

		_lastNormalizeTime = 0.0f;
		OnStateUpdate(animator, stateInfo, layerIndex);
	}

	//int DebugStateHash = 0;
	float _lastNormalizeTime;
	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (s_bDisableMecanimEvent) return;
#endif

		// for animator.speed >= 0.0f

		//if (DebugStateHash == 0)
		//	DebugStateHash = Animator.StringToHash("Base Layer.Skill.attack_cutdown");

		int lastLoop = (int)_lastNormalizeTime;
		int currentLoop = (int)stateInfo.normalizedTime;
		float lastNormalizedTime = _lastNormalizeTime;
		float currentNormalizedTime = stateInfo.normalizedTime;
#if UNITY_EDITOR
		// 툴에서 액션 선택하고 Play눌러서 마지막에 다다를때 꼭 last 0 / current 1 로 와서 모든 시그널들이 1회씩 호출되길래 예외처리 해둔다.
		if (stateInfo.loop || s_bForceCallUpdate) lastNormalizedTime -= lastLoop;
		if (stateInfo.loop || s_bForceCallUpdate) currentNormalizedTime -= currentLoop;   //	0.0 ~ 0.99999
#else
		if (stateInfo.loop) lastNormalizedTime -= lastLoop;
		if (stateInfo.loop) currentNormalizedTime -= currentLoop;   //	0.0 ~ 0.99999
#endif

		if (animator.IsInTransition(0))
		{
			AnimatorStateInfo nextAnimatorStateInfo = animator.GetNextAnimatorStateInfo(0);
			if (stateInfo.fullPathHash == nextAnimatorStateInfo.fullPathHash)
				currentNormalizedTime = nextAnimatorStateInfo.normalizedTime;
		}

		// RangeSignal인데도 StartTime과 EndTime을 같은 값으로 설정할때가 있다. 대표적으로 SummonSignal. 이땐 동시에 Start, End를 호출해준다.
		if (RangeSignal && StartTime < EndTime)
		{
			if (lastNormalizedTime <= currentNormalizedTime)
			{
				if (lastNormalizedTime <= StartTime && StartTime <= currentNormalizedTime && currentNormalizedTime < EndTime)
					OnRangeSignalStart(animator, stateInfo, layerIndex);
				if (lastNormalizedTime <= EndTime && EndTime < currentNormalizedTime && lastNormalizedTime > StartTime)
					OnRangeSignalEnd(animator, stateInfo, layerIndex);
			}

			if (StartTime <= currentNormalizedTime && currentNormalizedTime < EndTime)
				OnRangeSignal(animator, stateInfo, layerIndex);

			if (lastNormalizedTime > currentNormalizedTime)
			{
				if (lastNormalizedTime <= EndTime && EndTime <= 1.0f && StartTime < lastNormalizedTime)
					OnRangeSignalEnd(animator, stateInfo, layerIndex);
				if (0.0f <= StartTime && StartTime <= currentNormalizedTime && currentNormalizedTime < EndTime)
					OnRangeSignalStart(animator, stateInfo, layerIndex);
			}
		}
		else
		{
			if (lastNormalizedTime <= currentNormalizedTime)
			{
				if (lastNormalizedTime <= StartTime && StartTime < currentNormalizedTime)
				{
					if (RangeSignal)
					{
						OnRangeSignalStart(animator, stateInfo, layerIndex);
						OnRangeSignalEnd(animator, stateInfo, layerIndex);
					}
					else
					{
						OnSignal(animator, stateInfo, layerIndex);
						//Debug.LogFormat("lastNormalized = {0} / currentNormalized = {1}", lastNormalizedTime, currentNormalizedTime);
					}
				}
			}
			else
			{
				if (lastNormalizedTime <= StartTime && StartTime <= 1.0f)
				{
					if (RangeSignal)
					{
						OnRangeSignalStart(animator, stateInfo, layerIndex);
						OnRangeSignalEnd(animator, stateInfo, layerIndex);
					}
					else
					{
						OnSignal(animator, stateInfo, layerIndex);
						//Debug.LogFormat("22222 : lastTime = {0} / startTime = {1}", lastNormalizedTime, StartTime);
					}
				}
				else if (0.0f <= StartTime && StartTime < currentNormalizedTime)
				{
					if (RangeSignal)
					{
						OnRangeSignalStart(animator, stateInfo, layerIndex);
						OnRangeSignalEnd(animator, stateInfo, layerIndex);
					}
					else
					{
						OnSignal(animator, stateInfo, layerIndex);
						//Debug.Log("111111111111");
					}
				}
			}
		}
		_lastNormalizeTime = stateInfo.normalizedTime;
	}
	
	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	//override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
	
	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
	
	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	virtual public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
	}

	virtual public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
	}

	virtual public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
	}

	virtual public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
	}

	public bool IsInRange(AnimatorStateInfo stateInfo)
	{
		if (!RangeSignal) return false;

		float currentNormalizedTime = stateInfo.normalizedTime;
		if (stateInfo.loop)
			currentNormalizedTime -= ((int)stateInfo.normalizedTime);   //	0.0 ~ 0.99999
		if (StartTime <= currentNormalizedTime && currentNormalizedTime <= EndTime)
			return true;

		return false;
	}

#if UNITY_EDITOR
	virtual public void OnGUI_PropertyWindow()
	{
	}

	virtual public void OnDrawGizmo(Transform t)
	{
	}
#endif
}
