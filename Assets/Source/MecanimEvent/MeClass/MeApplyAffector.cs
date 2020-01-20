using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using ActorStatusDefine;

public class MeApplyAffector : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public string affectorValueId;
	public int affectorValueLevel;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		affectorValueId = EditorGUILayout.TextField("AffectorValueId :", affectorValueId);
		affectorValueLevel = EditorGUILayout.IntField("Level :", affectorValueLevel);
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

		HitParameter hitParameter = new HitParameter();
		hitParameter.statusBase = new StatusBase();
		_actor.actorStatus.CopyStatusBase(ref hitParameter.statusBase);
		SkillProcessor.CopyEtcStatus(ref hitParameter.statusStructForHitObject, _actor);
		hitParameter.statusStructForHitObject.skillLevel = affectorValueLevel;
		_actor.affectorProcessor.ApplyAffectorValue(affectorValueId, hitParameter);
	}
}