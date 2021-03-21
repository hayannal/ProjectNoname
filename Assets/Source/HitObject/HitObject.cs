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

	public enum ePresetType
	{
		Single,
		Multi,				// 비중복 프리셋. 하나의 시작위치에서 여러개로 퍼지는 구조이기때문에 시그널은 하나만 설정하고 루프돌면서 여러개를 생성하는 구조다.
		DuplicatedMulti,	// 중복 프리셋. 시작위치가 달라야 구분되기때문에 멀티타겟의 개수만큼 시그널을 설정해야한다.
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
			TargetingProcessor targetingProcessor = parentActor.targetingProcessor;
			if (targetingProcessor != null)
			{
				int targetIndex = -1;
				bool useTargetIndex = false;
				bool useTargetList = false;
				switch (meHit.presetType)
				{
					case ePresetType.Single:
						targetIndex = 0;
						useTargetIndex = (targetingProcessor.GetTargetCount() > 0);
						break;
					case ePresetType.Multi:
						targetingProcessor.FindPresetMultiTargetMonsterList(meHit);
						useTargetList = (targetingProcessor.GetTargetCount() > 0);
						break;
					case ePresetType.DuplicatedMulti:
						targetingProcessor.FindPresetMultiTargetMonsterList(meHit);
						useTargetIndex = (targetingProcessor.GetTargetCount() > 0);
						if (useTargetIndex)
						{
							targetIndex = hitSignalIndexInAction;
							if (targetIndex >= targetingProcessor.GetTargetCount())
								targetIndex = targetIndex % targetingProcessor.GetTargetCount();
						}
						break;
				}

				bool checkPresetHitEffect = false;
				Vector3 presetEffectPosition = Vector3.zero;
				if (useTargetIndex && targetingProcessor.GetTarget(targetIndex) != null)
				{
					ApplyPreset(spawnTransform, meHit, parentActor, parentTransform, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, targetingProcessor.GetTarget(targetIndex));
				}
				else if (useTargetList)
				{
					// 비중복의 경우엔 설정되어있는 개수만큼 돌아야하는데 타겟의 개수가 부족하면 그 개수까지만 도는 형태다.
					for (int i = 0; i < meHit.multiPresetCount; ++i)
					{
						Collider targetCollider = targetingProcessor.GetTarget(i);
						if (targetCollider == null)
							break;
						ApplyPreset(spawnTransform, meHit, parentActor, parentTransform, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, targetCollider);
					}
				}
				else if (targetingProcessor.IsRegisteredCustomTargetPosition())
				{
					checkPresetHitEffect = true;
					presetEffectPosition = targetingProcessor.GetCustomTargetPosition(0);
					presetEffectPosition.y = 1.0f;

					// 원래 여기에다가 Preset일때 게이트필라 클릭 이동 작업을 추가하려고 했는데
					// 없는 시스템 자꾸 넣는거보다 차라리 기존 시스템을 쓰는게 낫다고 판단해서 투명 HitArea를 만드는 것으로 처리하기로 했다.
					// 높이는 2.1에서 2.2로 만들어야하는데, 안그러면 몬스터한테 데미지도 안들어가는데 RimBlink가 먹이게 되버려서 이상하게 보여진다.
				}
				else
				{
					checkPresetHitEffect = true;
					presetEffectPosition = GetFallbackTargetPosition(parentActor.cachedTransform);
					presetEffectPosition.y = 1.0f;
				}

				if (checkPresetHitEffect)
				{
					if (meHit.ignorePresetHitEffectOnCustomTargetPosition == false)
					{
						if (meHit.showHitEffect)
							HitEffect.ShowHitEffect(meHit, presetEffectPosition, (parentTransform.position - presetEffectPosition).normalized, 0);
						if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
							HitEffect.ShowHitEffectLineRenderer(meHit, GetSpawnPosition(spawnTransform, meHit, parentTransform, parentActor, hitSignalIndexInAction), presetEffectPosition);
					}

					// check wall
					if (meHit.checkRaycastWallInArea)
					{
						Vector3 attackerPosition = spawnTransform.position;
						Vector3 attackerForward = spawnTransform.forward;
						float length = 1.0f;
						if (parentActor.IsPlayerActor())
						{
							PlayerActor playerActor = parentActor as PlayerActor;
							if (playerActor != null)
								length = playerActor.playerAI.currentAttackRange;
						}
						Vector3 contactPoint = Vector3.zero;
						Vector3 contactNormal = Vector3.zero;
						if (CheckRaycastWall(attackerPosition, attackerForward, length, meHit.raycastWallHeight, ref contactPoint, ref contactNormal))
						{
							if (meHit.showHitEffect)
								HitEffect.ShowHitEffect(meHit, contactPoint, contactNormal, 0);
							if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
								HitEffect.ShowHitEffectLineRenderer(meHit, GetSpawnPosition(spawnTransform, meHit, parentTransform, parentActor, hitSignalIndexInAction), contactPoint);
						}
					}
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

			// RangeSignal은 같은 프레임의 Update에서 스스로 체크할거라 여기서 검사할 필요 없다.
			if (meHit.RangeSignal == false && meHit.lifeTime == 0.0f)
			{
				if (meHit.targetDetectType == eTargetDetectType.Area)
				{
					if (meHit.areaHitLifeTimeEarlyOffset == 0.0f)
						CheckHitArea(areaPosition, areaDirection, meHit, statusBase, statusStructForHitObject, GetGatePillarCompareTime(0.0f, parentHitObjectCreateTime));
				}
				else if (meHit.targetDetectType == eTargetDetectType.SphereCast)
				{
					areaDirection = GetSpawnDirection(areaPosition, spawnTransform, meHit, parentTransform, GetTargetPosition(meHit, parentActor, hitSignalIndexInAction), parentActor.targetingProcessor);
					endPosition = CheckSphereCast(areaPosition, areaDirection, meHit, statusBase, statusStructForHitObject, GetGatePillarCompareTime(0.0f, parentHitObjectCreateTime));
				}
			}

			// 원래 circular 처리는 Collider 타입에서만 했었는데 Area도 있으면 편할거 같아서 추가해본다. lifeTime이 있는 Area만 해당된다.
			bool ignoreAreaMainHitObjectByGenerator = false;
			for (int i = 0; i < meHit.circularSectorCount; ++i)
			{
				float centerAngleY = meHit.circularSectorUseWorldSpace ? meHit.circularSectorWorldSpaceCenterAngleY : Quaternion.LookRotation(areaDirection).eulerAngles.y;
				float baseAngle = meHit.circularSectorCount % 2 == 0 ? centerAngleY - (meHit.circularSectorBetweenAngle / 2f) : centerAngleY;
				float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, meHit.circularSectorBetweenAngle);
				HitObject circularSectorHitObject = GetCachedHitObject(meHit, areaPosition, Quaternion.Euler(0.0f, angle, 0.0f));
				if (circularSectorHitObject == null)
					continue;
				circularSectorHitObject.InitializeHitObject(meHit, parentActor, statusBase, parentHitObjectCreateTime, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);
			}

			if (meHit.continuousHitObjectGeneratorBaseList != null)
			{
				for (int i = 0; i < meHit.continuousHitObjectGeneratorBaseList.Count; ++i)
				{
					// Area는 항상 forward로만 만들었었는데 Area Generator로 사용될때만 방향값을 예외처리 하기로 한다.
					// Quaternion.LookRotation(areaDirection) 대신 아래 areaGeneratorRotation를 사용하기로 한다.
					Quaternion areaGeneratorRotation = Quaternion.LookRotation(GetSpawnDirection(areaPosition, spawnTransform, meHit, parentTransform, GetTargetPosition(meHit, parentActor, hitSignalIndexInAction), parentActor.targetingProcessor));
					ContinuousHitObjectGeneratorBase continuousHitObjectGenerator = BattleInstanceManager.instance.GetContinuousHitObjectGenerator(meHit.continuousHitObjectGeneratorBaseList[i].gameObject, areaPosition, areaGeneratorRotation);
					ignoreAreaMainHitObjectByGenerator |= continuousHitObjectGenerator.ignoreMainHitObject;
					continuousHitObjectGenerator.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);
				}
			}
			if (meHit.ignoreMainHitObjectByCircularSector || ignoreAreaMainHitObjectByGenerator)
				return null;

			// HitObject 프리팹이 있거나 lifeTime이 있다면 생성하고 아니면 패스.
			// 디폴트는 areaRotationY를 반영하지 않은채 areaDirection대로 만드는거다. 이게 기존 area공격에서 해왔던거고
			// applyRootTransformRotation 값이 켜져있을땐 HitObject Transform자체를 회전시키면 된다.
			// 이건 LowPolyMagmadar의 방사형 포에서 쓰는건데 이펙트를 히트오브젝트로 직접 써서 회전시켜야 하는 상황에 사용된다.
			Quaternion rotation = Quaternion.identity;
			if (meHit.applyRootTransformRotation)
				rotation = Quaternion.LookRotation(Quaternion.Euler(0.0f, meHit.areaRotationY, 0.0f) * areaDirection);
			else
				rotation = Quaternion.LookRotation(areaDirection);
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
		Quaternion defaultRotation = Quaternion.LookRotation(GetSpawnDirection(defaultPosition, spawnTransform, meHit, parentTransform, targetPosition, parentActor.targetingProcessor));
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
				Quaternion rotation = Quaternion.LookRotation(GetSpawnDirection(defaultPosition, spawnTransform, meHit, parentTransform, targetPosition, parentActor.targetingProcessor));
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

	static void ApplyPreset(Transform spawnTransform, MeHitObject meHit, Actor parentActor, Transform parentTransform, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Collider targetCollider)
	{
		if (targetCollider == null)
			return;
		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(targetCollider);
		if (affectorProcessor == null)
			return;
		if (!Team.CheckTeamFilter(parentActor.team.teamId, targetCollider, meHit.teamCheckType))
			return;
		float colliderRadius = ColliderUtil.GetRadius(targetCollider);
		if (colliderRadius == -1.0f)
			return;

		// check wall
		if (meHit.checkRaycastWallInArea)
		{
			Vector3 attackerPosition = spawnTransform.position;
			Vector3 attackerForward = spawnTransform.forward;
			// 위 Preset에서는 attackRange만큼 검사했지만 여기서는 타겟과의 사이 거리만 체크해야한다.
			Vector3 diff = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider).position - attackerPosition;
			diff.y = 0.0f;
			float length = diff.magnitude;
			Vector3 contactPoint = Vector3.zero;
			Vector3 contactNormal = Vector3.zero;
			if (CheckRaycastWall(attackerPosition, attackerForward, length, meHit.raycastWallHeight, ref contactPoint, ref contactNormal))
			{
				if (meHit.showHitEffect)
					HitEffect.ShowHitEffect(meHit, contactPoint, contactNormal, 0);
				if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
					HitEffect.ShowHitEffectLineRenderer(meHit, GetSpawnPosition(spawnTransform, meHit, parentTransform, parentActor, hitSignalIndexInAction), contactPoint);
				return;
			}
		}

		// 플래그 초기화
		Transform targetColliderTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
		Vector3 contactPointBase = targetColliderTransform.position;
		bool ignoreApplyAffectorValue = false;
		bool ignoreShowHitEffect = false;

		// 거리제한이 있는 캐릭터의 Preset이라면 거리를 벗어났는지 판단해줘야한다.
		float attackRange = 0.0f;
		if (parentActor.IsPlayerActor())
		{
			PlayerActor playerActor = parentActor as PlayerActor;
			if (playerActor != null)
				attackRange = playerActor.playerAI.currentAttackRange;
		}
		if (attackRange > 0.0f)
		{
			// 근데 검사할때 너무 칼같이 검사하면 경계에 닿았다가 조금 멀어진 적한테 미스나게 되니 어느정도 보정을 해주기로 한다.
			Vector3 diff = parentTransform.position - targetColliderTransform.position;
			diff.y = 0.0f;
			if (diff.magnitude - colliderRadius > (attackRange + 0.5f))
			{
				// 멀어졌다고 판단되면 AffectorValue도 적용하지 않고
				ignoreApplyAffectorValue = true;

				// 히트이펙트 위치도 변경해줘야한다. 정면이 안어색한거 같으니 정면으로 해본다.
				contactPointBase = parentTransform.position + parentTransform.forward * attackRange;
			}
		}

		// Preset타입은 Burrow를 공격할 수 없다.
		if (affectorProcessor.IsContinuousAffectorType(eAffectorType.Burrow) || BurrowOnStartAffector.CheckBurrow(affectorProcessor) || TargetingProcessor.IsOutOfRange(affectorProcessor))
		{
			ignoreApplyAffectorValue = true;
			contactPointBase.y = 0.0f;
		}

		// 프리셋이면서 라인렌더러가 있다면 contactPointBase의 높이는 0이어야한다.
		if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
			contactPointBase.y = 0.0f;

		// 점프 중인 상대는 presetAnimatorRoot가 켜있는 UnicornCharacter만 공격가능하다.
		if (JumpAffector.CheckJump(affectorProcessor))
		{
			if (meHit.presetAnimatorRoot)
				contactPointBase.y = affectorProcessor.actor.actionController.cachedAnimatorTransform.position.y;
			else
				ignoreApplyAffectorValue = true;
		}

		HitParameter hitParameter = new HitParameter();
		hitParameter.hitNormal = parentTransform.forward;
		hitParameter.contactNormal = (parentTransform.position - targetColliderTransform.position).normalized;
		hitParameter.contactPoint = contactPointBase + (hitParameter.contactNormal * colliderRadius * 0.7f);
		hitParameter.contactPoint.y += (targetCollider.bounds.size.y == 0.0f) ? 1.0f : targetCollider.bounds.size.y * 0.5f;
		hitParameter.statusBase = statusBase;

		// hitEffectLineRenderer는 보여야 DynaMob 공격이 버로우나 점프중인 대상을 타겟으로 하더라도 제대로 보이게 된다.
		if (meHit.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
			HitEffect.ShowHitEffectLineRenderer(meHit, GetSpawnPosition(spawnTransform, meHit, parentTransform, parentActor, hitSignalIndexInAction), hitParameter.contactPoint);

		// 데미지를 가할 수 없는 Preset이라면 조건에 따라 hitEffect를 보여줄지 결정해야한다.
		// RpgKnight같은 경우엔 버로우된 몹을 때리려고 할땐 hitEffect를 보여주면 안되나 DynaMob처럼 히트가 안들어가도 보여줘야 하는 경우 둘 다를 처리하기 위함이다.
		if (meHit.ignorePresetHitEffectOnCustomTargetPosition && ignoreApplyAffectorValue)
			ignoreShowHitEffect = true;

		if (meHit.showHitEffect && ignoreShowHitEffect == false)
			HitEffect.ShowHitEffect(meHit, hitParameter.contactPoint, hitParameter.contactNormal, hitParameter.statusStructForHitObject.weaponIDAtCreation);

		if (ignoreApplyAffectorValue)
			return;

		CopyEtcStatusForHitObject(ref hitParameter.statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);
		ApplyAffectorValue(affectorProcessor, meHit.affectorValueIdList, hitParameter);

		if (meHit.showHitBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
			HitBlink.ShowHitBlink(affectorProcessor.cachedTransform);
		if (meHit.showHitRimBlink && (meHit.affectorValueIdList == null || meHit.affectorValueIdList.Count == 0))
			HitRimBlink.ShowHitRimBlink(affectorProcessor.cachedTransform, hitParameter.contactNormal);
	}

	public static HitObject GetCachedHitObject(MeHitObject meHit, Vector3 position, Quaternion rotation)
	{
		HitObject hitObject = null;
		if (meHit.hitObjectPrefab != null)
		{
			hitObject = BattleInstanceManager.instance.GetCachedHitObject(meHit.hitObjectPrefab, position, rotation);
		}
		else if (meHit.lifeTime > 0.0f || meHit.RangeSignal)
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

			// BurrowOnStart는 올라와있는 상태에서 쏠테니 처리하지 않는다.
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
					spawnPosition = t.position + offsetPosition;
				}
				else
				{
					//spawnPosition = t.TransformPoint(offset);   // meHit.offset * parentTransform.localScale
					// 본에서 하는거라 스케일에 영향받으면 원하는 offset만큼 적용되지 않을때가 있다.
					spawnPosition = TransformUtil.TransformPointUnscaled(t, offset);
				}
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

		Transform baseTransform = parentActorTransform;
		if (meHit.createPositionType == eCreatePositionType.Bone && meHit.useBoneRotation)
			baseTransform = spawnTransform;

		Vector3 basePosition = baseTransform.position;
		Vector3 parallelOffset = Vector3.zero;
		parallelOffset.x = ((parallelCount - 1) * 0.5f * parallelDistance) * -1.0f + parallelDistance * parallelIndex;
		Vector3 offsetPosition = baseTransform.TransformPoint(parallelOffset);
		offsetPosition -= basePosition;
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

			bool sleepingTarget = false;
			if (parentActor.IsPlayerActor() && BattleInstanceManager.instance.playerActor == parentActor)
				sleepingTarget = BattleInstanceManager.instance.playerActor.playerAI.IsSleepingTarget();

			TargetingProcessor targetingProcessor = parentActor.targetingProcessor;
			if (targetingProcessor.GetTarget(targetIndex) != null && sleepingTarget == false)
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

	public static Vector3 GetSpawnDirection(Vector3 spawnPosition, Transform spawnTransform, MeHitObject meHit, Transform parentActorTransform, Vector3 targetPosition, TargetingProcessor targetingProcessor = null)
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
		if (result == Vector3.zero)
			result = Vector3.forward;
		if (meHit.startDirectionType == HitObjectMovement.eStartDirectionType.Direction && meHit.useWorldSpaceDirection)
			return result;
		if (meHit.createPositionType == eCreatePositionType.Bone && meHit.useBoneRotation)
			return spawnTransform.TransformDirection(result);
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

	static float GetAreaRotationY(MeHitObject meHit, float lifeTimeRatio)
	{
		// 현재는 RangeHitObject를 지원하지 않는다. 계산하는 과정에서 Start와 End사이의 시간이 필요한데 얻어오려면 애니메이션까지 얻어와서 해야한다. 그러니 우선 패스
		if (meHit.RangeSignal)
			return meHit.areaRotationY;

		// lifeTimeRatio를 얻어올 수 없는 상태라면 시작값을 리턴한다.
		if (lifeTimeRatio == 0.0f)
			return meHit.areaRotationY;

		// 시그널에서 AreaRotationYChange를 사용하지 않는다고 되어있다면 역시 시작값을 리턴한다.
		if (meHit.useAreaRotationYChange == false)
			return meHit.areaRotationY;

		return Mathf.Lerp(meHit.areaRotationY, meHit.targetAreaRotationY, lifeTimeRatio);
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

		if (normalAttack)
			WallThroughHitObjectAffector.CheckThrough(actor.affectorProcessor, ref statusStructForHitObject.wallThroughByAffector, ref statusStructForHitObject.quadThroughByAffector);
	}

	static Collider[] s_colliderList = null;
	static List<AffectorProcessor> s_listAppliedAffectorProcessor;
	static void CheckHitArea(Vector3 areaPosition, Vector3 areaForward, MeHitObject meHit, StatusBase statusBase, StatusStructForHitObject statusForHitObject, float gatePillarCompareTime,
		List<AffectorProcessor> listOneHitPerTarget = null, Dictionary<AffectorProcessor, float> dicHitStayTime = null, float lifeTimeRatio = 0.0f)
	{
		if (s_colliderList == null)
			s_colliderList = new Collider[100];

		// step 1. Physics.OverlapSphere
		float maxDistance = meHit.areaDistanceMax;
		maxDistance = Mathf.Max(Mathf.Abs(meHit.areaHeightMax), maxDistance);
		maxDistance = Mathf.Max(Mathf.Abs(meHit.areaHeightMin), maxDistance);
		// 최대 사거리의 지하에 있는 Burrow몬스터도 타겟에 포함되려면 일정량 버퍼가 필요하다.
		maxDistance *= 1.5f;
		int resultCount = Physics.OverlapSphereNonAlloc(areaPosition, maxDistance, s_colliderList); // meHit.areaDistanceMax * parentTransform.localScale.x

		// step 2. Check each object.
		float distanceMin = meHit.areaDistanceMin; // * parentTransform.localScale.x;
		float distanceMax = meHit.areaDistanceMax; // * parentTransform.localScale.x;

		// 루트 히트오브젝트를 직접 회전하는 경우엔 forward대로만 체크하면 된다.
		Vector3 forward = Vector3.forward;
		if (meHit.applyRootTransformRotation)
			forward = areaForward;
		else
			forward = Quaternion.Euler(0.0f, GetAreaRotationY(meHit, lifeTimeRatio), 0.0f) * areaForward;

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

			// object height
			float colliderHeight = ColliderUtil.GetHeight(col);
			if (colliderHeight == -1.0f) continue;

			// check sub parts
			// 이론대로라면 파츠를 가진 타겟을 처리할때
			// 해당 타겟의 파츠 중 어느 하나를 처리했다면 다른 파츠들은 continue시켜서 두번 처리되지 않게 하는건데
			// 이 로직에선 affectorProcessor를 가지고 처리하기 때문에 monsterActor 같은데 들어가서 파츠몹인지를 검사하기가 애매하다.
			// 그래서 아예 MonsterParts 같은 스크립트도 만들지 말고 컬리더만 여러개 부착시킨 후
			// 여러개의 컬리더 중 하나라도 처리되면 나머지는 처리하지 않도록 해본다.
			if (s_listAppliedAffectorProcessor.Contains(affectorProcessor))
				continue;

			// distance
			Vector3 targetPosition = BattleInstanceManager.instance.GetTransformFromCollider(col).position;
			Vector3 diff = targetPosition - areaPosition;
			diff.y = 0.0f;
			if (diff.magnitude + colliderRadius < distanceMin) continue;
			if (diff.magnitude - colliderRadius > distanceMax) continue;

			// angle
			float angle = Vector3.Angle(forward, diff.normalized);
			float hypotenuse = Mathf.Sqrt(diff.sqrMagnitude + colliderRadius * colliderRadius);
			float adjustAngle = Mathf.Rad2Deg * Mathf.Acos(diff.magnitude / hypotenuse);
			if (meHit.areaAngle * 0.5f < angle - adjustAngle) continue;

			// height
			if (targetPosition.y > areaPosition.y + meHit.areaHeightMax || targetPosition.y + colliderHeight < areaPosition.y + meHit.areaHeightMin)
				continue;

			// check wall
			if (meHit.checkRaycastWallInArea)
			{
				Vector3 attackerPosition = areaPosition;
				Vector3 attackerForward = forward;
				float length = diff.magnitude;
				bool useAreaPosition = false;
				// Bei나 Yuka가 쓰는 원거리 장판은 장판의 중심에서 몹사이를 판단해야해서 attackActor를 구할 필요가 없지만 areaForward를 다시 구해야한다.
				if (meHit.lifeTime > 0 && Mathf.Abs(meHit.areaHeightMin - BurrowAffector.s_BurrowPositionY) < 0.01f)
				{
					useAreaPosition = true;
					attackerForward = diff.normalized;
				}
				if (useAreaPosition == false)
				{
					Actor attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(statusForHitObject.actorInstanceId);
					if (attackerActor != null)
					{
						attackerPosition = attackerActor.cachedTransform.position;
						attackerForward = attackerActor.cachedTransform.forward;
						Vector3 attackerPositionDiff = BattleInstanceManager.instance.GetTransformFromCollider(col).position - attackerPosition;
						attackerPositionDiff.y = 0.0f;
						length = attackerPositionDiff.magnitude;
					}
				}
				Vector3 contactPoint = Vector3.zero;
				Vector3 contactNormal = Vector3.zero;
				if (CheckRaycastWall(attackerPosition, attackerForward, length, meHit.raycastWallHeight, ref contactPoint, ref contactNormal))
					continue;
			}

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
				// 어펙터 적용가능할 타임에 하는게 가장 효율적이다.
				if (GatePillar.instance != null && GatePillar.instance.gameObject.activeSelf)
					GatePillar.instance.CheckHitObject(statusForHitObject.teamId, gatePillarCompareTime, col);

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

				// BaseDamageAffector에서 처리할까 하다가 applyCollisionDamageInterval여부도 statusStructForHitObject안에 넣어야하고
				// 꼭 BaseDamageAffector에서 데미지 들어가는걸 확인할 필요가 없는거 같아서 여기서 처리하기로 한다.
				if (meHit.applyCollisionDamageInterval && hitParameter.statusStructForHitObject.monsterActor)
				{
					Actor attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
					if (attackerActor != null)
					{
						MonsterActor monsterActor = attackerActor as MonsterActor;
						if (monsterActor != null)
						{
							if (meHit.useHitStay && meHit.hitStayInterval > 0.0f)
								monsterActor.ApplyCollisionStayInterval(meHit.hitStayInterval);
							else
								monsterActor.ApplyCollisionStayInterval();
						}
					}
				}

				if (listOneHitPerTarget != null && meHit.oneHitPerTarget)
					listOneHitPerTarget.Add(affectorProcessor);

				s_listAppliedAffectorProcessor.Add(affectorProcessor);
			}
		}
	}

	static bool CheckRaycastWall(Vector3 areaPosition, Vector3 areaForward, float maxDistance, float raycastWallHeight, ref Vector3 contactPoint, ref Vector3 contactNormal)
	{
		if (s_raycastHitList == null)
			s_raycastHitList = new RaycastHit[100];

		areaPosition.y = raycastWallHeight;
		int resultCount = Physics.RaycastNonAlloc(areaPosition, areaForward, s_raycastHitList, maxDistance, 1);
		for (int i = 0; i < resultCount; ++i)
		{
			if (i >= s_raycastHitList.Length)
				break;

			Collider col = s_raycastHitList[i].collider;
			if (col.isTrigger)
				continue;

			if (BattleInstanceManager.instance.GetHitObjectFromCollider(col) != null)
				continue;

			AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
			if (affectorProcessor != null)
				continue;

			contactPoint = s_raycastHitList[i].point;
			contactNormal = s_raycastHitList[i].normal;
			return true;
		}

		return false;
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
		Vector3 endPosition = spawnPosition + spawnForward * reservedNearestDistance;
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

			if (NodeWarGround.instance != null && NodeWarGround.instance.CheckPlaneCollider(col))
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
	public Vector3 createPosition { get { return _createPosition; } }
	public HitObjectMovement hitObjectMovement { get { return _hitObjectMovement; } }

	MeHitObject _signal;
	float _createTime;
	float _additionalLifeTime;
	Vector3 _createPosition;
	Vector3 _createForward;
	float _parentHitObjectCreateTime;
	float _dynamicMaxDistanceByTargetDistance;
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
	bool _wallThrough;
	bool _quadThrough;

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
		_createForward = cachedTransform.forward;
		_statusBase = statusBase;
		CopyEtcStatusForHitObject(ref _statusStructForHitObject, parentActor, meHit, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack);
		_additionalLifeTime = LifeTimeHitObjectAffector.GetAddLifeTime(parentActor.affectorProcessor);

		if (_signal.useDynamicMaxDistanceByTargetDistance && parentActor.targetingProcessor.GetTarget() != null)
		{
			Collider targetCollider = parentActor.targetingProcessor.GetTarget();
			Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(targetCollider);
			if (targetTransform != null)
			{
				Vector3 diff = targetTransform.position - cachedTransform.position;
				diff.y = 0.0f;
				_dynamicMaxDistanceByTargetDistance = diff.magnitude;
			}
		}

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
		Team.SetTeamLayer(gameObject, teamLayerType, true, true);

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
		if (_signal.onlyUsedAsTrigger == false)
		{
			if (_collider != null) _collider.isTrigger = false;
			if (_signal.useHitStay && _signal.targetDetectType == eTargetDetectType.Collider)
			{
				if (_triggerForHitStay == null)
				{
					_triggerForHitStay = ObjectUtil.CopyComponent<Collider>(_collider, gameObject);
					_triggerForHitStay.isTrigger = true;
				}
			}
		}

		// 사실은 기본으로 들고있는 애들은 팩을 못먹게 할테니 덧셈 대신 덮어쓰는게 맞는데 어차피 0일테니 그냥 덧셈으로 해둔다.
		_remainMonsterThroughCount = _signal.monsterThroughCount + statusStructForHitObject.monsterThroughAddCountByLevelPack;
		_remainBounceWallQuadCount = _signal.bounceWallQuadCount + statusStructForHitObject.bounceWallQuadAddCountByLevelPack;
		_remainRicochetCount = _signal.ricochetCount + statusStructForHitObject.ricochetAddCountByLevelPack;
		_wallThrough = (_signal.wallThrough || statusStructForHitObject.wallThroughByAffector);
		_quadThrough = (_signal.quadThrough || statusStructForHitObject.quadThroughByAffector);

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
				_hitObjectMovement.InitializeSignal(this, meHit, parentActor, _rigidbody, hitSignalIndexInAction);
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
		UpdateDisableTrigger();

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

		if (gameObject == null || gameObject.activeSelf == false)
			return;

		// Range 시그널이 아닌 Area는 자체적으로 시간값 가지고 검사한다. 발사체 형태의 부채꼴을 처리하기 위함.
		if (_signal.RangeSignal == false && _signal.lifeTime > 0.0f)
			UpdateAreaOrSphereCast();

		if (_signal.targetDetectType == eTargetDetectType.Area && _signal.removeColliderArea)
		{
			bool removed = false;
			RemoveColliderHitObjectAffector.Remove(cachedTransform.position, _signal.areaDistanceMax, _signal.areaAngle, cachedTransform.forward, _statusStructForHitObject.teamId, ref removed);
		}
	}

	public void UpdateAreaOrSphereCast()
	{
		float lifeTimeRatio = 0.0f;
		if (_signal.targetDetectType == eTargetDetectType.Area && _signal.RangeSignal == false)
		{
			// areaRotationY값을 트랜스폼에 적용하는 히트오브젝트인데 useAreaRotationYChange까지 켜있다면 시간에 따라 트랜스폼을 회전시켜야한다.
			// 회전은 ignoreAreaHitLifeTimeRange범위 밖에서도 적용되는게 맞아서 위에서 하기로 한다.
			if (_signal.applyRootTransformRotation && _signal.useAreaRotationYChange)
			{
				lifeTimeRatio = (Time.time - _createTime) / _signal.lifeTime;

				// 그런데 이미 부모의 forward값을 비롯해 areaRotationY값이 트랜스폼에 다 적용된 상태라 델타로 계산하기엔 오차가 생길 수 있다.
				// 그래서 최초값을 기억시켜놨다가 곱해서 쓰기로 한다.
				float targetAreaRotationY = GetAreaRotationY(_signal, lifeTimeRatio);
				float diffAreaRotationY = targetAreaRotationY - _signal.areaRotationY;
				cachedTransform.forward = Quaternion.Euler(0.0f, diffAreaRotationY, 0.0f) * _createForward;
			}

			if (_signal.areaHitLifeTimeEarlyOffset > 0.0f && Time.time < _createTime + _signal.areaHitLifeTimeEarlyOffset)
			{
				Debug.LogError("Using areaHitLifeTimeEarlyOffset!!");
				return;
			}
			if (_signal.ignoreAreaHitLifeTimeRange.x > 0.0f && Time.time < _createTime + _signal.ignoreAreaHitLifeTimeRange.x)
				return;
			if (_signal.ignoreAreaHitLifeTimeRange.y > 0.0f && Time.time > _createTime + _signal.ignoreAreaHitLifeTimeRange.y)
				return;

			// 나눗셈 연산이 있어서 위에서 하지 않았을때만 처리.
			if (lifeTimeRatio == 0.0f)
				lifeTimeRatio = (Time.time - _createTime) / _signal.lifeTime;
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
				CheckHitArea(cachedTransform.position, cachedTransform.forward, _signal, _statusBase, _statusStructForHitObject, GetGatePillarCompareTime(), _listOneHitPerTarget, _dicHitStayTime, lifeTimeRatio);
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
		// lifeTime -1일때 무제한 처리를 처음부터 해두지 않아서 고쳐야할게 너무 많다.
		// 그래서 진짜 예외처리긴 한데 10000 이상으로 되어있으면 무제한으로 처리하기로 한다.
		if (_signal.lifeTime >= 10000.0f)
			return;

		if (_signal.RangeSignal == false && _createTime + _additionalLifeTime + _signal.lifeTime < Time.time)
		{
			OnFinalizeByLifeTime();
			return;
		}
	}

	void UpdateMaxDistance()
	{
		if (_signal.movable == false)
			return;
		float maxDistance = _signal.maxDistance;
		if (_signal.useDynamicMaxDistanceByTargetDistance && _dynamicMaxDistanceByTargetDistance > 0.0f)
			maxDistance = _dynamicMaxDistanceByTargetDistance;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
		{
			if (maxDistance == 0.0f || (_hitObjectMovement != null && _hitObjectMovement.IsAppliedRicochet()))
				maxDistance = NodeWarProcessor.SpawnDistance;
		}
		else
		{
			if (_hitObjectMovement != null && _hitObjectMovement.IsAppliedRicochet())
				return;
			if (maxDistance == 0.0f)
				return;
		}

		Vector3 diff = cachedTransform.position - _createPosition;
		diff.y = 0.0f;
		if (diff.sqrMagnitude > maxDistance * maxDistance)
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
		_ignoreWallCollidedEffect = false;

		// 히트 오브젝트 애니메이터를 발동시켜놨으면 첫번째 프레임이 호출될때까지는 기다려야한다.
		if (_hitObjectAnimatorStarted && ignoreAnimator == false)
		{
			_hitObjectAnimatorStarted = false;
			_waitHitObjectAnimatorUpdateCount = HitObjectAnimatorUpdateWaitCount;
			return;
		}

		if (_listStayedCollider != null)
			_listStayedCollider.Clear();

		BattleInstanceManager.instance.OnFinalizeHitObject(this, _collider);
		//Destroy(gameObject);
		gameObject.SetActive(false);
	}

	void OnFinalizeByCollision(bool plane = false, bool actor = false)
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
			if (plane == false)
			{
				if (_hitObjectAnimator.OnFinalizeByCollision())
					_hitObjectAnimatorStarted = true;
				// ChaDragon 이 캐릭터를 맞출때는 파생되지 않으나 벽을 맞출때는 파생하는 프로젝타일을 날리면서 이런 예외처리가 들어가게 되었다.
				// 나중에 더 세분화해야하면 인자 전달 방식을 바꿔야할거 같다.
				// 애니메이터에는 OnCollision이나 OnCollisionExceptActor 둘중에 하나만 써야한다. 동시에 쓸순 없다.
				if (actor == false && _hitObjectAnimator.OnFinalizeByCollisionExceptActor())
					_hitObjectAnimatorStarted = true;
			}
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

	public void OnFinalizeByRemove(float hitEffectShowRate = 1.0f)
	{
		if (_signal.showHitEffect)
		{
			if (hitEffectShowRate >= 1.0f || (hitEffectShowRate > 0.0f && Random.value <= hitEffectShowRate))
				HitEffect.ShowHitEffect(_signal, cachedTransform.position, cachedTransform.forward, _statusStructForHitObject.weaponIDAtCreation);
		}
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

	bool _ignoreWallCollidedEffect = false;
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

			// 동시에 하나의 오브젝트에 OnCollisionEnter되면 가장 앞에 있는게 먼저 affectorProcessor를 처리하게 되는데
			// 이게 몹이었고 죽는다면 rigidbody의 detect가 꺼지고 collider가 disable로 바뀌게 된다.
			// affectorProcessor의 액터에 접근해서 rigidbody를 검사하는거보다 여기서 처리하는게 더 안전하고 빠르니
			// collider가 꺼있으면 바로 through처리를 해주기로 한다.
			if (col.enabled == false)
			{
				if (_hitObjectMovement != null)
					_hitObjectMovement.ReinitializeForThrough();
				//Debug.Log("disabled collider");
				continue;
			}

			// 벽 구석에 쐈을때 동시에 두 벽에 맞는 경우가 생겼다. 볼이 클수록 더 발생확률이 높은데
			// 그렇다고 일일이 등록하긴 뭐해서 이런식으로 처리해보기로 한다.
			if (_signal.contactAll == false && contact.thisCollider.enabled == false)
				continue;

			if (BattleInstanceManager.instance.planeCollider != null && BattleInstanceManager.instance.planeCollider == col)
			{
				planeCollided = true;
				wallNormal = contact.normal;
			}

			if (NodeWarGround.instance != null && NodeWarGround.instance.CheckPlaneCollider(col))
			{
				planeCollided = true;
				wallNormal = contact.normal;
			}

			if (BattleInstanceManager.instance.currentGround != null && BattleInstanceManager.instance.currentGround.CheckQuadCollider(col))
			{
				groundQuadCollided = true;
				wallNormal = contact.normal;

				if (_quadThrough)
					AddIgnoreList(col, false);
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

				if (ignoreAffectorProcessor == false && RemoveColliderHitObjectAffector.IsIgnoreColliderHitObject(affectorProcessor))
					ignoreAffectorProcessor = true;

				if (ignoreAffectorProcessor == false && _signal.useHitStay == false)
				{
					OnCollisionEnterAffectorProcessor(affectorProcessor, contact.point, contact.normal);
					if (_signal.oneHitPerTarget)
						_listOneHitPerTarget.Add(affectorProcessor);
					if (_remainRicochetCount > 0 && _hitObjectMovement != null)
						_hitObjectMovement.AddRicochet(col, _remainRicochetCount == (_signal.ricochetCount + statusStructForHitObject.ricochetAddCountByLevelPack));
					monsterCollided = true;

					if (_remainMonsterThroughCount > 0 || _remainMonsterThroughCount == -1)
						AddIgnoreList(col, true);
					// 리코세는 가능여부 판단하고 해야해서 OnPostCollided함수 안에서 한다.

					if (_signal.movementType == HitObjectMovement.eMovementType.FollowTarget && _signal.followMeOnHitTarget && _hitObjectMovement != null && _hitObjectMovement.IsTargetActor(affectorProcessor.actor))
						_hitObjectMovement.ChangeFollowTargetActor(BattleInstanceManager.instance.FindActorByInstanceId(statusStructForHitObject.actorInstanceId));

					s_listAppliedAffectorProcessor.Add(affectorProcessor);
				}
				else
				{
					// 관통형일때를 대비해서 monsterCollided는 체크해야 제대로 처리된다.
					// 리코세와 hitStay를 같이 켤일은 없을테니 이건 고려하지 않기로 한다.
					if (_remainMonsterThroughCount > 0 || _remainMonsterThroughCount == -1)
					{
						monsterCollided = true;

						// 어차피 OnTriggerStay쪽에서 처리하기 때문에 기본 컬리더는 Ignore해도 된다.
						AddIgnoreList(col, true);
					}
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
					AddIgnoreList(col, true);
				}
				else
				{
					wallCollided = true;
					wallNormal = contact.normal;

					if (_wallThrough)
						AddIgnoreList(col, false);
				}
			}

			collided = planeCollided || groundQuadCollided || wallCollided || monsterCollided;
			if (collided)
			{
				bool ignoreEffect = false;
				if (wallCollided && _wallThrough)
				{
					// JellyFishGirl
					if (_signal.movementType == HitObjectMovement.eMovementType.Howitzer)
						ignoreEffect = true;
					else if (_signal.movementType == HitObjectMovement.eMovementType.Direct)
					{
						if (_statusStructForHitObject.monsterActor)
							ignoreEffect = true;
						if (_statusStructForHitObject.monsterActor == false)
						{
							// RPG Knight
							if (_remainRicochetCount > 0)
								ignoreEffect = true;
							// Linhi. only first wallThrough
							if (_quadThrough)
							{
								if (_ignoreWallCollidedEffect)
									ignoreEffect = true;
								else
									_ignoreWallCollidedEffect = true;
							}
						}
					}
				}

				if (groundQuadCollided && _quadThrough)
				{
					if (_signal.movementType == HitObjectMovement.eMovementType.Direct)
					{
						// FairyFlower_Green
						if (_statusStructForHitObject.monsterActor)
							ignoreEffect = true;
					}
				}

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

	public void OnPostCollided(bool collided, bool planeCollided, bool groundQuadCollided, bool wallCollided, bool monsterCollided, Vector3 wallNormal, bool forceBarrierThrough)
	{
		if (collided == false)
			return;

		// Check End of HitObject
		bool useThrough = false;
		bool useBounce = false;
		if (monsterCollided)
		{
			// 다단히트가 아닌 일반 관통샷에서도 속도 저하를 하려면 이 코드를 활성화 하면 되는데
			// 이런식으로 쓸일이 있을까 싶다.
			// 우선은 주석처리만 해둔다.
			//if (_signal.overrideSpeedOnCollision > 0.0f && _signal.overrideSpeedTimeOnCollision > 0.0f && hitObjectMovement != null)
			//	hitObjectMovement.ChangeOverrideSpeed(_signal.overrideSpeedOnCollision, _signal.overrideSpeedTimeOnCollision);

			bool ricochetApplied = false;
			if (_remainRicochetCount > 0 && _hitObjectMovement != null && _hitObjectMovement.IsEnableRicochet(_statusStructForHitObject.teamId, _signal.teamCheckType))
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
					++_statusStructForHitObject.ricochetIndex;
					_remainRicochetCount -= 1;
					if (colliderEnabled && _signal.useTimerRicochet == false)
					{
						if (_signal.useHitStay == false)
						{
							Collider lastRicochetCollider = _hitObjectMovement.GetLastRicochetCollider();
							if (lastRicochetCollider != null)
								AddIgnoreList(lastRicochetCollider, true);
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
				++_statusStructForHitObject.monsterThroughIndex;
				if (_remainMonsterThroughCount > 0) _remainMonsterThroughCount -= 1;
				useThrough = true;
			}
			else
			{
				OnFinalizeByCollision(false, true);
				return;
			}
		}

		if (wallCollided)
		{
			if (forceBarrierThrough)
				useThrough = true;
			else if (_remainBounceWallQuadCount > 0)
			{
				++_statusStructForHitObject.bounceWallQuadIndex;
				_remainBounceWallQuadCount -= 1;
				useBounce = true;
			}
			else if (_wallThrough)
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
				++_statusStructForHitObject.bounceWallQuadIndex;
				_remainBounceWallQuadCount -= 1;
				useBounce = true;
			}
			else if (_quadThrough)
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

	// Timer Ricochet 기능 구현하면서 추가한 함수. 특정 시간 이후에 강제로 이 몹이 맞은거처럼 처리해야해서
	// 위 OnCollisionEnter에서 필요한 기능만 복사해오게 되었다.
	public void OnCollisionByCollider(Collider col)
	{
		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(col);
		if (affectorProcessor == null)
			return;

		bool ignoreAffectorProcessor = false;
		if (_signal.oneHitPerTarget)
		{
			if (_listOneHitPerTarget == null) _listOneHitPerTarget = new List<AffectorProcessor>();
			if (_listOneHitPerTarget.Contains(affectorProcessor))
				ignoreAffectorProcessor = true;
		}

		Vector3 contactPoint = affectorProcessor.cachedTransform.position;
		contactPoint.y = cachedTransform.position.y;
		Vector3 contactNormal = -cachedTransform.forward;
		if (ignoreAffectorProcessor == false && _signal.useHitStay == false)
		{
			OnCollisionEnterAffectorProcessor(affectorProcessor, contactPoint, contactNormal);
			if (_signal.oneHitPerTarget)
				_listOneHitPerTarget.Add(affectorProcessor);
			if (_remainRicochetCount > 0 && _hitObjectMovement != null)
				_hitObjectMovement.AddRicochet(col, _remainRicochetCount == (_signal.ricochetCount + statusStructForHitObject.ricochetAddCountByLevelPack));

			if (_remainMonsterThroughCount > 0 || _remainMonsterThroughCount == -1)
				AddIgnoreList(col, true);
			// 리코세는 가능여부 판단하고 해야해서 OnPostCollided함수 안에서 한다.
		}

		if (_signal.showHitEffect)
			HitEffect.ShowHitEffect(_signal, contactPoint, contactNormal, _statusStructForHitObject.weaponIDAtCreation);
		if (_signal.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
			HitEffect.ShowHitEffectLineRenderer(_signal, GetHitEffectLineRendererStartPosition(contactPoint), contactPoint);

		OnPostCollided(true, false, false, false, true, Vector3.forward, false);
	}

	List<Collider> _listStayedCollider;
	void UpdateDisableTrigger()
	{
		// 한번이라도 OnTriggerStay로 처리한 collider들을 리스트에 모아두고
		// OnTiggerExit가 오거나 죽어서 collider.enabled가 꺼지는걸 판단해서 전부 다 해제될 경우에만 trigger 상태를 풀어야한다.
		// 안그러면 두번째 오브젝트랑 겹쳐있는 상태에서 trigger가 풀리게 된다.
		if (_tempTriggerOnCollision && _collider.isTrigger)
		{
			if (_listStayedCollider != null)
			{
				// exit되지 않은 컬리더 중에 죽음 등에 의해 꺼진게 있다면
				for (int i = _listStayedCollider.Count - 1; i >= 0; --i)
				{
					if (_listStayedCollider[i].enabled == false)
					{
						_listStayedCollider.RemoveAt(i);
						continue;
					}

					// 정 안되면 여기서 거리 검사라도 해서 제외해야하지 않을까.
					// 추가하게 된다면 IgnoreList에서 Intersect호출하는 형태로 검사하면 될거다.
					Bounds newBounds = new Bounds(_collider.bounds.center, _collider.bounds.size + new Vector3(0.1f, 0.1f, 0.1f));
					if (newBounds.Intersects(_listStayedCollider[i].bounds) == false)
					{
						_listStayedCollider.RemoveAt(i);
						continue;
					}
				}
			}

			// 정말 가끔 벽을 투과하는 현상이 나와서 처음엔 OnTriggerExit가 오지 않는건줄 알았는데 아니었다.
			// OnCollisionEnter에서 _tempTriggerOnCollision true로 셋팅하고 나서 OnTriggerStay가 호출되지 않아 해제될 방법이 없었던거다.
			// 그래서 차라리 _listStayedCollider가 0일때 자동 해제되는 형태로 코드를 짜보기로 한다.
			if (_listStayedCollider == null || _listStayedCollider.Count == 0)
			{
				_collider.isTrigger = false;
				_tempTriggerOnCollision = false;
			}
		}
	}

	void OnTriggerExit(Collider collider)
	{
		if (_listStayedCollider == null || _listStayedCollider.Contains(collider) == false)
			return;

		if (_listStayedCollider.Count == 1)
		{
			_listStayedCollider.Clear();

			// 하나만 있는 상태에서만 리스트 클리어하고 바로 컬리젼 상태를 끄면 된다.
			// 조건 검사 안하고 꺼버리면 동시에 여러개의 오브젝트를 투과중인 히트오브젝트가 컬리더로 변하면서 위치가 틀어지게 되니 이렇게 조건검사 해야한다.
			if (_tempTriggerOnCollision && _collider.isTrigger)
			{
				_collider.isTrigger = false;
				_tempTriggerOnCollision = false;
			}
		}
		else
			_listStayedCollider.Remove(collider);
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
		if (_triggerForHitStay == null && _signal.onlyUsedAsTrigger == false)
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
			if (affectorProcessor != null && CheckHitStayInterval(affectorProcessor, _dicHitStayTime, _signal, _statusStructForHitObject.actorInstanceId) && RemoveColliderHitObjectAffector.IsIgnoreColliderHitObject(affectorProcessor) == false)
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

				if (_signal.overrideSpeedOnCollision > 0.0f && _signal.overrideSpeedTimeOnCollision > 0.0f && hitObjectMovement != null)
					hitObjectMovement.ChangeOverrideSpeed(_signal.overrideSpeedOnCollision, _signal.overrideSpeedTimeOnCollision);

				if (_signal.showHitEffect)
					HitEffect.ShowHitEffect(_signal, contactPoint, contactNormal, _statusStructForHitObject.weaponIDAtCreation);
				if (_signal.hitEffectLineRendererType != HitEffect.eLineRendererType.None)
					HitEffect.ShowHitEffectLineRenderer(_signal, GetHitEffectLineRendererStartPosition(contactPoint), contactPoint);

				// 해제를 위해 별도의 리스트에 넣어둔다.
				// OnCollisionEnter할때 넣는 것도 답은 아닌게 몬스터가 연속으로 서있을 경우 첫번째 몬스터에겐 OnCollisionEnter가 호출되겠지만
				// trigger로 변환한 후 닿는 두번째 몬스터는 OnCollisionEnter가 호출되지 않는다.
				// 그러니 여기서 처리하는 형태로 구현해보기로 한다.
				if (_listStayedCollider == null)
					_listStayedCollider = new List<Collider>();
				if (_listStayedCollider.Contains(col) == false)
					_listStayedCollider.Add(col);
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
	const float DefaultResetIgnoreSqrMagnitude = 0.01f;
	List<Collider> _listIgnoreCollider;
	Dictionary<Collider, bool> _dicIgnoreColliderAddFrame;
	Dictionary<Collider, Vector3> _dicIgnoreColliderAddPosition;
	void AddIgnoreList(Collider collider, bool movableObject)
	{
		if (_listIgnoreCollider == null)
			_listIgnoreCollider = new List<Collider>();
		if (_dicIgnoreColliderAddFrame == null)
			_dicIgnoreColliderAddFrame = new Dictionary<Collider, bool>();
		if (_dicIgnoreColliderAddPosition == null)
			_dicIgnoreColliderAddPosition = new Dictionary<Collider, Vector3>();
		if (_listIgnoreCollider.Contains(collider))
			return;
		_listIgnoreCollider.Add(collider);
		Physics.IgnoreCollision(_collider, collider);

		if (_dicIgnoreColliderAddFrame.ContainsKey(collider))
			_dicIgnoreColliderAddFrame[collider] = true;
		else
			_dicIgnoreColliderAddFrame.Add(collider, true);

		// WallThrough로 천천히 쏠때 투과하지 못한채 진입시점에 막히는 현상이 나타났다.
		// 그래서 충돌시점의 위치를 기억해놨다가 0.1이상은 움직여야 제거처리를 하게 막아둔다.
		// 움직이는 오브젝트일 경우엔 안하는게 맞을거 같아서 actor나 배리어가 아닐때만 체크해본다.
		if (movableObject == false)
		{
			if (_dicIgnoreColliderAddPosition.ContainsKey(collider))
				_dicIgnoreColliderAddPosition[collider] = cachedTransform.position;
			else
				_dicIgnoreColliderAddPosition.Add(collider, cachedTransform.position);
		}
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
		if (_dicIgnoreColliderAddPosition.ContainsKey(collider))
			_dicIgnoreColliderAddPosition[collider] = Vector3.down;
	}

	void ClearIgnoreList()
	{
		if (_listIgnoreCollider == null)
			return;
		for (int i = _listIgnoreCollider.Count - 1; i >= 0; --i)
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
		float resetSqrMagnitude = DefaultResetIgnoreSqrMagnitude;
		if (_signal.overrideResetIgnoreSqrMagnitude > 0.0f)
			resetSqrMagnitude = _signal.overrideResetIgnoreSqrMagnitude;
		for (int i = _listIgnoreCollider.Count - 1; i >= 0; --i)
		{
			if (_dicIgnoreColliderAddFrame.ContainsKey(_listIgnoreCollider[i]) && _dicIgnoreColliderAddFrame[_listIgnoreCollider[i]])
			{
				_dicIgnoreColliderAddFrame[_listIgnoreCollider[i]] = false;
				continue;
			}

			if (_dicIgnoreColliderAddPosition.ContainsKey(_listIgnoreCollider[i]) && _dicIgnoreColliderAddPosition[_listIgnoreCollider[i]] != Vector3.down)
			{
				Vector3 diff = _dicIgnoreColliderAddPosition[_listIgnoreCollider[i]] - cachedTransform.position;
				if (diff.sqrMagnitude < resetSqrMagnitude)
					continue;
			}

			// Bound대로 했더니 IgnoreList에서 빼는순간 곧바로 OnCollisionEnter가 들어오는 경우가 생겼다.
			// 그래서 발사체 Bound의 크기에 0.1씩 더해서 계산하는거로 바꾸기로 한다.
			// 우선 이렇게 바꾸고나서도 펑기나 래빗한테 잘 들어가서 이렇게 하기로 한다.
			Bounds newBounds = new Bounds(_collider.bounds.center, _collider.bounds.size + new Vector3(0.1f, 0.1f, 0.1f));
			if (newBounds.Intersects(_listIgnoreCollider[i].bounds) == false)
			{
				RemoveIgnoreList(_listIgnoreCollider[i]);
				break;
			}
		}
	}
	#endregion


	#region Etc
	public bool IsIgnoreRemoveColliderAffector()
	{
		// Area타입이나 SphereCast타입이면 Collider가 없기 때문에 아예 호출조차 안될거다.
		if (_signal == null)
			return false;
		return _signal.ignoreRemoveColliderAffector;
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
