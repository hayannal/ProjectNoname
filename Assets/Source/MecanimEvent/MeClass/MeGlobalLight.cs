using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeGlobalLight : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public bool set = false;
	public float globalLightIntensityRatio = 1.0f;
	public float resetTimer;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		set = EditorGUILayout.Toggle("Set Or Reset :", set);
		globalLightIntensityRatio = EditorGUILayout.FloatField("Intensity Ratio :", globalLightIntensityRatio);
		resetTimer = EditorGUILayout.FloatField("Safe Reset Timer :", resetTimer);
	}
#endif

	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (set)
			EnvironmentSetting.SetGlobalLightIntensityRatio(globalLightIntensityRatio, resetTimer);
		else
			EnvironmentSetting.ResetGlobalLightIntensityRatio();
	}
}