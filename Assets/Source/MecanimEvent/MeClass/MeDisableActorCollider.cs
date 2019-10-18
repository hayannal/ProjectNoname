using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeDisableActorCollider : MecanimEventBase
{
	override public bool RangeSignal { get { return true; } }

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
	}
#endif

	Actor _actor = null;
	bool _waitEnd = false;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		if (_actor != null)
			HitObject.EnableRigidbodyAndCollider(false, _actor.GetRigidbody(), _actor.GetCollider());

		_waitEnd = true;
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor != null)
			HitObject.EnableRigidbodyAndCollider(true, _actor.GetRigidbody(), _actor.GetCollider());

		_waitEnd = false;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_waitEnd == true)
			OnRangeSignalEnd(animator, stateInfo, layerIndex);
	}
}