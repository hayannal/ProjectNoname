﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewMeAttackIndicator", menuName = "CustomAsset/Create MeAttackIndicator", order = 1102)]
public class MeAttackIndicator : MecanimEventBase
{
	public enum eCreatePositionType
	{
		LocalPosition,
		TargetPosition,
		RushPosition,
		WorldPosition,
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
	public bool registerCustomTargetPosition;

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
					registerCustomTargetPosition = EditorGUILayout.Toggle("Register Custom Target Position :", registerCustomTargetPosition);
					break;
				case eCreatePositionType.RushPosition:
					offset.z = EditorGUILayout.FloatField("Distance Offset :", offset.z);
					break;
				case eCreatePositionType.WorldPosition:
					offset = EditorGUILayout.Vector3Field("Position :", offset);
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

							// 점프 시간은 줄어들면 안되기 때문에 _endTime 계산해놓고나서 _targetPosition과 _diff를 새로 계산해야한다.
							if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
							{
								Vector3 diff = createPosition - _actor.cachedTransform.position;
								diff = diff.normalized * 0.01f;
								createPosition = _actor.cachedTransform.position + diff;
							}
							else
							{
								float moveSpeedAddRate = _actor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.MoveSpeedAddRate);
								if (moveSpeedAddRate < 0.0f)
								{
									Vector3 diff = createPosition - _actor.cachedTransform.position;
									diff = diff.normalized * diff.magnitude * (1.0f + moveSpeedAddRate);
									createPosition = _actor.cachedTransform.position + diff;
								}
							}

							if (registerCustomTargetPosition)
								_actor.targetingProcessor.SetCustomTargetPosition(createPosition);
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
				case eCreatePositionType.WorldPosition:
					createPosition = offset;
					createRotation = Quaternion.LookRotation(startDirection);
					parentTransform = null;
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

	public void InitializeForGenerator(Vector3 position, Quaternion rotation, Transform spawnTransform)
	{
#if UNITY_EDITOR
		if (overrideLifeTime == 0.0f)
		{
			Debug.LogError("AttackIndicator for Generator need overrideLifeTime!");
			return;
		}
#endif

		AttackIndicator attackIndicator = BattleInstanceManager.instance.GetCachedAttackIndicator(attackIndicatorPrefab, position, rotation, attachPrefab ? spawnTransform : null);
		attackIndicator.InitializeAttackIndicator(this);
		attackIndicator.SetLifeTime(overrideLifeTime);
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