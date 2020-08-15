using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeSubAnimator : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public string boneName;
	public string stateName;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		boneName = EditorGUILayout.TextField("Bone Name :", boneName);
		stateName = EditorGUILayout.TextField("State Name :", stateName);
	}
#endif

	DummyFinder _dummyFinder = null;
	Animator _subAnimator = null;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (string.IsNullOrEmpty(boneName))
			return;

		if (_dummyFinder == null) _dummyFinder = animator.GetComponent<DummyFinder>();
		if (_dummyFinder == null) _dummyFinder = animator.gameObject.AddComponent<DummyFinder>();

		if (_subAnimator == null)
		{
			Transform findTransform = _dummyFinder.FindTransform(boneName);
			if (findTransform != null)
				_subAnimator = findTransform.GetComponent<Animator>();
		}

		if (_subAnimator != null)
			_subAnimator.CrossFade(BattleInstanceManager.instance.GetActionNameHash(stateName), 0.05f, 0, 0.0f);
	}
}