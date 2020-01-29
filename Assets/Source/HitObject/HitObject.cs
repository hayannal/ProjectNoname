using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using MecanimStateDefine;

public class HitObject : MonoBehaviour
{
	public enum eTargetDetectType
	{
		Preset,
		Area,
		Collider,
		SphereCast,
	}

	public enum eCreatePositionType
	{
		Offset,
		Bone,
		TargetPosition,
	}

	#region staticFunction
	public static HitObject InitializeHit(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, StatusBase statusBase, float parentHitObjectCreateTime, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack)
	{
		// step 0. 셋팅되어 오지 않았다면 parentActor로부터 1회 계산해서 쭉 사용한다. 히트오브젝트를 한 프레임에도 여러개 만들기때문에 공용으로 사용한다.
		if (statusBase == null)
		{
			statusBase = new StatusBase();
			parentActor.actorStatus.CopyStatusBase(ref statusBase);
		}

		// step 1. Find Target and Reaction
		if (meHit.targetDetectType == eTargetDetectType.Preset)
		{
			// Preset은 hitObject객체를 만들지 않는다.
			TargetingProcessor targetSystem = parentActor.targetingProcessor;
			if (targetSystem != null)
			{
				for (int i = 0; i < targetSystem.GetTargetCount(); ++i)
				{
					Collider targetCollider = targetSystem.GetTargetList()[i];
					if (targetCollider == null)
						continue;
					AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
					if (affectorProcessor == null)
						continue;
					if (!Team.CheckTeamFilter(parentActor.team.teamId, targetCollider, meHit.teamCheckType))
						continue;
					float colliderRadius = ColliderUtil.GetRadius(targetCollider);
					if (colliderRadius == -1.0f)
						continue;

					HitParameter hitParameter = new HitParameter();
					Transform targetColliderTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
					hitParameter.hitNormal = parentTransform.forward;
					hitParameter.contactNormal = (parentTransform.position - targetColliderTransform.position).normalized;
					hitParameter.contactPoint = targetColliderTransform.position + (hitParameter.contactNormal * colliderRadius * 0.7f);
					hitParameter.contactPoint.y += targetCollider.bounds.size.y * 0.5f;
					hitParameter.statusBase = statusBase;
					CopyEtcStatusForHitObject(ref hitParameter.statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);

					ApplyAffectorValue(affectorProcessor, meHit.affectorValueIdList, hitParameter);

					if (meHit.showHitEffect)
						HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, hitParameter.statusStructForHitObject.weaponIDAtCreation);
					if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
						HitEffect.ShowHitEffectLineRenderer(meHit, GetSpawnPosition(spawnTransform, meHit, parentTransform, parentActor, hitSignalIndexInAction), hitParameter.contactPoint);
					if (meHit.showHitBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
						HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
					if (meHit.showHitRimBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
						HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);
				}
			}
			return null;
		}
		else if (meHit.targetDetectType == eTargetDetectType.Area || meHit.targetDetectType == eTargetDetectType.SphereCast)
		{
			StatusStructForHitObject statusStructForHitObject = new StatusStructForHitObject();
			CopyEtcStatusForHitObject(ref statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);

			Vector3 areaPosition = GetSpawnPosition(spawnTransform, meHit, parentTransform, parentActor, hitSignalIndexInAction);
			Vector3 areaDirection = spawnTransform.forward;
			Vector3 endPosition = Vector3.zero;
			if (meHit.targetDetectType == eTargetDetectType.Area)
				CheckHitArea(areaPosition, areaDirection, meHit, statusBase, statusStructForHitObject, GetGatePillarCompareTime(0.0f, parentHitObjectCreateTime));
			else if (meHit.targetDetectType == eTargetDetectType.SphereCast)
			{
				areaDirection = GetSpawnDirection(areaPosition, meHit, parentTransform, GetTargetPosition(meHit, parentActor, hitSignalIndexInAction), parentActor.targetingProcessor);
				endPosition = CheckSphereCast(areaPosition, areaDirection, meHit, statusBase, statusStructForHitObject, GetGatePillarCompareTime(0.0f, parentHitObjectCreateTime));
			}

			// HitObject 프리팹이 있거나 lifeTime이 있다면 생성하고 아니면 패스.
			Quaternion rotation = Quaternion.LookRotation(areaDirection);
			HitObject hitObject = GetCachedHitObject(meHit, areaPosition, rotation);
			if (hitObject != null)
			{
				hitObject.InitializeHitObject(meHit, parentActor, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);
				if (meHit.targetDetectType == eTargetDetectType.SphereCast)
				{
					// attach child
					hitObject.cachedTransform.parent = spawnTransform;
					hitObject._hitObjectSphereCastRayPath.SetEndPosition(endPosition);
				}
			}
			return hitObject;
		}

		// step2. Collider타입은 상황에 맞게 1개 혹은 여러개 만들어야한다.
		Vector3 targetPosition = GetTargetPosition(meHit, parentActor, hitSignalIndexInAction);
		Vector3 defaultPosition = GetSpawnPosition(spawnTransform, meHit, parentTransform, parentActor, hitSignalIndexInAction);
		Quaternion defaultRotation = Quaternion.LookRotation(GetSpawnDirection(defaultPosition, meHit, parentTransform, targetPosition, parentActor.targetingProcessor));
		bool normalAttack = parentActor.actionController.mecanimState.IsState((int)eMecanimState.Attack);
		int parallelAddCountByLevelPack = normalAttack ? ParallelHitObjectAffector.GetAddCount(parentActor.affectorProcessor) : 0;
		int parallelCount = meHit.parallelCount + parallelAddCountByLevelPack;
		if (parallelCount > 0)
		{
			float parallelDistance = meHit.parallelDistance;
			if (parallelDistance == 0.0f && parallelAddCountByLevelPack > 0) parallelDistance = ParallelHitObjectAffector.GetDistance(parentActor.affectorProcessor);
			for (int i = 0; i < parallelCount; ++i)
			{
				Vector3 position = GetParallelSpawnPosition(spawnTransform, meHit, parentTransform, parallelCount, i, parallelDistance, parentActor, hitSignalIndexInAction);
				Quaternion rotation = Quaternion.LookRotation(GetSpawnDirection(defaultPosition, meHit, parentTransform, targetPosition, parentActor.targetingProcessor));
				HitObject parallelHitObject = GetCachedHitObject(meHit, position, rotation);
				if (parallelHitObject == null)
					continue;
				parallelHitObject.InitializeHitObject(meHit, parentActor, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);
			}
		}

		bool ignoreMainHitObjectByGenerator = false;

		// 다른 어펙터와 달리 지속시간 대신 장판에 서있을때만 추가되는 어펙터다.
		int positionBuffCircularSectorCount = normalAttack ? PositionBuffAffector.GetCircularSectorCount(parentActor.affectorProcessor) : 0;
		int circularSectorCount = meHit.circularSectorCount;
		if (positionBuffCircularSectorCount > 0)
		{
			circularSectorCount = positionBuffCircularSectorCount;
			ignoreMainHitObjectByGenerator = true;
		}
		for (int i = 0; i < circularSectorCount; ++i)
		{
			float centerAngleY = meHit.circularSectorUseWorldSpace ? meHit.circularSectorWorldSpaceCenterAngleY : defaultRotation.eulerAngles.y;
			float baseAngle = circularSectorCount % 2 == 0 ? centerAngleY - (meHit.circularSectorBetweenAngle / 2f) : centerAngleY;
			float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, meHit.circularSectorBetweenAngle);
			HitObject circularSectorHitObject = GetCachedHitObject(meHit, defaultPosition, Quaternion.Euler(0.0f, angle, 0.0f));
			if (circularSectorHitObject == null)
				continue;
			circularSectorHitObject.InitializeHitObject(meHit, parentActor, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);
		}

