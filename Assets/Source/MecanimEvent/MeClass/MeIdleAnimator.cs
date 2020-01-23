using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeIdleAnimator : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public bool enable;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		enable = EditorGUILayout.Toggle("Enable Idle Animator :", enable);
	}
#endif

	IdleAnimator _idleAnimator;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_idleAnimator == null)
			_idleAnimator = animator.GetComponent<IdleAnimator>();
		if (_idleAnimator != null)
			_idleAnimator.enabled = enable;
	}
}