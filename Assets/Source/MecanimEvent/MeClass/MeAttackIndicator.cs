using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeAttackIndicator : MecanimEventBase
{
	public enum eCreatePositionType
	{
		LocalPosition,
		TargetPosition,
		RushPosition,
		//WorldPosition,
	}

	override public bool RangeSignal { get { return true; } }

	public AttackIndicator.eIndicatorType indicatorType;
	public GameObject attackIndicatorPrefab;
	public float overrideLifeTime;
	public Vector3 offset;
	public Vector3 startDirection = Vector3.forward;
	public float areaRadius = 1.0f;
	public bool attachPrefab = true;
	public eCreatePositionType createPositionType;
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
		overrideLifeTime = EditorGUILayout.FloatField("Override LifeTime :", overrideLifeTime);

		if (indicatorType == AttackIndicator.eIndicatorType.Prefab)
		{
			areaRadius = EditorGUILayout.FloatField("Area Radius :", areaRadius);
			attachPrefab = EditorGUILayout.Toggle("Attach Prefab :", attachPrefab);
			createPositionType = (eCreatePositionType)EditorGUILayout.EnumPopup("Create Position Type :", createPositionType);
			switch (createPositionType)
			{
				case eCreatePositionType.LocalPosition:
				case eCreatePositionType.TargetPosition:
					offset = EditorGUILayout.Vector3Field("Offset :", offset);
					break;
				case eCreatePositionType.RushPosition:
					offset.z = EditorGUILayout.FloatField("Distance Offset :", offset.z);
					break;
			}
			startDirection = EditorGUILayout.Vector3Field("Direction :", startDirection);
		}
		else if (indicatorType == AttackIndicator.eIndicatorType.Line)
		{
			attachPrefab = true;
			createPositionType = eCreatePositionType.LocalPosition;
			offset = EditorGUILayout.Vector3Field("Offset :", offset);
			startDirection = EditorGUILayout.Vector3Field("Direction :", startDirection);

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
	Actor _actor = null;
	bool _waitEnd = false;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}
		if (_actor == null)
			return;

		if (_attackIndicator == null)
		{
			Vector3 createPosition = Vector3.zero;
			Quaternion createRotation = Quaternion.identity;
			Transform parentTransform = null;
			switch (createPositionType)
			{
				case eCreatePositionType.LocalPosition:
					createPosition = _actor.cachedTransform.TransformPoint(offset);
					createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(startDirection));
					parentTransform = _actor.cachedTransform;
					break;
				case eCreatePositionType.TargetPosition:
					if (_actor.targetingProcessor.GetTargetCount() > 0)
					{
						Collider targetCollider = _actor.targetingProcessor.GetTarget();
						Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
						if (targetTransform != null)
						{
							createPosition = targetTransform.TransformPoint(offset);
							createRotation = Quaternion.LookRotation(targetTransform.TransformDirection(startDirection));
							parentTransform = targetTransform;
						}
					}
					else
						return;
					break;
				case eCreatePositionType.RushPosition:
					if (_actor.targetingProcessor.GetTargetCount() > 0)
					{
						Collider targetCollider = _actor.targetingProcessor.GetTarget();
						Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
						if (targetTransform != null)
						{
							if (offset.z == 0.0f)
							{
								createPosition = targetTransform.position;
							}
							else
							{
								Vector3 diff = targetTransform.position - _actor.cachedTransform.position;
								float length = diff.magnitude + offset.z;
								if (length < 0.0f)
									length = 0.0f;
								createPosition = _actor.cachedTransform.position + diff.normalized * length;
							}
							createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(startDirection));
							parentTransform = _actor.cachedTransform;
						}
						else
							return;
					}
					break;
			}
			_attackIndicator = BattleInstanceManager.instance.GetCachedAttackIndicator(attackIndicatorPrefab, createPosition, createRotation, attachPrefab ? parentTransform : null);
			_attackIndicator.InitializeAttackIndicator(this);
		}

		if (overrideLifeTime == 0.0f)
			_waitEnd = true;
		else
		{
			_attackIndicator.SetLifeTime(overrideLifeTime);
			_attackIndicator = null;
		}
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_waitEnd == false)
			return;

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