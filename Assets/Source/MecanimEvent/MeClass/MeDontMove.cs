using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeDontMove : MecanimEventBase
{
	override public bool RangeSignal { get { return true; } }

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
	}
#endif

	LocalPlayerController _localPlayerController = null;
	PathFinderController _pathFinderController = null;
	bool _waitEnd = false;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_localPlayerController == null && _pathFinderController == null)
		{
			_localPlayerController = animator.transform.parent.GetComponent<LocalPlayerController>();
			if (_localPlayerController == null)
				_pathFinderController = animator.transform.parent.GetComponent<PathFinderController>();
		}

		if (_localPlayerController != null)
			_localPlayerController.dontMove = true;
		if (_pathFinderController != null)
			_pathFinderController.dontMove = true;

		_waitEnd = true;
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_localPlayerController != null)
			_localPlayerController.dontMove = false;
		if (_pathFinderController != null)
			_pathFinderController.dontMove = false;

		_waitEnd = false;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_waitEnd == true)
			OnRangeSignalEnd(animator, stateInfo, layerIndex);
	}
}