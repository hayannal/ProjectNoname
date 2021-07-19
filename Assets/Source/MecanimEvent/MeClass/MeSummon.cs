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
		RandomWorldPosition,
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
	public bool noLoop;
	public GameObject summonEffectPrefab;

	// 동시 소환 개수. 0이면 관리하지 않는다.
	public int activeMaxCount;
	public GameObject prevObjectDisableEffectPrefab;

	// 시그널 발동 후 이동을 해도 진행되는 형태의 소환이 필요해졌다.
	// 데몬 헌트리스와 스팀펑크 로봇의 소환에서 사용된다.
	public float spawnDelay;

	// 요일던전 보스때문에 추가한 기능. 0.0f 보다 클때는 굴려서 통과해야만 소환을 수행한다.
	public float randomSummonRate;
	// 마찬가지로 요일던전 보스 때문에 추가한 기능. 고정몬스터의 경우 랜덤포지션으로 찾은 위치에 이미 몬스터가 있을 경우 소환을 수행하지 않고 패스하도록 한다.
	public bool checkOverlapPosition;

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

		castingLoopEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Casting Loop Effect Object :", castingLoopEffectPrefab, typeof(GameObject), false);
		noLoop = EditorGUILayout.Toggle("No Loop! :", noLoop);
		summonEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Summon Effect Object :", summonEffectPrefab, typeof(GameObject), false);

		activeMaxCount = EditorGUILayout.IntField("Max Count :", activeMaxCount);
		if (activeMaxCount > 0)
			prevObjectDisableEffectPrefab = (GameObject)EditorGUILayout.ObjectField("Prev Object Disable Effect :", prevObjectDisableEffectPrefab, typeof(GameObject), false);
		spawnDelay = EditorGUILayout.FloatField("Spawn Delay :", spawnDelay);

		randomSummonRate = EditorGUILayout.FloatField("Random Rate :", randomSummonRate);
		checkOverlapPosition = EditorGUILayout.Toggle("Check Overlap Position! :", checkOverlapPosition);
	}
