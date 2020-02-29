using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeChangeMecanimState : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public float rate;
	public string stateName;
	public float fadeDuration = 0.05f;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		rate = EditorGUILayout.FloatField("Rate :", rate);
		stateName = EditorGUILayout.TextField("State Name :", stateName);
		fadeDuration = EditorGUILayout.FloatField("Fade Duration :", fadeDuration);
	}
#endif

	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		// AI의 파생 - 중단을 만들기 위해 추가한 시그널이다.
		// 적당히 특수 어펙터나 상태가 꼬이지 않을 타이밍이 Idle같은거로 전환하면 공격도중 중단하거나 다른걸 할 수 있게 된다.
		if (rate > 0.0f && Random.value <= rate)
			animator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(stateName), fadeDuration);
	}
}