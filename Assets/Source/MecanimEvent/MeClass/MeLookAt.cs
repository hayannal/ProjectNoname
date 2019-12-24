using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using ECM.Controllers;

public class MeLookAt : MecanimEventBase
{
	override public bool RangeSignal { get { return true; } }
	public bool lookAtTarget;
	public bool lookAtRandom;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		lookAtTarget = EditorGUILayout.Toggle("LookAt Target :", lookAtTarget);
		if (lookAtTarget) lookAtRandom = false;
		lookAtRandom = EditorGUILayout.Toggle("LookAt Random :", lookAtRandom);
		if (lookAtRandom) lookAtTarget = false;
	}
#endif

	BaseCharacterController _baseCharacterController = null;
	Actor _actor = null;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_baseCharacterController == null)
		{
			if (animator.transform.parent != null)
				_baseCharacterController = animator.transform.parent.GetComponent<BaseCharacterController>();
			if (_baseCharacterController == null)
				_baseCharacterController = animator.GetComponent<BaseCharacterController>();
		}
		if (lookAtTarget && _actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		if (lookAtRandom && _baseCharacterController != null)
			_baseCharacterController.movement.rotation = Quaternion.LookRotation(new Vector3(0.0f, Random.Range(0.0f, 360.0f), 0.0f));
	}

	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (lookAtTarget && _baseCharacterController != null && _actor != null && _actor.targetingProcessor.GetTargetCount() > 0)
			_baseCharacterController.movement.rotation = Quaternion.LookRotation(_actor.targetingProcessor.GetTargetPosition(0) - _actor.cachedTransform.position);
	}
}