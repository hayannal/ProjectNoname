using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeSummon : MecanimEventBase
{
	public enum eCreatePositionType
	{
		WorldPosition,
		LocalPosition,
		TargetPosition,
	}

	override public bool RangeSignal { get { return true; } }
	public GameObject monsterPrefab;
	public eCreatePositionType createPositionType;
	public Vector3 offset;
	public Vector3 direction = Vector3.forward;
	public GameObject castingLoopEffectPrefab;
	public GameObject summonEffectPrefab;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		monsterPrefab = (GameObject)EditorGUILayout.ObjectField("Object :", monsterPrefab, typeof(GameObject), false);
		createPositionType = (eCreatePositionType)EditorGUILayout.EnumPopup("Create Position Type :", createPositionType);
		if (createPositionType == eCreatePositionType.WorldPosition)
			offset = EditorGUILayout.Vector3Field("Position :", offset);
		else
			offset = EditorGUILayout.Vector3Field("Offset :", offset);
		direction = EditorGUILayout.Vector3Field("Direction :", direction);
		castingLoopEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Casting Effect Object :", castingLoopEffectPrefab, typeof(GameObject), false);
		summonEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Summon Effect Object :", summonEffectPrefab, typeof(GameObject), false);
	}
#endif


	Vector3 _createPosition;
	Quaternion _createRotation;
	Transform _castingLoopEffectTransform;
	Actor _actor;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		_createPosition = Vector3.zero;
		switch (createPositionType)
		{
			case eCreatePositionType.WorldPosition:
				_createPosition = offset;
				_createRotation = Quaternion.LookRotation(direction);
				break;
			case eCreatePositionType.LocalPosition:
				if (_actor != null)
				{
					_createPosition = _actor.cachedTransform.TransformPoint(offset);
					_createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(direction));
				}
				break;
			case eCreatePositionType.TargetPosition:
				if (_actor.targetingProcessor.GetTargetCount() > 0)
				{
					Collider targetCollider = _actor.targetingProcessor.GetTarget();
					Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
					if (targetTransform != null)
					{
						_createPosition = targetTransform.TransformPoint(offset);
						_createRotation = Quaternion.LookRotation(targetTransform.TransformDirection(direction));
					}
				}
				break;
		}

		if (castingLoopEffectPrefab != null)
			_castingLoopEffectTransform = BattleInstanceManager.instance.GetCachedObject(castingLoopEffectPrefab, _createPosition, Quaternion.identity).transform;
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_castingLoopEffectTransform != null)
		{
			_castingLoopEffectTransform.gameObject.SetActive(false);
			_castingLoopEffectTransform = null;
		}

		if (summonEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(summonEffectPrefab, _createPosition, Quaternion.identity);

		// 겹쳐서 소환되는거 처리하기 위해 overlapPositionFrame 체크해둔다.
		GameObject newObject = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, _createPosition, _createRotation, _actor.cachedTransform.parent);
		GroupMonster groupMonster = newObject.GetComponent<GroupMonster>();
		if (groupMonster != null)
		{
			for (int i = 0; i < groupMonster.listMonsterActor.Count; ++i)
				groupMonster.listMonsterActor[i].checkOverlapPositionFrameCount = 100;
		}

		MonsterActor monsterActor = null;
		if (groupMonster == null)
		{
			monsterActor = newObject.GetComponent<MonsterActor>();
			if (monsterActor != null)
				monsterActor.checkOverlapPositionFrameCount = 100;
		}
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_castingLoopEffectTransform != null)
		{
			_castingLoopEffectTransform.gameObject.SetActive(false);
			_castingLoopEffectTransform = null;
		}
	}
}