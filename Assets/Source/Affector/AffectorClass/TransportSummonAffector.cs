using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class TransportSummonAffector : AffectorBase
{
	float _endTime;
	Vector3 _transportPosition;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// 먼저 내비 위의 임의의 위치에 히트오브젝트 소환
		_transportPosition = Vector3.zero;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
		{
			// 플레이어 주변에서 생성하면 될듯
			_transportPosition = _actor.cachedTransform.position;
			Vector2 randomRadius = Random.insideUnitCircle * 5.5f;
			_transportPosition += new Vector3(randomRadius.x, 0.0f, randomRadius.y);
		}
		else if (_actor.targetingProcessor.GetTarget() == null)
		{
			// 타겟이 없다면 네비가 없을거다. 이럴땐 주변에 소환하기로 한다.
			_transportPosition = _actor.cachedTransform.position;
			Vector2 randomRadius = Random.insideUnitCircle.normalized * 2.0f;
			_transportPosition += new Vector3(randomRadius.x, 0.0f, randomRadius.y);
			if (BattleManager.instance != null && BattleManager.instance.IsNodeWar() == false && BattleInstanceManager.instance.currentGround != null)
				_transportPosition = BattleInstanceManager.instance.currentGround.SamplePositionInQuadBound(_transportPosition);
		}
		else
		{
			_transportPosition = GetRandomWorldPosition();
		}
		CreateHitObject();
		BattleInstanceManager.instance.FinalizeAllSummonObject();

		_spawnStage = StageManager.instance.playStage;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	int _spawnStage;
	public override void FinalizeAffector()
	{
		// 맵이동을 하고나서는 동작하면 안되는거라
		if (StageManager.instance.playStage != _spawnStage)
			return;

		// 일정 대기 시간 후에 같은 자리에 소환
		SummonObject();
	}

	// Summon 시그널에서 가져와 변형해서 쓴다.
	int _agentTypeID = -1;
	Vector3 GetRandomWorldPosition()
	{
		int tryBreakCount = 0;
		// 소환할 몬스터가 비슷한 Agent일거라 생각하고 코딩
		if (_agentTypeID == -1) _agentTypeID = MeLookAt.GetAgentTypeID(_actor);
		while (true)
		{
			Vector3 desirePosition = Vector3.zero;

			// 스테이지 있을거라 가정
			if (BattleInstanceManager.instance.currentGround != null)
				desirePosition = BattleInstanceManager.instance.currentGround.GetRandomPositionInQuadBound(1.0f);

			NavMeshHit hit;
			NavMeshQueryFilter navMeshQueryFilter = new NavMeshQueryFilter();
			navMeshQueryFilter.areaMask = NavMesh.AllAreas;
			navMeshQueryFilter.agentTypeID = BattleInstanceManager.instance.bulletFlyingAgentTypeID;

			if (NavMesh.SamplePosition(desirePosition, out hit, 1.0f, navMeshQueryFilter))
				return hit.position;

			// exception handling
			++tryBreakCount;
			if (tryBreakCount > 200)
			{
				Debug.LogErrorFormat("TransportAttackAffector Random Position Error.");
				return desirePosition;
			}
		}
	}

	//AffectorValueLevelTableData _createHitObjectAffectorValue;
	void CreateHitObject()
	{
		// DelayedCreateHitObjectAffector에서 했던거처럼 eAffectorType.CreateHitObject 에다가 전달하는거로는 위치를 정해줄 수가 없어서 직접 처리하기로 한다.
		// 코드복사가 조금 있지만 어쩔 수 없다.
		//if (_createHitObjectAffectorValue == null)
		//{
		//	_createHitObjectAffectorValue = new AffectorValueLevelTableData();
		//	_createHitObjectAffectorValue.sValue1 = _affectorValueLevelTableData.sValue1;
		//}
		//_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.CreateHitObject, _createHitObjectAffectorValue, _actor, false);

		GameObject meHitObjectInfoPrefab = FindPreloadObject(_affectorValueLevelTableData.sValue1);
		if (meHitObjectInfoPrefab == null)
			return;

		MeHitObjectInfo info = meHitObjectInfoPrefab.GetComponent<MeHitObjectInfo>();
		if (info == null)
			return;

		Transform spawnTransform = _actor.cachedTransform;
		Transform parentTransform = _actor.cachedTransform;
		Actor parentActor = _actor;

		// 월드좌표 타입이므로 넣어두면 된다.
		info.meHit.offset = _transportPosition;
		HitObject hitObject = HitObject.InitializeHit(spawnTransform, info.meHit, parentActor, parentTransform, null, 0.0f, 0, 0, 0);
		if (hitObject != null)
			hitObject.OverrideSkillLevel(_affectorValueLevelTableData.level);
	}

	void SummonObject()
	{
		GameObject summonPrefab = FindPreloadObject(_affectorValueLevelTableData.sValue2);
		if (summonPrefab == null)
			return;

		GameObject newObject = BattleInstanceManager.instance.GetCachedObject(summonPrefab, _transportPosition, Quaternion.identity);

		// 이건 무조건 등록해놔야한다. 그래야 맵 넘어갈때 사라지는 처리가 적용된다.
		BattleInstanceManager.instance.OnInitializeSummonObject(newObject);
	}
}