using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeBattleToast : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public string toastStringId;
	public float showTime = 2.0f;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		toastStringId = EditorGUILayout.TextField("String Id :", toastStringId);
		showTime = EditorGUILayout.FloatField("Show Time :", showTime);
	}
#endif

	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString(toastStringId), showTime);
	}
}