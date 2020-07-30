using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using MEC;

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
	public bool applyParentTeamId;
	public bool excludeMonsterCount;
	public eCreatePositionType createPositionType;
	public Vector3 offset;
	public Vector2 randomPositionRadiusRange;
	public Vector3 direction = Vector3.forward;
	public bool checkNavPosition = true;
	public bool calcCreatePositionInEndSignal;
	public bool disableOnMapChanged;
	public GameObject castingLoopEffectPrefab;
	public GameObject summonEffectPrefab;

	// 동시 소환 개수. 0이면 관리하지 않는다.
	public int activeMaxCount;
	public GameObject prevObjectDisableEffectPrefab;

	// 시그널 발동 후 이동을 해도 진행되는 형태의 소환이 필요해졌다.
	// 데몬 헌트리스와 스팀펑크 로봇의 소환에서 사용된다.
	public float spawnDelay;

#if UNITY_EDITOR
	override public void OnGUI_PropertyWindow()
	{
		summonPrefab = (GameObject)EditorGUILayout.ObjectField("Object :", summonPrefab, typeof(GameObject), false);
		summonMonster = EditorGUILayout.Toggle("Summon Monster :", summonMonster);
		if (summonMonster)
		{
			applyParentTeamId = EditorGUILayout.Toggle("Apply Parent TeamId :", applyParentTeamId);
			excludeMonsterCount = EditorGUILayout.Toggle("Exclude Monster Count :", excludeMonsterCount);
		}

		createPositionType = (eCreatePositionType)EditorGUILayout.EnumPopup("Create Position Type :", createPositionType);
		if (createPositionType == eCreatePositionType.WorldPosition)
			offset = EditorGUILayout.Vector3Field("Position :", offset);
		else if (createPositionType == eCreatePositionType.TargetDistanceRatio)
			offset.x = EditorGUILayout.FloatField("Distance Ratio :", offset.x);
		else
			offset = EditorGUILayout.Vector3Field("Offset :", offset);
		randomPositionRadiusRange = EditorGUILayout.Vector2Field("Random Position Radius Range :", randomPositionRadiusRange);
		direction = EditorGUILayout.Vector3Field("Direction :", direction);
		checkNavPosition = EditorGUILayout.Toggle("Check Nav Position :", checkNavPosition);
		calcCreatePositionInEndSignal = EditorGUILayout.Toggle("Calc End Signal :", calcCreatePositionInEndSignal);

		castingLoopEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Casting Effect Object :", castingLoopEffectPrefab, typeof(GameObject), false);
		summonEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Summon Effect Object :", summonEffectPrefab, typeof(GameObject), false);

		activeMaxCount = EditorGUILayout.IntField("Max Count :", activeMaxCount);
		if (activeMaxCount > 0)
			prevObjectDisableEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Prev Object Disable Effect :", prevObjectDisableEffectPrefab, typeof(GameObject), false);
		spawnDelay = EditorGUILayout.FloatField("Spawn Delay :", spawnDelay);
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

		if (activeMaxCount > 0)
		{
			// 우선 맵이동 등으로 꺼져있는지를 판단해 리스트에서 정리해 둔 후
			if (_listActiveObject != null && _listActiveObject.Count > 0)
			{
				for (int i = _listActiveObject.Count - 1; i >= 0; --i)
				{
					if (_listActiveObject[i] == null || _listActiveObject[i].activeSelf == false)
						_listActiveObject.RemoveAt(i);
				}
			}

			int currentCount = 0;
			if (_listActiveObject != null) currentCount += _listActiveObject.Count;
			if (_reservedSummonFrameCount != 0) currentCount += 1;

			// 개수 제한 걸린만큼 기존 오브젝트를 비활성화 시켜야한다.
			if (currentCount >= activeMaxCount)
			{
				if (_reservedSummonFrameCount != 0)
				{
					// 만드려고 하는 Summon을 취소해야한다. 여기선 이펙트를 만들지 않고 패스한다.
					_reservedSummonFrameCount = 0;

					if (_castingLoopEffectTransform != null)
					{
						_castingLoopEffectTransform.gameObject.SetActive(false);
						_castingLoopEffectTransform = null;
					}
				}
				else if (_listActiveObject.Count > 0)
				{
					GameObject firstObject = _listActiveObject[0];
					_listActiveObject.RemoveAt(0);
					firstObject.SetActive(false);
					if (prevObjectDisableEffectPrefab != null)
						BattleInstanceManager.instance.GetCachedObject(prevObjectDisableEffectPrefab, firstObject.transform.position, Quaternion.identity);
				}
			}
		}
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
					else
					{
						if (BattleManager.instance != null && BattleManager.instance.IsNodeWar() == false && BattleInstanceManager.instance.currentGround != null)
							_createPosition = BattleInstanceManager.instance.currentGround.SamplePositionInQuadBound(_createPosition);
					}
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
				else
				{
					_createPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);
					if (checkNavPosition) _createPosition = GetNavPosition(_createPosition);
					else
					{
						if (BattleManager.instance != null && BattleManager.instance.IsNodeWar() == false && BattleInstanceManager.instance.currentGround != null)
							_createPosition = BattleInstanceManager.instance.currentGround.SamplePositionInQuadBound(_createPosition);
					}
					_createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(direction));
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
						_createRotation = Quaternion.LookRotation(targetTransform.position - _actor.cachedTransform.position);
					}
				}
				else
				{
					_createPosition = HitObject.GetFallbackTargetPosition(_actor.cachedTransform);
					if (checkNavPosition) _createPosition = GetNavPosition(_createPosition);
					else
					{
						if (BattleManager.instance != null && BattleManager.instance.IsNodeWar() == false && BattleInstanceManager.instance.currentGround != null)
							_createPosition = BattleInstanceManager.instance.currentGround.SamplePositionInQuadBound(_createPosition);
					}
					_createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(direction));
				}
				break;
		}
		Vector2 randomRadius = Random.insideUnitCircle * randomPositionRadiusRange;
		_createPosition += new Vector3(randomRadius.x, 0.0f, randomRadius.y);
	}

	int _agentTypeID = -1;
	Vector3 GetNavPosition(Vector3 desirePosition)
	{
		Vector3 result = Vector3.zero;
		float maxDistance = 1.0f;
		int tryBreakCount = 0;
		desirePosition.y = 0.0f;
		if (_agentTypeID == -1) _agentTypeID = MeLookAt.GetAgentTypeID(_actor);
		while (true)
		{
			// AI쪽 코드에서 가져와서 변형
			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = _agentTypeID;
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			{
				result = desirePosition;
				break;
			}
			if (NavMesh.SamplePosition(desirePosition, out hit, maxDistance, navMeshQueryFilter))
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

	// 위임 객체가 없기 때문에 리스트로 관리할 순 없고 예약은 1개만 가능하다.
	// 그러니 소환중에 또 소환하면 소환하려던걸 취소하고 다시 소환이 시작되는 구조다.
	//List<int> _listReservedSummonFrameCount;
	int _reservedSummonFrameCount;
	List<GameObject> _listActiveObject;
	override public void OnRangeSignalEnd(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (_actor == null)
		{
			if (animator.transform.parent != null)
				_actor = animator.transform.parent.GetComponent<Actor>();
			if (_actor == null)
				_actor = animator.GetComponent<Actor>();
		}

		if (spawnDelay == 0.0f)
			Summon();
		else
			Timing.RunCoroutine(DelayedSummon(spawnDelay, Time.frameCount));
	}

	IEnumerator<float> DelayedSummon(float delayTime, int frameCount)
	{
		if (activeMaxCount > 0)
			_reservedSummonFrameCount = frameCount;

		// 여기서 stage를 기록해두고
		int playStage = StageManager.instance.playStage;

		yield return Timing.WaitForSeconds(delayTime);

		// avoid gc
		if (this == null)
			yield break;
		if (_actor == null)
			yield break;
		if (_actor.gameObject == null)
			yield break;
		if (_actor.gameObject.activeSelf == false)
			yield break;

		// 맵이동하면 yield break.
		// 스테이지 이동 이벤트를 받기가 애매해서 이렇게 stage 비교로 처리해본다.
		if (playStage != StageManager.instance.playStage)
		{
			if (_castingLoopEffectTransform != null)
			{
				_castingLoopEffectTransform.gameObject.SetActive(false);
				_castingLoopEffectTransform = null;
			}
			yield break;
		}

		if (activeMaxCount > 0)
		{
			if (_reservedSummonFrameCount != frameCount)
			{
				// 연속된 소환 시그널로 이전 소환은 취소해야한다.
				yield break;
			}
			_reservedSummonFrameCount = 0;
		}
		Summon();
	}

	void Summon()
	{
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
					groupMonster.listMonsterActor[i].excludeMonsterCount = excludeMonsterCount;
					if (applyParentTeamId)
						groupMonster.listMonsterActor[i].reservedAllyTeam = (_actor.team.teamId == (int)Team.eTeamID.DefaultAlly);
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
					monsterActor.excludeMonsterCount = excludeMonsterCount;
					if (applyParentTeamId)
						monsterActor.reservedAllyTeam = (_actor.team.teamId == (int)Team.eTeamID.DefaultAlly);
				}
			}
		}
		if (disableOnMapChanged)
		{
			BattleInstanceManager.instance.OnInitializeSummonObject(newObject);
		}
		if (activeMaxCount > 0)
		{
			if (_listActiveObject == null)
				_listActiveObject = new List<GameObject>();
			_listActiveObject.Add(newObject);
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