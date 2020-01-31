using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;

// Tool 저장기능을 위한 Base
public class ControlStateBase : StateMachineBehaviour
{
	public enum eControlStateType
	{
		PlayState,
		PlayRandomState,
	}

	public static ControlStateBase CreateControlState(AnimatorState targetState, eControlStateType stateType)
	{
		ControlStateBase stateBase = null;
		switch (stateType)
		{
			#region USER_CODE
			case eControlStateType.PlayState: stateBase = targetState.AddStateMachineBehaviour<PlayState>(); break;
			case eControlStateType.PlayRandomState: stateBase = targetState.AddStateMachineBehaviour<PlayRandomState>(); break;
			#endregion
		}
		return stateBase;
	}

	public static eControlStateType GetControlStateType(ControlStateBase stateBase)
	{
		#region USER_CODE
		if (stateBase is PlayState) return eControlStateType.PlayState;
		if (stateBase is PlayRandomState) return eControlStateType.PlayRandomState;
		return eControlStateType.PlayState;
		#endregion
	}

	// OnStateEnter is called before OnStateEnter is called on any state inside this state machine
	//override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateExit is called before OnStateExit is called on any state inside this state machine
	//override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateMove is called before OnStateMove is called on any state inside this state machine
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateIK is called before OnStateIK is called on any state inside this state machine
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateMachineEnter is called when entering a state machine via its Entry Node
	//override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
	//{
	//    
	//}

	// OnStateMachineExit is called when exiting a state machine via its Exit Node
	//override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
	//{
	//    
	//}
}