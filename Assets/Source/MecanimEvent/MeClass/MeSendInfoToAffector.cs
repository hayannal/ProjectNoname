using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeSendInfoToAffector : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public eAffectorType affectorType;
	public string sendInfo;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		affectorType = (eAffectorType)EditorGUILayout.EnumPopup("Affector Type :", affectorType);
		sendInfo = EditorGUILayout.TextField("Send Info :", sendInfo);
	}
#endif

	Actor _actor;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
		}
		if (_actor == null)
			return;

		AffectorBase affectorBase = _actor.affectorProcessor.GetFirstContinuousAffector(affectorType);
		if (affectorBase != null)
			affectorBase.SendInfo(sendInfo);
	}
}