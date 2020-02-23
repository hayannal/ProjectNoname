using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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
		TargetDistanceRatio,
	}

	override public bool RangeSignal { get { return true; } }
	public GameObject summonPrefab;
	public bool summonMonster = true;
	public eCreatePositionType createPositionType;
	public Vector3 offset;
	public Vector3 direction = Vector3.forward;
	public bool checkNavPosition = true;
	public bool calcCreatePositionInEndSignal;
	public bool disableOnMapChanged;
	public GameObject castingLoopEffectPrefab;
	public GameObject summonEffectPrefab;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		summonPrefab = (GameObject)EditorGUILayout.ObjectField("Object :", summonPrefab, typeof(GameObject), false);
		summonMonster = EditorGUILayout.Toggle("Summon Monster :", summonMonster);

		createPositionType = (eCreatePositionType)EditorGUILayout.EnumPopup("Create Position Type :", createPositionType);
		if (createPositionType == eCreatePositionType.WorldPosition)
			offset = EditorGUILayout.Vector3Field("Position :", offset);
		else if (createPositionType == eCreatePositionType.TargetDistanceRatio)
			offset.x = EditorGUILayout.FloatField("Distance Ratio :", offset.x);
		else
			offset = EditorGUILayout.Vector3Field("Offset :", offset);
		direction = EditorGUILayout.Vector3Field("Direction :", direction);
		checkNavPosition = EditorGUILayout.Toggle("Check Nav Position :", checkNavPosition);
		calcCreatePositionInEndSignal = EditorGUILayout.Toggle("Calc End Signal :", calcCreatePositionInEndSignal);

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

		if (calcCreatePositionInEndSignal == false)
			CalcCreatePosition();

		if (castingLoopEffectPrefab != null)
			_castingLoopEffectTransform = BattleInstanceManager.instance.GetCachedObject(castingLoopEffectPrefab, _createPosition, Quaternion.identity).transform;
	}

	void CalcCreatePosition()
	{
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
					if (checkNavPosition) _createPosition = GetNavPosition(_createPosition);
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
						if (checkNavPosition) _createPosition = GetNavPosition(_createPosition);
						_createRotation = Quaternion.LookRotation(targetTransform.TransformDirection(direction));
					}
				}
				break;
			case eCreatePositionType.TargetDistanceRatio:
				if (_actor.targetingProcessor.GetTargetCount() > 0)
				{
					Collider targetCollider = _actor.targetingProcessor.GetTarget();
					Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
					if (targetTransform != null)
					{
						_createPosition = Vector3.Lerp(_actor.cachedTransform.position, targetTransform.position, offset.x);
						if (checkNavPosition) _createPosition = GetNavPosition(_createPosition);
						_createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(direction));
					}
				}
				else
				{
					_createPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);
					if (checkNavPosition) _createPosition = GetNavPosition(_createPosition);
					else
					{
						if (BattleInstanceManager.instance.currentGround != null)
							_createPosition = BattleInstanceManager.instance.currentGround.SamplePositionInQuadBound(_createPosition);
					}
					_createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(direction));
				}
				break;
		}
	}

	Vector3 GetNavPosition(Vector3 desirePosition)
	{
		Vector3 result = Vector3.zero;
		float maxDistance = 1.0f;
		int tryBreakCount = 0;
		desirePosition.y = 0.0f;
		while (true)
		{
			// AI쪽 코드에서 가져와서 변형
			NavMeshHit hit;
			if (NavMesh.SamplePosition(desirePosition, out hit, maxDistance, NavMesh.AllAreas))
			{
				result = hit.position;
				break;
			}
			maxDistance += 1.0f;

			// exception handling
			++tryBreakCount;
			if (tryBreakCount > 50)
			{
				Debug.LogError("MeSummon NavPosition Error. Not found valid nav position.");
				return desirePosition;
			}
		}
		return result;
	}

	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		if (_castingLoopEffectTransform != null)
		{
			_castingLoopEffectTransform.gameObject.SetActive(false);
			_castingLoopEffectTransform = null;
		}

		if (calcCreatePositionInEndSignal)
			CalcCreatePosition();

		if (summonEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(summonEffectPrefab, _createPosition, Quaternion.identity);

		GameObject newObject = BattleInstanceManager.instance.GetCachedObject(summonPrefab, _createPosition, _createRotation, _actor.cachedTransform.parent);
		if (summonMonster)
		{
			// 겹쳐서 소환되는거 처리하기 위해 overlapPositionFrame 체크해둔다.
			GroupMonster groupMonster = newObject.GetComponent<GroupMonster>();
			if (groupMonster != null)
			{
				for (int i = 0; i < groupMonster.listMonsterActor.Count; ++i)
				{
					groupMonster.listMonsterActor[i].summonMonster = true;
					groupMonster.listMonsterActor[i].checkOverlapPositionFrameCount = 100;
				}
			}

			MonsterActor monsterActor = null;
			if (groupMonster == null)
			{
				monsterActor = newObject.GetComponent<MonsterActor>();
				if (monsterActor != null)
				{
					monsterActor.summonMonster = true;
					monsterActor.checkOverlapPositionFrameCount = 100;
				}
			}
		}
		if (disableOnMapChanged)
		{
			BattleInstanceManager.instance.OnInitializeSummonObject(newObject);
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