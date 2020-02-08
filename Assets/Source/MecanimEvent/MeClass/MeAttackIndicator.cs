using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeAttackIndicator : MecanimEventBase
{
	override public bool RangeSignal { get { return true; } }

	public AttackIndicator.eIndicatorType indicatorType;
	public GameObject attackIndicatorPrefab;
	public Vector3 offset;
	public Vector3 startDirection = Vector3.forward;
	public float areaRadius = 1.0f;
	public float lineMaxDistance;
	public float lineWidth = 1.0f;
	public float sphereCastRadius = 0.2f;
	public bool wallThrough;
	public bool quadThrough;
	//public int bounceWallQuadCount;	// not supported

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		indicatorType = (AttackIndicator.eIndicatorType)EditorGUILayout.EnumPopup("Attack Indicator Type :", indicatorType);
		attackIndicatorPrefab = (GameObject)EditorGUILayout.ObjectField("Object :", attackIndicatorPrefab, typeof(GameObject), false);
		offset = EditorGUILayout.Vector3Field("Offset :", offset);
		startDirection = EditorGUILayout.Vector3Field("Direction :", startDirection);
		if (indicatorType == AttackIndicator.eIndicatorType.Prefab)
		{
			areaRadius = EditorGUILayout.FloatField("Area Radius :", areaRadius);
		}
		else if (indicatorType == AttackIndicator.eIndicatorType.Line)
		{
			lineMaxDistance = EditorGUILayout.FloatField("Max Distance :", lineMaxDistance);
			lineWidth = EditorGUILayout.FloatField("Width :", lineWidth);
			sphereCastRadius = EditorGUILayout.FloatField("SphereCast Radius :", sphereCastRadius);
			wallThrough = EditorGUILayout.Toggle("Wall Through :", wallThrough);
			quadThrough = EditorGUILayout.Toggle("Quad Through :", quadThrough);
			//bounceWallQuadCount = EditorGUILayout.IntField("Bounce Wall Quad Count :", bounceWallQuadCount);
		}
	}
#endif

	AttackIndicator _attackIndicator;
	Transform _spawnTransform;
	bool _waitEnd = false;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_spawnTransform == null)
		{
			if (animator.transform.parent != null)
				_spawnTransform = animator.transform.parent;
			if (_spawnTransform == null)
				_spawnTransform = animator.transform;
		}
		if (_spawnTransform != null)
		{
			if (_attackIndicator == null)
			{
				_attackIndicator = BattleInstanceManager.instance.GetCachedAttackIndicator(attackIndicatorPrefab, _spawnTransform.TransformPoint(offset), Quaternion.LookRotation(_spawnTransform.TransformDirection(startDirection)), _spawnTransform);
				_attackIndicator.InitializeAttackIndicator(this);
			}
		}
		_waitEnd = true;
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_attackIndicator != null)
		{
			_attackIndicator.FinalizeAttackIndicator();
			_attackIndicator = null;
		}
		_waitEnd = false;
	}

	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_waitEnd == true)
			OnRangeSignalEnd(animator, stateInfo, layerIndex);
	}
}