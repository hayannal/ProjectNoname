using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeAnimatorParameter : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }

	public string parameterName;
	public AnimatorControllerParameterType parameterType;
	public float fValue;
	public int iValue;
	public bool bValue;
	public bool triggerValue;

	public bool useRandombValue;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		parameterName = EditorGUILayout.TextField("Parameter Name :", parameterName);
		parameterType = (AnimatorControllerParameterType)EditorGUILayout.EnumPopup("Parameter Type :", parameterType);
		switch (parameterType)
		{
			case AnimatorControllerParameterType.Float:
				fValue = EditorGUILayout.FloatField("Value :", fValue);
				break;
			case AnimatorControllerParameterType.Int:
				iValue = EditorGUILayout.IntField("Value :", iValue);
				break;
			case AnimatorControllerParameterType.Bool:
				bValue = EditorGUILayout.Toggle("Value", bValue);
				useRandombValue = EditorGUILayout.Toggle("Use Random", useRandombValue);
				break;
			case AnimatorControllerParameterType.Trigger:
				triggerValue = EditorGUILayout.Toggle("Value", triggerValue);
				break;
		}
	}
#endif

	int _hash;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_hash == 0)
			_hash = BattleInstanceManager.instance.GetActionNameHash(parameterName);

		switch (parameterType)
		{
			case AnimatorControllerParameterType.Float:
				animator.SetFloat(_hash, fValue);
				break;
			case AnimatorControllerParameterType.Int:
				animator.SetInteger(_hash, iValue);
				break;
			case AnimatorControllerParameterType.Bool:
				if (useRandombValue)
					bValue = (Random.value > 0.5f) ? true : false;
				animator.SetBool(_hash, bValue);
				break;
			case AnimatorControllerParameterType.Trigger:
				if (triggerValue)
					animator.SetTrigger(_hash);
				else
					animator.ResetTrigger(_hash);
				break;
		}
	}
}