#endif


	Vector3 _createPosition;
	Quaternion _createRotation;
	Transform _castingLoopEffectTransform;
	bool _ignoreSummonByRandomResult;
	bool _ignoreSummonByOverlap;
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

		if (randomSummonRate > 0.0f)
		{
			// 0.8f 가 적혀있으면 80% 확률로 소환하는거다.
			if (Random.value > randomSummonRate)
			{
				_ignoreSummonByRandomResult = true;
				return;
			}
		}

		if (calcCreatePositionInEndSignal == false)
			CalcCreatePosition();

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
			else
			{
				int currentCount = 0;
				if (_listActiveObject != null) currentCount += _listActiveObject.Count;

				// 개수 제한 걸린만큼 기존 오브젝트를 비활성화 시켜야한다.
				if (currentCount >= activeMaxCount)
				{
					GameObject firstObject = _listActiveObject[0];
					_listActiveObject.RemoveAt(0);
					firstObject.SetActive(false);
					if (prevObjectDisableEffectPrefab != null)
						BattleInstanceManager.instance.GetCachedObject(prevObjectDisableEffectPrefab, firstObject.transform.position, Quaternion.identity);
				}
			}
		}

		if (checkOverlapPosition && createPositionType == eCreatePositionType.WorldPosition && calcCreatePositionInEndSignal == false)
		{
			// _createPosition에 위치가 저장되어있을텐데 여기에 몬스터가 이미 있다고 판단될 경우
			// 하필 근데 시그널이 동시에 실행되다보니 시그널끼리 겹치는건 막을수가 없다.
			// 결국 랜덤포지션일때도 못쓰고 월드 포지션이면서 calcCreatePositionInEndSignal값이 false인 경우에만 쓸 수 있는 한정적인 기능이 되었다.
			List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
			for (int i = 0; i < listMonsterActor.Count; ++i)
			{
				// x z 만 검사해야한다. 지하에 있는 경우에도 생성하면 안되니 y는 검사하지 않는다.
				Vector3 diff = listMonsterActor[i].cachedTransform.position - _createPosition;
				diff.y = 0.0f;
				if (diff.sqrMagnitude < 0.5f * 0.5f)
				{
					_ignoreSummonByOverlap = true;
					return;
				}
			}
		}

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
						_createRotation = Quaternion.LookRotation(_actor.cachedTransform.TransformDirection(direction));
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
			case eCreatePositionType.RandomWorldPosition:
				_createPosition = GetRandomWorldPosition();
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

	// TeleportTargetPositionAffector 에서 가져와 변형해서 쓴다.
	Vector3 GetRandomWorldPosition()
	{
		int tryBreakCount = 0;
		// 소환할 몬스터가 비슷한 Agent일거라 생각하고 코딩
		if (_agentTypeID == -1) _agentTypeID = MeLookAt.GetAgentTypeID(_actor);
		while (true)
		{
			Vector3 desirePosition = Vector3.zero;

			// 이걸 쓰게될 보스는 스테이지 보스라서 currentGround가 있다고 가정하고 처리해둔다.
			if (BattleInstanceManager.instance.currentGround != null)
				desirePosition = BattleInstanceManager.instance.currentGround.GetRandomPositionInQuadBound(1.0f);

			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = _agentTypeID;

			if (NavMesh.SamplePosition(desirePosition, out hit, 1.0f, navMeshQueryFilter))
			{
				Vector3 diff = hit.position - BattleInstanceManager.instance.playerActor.cachedTransform.position;
				diff.y = 0.0f;
				if (diff.sqrMagnitude > (1.5f * 1.5f))
					return new Vector3(hit.position.x, 0.0f, hit.position.z);
			}

			// exception handling
			++tryBreakCount;
			if (tryBreakCount > 200)
			{
				Debug.LogErrorFormat("Summon Random Position Error.");
				return desirePosition;
			}
		}
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

		// 랜덤을 굴려야하는 상황이라면 Start에서 굴렸을거다. 그 결과값이 false면 소환하지 않고 리턴한다. 해당 값은 1회 쓰고 초기화한다.
		if (randomSummonRate > 0.0f && _ignoreSummonByRandomResult)
		{
			_ignoreSummonByRandomResult = false;
			return;
		}

		// 위와 마찬가지로 플래그 걸려있으면 패스하고 초기화 시켜둔다.
		if (checkOverlapPosition && _ignoreSummonByOverlap)
		{
			_ignoreSummonByOverlap = false;
			return;
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

		// static으로 만들면 소환 도중에 씬 이동 할때나 기타 예외 상황이 발생했을때 리셋하기가 애매해서 BattleInstanceManager에서 카운트 하기로 한다.
		// 소환 도중에 몹이 죽을때 BattleModeProcessorBase.OnDieMonsterList 를 호출하지 않게 하기 위함이다.
		// RefCount가 1이라도 높으면 Delayed로 소환중인 몬스터가 있다는거다.
		// EvilLich의 경우엔 spawnDelay를 쓰지 않고 애니메이션 안에 있기 때문에 Die로 전환시 알아서 취소되니 RefCount를 증가시킬 필요가 없다.
		if (_actor.IsMonsterActor() && summonMonster)
			BattleInstanceManager.instance.AddDelayedSummonMonsterRefCount(1);

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

		// 체험모드 중에 소환을 걸어둔채로 체험모드를 나갔다면 소환을 취소
		if (ExperienceCanvas.instance != null && ExperienceCanvas.instance.gameObject.activeSelf == false && MainSceneBuilder.instance.lobby)
			yield break;

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

		yield return Timing.WaitForOneFrame;

		if (_actor.IsMonsterActor() && summonMonster)
			BattleInstanceManager.instance.AddDelayedSummonMonsterRefCount(-1);
	}

	void Summon()
	{
		if (calcCreatePositionInEndSignal)
			CalcCreatePosition();

		if (_castingLoopEffectTransform != null && noLoop == false)
		{
			_castingLoopEffectTransform.gameObject.SetActive(false);
			_castingLoopEffectTransform = null;
		}

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
		if (_castingLoopEffectTransform != null && spawnDelay == 0.0f && noLoop == false)
		{
			_castingLoopEffectTransform.gameObject.SetActive(false);
			_castingLoopEffectTransform = null;
		}
	}
}