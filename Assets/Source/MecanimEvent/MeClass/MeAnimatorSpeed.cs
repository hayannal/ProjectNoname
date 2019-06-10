using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeAnimatorSpeed : MecanimEventBase {

	override public bool RangeSignal { get { return true; } }
	public float speed = 1.0f;

	#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		speed = EditorGUILayout.FloatField("Speed :", speed);
	}
	#endif

	float _prevSpeed = 0.0f;
	bool _waitEnd = false;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		_prevSpeed = animator.speed;
		animator.speed = speed;
		_waitEnd = true;
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		animator.speed = _prevSpeed;
		_waitEnd = false;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_waitEnd == true)
			OnRangeSignalEnd(animator, stateInfo, layerIndex);
	}
}