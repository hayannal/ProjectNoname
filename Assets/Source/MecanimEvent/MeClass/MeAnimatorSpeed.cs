﻿using System.Collections;
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

	void OnDisable()
	{
		if (_waitEnd == true && _animator != null)
		{
			_animator.speed = _prevSpeed;
			_waitEnd = false;
		}
	}

	float _prevSpeed = 0.0f;
	bool _waitEnd = false;
	Animator _animator;
	AffectorProcessor _affectorProcessor;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		if (_affectorProcessor == null)
		{
			if (animator.transform.parent != null)
				_affectorProcessor = animator.transform.parent.GetComponent<AffectorProcessor>();
		}

		if (_prevSpeed == 0.0f)
			_prevSpeed = animator.speed;
		animator.speed = speed;
		_animator = animator;
		_waitEnd = true;

		if (_affectorProcessor != null)
			_affectorProcessor.OnModifyAnimatorSpeed(true);
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
#if UNITY_EDITOR
		if (MecanimEventBase.s_bDisableMecanimEvent || MecanimEventBase.s_bForceCallUpdate)
			return;
#endif

		animator.speed = _prevSpeed;
		_waitEnd = false;

		if (_affectorProcessor != null)
			_affectorProcessor.OnModifyAnimatorSpeed(false);
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