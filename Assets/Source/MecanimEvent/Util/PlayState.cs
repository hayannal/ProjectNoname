using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 일반적인 경우엔 State Machine 내에서 Entry에 연결된 State가 자동으로 호출되는게 정상이나
// 같은 프레임에 Idle을 실행하면서 Trigger를 켰다던지에 의해
// 애니메이터가 제대로 동작하지 않는 경우가 생겼다.
// 그래서 추가한게 이 PlayState 스크립트다.
// StateMachine이 제대로 실행시키지 못할때 사용하면 될거다.
public class PlayState : ControlStateBase
{
	public string stateName;

	int _lastState = 0;
	// OnStateEnter is called before OnStateEnter is called on any state inside this state machine
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (animator.GetNextAnimatorStateInfo(0).fullPathHash == _lastState)
			return;
		if (string.IsNullOrEmpty(stateName))
			return;

		//if (_lastState == 0)
		//{
			_lastState = BattleInstanceManager.instance.GetActionNameHash(stateName);
			animator.CrossFade(_lastState, 0.05f);
		//}
	}

	// OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	//{
	//    
	//}

	// OnStateExit is called before OnStateExit is called on any state inside this state machine
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_lastState == stateInfo.fullPathHash && stateInfo.normalizedTime != 0.0f)
			_lastState = 0;
	}

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