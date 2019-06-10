//#define DEBUG_ON_DAMAGE_CHANGE_STATE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeState : MecanimEventBase {
	
	override public bool RangeSignal { get { return true; } }
	public int state;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		state = EditorGUILayout.IntField("StateID :", state);
	}
#endif
	

	MecanimState mecanimState = null;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if DEBUG_ON_DAMAGE_CHANGE_STATE
		if (GameManager.Instance.testFlag)
		{
			if (animator.transform.name.Contains("Swordman"))
			{
				Debug.Log("State Signal Start : " + Time.frameCount);
				GameManager.Instance.testFlag = false;
			}
		}
#endif
		
		if (mecanimState == null)
			mecanimState = animator.GetComponent<MecanimState>();
		if (mecanimState != null)
			mecanimState.StartState(state, stateInfo.fullPathHash);
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (mecanimState == null)
			mecanimState = animator.GetComponent<MecanimState>();
		if (mecanimState != null)
			mecanimState.EndState(state, stateInfo.fullPathHash);
	}

	/*
	override public void OnRangeSignal (Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (stateInfo.fullPathHash == DebugStateHash)
		{
			Debug.Log("Debug State" + stateInfo.normalizedTime);
		}
	}
	*/
}