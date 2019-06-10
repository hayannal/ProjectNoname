using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeEffect : MecanimEventBase {

	override public bool RangeSignal { get { return false; } }
	public GameObject effectData;
	public Vector3 offset;
	public Vector3 direction = Vector3.forward;
	
#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		effectData = (GameObject)EditorGUILayout.ObjectField("Object :", effectData, typeof(GameObject), false);
		offset = EditorGUILayout.Vector3Field("Offset :", offset);
		direction = EditorGUILayout.Vector3Field("Direction :", direction);
	}
#endif

	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		//Vector3 result = offset * animator.transform.localScale.x;
		Vector3 rotation = animator.transform.TransformDirection(direction);
		Instantiate(effectData, animator.transform.TransformPoint(offset), Quaternion.LookRotation(rotation));
	}
}