		if (meHit.continuousHitObjectGeneratorBaseList != null)
		{
			for (int i = 0; i < meHit.continuousHitObjectGeneratorBaseList.Count; ++i)
			{
				ContinuousHitObjectGeneratorBase continuousHitObjectGenerator = BattleInstanceManager.instance.GetContinuousHitObjectGenerator(meHit.continuousHitObjectGeneratorBaseList[i].gameObject, defaultPosition, defaultRotation);
				ignoreMainHitObjectByGenerator |= continuousHitObjectGenerator.ignoreMainHitObject;
				continuousHitObjectGenerator.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);
			}
		}

		// 대각샷 좌우샷 후방샷 레벨팩
		int diagonalNwayAddCount = normalAttack ? DiagonalNwayGeneratorAffector.GetAddCount(parentActor.affectorProcessor) : 0;
		if (diagonalNwayAddCount > 0)
		{
			ContinuousHitObjectGeneratorBase continuousHitObjectGenerator = BattleInstanceManager.instance.GetContinuousHitObjectGenerator(BattleManager.instance.diagonalNwayGeneratorPrefab, defaultPosition, defaultRotation);
			ignoreMainHitObjectByGenerator |= continuousHitObjectGenerator.ignoreMainHitObject;
			continuousHitObjectGenerator.createCount = diagonalNwayAddCount;
			continuousHitObjectGenerator.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);
		}

		int leftRightNwayAddCount = normalAttack ? LeftRightNwayGeneratorAffector.GetAddCount(parentActor.affectorProcessor) : 0;
		if (leftRightNwayAddCount > 0)
		{
			ContinuousHitObjectGeneratorBase continuousHitObjectGenerator = BattleInstanceManager.instance.GetContinuousHitObjectGenerator(BattleManager.instance.leftRightNwayGeneratorPrefab, defaultPosition, defaultRotation);
			ignoreMainHitObjectByGenerator |= continuousHitObjectGenerator.ignoreMainHitObject;
			continuousHitObjectGenerator.createCount = leftRightNwayAddCount;
			continuousHitObjectGenerator.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);
		}

		int backNwayAddCount = normalAttack ? BackNwayGeneratorAffector.GetAddCount(parentActor.affectorProcessor) : 0;
		if (backNwayAddCount > 0)
		{
			ContinuousHitObjectGeneratorBase continuousHitObjectGenerator = BattleInstanceManager.instance.GetContinuousHitObjectGenerator(BattleManager.instance.backNwayGeneratorPrefab, defaultPosition, defaultRotation);
			ignoreMainHitObjectByGenerator |= continuousHitObjectGenerator.ignoreMainHitObject;
			continuousHitObjectGenerator.createCount = backNwayAddCount;
			continuousHitObjectGenerator.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);
		}

		bool createMainHitObject = true;
		if (meHit.ignoreMainHitObjectByParallel || meHit.ignoreMainHitObjectByCircularSector || ignoreMainHitObjectByGenerator)
			createMainHitObject = false;
		if (parallelAddCountByLevelPack > 0)
			createMainHitObject = false;
		if (createMainHitObject)
		{
			HitObject hitObject = GetCachedHitObject(meHit, defaultPosition, defaultRotation);
			if (hitObject != null)
				hitObject.InitializeHitObject(meHit, parentActor, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);
			return hitObject;
		}
		return null;
	}

	public static HitObject GetCachedHitObject(MeHitObject meHit, Vector3 position, Quaternion rotation)
	{
		HitObject hitObject = null;
		if (meHit.hitObjectPrefab != null)
		{
			hitObject = BattleInstanceManager.instance.GetCachedHitObject(meHit.hitObjectPrefab, position, rotation);
		}
		else if (meHit.lifeTime > 0.0f)
		{
			hitObject = BattleInstanceManager.instance.GetEmptyHitObject(position, rotation);
		}
		return hitObject;
	}

	public static Vector3 GetSpawnPosition(Transform spawnTransform, MeHitObject meHit, Transform parentActorTransform, Actor parentActor, int hitSignalIndexInAction)
	{
		Vector3 offset = meHit.offset;

		if (parentActor != null && parentActor.affectorProcessor != null)
		{
			if (BurrowAffector.CheckBurrow(parentActor.affectorProcessor))
				offset.y -= BurrowAffector.s_BurrowPositionY;
		}

		Transform t = spawnTransform;
		switch (meHit.createPositionType)
		{
			case eCreatePositionType.Offset: t = parentActorTransform; break;
			case eCreatePositionType.TargetPosition: t = GetTargetTransform(meHit, parentActor.targetingProcessor, hitSignalIndexInAction); break;
		}
		Vector3 spawnPosition = Vector3.zero;
		if (t != null)
		{
			if (offset == Vector3.zero)
				spawnPosition = t.position;
			else
			{
				if (meHit.createPositionType == eCreatePositionType.Bone && meHit.useBoneRotation == false)
				{
					Vector3 parentActorPosition = parentActorTransform.position;
					Vector3 offsetPosition = parentActorTransform.TransformPoint(offset);
					offsetPosition -= parentActorPosition;
					return spawnTransform.position + offsetPosition;
				}
				else
					spawnPosition = t.TransformPoint(offset);   // meHit.offset * parentTransform.localScale
			}
		}
		else
		{
			if (meHit.createPositionType == eCreatePositionType.TargetPosition)
			{
				if (parentActor.targetingProcessor.IsRegisteredCustomTargetPosition())
					spawnPosition = parentActor.targetingProcessor.GetCustomTargetPosition(0);
				else
					spawnPosition = GetFallbackTargetPosition(parentActorTransform);
			}
			spawnPosition += offset;
		}

		if (meHit.fixedWorldPositionY)
			spawnPosition.y = offset.y;
		return spawnPosition;
	}

	static Vector3 GetParallelSpawnPosition(Transform spawnTransform, MeHitObject meHit, Transform parentActorTransform, int parallelCount, int parallelIndex, float parallelDistance, Actor parentActor, int hitSignalIndexInAction)
	{
		Vector3 baseSpawnPosition = GetSpawnPosition(spawnTransform, meHit, parentActorTransform, parentActor, hitSignalIndexInAction);

		Vector3 parentActorPosition = parentActorTransform.position;
		Vector3 parallelOffset = Vector3.zero;
		parallelOffset.x = ((parallelCount - 1) * 0.5f * parallelDistance) * -1.0f + parallelDistance * parallelIndex;
		Vector3 offsetPosition = parentActorTransform.TransformPoint(parallelOffset);
		offsetPosition -= parentActorPosition;
		return baseSpawnPosition + offsetPosition;
	}

	public static Transform GetTargetTransform(MeHitObject meHit, TargetingProcessor targetingProcessor, int hitSignalIndexInAction)
	{
		int targetIndex = -1;
		if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToFirstTarget)
			targetIndex = 0;
		else if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToMultiTarget)
			targetIndex = hitSignalIndexInAction;

		return targetingProcessor.GetTargetTransform(targetIndex);
	}

	public static Vector3 GetTargetPosition(MeHitObject meHit, Actor parentActor, int hitSignalIndexInAction)
	{
		Vector3 targetPosition = Vector3.zero;
		if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToFirstTarget || meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToMultiTarget)
		{
			int targetIndex = -1;
			if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToFirstTarget)
				targetIndex = 0;
			else if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.ToMultiTarget)
				targetIndex = hitSignalIndexInAction;

			TargetingProcessor targetingProcessor = parentActor.targetingProcessor;
			if (targetingProcessor.GetTarget(targetIndex) != null)
				targetPosition = targetingProcessor.GetTargetPosition(targetIndex);
			else if (targetingProcessor.IsRegisteredCustomTargetPosition())
				targetPosition = targetingProcessor.GetCustomTargetPosition(targetIndex);
			else
				targetPosition = GetFallbackTargetPosition(parentActor.cachedTransform);
		}
		return targetPosition;
	}

	public static Vector3 GetFallbackTargetPosition(Transform t)
	{
		Vector3 fallbackPosition = new Vector3(0.0f, 0.0f, 4.0f);
		return t.TransformPoint(fallbackPosition);
	}

	public static Vector3 GetSpawnDirection(Vector3 spawnPosition, MeHitObject meHit, Transform parentActorTransform, Vector3 targetPosition, TargetingProcessor targetingProcessor = null)
	{
		Vector3 result = Vector3.zero;
		switch (meHit.startDirectionType)
		{
			case HitObjectMovement.eStartDirectionType.Forward:
				result = Vector3.forward;
				break;
			case HitObjectMovement.eStartDirectionType.Direction:
				result = meHit.startDirection.normalized;
				break;
			case HitObjectMovement.eStartDirectionType.ToFirstTarget:
			case HitObjectMovement.eStartDirectionType.ToMultiTarget:
				Vector3 diffToTargetPosition = targetPosition - spawnPosition;
				// 땅에 쏘는 직사를 구현할땐 이 라인을 패스하면 된다.
				diffToTargetPosition.y = 0.0f;
				// world to local
				result = parentActorTransform.InverseTransformDirection(diffToTargetPosition.normalized);
				// 플레이어 캐릭터 몸통을 클릭할때 몸 뒤로 쏘는걸 방지하기 위해 체크한다.
				if (targetingProcessor != null && targetingProcessor.IsRegisteredCustomTargetPosition())
				{
					if (result.z < 0.0f)
						result *= -1.0f;
				}
				break;
		}
		if (meHit.leftRightRandomAngle != 0.0f || meHit.upDownRandomAngle != 0.0f || meHit.leftRandomAngle != 0.0f || meHit.rightRandomAngle != 0.0f)
		{
			Vector3 tempUp = Vector3.up;
			if (result == tempUp) tempUp = -Vector3.forward;
			Vector3 right = Vector3.Cross(-tempUp, result);
			Vector3 up = Vector3.Cross(right, result);

			if (meHit.bothRandomAngle)
			{
				if (meHit.leftRightRandomAngle != 0.0f)
				{
					Quaternion rotation = Quaternion.AngleAxis(Random.Range(-meHit.leftRightRandomAngle, meHit.leftRightRandomAngle), up);
					result = rotation * result;
				}
			}
			else
			{
				if (meHit.leftRandomAngle != 0.0f || meHit.rightRandomAngle != 0.0f)
				{
					Quaternion rotation = Quaternion.AngleAxis(Random.Range(-meHit.leftRandomAngle, meHit.rightRandomAngle), up);
					result = rotation * result;
				}
			}
			if (meHit.upDownRandomAngle != 0.0f)
			{
				Quaternion rotation = Quaternion.AngleAxis(Random.Range(-meHit.upDownRandomAngle, meHit.upDownRandomAngle), right);
				result = rotation * result;
			}
		}
		if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.Direction && meHit.useWorldSpaceDirection)
			return result;
		return parentActorTransform.TransformDirection(result);
	}

	static float GetGatePillarCompareTime(float createTime, float parentHitObjectCreateTime)
	{
		if (parentHitObjectCreateTime > 0.0f)
			return parentHitObjectCreateTime;

		if (createTime > 0.0f)
			return createTime;

		return Time.time;
	}

	static void CopyEtcStatusForHitObject(ref StatusStructForHitObject statusStructForHitObject, Actor actor, MeHitObject meHit, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack)
	{
		statusStructForHitObject.actorInstanceId = actor.GetInstanceID();
		statusStructForHitObject.teamId = actor.team.teamId;
		statusStructForHitObject.skillLevel = actor.actionController.GetCurrentSkillLevelByCurrentAction();

		statusStructForHitObject.targetDetectType = meHit.targetDetectType;
		statusStructForHitObject.weaponIDAtCreation = 0;
		//if (meHit.useWeaponHitEffect)
		//	statusStructForHitObject.weaponIDAtCreation = actor.GetWeaponID(meHit.weaponDummyName);
		statusStructForHitObject.hitSignalIndexInAction = hitSignalIndexInAction;
		if (meHit.affectorValueIdList.Count > 0)
		{
			statusStructForHitObject.showHitBlink = meHit.showHitBlink;
			statusStructForHitObject.showHitRimBlink = meHit.showHitRimBlink;
		}
		else
			statusStructForHitObject.showHitBlink = statusStructForHitObject.showHitRimBlink = false;
		statusStructForHitObject.monsterActor = actor.IsMonsterActor();
		statusStructForHitObject.bossMonsterActor = false;
		if (statusStructForHitObject.monsterActor)
		{
			MonsterActor monsterActor = actor as MonsterActor;
			if (monsterActor != null)
				statusStructForHitObject.bossMonsterActor = monsterActor.bossMonster;
		}
		statusStructForHitObject.repeatIndex = repeatIndex;
		statusStructForHitObject.repeatAddCountByLevelPack = repeatAddCountByLevelPack;
		statusStructForHitObject.monsterThroughIndex = 0;
		statusStructForHitObject.ricochetIndex = 0;
		statusStructForHitObject.bounceWallQuadIndex = 0;
		bool normalAttack = actor.actionController.mecanimState.IsState((int)eMecanimState.Attack);
		statusStructForHitObject.monsterThroughAddCountByLevelPack = normalAttack ? MonsterThroughHitObjectAffector.GetAddCount(actor.affectorProcessor) : 0;
		statusStructForHitObject.ricochetAddCountByLevelPack = normalAttack ? RicochetHitObjectAffector.GetAddCount(actor.affectorProcessor) : 0;
		statusStructForHitObject.bounceWallQuadAddCountByLevelPack = normalAttack ? BounceWallQuadHitObjectAffector.GetAddCount(actor.affectorProcessor) : 0;
		statusStructForHitObject.parallelAddCountByLevelPack = normalAttack ? ParallelHitObjectAffector.GetAddCount(actor.affectorProcessor) : 0;
		// repeat은 액션이 끝나고도 딜레이 기다렸다가 나갈 수 있기 때문에 여기서 체크하면 안된다.
		//statusStructForHitObject.repeatAddCountByLevelPack = normalAttack ? RepeatHitObjectAffector.GetAddCount(actor.affectorProcessor) : 0;
	}

	static Collider[] s_colliderList = null;
	static List<AffectorProcessor> s_listAppliedAffectorProcessor;
	static void CheckHitArea(Vector3 areaPosition, Vector3 areaForward, MeHitObject meHit, StatusBase statusBase, StatusStructForHitObject statusForHitObject, float gatePillarCompareTime,
		List<AffectorProcessor> listOneHitPerTarget = null, Dictionary<AffectorProcessor, float> dicHitStayTime = null)
	{
		if (s_colliderList == null)
			s_colliderList = new Collider[100];

		// step 1. Physics.OverlapSphere
		float maxDistance = meHit.areaDistanceMax;
		maxDistance = Mathf.Max(Mathf.Abs(meHit.areaHeightMax), maxDistance);
		maxDistance = Mathf.Max(Mathf.Abs(meHit.areaHeightMin), maxDistance);
		int resultCount = Physics.OverlapSphereNonAlloc(areaPosition, maxDistance, s_colliderList); // meHit.areaDistanceMax * parentTransform.localScale.x

		// step 2. Check each object.
		float distanceMin = meHit.areaDistanceMin; // * parentTransform.localScale.x;
		float distanceMax = meHit.areaDistanceMax; // * parentTransform.localScale.x;
		Vector3 forward = Quaternion.Euler(0.0f, meHit.areaRotationY, 0.0f) * areaForward;

		if (s_listAppliedAffectorProcessor == null)
			s_listAppliedAffectorProcessor = new List<AffectorProcessor>();
		s_listAppliedAffectorProcessor.Clear();

		for (int i = 0; i < resultCount; ++i)
		{
			if (i >= s_colliderList.Length)
				break;

			Collider col = s_colliderList[i];

			// affector processor
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor == null)
				continue;

			// team check
			if (!Team.CheckTeamFilter(statusForHitObject.teamId, col, meHit.teamCheckType))
				continue;

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(col);
			if (colliderRadius == -1.0f) continue;

			// check sub parts
			// 이론대로라면 파츠를 가진 타겟을 처리할때
			// 해당 타겟의 파츠 중 어느 하나를 처리했다면 다른 파츠들은 continue시켜서 두번 처리되지 않게 하는건데
			// 이 로직에선 affectorProcessor를 가지고 처리하기 때문에 monsterActor 같은데 들어가서 파츠몹인지를 검사하기가 애매하다.
			// 그래서 아예 MonsterParts 같은 스크립트도 만들지 말고 컬리더만 여러개 부착시킨 후
			// 여러개의 컬리더 중 하나라도 처리되면 나머지는 처리하지 않도록 해본다.
			if (s_listAppliedAffectorProcessor.Contains(affectorProcessor))
				continue;

			// distance
			Vector3 diff = BattleInstanceManager.instance.GetTransformFromCollider(col).position - areaPosition;
			diff.y = 0.0f;
			if (diff.magnitude + colliderRadius < distanceMin) continue;
			if (diff.magnitude - colliderRadius > distanceMax) continue;

			// angle
			float angle = Vector3.Angle(forward, diff.normalized);
			float hypotenuse = Mathf.Sqrt(diff.sqrMagnitude + colliderRadius * colliderRadius);
			float adjustAngle = Mathf.Rad2Deg * Mathf.Acos(diff.magnitude / hypotenuse);
			if (meHit.areaAngle * 0.5f < angle - adjustAngle) continue;

			if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
				GatePillar.instance.CheckHitObject(statusForHitObject.teamId, gatePillarCompareTime, col);

			bool ignoreAffectorProcessor = false;

			// one Hit Per Target
			if (listOneHitPerTarget != null && meHit.oneHitPerTarget && listOneHitPerTarget.Contains(affectorProcessor))
				ignoreAffectorProcessor = true;

			// hit stay
			if (dicHitStayTime != null && meHit.useHitStay && CheckHitStayInterval(affectorProcessor, dicHitStayTime, meHit, statusForHitObject.actorInstanceId) == false)
				ignoreAffectorProcessor = true;
			if (dicHitStayTime == null && meHit.useHitStay)
				ignoreAffectorProcessor = true;

			if (ignoreAffectorProcessor == false)
			{
				HitParameter hitParameter = new HitParameter();
				hitParameter.hitNormal = forward;
				hitParameter.contactNormal = -diff.normalized;
				hitParameter.contactPoint = BattleInstanceManager.instance.GetTransformFromCollider(col).position + (hitParameter.contactNormal * colliderRadius * 0.7f);
				hitParameter.contactPoint.y += (meHit.areaHeightMin + meHit.areaHeightMax) * 0.5f;
				hitParameter.statusBase = statusBase;
				hitParameter.statusStructForHitObject = statusForHitObject;

				ApplyAffectorValue(affectorProcessor, meHit.affectorValueIdList, hitParameter);

				if (meHit.showHitEffect)
					HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, statusForHitObject.weaponIDAtCreation);
				if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
					HitEffect.ShowHitEffectLineRenderer(meHit, areaPosition, hitParameter.contactPoint);
				if (meHit.showHitBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
					HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
				if (meHit.showHitRimBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
					HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);

				if (listOneHitPerTarget != null && meHit.oneHitPerTarget)
					listOneHitPerTarget.Add(affectorProcessor);

				s_listAppliedAffectorProcessor.Add(affectorProcessor);
			}
		}
	}

	static RaycastHit[] s_raycastHitList = null;
	static List<RaycastHit> s_listMonsterRaycastHit = null;
	static Vector3 CheckSphereCast(Vector3 spawnPosition, Vector3 spawnForward, MeHitObject meHit, StatusBase statusBase, StatusStructForHitObject statusForHitObject, float gatePillarCompareTime,
		List<AffectorProcessor> listOneHitPerTarget = null, Dictionary<AffectorProcessor, float> dicHitStayTime = null)
	{
		if (s_raycastHitList == null)
			s_raycastHitList = new RaycastHit[100];
		if (s_listMonsterRaycastHit == null)
			s_listMonsterRaycastHit = new List<RaycastHit>();
		s_listMonsterRaycastHit.Clear();

		// step 1. Physics.SphereCastNonAlloc
		int resultCount = Physics.SphereCastNonAlloc(spawnPosition, meHit.sphereCastRadius, spawnForward, s_raycastHitList, meHit.defaultSphereCastDistance);

		// step 2. Through Test
		float reservedNearestDistance = meHit.defaultSphereCastDistance;
		Vector3 endPosition = Vector3.zero;
		for (int i = 0; i < resultCount; ++i)
		{
			if (i >= s_raycastHitList.Length)
				break;

			bool planeCollided = false;
			bool groundQuadCollided = false;
			bool wallCollided = false;
			bool monsterCollided = false;
			Vector3 wallNormal = Vector3.forward;

			Collider col = s_raycastHitList[i].collider;
			if (col.isTrigger)
				continue;

			if (BattleInstanceManager.instance.GetHitObjectFromCollider(col) != null)
				continue;

			if (BattleInstanceManager.instance.planeCollider != null && BattleInstanceManager.instance.planeCollider == col)
			{
				planeCollided = true;
				wallNormal = s_raycastHitList[i].normal;
			}

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
			{
				groundQuadCollided = true;
				wallNormal = s_raycastHitList[i].normal;
			}

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null)
			{
				if (Team.CheckTeamFilter(statusForHitObject.teamId, col, meHit.teamCheckType))
					monsterCollided = true;
			}
			else if (planeCollided == false && groundQuadCollided == false)
			{
				wallCollided = true;
				wallNormal = s_raycastHitList[i].normal;
			}

			if (planeCollided)
			{
				if (reservedNearestDistance > s_raycastHitList[i].distance)
				{
					reservedNearestDistance = s_raycastHitList[i].distance;
					endPosition = s_raycastHitList[i].point;
				}
			}

			if (groundQuadCollided && meHit.quadThrough == false)
			{
				if (reservedNearestDistance > s_raycastHitList[i].distance)
				{
					reservedNearestDistance = s_raycastHitList[i].distance;
					endPosition = s_raycastHitList[i].point;
				}
			}

			if (wallCollided && meHit.wallThrough == false)
			{
				if (reservedNearestDistance > s_raycastHitList[i].distance)
				{
					reservedNearestDistance = s_raycastHitList[i].distance;
					endPosition = s_raycastHitList[i].point;
				}
			}

			// 몹관통이 특정 숫자로 되어있으면 거기까지만 딱 관통되야하기 때문에 정렬이 필요하다. 그래서 몬스터껀 따로 모아둔다.
			if (monsterCollided)
			{
				s_listMonsterRaycastHit.Add(s_raycastHitList[i]);
			}
		}

		// wall HitEffect 처리 필요하려나.

		// 몬스터 raycast 정보는 정렬
		s_listMonsterRaycastHit.Sort((a, b) => a.distance.CompareTo(b.distance));

		if (s_listAppliedAffectorProcessor == null)
			s_listAppliedAffectorProcessor = new List<AffectorProcessor>();
		s_listAppliedAffectorProcessor.Clear();

		// step 3. Check each object.
		int monsterThroughCount = 0;
		for (int i = 0; i < s_listMonsterRaycastHit.Count; ++i)
		{
			// 예상 최단거리보다 넘어서는건 패스한다.
			if (s_listMonsterRaycastHit[i].distance > reservedNearestDistance)
				continue;

			Collider col = s_listMonsterRaycastHit[i].collider;

			// affector processor
			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor == null)
				continue;

			// object radius
			float colliderRadius = ColliderUtil.GetRadius(col);
			if (colliderRadius == -1.0f) continue;

			// check sub parts
			if (s_listAppliedAffectorProcessor.Contains(affectorProcessor))
				continue;

			bool ignoreAffectorProcessor = false;

			// one Hit Per Target
			if (listOneHitPerTarget != null && meHit.oneHitPerTarget && listOneHitPerTarget.Contains(affectorProcessor))
				ignoreAffectorProcessor = true;

			// hit stay
			if (dicHitStayTime != null && meHit.useHitStay && CheckHitStayInterval(affectorProcessor, dicHitStayTime, meHit, statusForHitObject.actorInstanceId) == false)
				ignoreAffectorProcessor = true;
			if (dicHitStayTime == null && meHit.useHitStay)
				ignoreAffectorProcessor = true;

			if (ignoreAffectorProcessor == false)
			{
				HitParameter hitParameter = new HitParameter();
				hitParameter.hitNormal = spawnForward;
				hitParameter.contactNormal = s_listMonsterRaycastHit[i].normal;
				hitParameter.contactPoint = s_listMonsterRaycastHit[i].point;
				hitParameter.statusBase = statusBase;
				hitParameter.statusStructForHitObject = statusForHitObject;

				ApplyAffectorValue(affectorProcessor, meHit.affectorValueIdList, hitParameter);

				if (meHit.showHitEffect)
					HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, statusForHitObject.weaponIDAtCreation);
				//if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
				//	HitEffect.ShowHitEffectLineRenderer(meHit, areaPosition, hitParameter.contactPoint);
				if (meHit.showHitBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
					HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
				if (meHit.showHitRimBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
					HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);

				if (listOneHitPerTarget != null && meHit.oneHitPerTarget)
					listOneHitPerTarget.Add(affectorProcessor);

				s_listAppliedAffectorProcessor.Add(affectorProcessor);
			}

			// Ray발사라서 PiercingHitObjectAffector에 영향받지 않는다.
			if (meHit.monsterThroughCount == 0)
			{
				endPosition = s_listMonsterRaycastHit[i].point;
				break;
			}
			if (meHit.monsterThroughCount > 0)
			{
				if (meHit.monsterThroughCount == monsterThroughCount)
				{
					endPosition = s_listMonsterRaycastHit[i].point;
					break;
				}
				++monsterThroughCount;
			}
		}

		return endPosition;
	}

	static void ApplyAffectorValue(AffectorProcessor affectorProcessor, List<string> listAffectorValueId, HitParameter hitParameter)
	{
		if (listAffectorValueId == null || listAffectorValueId.Count == 0) return;
		if (affectorProcessor == null) return;

		for (int i = 0; i < listAffectorValueId.Count; ++i)
			affectorProcessor.ApplyAffectorValue(listAffectorValueId[i], hitParameter, false, true);
	}
	#endregion


	public StatusStructForHitObject statusStructForHitObject { get { return _statusStructForHitObject; } }
	public float createTime { get { return _createTime; } }
	public HitObjectMovement hitObjectMovement { get { return _hitObjectMovement; } }

	MeHitObject _signal;
	float _createTime;
	Vector3 _createPosition;
	float _parentHitObjectCreateTime;
	StatusBase _statusBase;
	StatusStructForHitObject _statusStructForHitObject;
	Rigidbody _rigidbody { get; set; }
	Collider _collider { get; set; }
	List<TrailRenderer> _listTrailRendererAfterCollision;
	List<GameObject> _listDisableObjectAfterCollision;
	List<ParticleSystemRenderer> _listDisableParticleSystemRendererAfterCollision;
	bool _disableSelfObjectAfterCollision;

	// 기본적으로 벽 튕기는 처리나 투과를 위해서 모든 히트오브젝트는 컬리더로 되어있다.
	// 컬리젼으로 시작했다가 충돌시 잠시 트리거로 바뀌는걸 기억하기 위해 bool변수 하나 만들어둔다.
	bool _tempTriggerOnCollision;
	// 그러나 hitStay처럼 파고들어야하는 히트 오브젝트들은 기본 컬리더 말고 타격용 트리거를 추가로 가지고 있어야한다.
	// trigger만 존재하는 히트오브젝트의 가장 큰 단점이 빨라지면 결국 투과하기 때문에 충돌감지가 제대로 안된다는건데
	// OnTrigger에서 Ray를 쏘든 Collision으로 한프레임 바꾸든 혹은 매프레임 이전 포지션에 SphereCast를 쏘든 완벽하게 처리하려면 추가코드가 많이 필요하게 된다.
	// 그래서 차라리 개념을 바꿔서
	// 히트오브젝트에 붙은 컬리더와 똑같은 형태의 트리거를 만들어서 본체는 원래대로 충돌감지하고 이 트리거는 hitStay용도로 쓰기로 한다.
	Collider _triggerForHitStay;

	int _remainMonsterThroughCount;
	int _remainBounceWallQuadCount;
	int _remainRicochetCount;

	HitObjectMovement _hitObjectMovement;
	HitObjectLineRenderer _hitObjectLineRenderer;
	HitObjectAnimator _hitObjectAnimator;
	Animator _animator;
	HitObjectSphereCastRayPath _hitObjectSphereCastRayPath;

	void Awake()
	{
		_animator = GetComponent<Animator>();
	}

	public void InitializeHitObject(MeHitObject meHit, Actor parentActor, StatusBase statusBase, float parentHitObjectCreateTime, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack)
	{
		_signal = meHit;
		_createTime = Time.time;
		_parentHitObjectCreateTime = parentHitObjectCreateTime;
		_createPosition = cachedTransform.position;
		_statusBase = statusBase;
		CopyEtcStatusForHitObject(ref _statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);

#if UNITY_EDITOR
		//Debug.LogFormat("HitObject Create Time = {0}", _createTime);
#endif

		Team.eTeamLayer teamLayerType = Team.eTeamLayer.TeamLayer_Amount;
		switch (_statusStructForHitObject.teamId)
		{
			case (int)Team.eTeamID.DefaultAlly:
				switch (_signal.teamCheckType)
				{
					case Team.eTeamCheckFilter.Enemy: teamLayerType = Team.eTeamLayer.TEAM0_HITOBJECT_LAYER; break;
					case Team.eTeamCheckFilter.Ally: teamLayerType = Team.eTeamLayer.TEAM1_HITOBJECT_LAYER; break;
				}
				break;
			case (int)Team.eTeamID.DefaultMonster:
				switch (_signal.teamCheckType)
				{
					case Team.eTeamCheckFilter.Enemy: teamLayerType = Team.eTeamLayer.TEAM1_HITOBJECT_LAYER; break;
					case Team.eTeamCheckFilter.Ally: teamLayerType = Team.eTeamLayer.TEAM0_HITOBJECT_LAYER; break;
				}
				break;
		}
		Team.SetTeamLayer(gameObject, teamLayerType);

		if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
		if (_collider == null) _collider = GetComponentInChildren<Collider>();
		EnableRigidbodyAndCollider(true);
		InitializeDisableObject();

		if (_signal.targetDetectType != eTargetDetectType.Collider)
		{
			if (_collider != null) _collider.enabled = false;
		}

		// hitStay를 쓰면 트리거 하나를 추가로 가지고 있어야한다. 여기에 몹Through를 켜면 긁고 지나가는 투과형 다단히트 오브젝트가 된다.
		_tempTriggerOnCollision = false;
		if (_collider != null) _collider.isTrigger = false;
		if (_signal.useHitStay && _signal.targetDetectType == eTargetDetectType.Collider)
		{
			if (_triggerForHitStay == null)
			{
				_triggerForHitStay = ObjectUtil.CopyComponent<Collider>(_collider, gameObject);
				_triggerForHitStay.isTrigger = true;
			}
		}

		// 사실은 기본으로 들고있는 애들은 팩을 못먹게 할테니 덧셈 대신 덮어쓰는게 맞는데 어차피 0일테니 그냥 덧셈으로 해둔다.
		_remainMonsterThroughCount = _signal.monsterThroughCount + statusStructForHitObject.monsterThroughAddCountByLevelPack;
		_remainBounceWallQuadCount = _signal.bounceWallQuadCount + statusStructForHitObject.bounceWallQuadAddCountByLevelPack;
		_remainRicochetCount = _signal.ricochetCount + statusStructForHitObject.ricochetAddCountByLevelPack;

		// Sub Component
		if (meHit.lifeTime > 0.0f)
		{
			if (meHit.movable)
			{
				if (_hitObjectMovement == null)
				{
					_hitObjectMovement = GetComponent<HitObjectMovement>();
					if (_hitObjectMovement == null) _hitObjectMovement = gameObject.AddComponent<HitObjectMovement>();
				}
				_hitObjectMovement.InitializeSignal(meHit, parentActor, _rigidbody, hitSignalIndexInAction);
			}
			if (meHit.useLineRenderer)
			{
				if (_hitObjectLineRenderer == null)
				{
					_hitObjectLineRenderer = GetComponent<HitObjectLineRenderer>();
					if (_hitObjectLineRenderer == null) _hitObjectLineRenderer = gameObject.AddComponent<HitObjectLineRenderer>();
				}
				_hitObjectLineRenderer.InitializeSignal(meHit);
			}
			if (_animator != null)
			{
				if (_hitObjectAnimator == null)
				{
					_hitObjectAnimator = GetComponent<HitObjectAnimator>();
					if (_hitObjectAnimator == null) _hitObjectAnimator = gameObject.AddComponent<HitObjectAnimator>();
				}
				_hitObjectAnimator.InitializeSignal(parentActor, _animator, _statusBase, _createTime);
				_hitObjectAnimatorStarted = false;
				_waitHitObjectAnimatorUpdateCount = 0;
			}
		}
		if (meHit.targetDetectType == eTargetDetectType.SphereCast)
		{
			if (_hitObjectSphereCastRayPath == null)
				_hitObjectSphereCastRayPath = GetComponent<HitObjectSphereCastRayPath>();
			if (_hitObjectSphereCastRayPath != null)
				_hitObjectSphereCastRayPath.InitializeSignal(this, parentActor);
		}

		BattleInstanceManager.instance.OnInitializeHitObject(this, _collider);
	}

	void InitializeDisableObject()
	{
		if (_listDisableObjectAfterCollision == null)
		{
			_listDisableObjectAfterCollision = new List<GameObject>();
			_listTrailRendererAfterCollision = new List<TrailRenderer>();
			_listDisableParticleSystemRendererAfterCollision = new List<ParticleSystemRenderer>();

			HitObjectDisableAfterCollision hitObjectDisableAfterCollision = GetComponentInChildren<HitObjectDisableAfterCollision>();
			if (hitObjectDisableAfterCollision != null)
			{
				_disableSelfObjectAfterCollision = hitObjectDisableAfterCollision.disableHitObjectAfterCollision;
				for (int i = 0; i < hitObjectDisableAfterCollision.DeactivateObjectsAfterCollision.Length; ++i)
					_listDisableObjectAfterCollision.Add(hitObjectDisableAfterCollision.DeactivateObjectsAfterCollision[i]);
				for (int i = 0; i < hitObjectDisableAfterCollision.DisableParticlesAfterCollision.Length; ++i)
				{
					ParticleSystemRenderer particleSystemRenderer = hitObjectDisableAfterCollision.DisableParticlesAfterCollision[i].GetComponent<ParticleSystemRenderer>();
					if (particleSystemRenderer != null)
						_listDisableParticleSystemRendererAfterCollision.Add(particleSystemRenderer);
				}
			}

			RFX4_PhysicsMotion physicsMotion = GetComponentInChildren<RFX4_PhysicsMotion>();
			if (physicsMotion != null)
			{
				for (int i = 0; i < physicsMotion.DeactivateObjectsAfterCollision.Length; ++i)
					_listDisableObjectAfterCollision.Add(physicsMotion.DeactivateObjectsAfterCollision[i]);
			}

			ProjectileMoveScript projectileMoveScript = GetComponent<ProjectileMoveScript>();
			if (projectileMoveScript != null)
			{
				for (int i = 0; i < projectileMoveScript.trails.Count; ++i)
				{
					TrailRenderer trailRenderer = projectileMoveScript.trails[i].GetComponent<TrailRenderer>();
					if (trailRenderer != null)
						_listTrailRendererAfterCollision.Add(trailRenderer);
					else
						_listDisableObjectAfterCollision.Add(projectileMoveScript.trails[i]);
				}
				// self?
				_disableSelfObjectAfterCollision = true;
			}
		}
		else
		{
			// Reactive by pool
			for (int i = 0; i < _listDisableObjectAfterCollision.Count; ++i)
				_listDisableObjectAfterCollision[i].SetActive(true);
			for (int i = 0; i < _listTrailRendererAfterCollision.Count; ++i)
				_listTrailRendererAfterCollision[i].Clear();
			for (int i = 0; i < _listDisableParticleSystemRendererAfterCollision.Count; ++i)
				_listDisableParticleSystemRendererAfterCollision[i].enabled = true;
		}
	}

	public void OverrideSkillLevel(int level)
	{
		// 히트오브젝트를 생성하고 나서 액션에 따라 자동으로 설정된 레벨 대신 임의의 레벨을 덮어써야할때가 있다. 대표적으로 마인. 힐장판.
		_statusStructForHitObject.skillLevel = level;
	}

	void Update()
	{
		UpdateIgnoreList();

		if (_waitHitObjectAnimatorUpdateCount > 0)
		{
			_waitHitObjectAnimatorUpdateCount -= 1;
			if (_waitHitObjectAnimatorUpdateCount == 0)
			{
				BattleInstanceManager.instance.OnFinalizeHitObject(this, _collider);
				gameObject.SetActive(false);
			}
			return;
		}

		UpdateLifeTime();
		UpdateMaxDistance();

		// Range 시그널이 아닌 Area는 자체적으로 시간값 가지고 검사한다. 발사체 형태의 부채꼴을 처리하기 위함.
		if (_signal.RangeSignal == false && _signal.lifeTime > 0.0f)
			UpdateAreaOrSphereCast();
	}

	public void UpdateAreaOrSphereCast()
	{
		if (_signal.targetDetectType == eTargetDetectType.Area && _signal.areaHitLifeTimeEarlyOffset > 0.0f)
		{
			if (_createTime + (_signal.lifeTime - _signal.areaHitLifeTimeEarlyOffset) < Time.time)
				return;
		}

		switch (_signal.targetDetectType)
		{
			case eTargetDetectType.Area:
			case eTargetDetectType.SphereCast:
				if (_signal.oneHitPerTarget)
				{
					if (_listOneHitPerTarget == null)
						_listOneHitPerTarget = new List<AffectorProcessor>();
				}
				if (_signal.useHitStay)
				{
					if (_dicHitStayTime == null)
						_dicHitStayTime = new Dictionary<AffectorProcessor, float>();
				}
				break;
		}

		// Range시그널은 시그널쪽에서 호출되서 처리된다. 히트오브젝트 스스로는 하지 않는다. 이래야 시그널 범위 넘어섰을때 자동으로 호출되지 않는다.
		switch (_signal.targetDetectType)
		{
			case eTargetDetectType.Area:
				CheckHitArea(cachedTransform.position, cachedTransform.forward, _signal, _statusBase, _statusStructForHitObject, GetGatePillarCompareTime(), _listOneHitPerTarget, _dicHitStayTime);
				break;
			case eTargetDetectType.SphereCast:
				Vector3 endPosition = CheckSphereCast(cachedTransform.position, cachedTransform.forward, _signal, _statusBase, _statusStructForHitObject, GetGatePillarCompareTime(), _listOneHitPerTarget, _dicHitStayTime);
				if (_hitObjectSphereCastRayPath != null)
					_hitObjectSphereCastRayPath.SetEndPosition(endPosition);
				break;
		}
	}

	public float GetGatePillarCompareTime()
	{
		return HitObject.GetGatePillarCompareTime(_createTime, _parentHitObjectCreateTime);
	}

	//Vector3 _prevPosition = Vector3.zero;
	//void LateUpdate()
	//{
	//	_prevPosition = cachedTransform.position;
	//}


	void UpdateLifeTime()
	{
		if (_signal.RangeSignal == false && _createTime + _signal.lifeTime < Time.time)
		{
			OnFinalizeByLifeTime();
			return;
		}
	}

	void UpdateMaxDistance()
	{
		if (_signal.movable == false)
			return;
		if (_signal.maxDistance == 0.0f)
			return;

		Vector3 diff = cachedTransform.position - _createPosition;
		diff.y = 0.0f;
		if (diff.sqrMagnitude > _signal.maxDistance * _signal.maxDistance)
			OnFinalizeByDistance();
	}

	int HitObjectAnimatorUpdateWaitCount = 3;
	int _waitHitObjectAnimatorUpdateCount = 0;
	bool _hitObjectAnimatorStarted = false;
	public void FinalizeHitObject(bool ignoreAnimator = false)
	{
		if (_listOneHitPerTarget != null)
			_listOneHitPerTarget.Clear();
		if (_dicHitStayTime != null)
			_dicHitStayTime.Clear();
		ClearIgnoreList();
		_settedHitEffectLineRendererStartPosition = false;

		// 히트 오브젝트 애니메이터를 발동시켜놨으면 첫번째 프레임이 호출될때까지는 기다려야한다.
		if (_hitObjectAnimatorStarted && ignoreAnimator == false)
		{
			_hitObjectAnimatorStarted = false;
			_waitHitObjectAnimatorUpdateCount = HitObjectAnimatorUpdateWaitCount;
			return;
		}

		BattleInstanceManager.instance.OnFinalizeHitObject(this, _collider);
		//Destroy(gameObject);
		gameObject.SetActive(false);
	}

	void OnFinalizeByCollision(bool plane = false)
	{
		EnableRigidbodyAndCollider(false);

		for (int i = 0; i < _listDisableObjectAfterCollision.Count; ++i)
			_listDisableObjectAfterCollision[i].SetActive(false);
		for (int i = 0; i < _listDisableParticleSystemRendererAfterCollision.Count; ++i)
			_listDisableParticleSystemRendererAfterCollision[i].enabled = false;

		if (_hitObjectLineRenderer != null)
			_hitObjectLineRenderer.DisableLineRenderer(false);
		if (_hitObjectAnimator != null)
		{
			if (plane == false && _hitObjectAnimator.OnFinalizeByCollision())
				_hitObjectAnimatorStarted = true;
			if (plane == true && _hitObjectAnimator.OnFinalizeByCollisionPlane())
				_hitObjectAnimatorStarted = true;
		}

		if (_disableSelfObjectAfterCollision)
			FinalizeHitObject();
	}

	public void OnFinalizeByLifeTime()
	{
		if (_waitHitObjectAnimatorUpdateCount > 0)
			return;

		EnableRigidbodyAndCollider(false);

		if (_hitObjectAnimator != null && _hitObjectAnimator.OnFinalizeByLifeTime())
			_hitObjectAnimatorStarted = true;

		FinalizeHitObject();
	}

	public void OnFinalizeByDistance()
	{
		if (_waitHitObjectAnimatorUpdateCount > 0)
			return;

		EnableRigidbodyAndCollider(false);

		// Distance에 의해 꺼질때는 ByCollision처럼 disableSelfObjectAfterCollision 검사해서 Finalize하는게 이펙트 없어질때 더 예뻐보인다.
		for (int i = 0; i < _listDisableObjectAfterCollision.Count; ++i)
			_listDisableObjectAfterCollision[i].SetActive(false);
		for (int i = 0; i < _listDisableParticleSystemRendererAfterCollision.Count; ++i)
			_listDisableParticleSystemRendererAfterCollision[i].enabled = false;

		if (_hitObjectAnimator != null && _hitObjectAnimator.OnFinalizeByDistance())
			_hitObjectAnimatorStarted = true;

		// 이것도 마찬가지.
		if (_disableSelfObjectAfterCollision)
			FinalizeHitObject();
	}

	public void OnFinalizeByRangeSignal()
	{
		// SphereCast 타입이라면 천천히 사라지게 하고 삭제를 위임한다.
		if (_hitObjectSphereCastRayPath != null)
		{
			_hitObjectSphereCastRayPath.DisableRayPath();
			return;
		}

		OnFinalizeByLifeTime();
	}

	public void OnFinalizeByRemove()
	{
		if (_signal.showHitEffect)
			HitEffect.ShowHitEffect(_signal, cachedTransform.position, cachedTransform.forward, _statusStructForHitObject.weaponIDAtCreation);
		//if (_signal.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
		//	HitEffect.ShowHitEffectLineRenderer(_signal, GetHitEffectLineRendererStartPosition(contact.point), contact.point);
		FinalizeHitObject(true);
	}



	bool _settedHitEffectLineRendererStartPosition = false;
	Vector3 _reservedHitEffectLineRendererStartPosition;
	Vector3 GetHitEffectLineRendererStartPosition(Vector3 nextPosition)
	{
		Vector3 returnValue = Vector3.zero;
		if (_settedHitEffectLineRendererStartPosition)
			returnValue = _reservedHitEffectLineRendererStartPosition;
		else
			returnValue = _createPosition;

		_settedHitEffectLineRendererStartPosition = true;
		_reservedHitEffectLineRendererStartPosition = nextPosition;
		return returnValue;
	}

	List<AffectorProcessor> _listOneHitPerTarget = null;
	void OnCollisionEnter(Collision collision)
	{
		if (s_listAppliedAffectorProcessor == null)
			s_listAppliedAffectorProcessor = new List<AffectorProcessor>();
		s_listAppliedAffectorProcessor.Clear();

		//Debug.Log("hit object collision enter");
		bool collided = false;
		bool planeCollided = false;
		bool groundQuadCollided = false;
		bool wallCollided = false;
		bool monsterCollided = false;
		bool forceBarrierThrough = false;
		Vector3 wallNormal = Vector3.forward;
		foreach (ContactPoint contact in collision.contacts)
		{
			Collider col = contact.otherCollider;
			if (col == null)
				continue;

			if (BattleInstanceManager.instance.planeCollider != null && BattleInstanceManager.instance.planeCollider == col)
			{
				planeCollided = true;
				wallNormal = contact.normal;
			}

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
			{
				groundQuadCollided = true;
				wallNormal = contact.normal;
			}

			bool ignoreAffectorProcessor = false;
			if (_triggerForHitStay != null && contact.thisCollider == _collider)
				ignoreAffectorProcessor = true;

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null)
			{
				// check sub parts
				if (s_listAppliedAffectorProcessor.Contains(affectorProcessor))
					continue;

				if (_signal.oneHitPerTarget)
				{
					if (_listOneHitPerTarget == null) _listOneHitPerTarget = new List<AffectorProcessor>();
					if (_listOneHitPerTarget.Contains(affectorProcessor))
						ignoreAffectorProcessor = true;
				}

				if (ignoreAffectorProcessor == false && _signal.useHitStay == false)
				{
					OnCollisionEnterAffectorProcessor(affectorProcessor, contact.point, contact.normal);
					if (_signal.oneHitPerTarget)
						_listOneHitPerTarget.Add(affectorProcessor);
					if (_remainRicochetCount > 0 && _hitObjectMovement != null)
						_hitObjectMovement.AddRicochet(col, _remainRicochetCount == _signal.ricochetCount);
					monsterCollided = true;

					if (_remainMonsterThroughCount > 0 || _remainMonsterThroughCount == -1)
						AddIgnoreList(col);
					// 리코세는 가능여부 판단하고 해야해서 OnPostCollided함수 안에서 한다.

					s_listAppliedAffectorProcessor.Add(affectorProcessor);
				}
			}
			else if (planeCollided == false && groundQuadCollided == false)
			{
				if (CreateWallAffector.TEAM0_BARRIER_LAYER != 0 && col.gameObject.layer == CreateWallAffector.TEAM0_BARRIER_LAYER && Vector3.Dot(cachedTransform.forward, contact.normal) > 0.0f)
				{
					// 적들이 쏜 총알이 레벨팩 배리어 안쪽에 맞으면 관통 처리를 해준다.
					// 이펙트는 뜨지 않게 하기위해 하단에서 wallCollided를 true로 바꾼다.
					//wallCollided = true;
					forceBarrierThrough = true;
					AddIgnoreList(col);
				}
				else
				{
					wallCollided = true;
					wallNormal = contact.normal;

					if (_signal.wallThrough)
						AddIgnoreList(col);
				}
			}

			collided = planeCollided || groundQuadCollided || wallCollided || monsterCollided;
			if (collided)
			{
				bool ignoreEffect = false;
				if (_signal.movementType == HitObjectMovement.eMovementType.Howitzer && _signal.wallThrough && wallCollided)
					ignoreEffect = true;

				if (ignoreEffect == false)
				{
					if (_signal.showHitEffect)
						HitEffect.ShowHitEffect(_signal, contact.point, contact.normal, _statusStructForHitObject.weaponIDAtCreation);
					if (_signal.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
						HitEffect.ShowHitEffectLineRenderer(_signal, GetHitEffectLineRendererStartPosition(contact.point), contact.point);
				}
			}

			if (collided && _signal.contactAll == false)
				break;
			if (forceBarrierThrough)
			{
				collided = wallCollided = true;
				break;
			}
		}

		OnPostCollided(collided, planeCollided, groundQuadCollided, wallCollided, monsterCollided, wallNormal, forceBarrierThrough);
	}

	void OnPostCollided(bool collided, bool planeCollided, bool groundQuadCollided, bool wallCollided, bool monsterCollided, Vector3 wallNormal, bool forceBarrierThrough)
	{
		if (collided == false)
			return;

		// Check End of HitObject
		bool useThrough = false;
		bool useBounce = false;
		if (monsterCollided)
		{
			bool ricochetApplied = false;
			if (_remainRicochetCount > 0 && _hitObjectMovement != null && _hitObjectMovement.IsEnableRicochet(_statusStructForHitObject.teamId))
			{
				// 리코세를 하기 위해선 각도에 따라 몹을 관통하기도 관통 안하기도 한다.
				// 그렇다고 이걸 일일이 각도 체크하면서 하기엔 위험부담이 있어서
				// 차라리 리코세 적용시에 몹의 몸 중심으로 옮겨놓고 트리거로 임시로 바꿔둔채(Through 하듯) 발사하는 식으로 풀게 되었다.
				// 그렇데 이렇게 할 경우 몹이 죽을때는 컬리더랑 리지드바디까지 다 끄기때문에 trigger로 해둔게 풀리지 않게 된다.
				// 그래서 해당몹의 컬리더 상태를 확인해서 처리하도록 한다.
				bool colliderEnabled = false;
				if (_hitObjectMovement.ApplyRicochet(ref colliderEnabled))
				{
					ricochetApplied = true;
					if (_statusStructForHitObject.ricochetAddCountByLevelPack > 0)
						++_statusStructForHitObject.ricochetIndex;
					_remainRicochetCount -= 1;
					if (colliderEnabled)
					{
						if (_signal.useHitStay == false)
						{
							Collider lastRicochetCollider = _hitObjectMovement.GetLastRicochetCollider();
							if (lastRicochetCollider != null)
								AddIgnoreList(lastRicochetCollider);
						}
						useThrough = true;
					}
					else
						return;
				}
			}

			if (ricochetApplied)
			{
				// nothing
			}
			else if ((_remainMonsterThroughCount > 0 || _remainMonsterThroughCount == -1))
			{
				if (_statusStructForHitObject.monsterThroughAddCountByLevelPack > 0)
					++_statusStructForHitObject.monsterThroughIndex;
				if (_remainMonsterThroughCount > 0) _remainMonsterThroughCount -= 1;
				useThrough = true;
			}
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		if (wallCollided)
		{
			if (forceBarrierThrough)
				useThrough = true;
			else if (_remainBounceWallQuadCount > 0)
			{
				if (_statusStructForHitObject.bounceWallQuadAddCountByLevelPack > 0)
					++_statusStructForHitObject.bounceWallQuadIndex;
				_remainBounceWallQuadCount -= 1;
				useBounce = true;
			}
			else if (_signal.wallThrough)
				useThrough = true;
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		if (groundQuadCollided)
		{
			if (_remainBounceWallQuadCount > 0)
			{
				if (_statusStructForHitObject.bounceWallQuadAddCountByLevelPack > 0)
					++_statusStructForHitObject.bounceWallQuadIndex;
				_remainBounceWallQuadCount -= 1;
				useBounce = true;
			}
			else if (_signal.quadThrough)
				useThrough = true;
			else
			{
				OnFinalizeByCollision();
				return;
			}
		}

		if (planeCollided)
		{
			OnFinalizeByCollision(true);
			return;
		}

		if (useBounce)
		{
			if (_hitObjectMovement != null)
				_hitObjectMovement.Bounce(wallNormal);
			return;
		}

		if (useThrough)
		{
			if (_signal.useHitStay && _triggerForHitStay != null)
			{
				_tempTriggerOnCollision = true;
				_collider.isTrigger = true;
			}
			if (_hitObjectMovement != null)
				_hitObjectMovement.ReinitializeForThrough();
			return;
		}

		OnFinalizeByCollision();
	}

	void OnTriggerExit(Collider collider)
	{
		// 중첩되어있는 오브젝트들을 위해서라도 refCount 형태로 관리해야하려나.
		if (_tempTriggerOnCollision && _collider.isTrigger)
		{
			_collider.isTrigger = false;
			_tempTriggerOnCollision = false;
		}
	}

	// 컬리젼도 Stay가 가능하다. 부착된 채로 떨어지기전까지 계속 호출되는 구조다.
	// 그러나 컬리젼으로 Stay를 판단하는 경우가 거의 없을거 같고 충분히 트리거로도 할 수 있는거라 코드 간결성을 위해 빼기로 한다.
	//void OnCollisionStay(Collision collision)
	//{
	//}

	// OnTriggerEnter 호출되는 프레임부터 같이 호출되기 때문에 Stay에서만 처리해도 괜찮다.
	// 사실 관통중이라면 충돌을 담당하는 컬리더가 임시로 trigger로 바뀌어져있을테고 이때는 Stay가 두번 같이 올 수 있는데
	// 어차피 Interval따라 데미지 처리할 수 있는지 체크할거기 때문에 여러번 와도 상관없긴 하다.
	// 그리고 모든 충돌체 관련 처리는 OnCollisionEnter에서 하기때문에 여기선 hitStay처리만 하면 된다.
	//RaycastHit[] _hitInfoList = null;
	void OnTriggerStay(Collider other)
	{
		if (_tempTriggerOnCollision && _collider.isTrigger)
		{
			// 여기서 리턴시켜버리면 hitStay오브젝트가 관통하는 순간엔 체크가 안되서 하면 안된다.
			//return;
		}
		if (_triggerForHitStay == null)
			return;
		if (other.isTrigger)
			return;

		Collider col = other;
		if (col == null)
			return;

		if (_signal.useHitStay)
		{
			if (_dicHitStayTime == null)
				_dicHitStayTime = new Dictionary<AffectorProcessor, float>();

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null && CheckHitStayInterval(affectorProcessor, _dicHitStayTime, _signal, _statusStructForHitObject.actorInstanceId))
			{
				Vector3 contactPoint = Vector3.zero;
				Vector3 contactNormal = Vector3.forward;

				//if (_hitInfoList == null)
				//	_hitInfoList = new RaycastHit[10];

				bool collided = false;
				// Stay에서는 cachedTransform.position에다 표시하는게 더 어울리는거 같다.
				//Vector3 diff = cachedTransform.position - _prevPosition;
				//int resultCount = Physics.SphereCastNonAlloc(_prevPosition, ColliderUtil.GetRadius(_collider), diff.normalized, _hitInfoList, diff.magnitude);
				//if (resultCount > 0)
				//{
				//	for (int i = 0; i < resultCount; ++i)
				//	{
				//		if (_hitInfoList[i].collider != col)
				//			continue;
				//
				//		contactPoint = _hitInfoList[i].point;
				//		contactNormal = _hitInfoList[i].normal;
				//		collided = true;
				//		break;
				//	}
				//}
				if (!collided)
				{
					contactPoint = cachedTransform.position;
					contactNormal = -cachedTransform.forward;
				}

				OnCollisionEnterAffectorProcessor(affectorProcessor, contactPoint, contactNormal);

				if (_signal.showHitEffect)
					HitEffect.ShowHitEffect(_signal, contactPoint, contactNormal, _statusStructForHitObject.weaponIDAtCreation);
				if (_signal.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
					HitEffect.ShowHitEffectLineRenderer(_signal, GetHitEffectLineRendererStartPosition(contactPoint), contactPoint);
			}
		}
	}

	Dictionary<AffectorProcessor, float> _dicHitStayTime = null;
	static bool CheckHitStayInterval(AffectorProcessor affectorProcessor, Dictionary<AffectorProcessor, float> dicHitStayTime, MeHitObject meHit, int creatorActorInstanceId)
	{
		if (meHit.hitStayIgnoreDuplicate)
			return affectorProcessor.CheckHitStayInterval(meHit.hitStayIdForIgnoreDuplicate, meHit.hitStayInterval, creatorActorInstanceId);

		if (dicHitStayTime.ContainsKey(affectorProcessor) == false)
		{
			dicHitStayTime.Add(affectorProcessor, Time.time);
			return true;
		}
		float lastTime = dicHitStayTime[affectorProcessor];
		if (Time.time > lastTime + meHit.hitStayInterval)
		{
			dicHitStayTime[affectorProcessor] = Time.time;
			return true;
		}
		return false;
	}

	void EnableRigidbodyAndCollider(bool enable)
	{
		EnableRigidbodyAndCollider(enable, _rigidbody, _collider, _triggerForHitStay);
	}

	public static void EnableRigidbodyAndCollider(bool enable, Rigidbody rigidbody, Collider collider, Collider additionalCollider = null, bool resetVelocityOnDisable = true)
	{
		if (rigidbody != null)
		{
			rigidbody.detectCollisions = enable;
			if (!enable && resetVelocityOnDisable) rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;
		}
		if (collider != null) collider.enabled = enable;
		if (additionalCollider != null) additionalCollider.enabled = enable;
	}

	void OnCollisionEnterAffectorProcessor(AffectorProcessor affectorProcessor, Vector3 contactPoint, Vector3 contactNormal)
	{
		// Reaction
		HitParameter hitParameter = new HitParameter();
		hitParameter.hitNormal = transform.forward;
		hitParameter.contactNormal = contactNormal;
		hitParameter.contactPoint = contactPoint;
		hitParameter.statusBase = _statusBase;
		hitParameter.statusStructForHitObject = _statusStructForHitObject;
		ApplyAffectorValue(affectorProcessor, _signal.affectorValueIdList, hitParameter);
		
		if (_signal.showHitBlink && (_signal.affectorValueIdList == null || _signal.affectorValueIdList.Count == 0))
			HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
		if (_signal.showHitRimBlink && (_signal.affectorValueIdList == null || _signal.affectorValueIdList.Count == 0))
			HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);
	}

	#region Ignore List
	List<Collider> _listIgnoreCollider;
	Dictionary<Collider, bool> _dicIgnoreColliderAddFrame;
	void AddIgnoreList(Collider collider)
	{
		if (_listIgnoreCollider == null)
			_listIgnoreCollider = new List<Collider>();
		if (_dicIgnoreColliderAddFrame == null)
			_dicIgnoreColliderAddFrame = new Dictionary<Collider, bool>();
		if (_listIgnoreCollider.Contains(collider))
			return;
		_listIgnoreCollider.Add(collider);
		Physics.IgnoreCollision(_collider, collider);

		if (_dicIgnoreColliderAddFrame.ContainsKey(collider))
			_dicIgnoreColliderAddFrame[collider] = true;
		else
			_dicIgnoreColliderAddFrame.Add(collider, true);
	}

	void RemoveIgnoreList(Collider collider)
	{
		if (_listIgnoreCollider == null)
			return;
		if (_listIgnoreCollider.Contains(collider) == false)
			return;
		_listIgnoreCollider.Remove(collider);
		Physics.IgnoreCollision(_collider, collider, false);

		if (_dicIgnoreColliderAddFrame.ContainsKey(collider))
			_dicIgnoreColliderAddFrame[collider] = false;
	}

	void ClearIgnoreList()
	{
		if (_listIgnoreCollider == null)
			return;
		for (int i = 0; i < _listIgnoreCollider.Count; ++i)
			RemoveIgnoreList(_listIgnoreCollider[i]);
		_listIgnoreCollider.Clear();
	}

	void UpdateIgnoreList()
	{
		// 벽에 붙은 몬스터를 관통할때 보니까
		// AddIgnoreList 했던 프레임에 Remove를 하고 있어서
		// 바로 다음 프레임에 또 다시 OnCollisionEnter가 호출되버렸다. 이러면서 연달아 두번 히트가 들어갔다.
		// 왜 Remove되는지 Physic 디버그창에서 확인해보니
		// 벽에 붙은 몬스터를 관통할땐 발사체가 몸체 앞으로 약간 튕겨져나오면서(평소엔 딱 붙은채로 있어서 다음 프레임에 Remove된다.)
		// Intersects함수가 false를 리턴하는거였다.
		// 그래서 어차피 AddIgnoreList 를 호출하는 프레임엔 검사를 할필요 없으니 건너뛰기로 해본다.
		if (_listIgnoreCollider == null)
			return;
		for (int i = 0; i < _listIgnoreCollider.Count; ++i)
		{
			if (_dicIgnoreColliderAddFrame.ContainsKey(_listIgnoreCollider[i]) && _dicIgnoreColliderAddFrame[_listIgnoreCollider[i]])
			{
				_dicIgnoreColliderAddFrame[_listIgnoreCollider[i]] = false;
				continue;
			}

			if (_collider.bounds.Intersects(_listIgnoreCollider[i].bounds) == false)
			{
				RemoveIgnoreList(_listIgnoreCollider[i]);
				break;
			}
		}
	}
	#endregion








	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
