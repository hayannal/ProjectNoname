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
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (_prevSpeed == 0.0f)
			_prevSpeed = animator.speed;
		animator.speed = speed;
		_waitEnd = true;
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		animator.speed = _prevSpeed;
		_waitEnd = false;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (_waitEnd == true)
			OnRangeSignalEnd(animator, stateInfo, layerIndex);
	}

	override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateUpdate(animator, stateInfo, layerIndex);

		if (_waitEnd == true && ((animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash != stateInfo.fullPathHash) || IsInRange(stateInfo) == false))
			OnRangeSignalEnd(animator, stateInfo, layerIndex);

#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
		{
			MecanimEventBase.s_fAnimatorSpeed = IsInRange(stateInfo) ? speed : 1.0f;
			return;
		}
#endif
	}
}