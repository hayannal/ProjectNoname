using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using ECM.Controllers;

public class MeLookAt : MecanimEventBase
{
	override public bool RangeSignal { get { return true; } }
	public bool lookAtTarget;
	public float leftRightRandomAngle;
	public float lootAtTargetOffsetAngle;
	public bool lookAtRandom;
	public float desireDistance = 5.0f;
	public float lerpPower = 60.0f;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		lookAtTarget = EditorGUILayout.Toggle("LookAt Target :", lookAtTarget);
		if (lookAtTarget)
		{
			lookAtRandom = false;
			leftRightRandomAngle = EditorGUILayout.FloatField("LeftRight Random Angle :", leftRightRandomAngle);
			if (leftRightRandomAngle > 0.0f) lootAtTargetOffsetAngle = 0.0f;
			lootAtTargetOffsetAngle = EditorGUILayout.FloatField("Offset Angle :", lootAtTargetOffsetAngle);
			if (lootAtTargetOffsetAngle != 0.0f) leftRightRandomAngle = 0.0f;
		}
		lookAtRandom = EditorGUILayout.Toggle("LookAt Random :", lookAtRandom);
		if (lookAtRandom)
		{
			lookAtTarget = false;
			desireDistance = EditorGUILayout.FloatField("Desire Distance :", desireDistance);
		}
		lerpPower = EditorGUILayout.FloatField("Lerp Power :", lerpPower);
	}
#endif

	Actor _actor = null;
	bool _initializedRandom = false;
	float _randomAngle;
	Vector3 _randomPosition;
	override public void OnRangeSignalStart(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

#if UNITY_EDITOR
		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.Rush))
		{
			// 원래라면 절대 들어오지 말아야하는데 러쉬 어펙터 실행 중에 LookAt이 실행된거다.
			//Debug.Break();
			Debug.LogError("Invalid call. Rush Affector is being applied.");
		}
#endif

		if (leftRightRandomAngle > 0.0f && _actor != null)
		{
			_randomAngle = Random.Range(-leftRightRandomAngle, leftRightRandomAngle);
			_initializedRandom = true;
		}

		if (lookAtRandom && _actor != null)
		{
			_randomPosition = GetRandomPosition();
			_initializedRandom = true;
			//_actor.baseCharacterController.movement.rotation = Quaternion.Euler(new Vector3(0.0f, Random.Range(0.0f, 360.0f), 0.0f));
		}
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		_initializedRandom = false;
	}

	override public void OnRangeSignal(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		Vector3 targetPosition = Vector3.zero;
		if (lookAtTarget && _actor != null && _actor != null)
		{
			if (_actor.targetingProcessor.GetTargetCount() > 0)
				targetPosition = _actor.targetingProcessor.GetTargetPosition(0);
			else
				return;
		}

		if (lookAtRandom && _initializedRandom)
			targetPosition = _randomPosition;

		Quaternion lookRotation = Quaternion.LookRotation(targetPosition - _actor.cachedTransform.position);
		if (lookAtTarget && leftRightRandomAngle > 0.0f && _initializedRandom)
		{
			Quaternion rotation = Quaternion.AngleAxis(_randomAngle, Vector3.up);
			lookRotation *= rotation;
		}
		if (lootAtTargetOffsetAngle != 0.0f)
		{
			Quaternion rotation = Quaternion.AngleAxis(lootAtTargetOffsetAngle, Vector3.up);
			lookRotation *= rotation;
		}

		if (lerpPower >= 60.0f)
			_actor.baseCharacterController.movement.rotation = lookRotation;
		else
			_actor.baseCharacterController.movement.rotation = Quaternion.Slerp(_actor.baseCharacterController.movement.rotation, lookRotation, lerpPower * Time.deltaTime);
	}



	int _agentTypeID = -1;
	Vector3 GetRandomPosition()
	{
		Vector3 randomPosition = Vector3.zero;
		Vector3 result = Vector3.zero;
		float maxDistance = 1.0f;
		int tryCount = 0;
		int tryBreakCount = 0;
		if (_agentTypeID == -1) _agentTypeID = GetAgentTypeID(_actor);
		while (true)
		{
			Vector2 randomCircle = Random.insideUnitCircle.normalized;
			Vector3 randomOffset = new Vector3(randomCircle.x * desireDistance, 0.0f, randomCircle.y * desireDistance);
			randomPosition = _actor.cachedTransform.position + randomOffset;

			// AI쪽 코드에서 가져와본다.
			randomPosition.y = 0.0f;

			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = _agentTypeID;
			if (NavMesh.SamplePosition(randomPosition, out hit, maxDistance, navMeshQueryFilter))
			{
				result = hit.position;
				break;
			}

			// exception handling
			++tryCount;
			if (tryCount > 20)
			{
				tryCount = 0;
				maxDistance += 1.0f;
			}

			++tryBreakCount;
			if (tryBreakCount > 400)
			{
				Debug.LogError("LookAtSignal RandomPosition Error. Not found valid random position.");
				return randomPosition;
			}
		}
		return result;
	}

	public static int GetAgentTypeID(Actor actor)
	{
		if (actor.IsMonsterActor())
		{
			MonsterActor monsterActor = actor as MonsterActor;
			if (monsterActor != null)
				return monsterActor.pathFinderController.agent.agentTypeID;
		}
		return 0;
	}
}