//#define DEBUG_ON_DAMAGE_CHANGE_STATE

using UnityEngine;
using System.Collections;

public class MecanimEventBase : StateMachineBehaviour {
	
	static public bool s_bDisableMecanimEvent = false;
	static public bool s_bForceCallUpdate = false;

	public virtual bool RangeSignal { get { return false; } }

	public float StartTime = 0.0f;
	public float EndTime = 1.0f;

	// OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (s_bForceCallUpdate) return;

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
		if (s_bDisableMecanimEvent) return;

		// for animator.speed >= 0.0f

		//if (DebugStateHash == 0)
		//	DebugStateHash = Animator.StringToHash("Base Layer.Skill.attack_cutdown");

		int lastLoop = (int)_lastNormalizeTime;
		int currentLoop = (int)stateInfo.normalizedTime;
		float lastNormalizedTime = _lastNormalizeTime - lastLoop;
		float currentNormalizedTime = stateInfo.normalizedTime - currentLoop;   //	0.0 ~ 0.99999

		if (animator.IsInTransition(0))
		{
			AnimatorStateInfo nextAnimatorStateInfo = animator.GetNextAnimatorStateInfo(0);
			if (stateInfo.fullPathHash == nextAnimatorStateInfo.fullPathHash)
				currentNormalizedTime = nextAnimatorStateInfo.normalizedTime;
		}

		if (RangeSignal)
		{
			if (StartTime <= currentNormalizedTime && currentNormalizedTime < EndTime)
				OnRangeSignal(animator, stateInfo, layerIndex);

			if (lastNormalizedTime <= currentNormalizedTime)
			{
				if (lastNormalizedTime <= StartTime && StartTime <= currentNormalizedTime && currentNormalizedTime < EndTime)
					OnRangeSignalStart(animator, stateInfo, layerIndex);
				if (lastNormalizedTime <= EndTime && EndTime < currentNormalizedTime && lastNormalizedTime > StartTime)
					OnRangeSignalEnd(animator, stateInfo, layerIndex);
			}
			else
			{
				if (lastNormalizedTime <= EndTime && EndTime <= 1.0f && StartTime < lastNormalizedTime && StartTime > currentNormalizedTime)
					OnRangeSignalEnd(animator, stateInfo, layerIndex);
				if (0.0f <= StartTime && StartTime <= currentNormalizedTime && EndTime < currentNormalizedTime && EndTime < lastNormalizedTime)
					OnRangeSignalStart(animator, stateInfo, layerIndex);
			}
		}
		else
		{
			if (lastNormalizedTime <= currentNormalizedTime)
			{
				if (lastNormalizedTime <= StartTime && StartTime < currentNormalizedTime)
				{
					OnSignal(animator, stateInfo, layerIndex);
					//Debug.Log("3333333333");
				}
			}
			else
			{
				if (lastNormalizedTime <= StartTime && StartTime <= 1.0f)
				{
					OnSignal(animator, stateInfo, layerIndex);
					//Debug.LogFormat("22222 : lastTime = {0} / startTime = {1}", lastNormalizedTime, StartTime);
				}
				else if (0.0f <= StartTime && StartTime < currentNormalizedTime)
				{
					OnSignal(animator, stateInfo, layerIndex);
					//Debug.Log("111111111111");
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

	// Not Used
	public bool IsInRange(AnimatorStateInfo stateInfo)
	{
		if (!RangeSignal) return false;
		if (StartTime <= stateInfo.normalizedTime && stateInfo.normalizedTime <= EndTime)
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
