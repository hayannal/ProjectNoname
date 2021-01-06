using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using MEC;

public class MeFlash : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }

	public float flashOnDuration = 0.2f;
	public float flashOnEndValue = 1.0f;
	public float flashOffDuration = 0.5f;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		flashOnDuration = EditorGUILayout.FloatField("On Duration :", flashOnDuration);
		flashOnEndValue = EditorGUILayout.FloatField("On EndValue :", flashOnEndValue);
		flashOffDuration = EditorGUILayout.FloatField("Off Duration :", flashOffDuration);
	}
#endif

	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		Timing.RunCoroutine(ScreenEffectProcess());
	}

	IEnumerator<float> ScreenEffectProcess()
	{
		FadeCanvas.instance.FadeOut(flashOnDuration, flashOnEndValue);
		yield return Timing.WaitForSeconds(flashOnDuration);

		if (this == null)
			yield break;

		FadeCanvas.instance.FadeIn(flashOffDuration);
	}
}