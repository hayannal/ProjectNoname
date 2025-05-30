﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayRandomState : ControlStateBase
{
	[Serializable]
	public class RandomStateInfo
	{
		public string stateName;
		public float weight;

		[NonSerialized] public float sumWeight;
	}
	public RandomStateInfo[] randomStateInfoList;

	float _sum = 0.0f;
	Actor _actor = null;
	int _lastState = 0;
	// OnStateEnter is called before OnStateEnter is called on any state inside this state machine
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (randomStateInfoList == null || randomStateInfoList.Length == 0)
			return;

		if (animator.GetNextAnimatorStateInfo(0).fullPathHash == _lastState)
			return;

		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		if (_actor == null)
			return;

		// 여기서 피검사를 해야 안전하려나. 자꾸 죽고나서 액션을 쓰는 경우가 발생하는거 같다.
		// 그냥 리턴하면 오히려 애니가 프리징 될테니 차라리 Die액션을 실행시켜본다.
		if (_actor.actorStatus.IsDie())
		{
			_actor.actionController.PlayActionByActionName("Die");
			return;
		}

		// 자꾸 이 루틴때문에 Random State 들어와서 아무것도 안할때가 있어서
		// 우선 제거해본다.
		//if (_lastState == 0)
		//{
			if (_sum == 0.0f)
			{
				for (int i = 0; i < randomStateInfoList.Length; ++i)
				{
					_sum += randomStateInfoList[i].weight;
					randomStateInfoList[i].sumWeight = _sum;
				}
			}
			float random = UnityEngine.Random.Range(0.0f, _sum);
			string selectedStateName = "";
			for (int i = 0; i < randomStateInfoList.Length; ++i)
			{
				if (random <= randomStateInfoList[i].sumWeight)
				{
					selectedStateName = randomStateInfoList[i].stateName;
					break;
				}
			}
			if (string.IsNullOrEmpty(selectedStateName) == false)
			{
				_lastState = BattleInstanceManager.instance.GetActionNameHash(selectedStateName);
				animator.CrossFade(_lastState, 0.05f);
			}
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
		//if (_lastState == stateInfo.fullPathHash && stateInfo.normalizedTime != 0.0f)
		//	_lastState = 0;

		// State Machine에 붙여놨더니 안에 들어있는 State들에 전부 적용되서 안에 들어있는 State들 호출될때마다
		// PlayRandomState Enter가 호출되는거였다.
		// 그래서 State Machine에 붙이는 구조를 버리고
		// 외부에 따로 State를 둔 후(Motion은 아예 할당하지 않음) 여기에 PlayRandomState를 붙이기로 한다.
		// 이 State는 호출되자마자 랜덤하게 바꾸는 역할만 한다.(애니가 실행되진 않는다.)
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
