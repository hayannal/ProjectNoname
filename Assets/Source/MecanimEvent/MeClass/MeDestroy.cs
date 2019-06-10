using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeDestroy : MecanimEventBase {

	override public bool RangeSignal { get { return false; } }
	public bool disableObject;
	public bool disableAnimator;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		disableObject = EditorGUILayout.Toggle("Disable Object :", disableObject);
		if (disableObject == false) disableAnimator = EditorGUILayout.Toggle("Disable Animator :", disableAnimator);
	}
#endif

	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
		if (disableAnimator)
			animator.enabled = false;
		else if (disableObject)
			animator.gameObject.SetActive(false);
		else
			Destroy(animator.gameObject);
	}
}