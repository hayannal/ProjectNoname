using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeContinuousAttack : MecanimEventBase
{
	override public bool RangeSignal { get { return false; } }
	public float attackDelay;
	public float overrideRate = 1.0f;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		attackDelay = EditorGUILayout.FloatField("Override Attack Delay :", attackDelay);
		overrideRate = EditorGUILayout.FloatField("Rate :", overrideRate);
	}
#endif

	Actor _actor = null;
	MonsterActor _monsterActor = null;
	override public void OnSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		if (_actor.IsMonsterActor() == false)
			return;
		if (_monsterActor == null)
			_monsterActor = _actor as MonsterActor;

		if (overrideRate == 0.0f)
			return;
		if (overrideRate < 1.0f)
		{
			if (Random.value > overrideRate)
				return;
		}

		_monsterActor.monsterAI.standbyContinuousAttack = true;
		_monsterActor.monsterAI.standbyContinuousAttackDelay = attackDelay;
	}
}