using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class BaseDamageAffector : AffectorBase {

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			// something else? for breakable object
			ShowHitBlink(hitParameter);
			return;
		}

		// BaseDamage는 IsDie 검사를 하는게 맞다. 여기서 리턴안하면 MonsterActor의 OnDie호출되면서 몬스터 카운트부터 다 꼬인다.
		if (_actor.actorStatus.IsDie())
			return;

		// 실명은 공격자꺼라 가장 먼저.
		// 하단의 빗맞음과 같이 처리하지 않고 최상위에서 먼저 돌린다.
		// 결과는 동일하게 Miss로 표현한다.

		// 횟수 보호막 검사가 비쥬얼상 무적이나 회피보다 먼저다.
		if (CountBarrierAffector.CheckBarrier(_affectorProcessor, hitParameter))
			return;

		// 무적 검사를 그다음
		if (InvincibleAffector.CheckInvincible(_affectorProcessor))
		{
			if (_actor.GetRigidbody() != null)
				_actor.GetRigidbody().velocity = Vector3.zero;
			FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Invincible, _actor);
			return;
		}

		// 회피 체크. IgnoreEvadeVisualAffector와 달리 실제로 빗맞음 체크하는 항목이다.
		// 저 어펙터가 없어도 이게 1이면 빗맞음 무시이며, %가 차오르지 않는 빗맞음 무시 필살기 공격을 만들때 쓸 예정이다.
		if (affectorValueLevelTableData.iValue3 == 0)
		{
			float evadeRate = _actor.actorStatus.GetValue(eActorStatus.EvadeRate);
			if (evadeRate > 0.0f && Random.value <= evadeRate)
			{
				// 안밀리게. 이 코드가 가장 간결하다.
				if (_actor.GetRigidbody() != null)
					_actor.GetRigidbody().velocity = Vector3.zero;

				// 미스 데미지 플로터 적용.
				FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Miss, _actor);
				return;
			}
		}

		//float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack] * 1000.0f / (_actor.actorStatus.GetValue(eActorStatus.Defense) + 1000.0f);
		//float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack] - _actor.actorStatus.GetValue(eActorStatus.Defense);
		float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack];

		// Calc Damage
		float damageRatio = 1.0f;
		switch (affectorValueLevelTableData.iValue1)
		{
			case 0:
				damageRatio = affectorValueLevelTableData.fValue1;
				break;
			case 1:
				int hitSignalIndexInAction = hitParameter.statusStructForHitObject.hitSignalIndexInAction;
				float[] damageRatioList = BattleInstanceManager.instance.GetCachedMultiHitDamageRatioList(affectorValueLevelTableData.sValue1);
				if (hitSignalIndexInAction < damageRatioList.Length)
					damageRatio = damageRatioList[hitSignalIndexInAction];
				else
					Debug.LogErrorFormat("Invalid hitSignalIndexInAction. index = {0}", hitSignalIndexInAction);
				break;
		}
		damage *= damageRatio;

		Actor attackerActor = null;
		if ((int)eActorStatus.CriticalRate < hitParameter.statusBase.valueList.Length)
		{
			float criticalRate = hitParameter.statusBase.valueList[(int)eActorStatus.CriticalRate];
			if (hitParameter.statusStructForHitObject.monsterActor == false)
			{
				float minimumCriticalRate = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("MinimumCriticalRate");
				criticalRate = Mathf.Max(criticalRate, minimumCriticalRate);
			}
			if (criticalRate > 0.0f && Random.value <= criticalRate)
			{
				float criticalDamageRate = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("DefaultCriticalDamageRate");
				criticalDamageRate += hitParameter.statusBase.valueList[(int)eActorStatus.CriticalDamageAddRate];
				if (attackerActor == null) attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
				if (attackerActor != null)
					criticalDamageRate += AddCriticalDamageByTargetHpAffector.GetValue(attackerActor.affectorProcessor, _actor.actorStatus.GetHPRatio());
				if (criticalDamageRate > 0.0f)
				{
					damage *= (1.0f + criticalDamageRate);
					FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Critical, _actor);
				}
			}
		}

		if (_actor.IsMonsterActor() && (int)eActorStatus.NormalMonsterDamageIncreaseAddRate < hitParameter.statusBase.valueList.Length)
		{
			MonsterActor monsterActor = _actor as MonsterActor;
			if (monsterActor != null)
			{
				float damageIncreaseAddRate = hitParameter.statusBase.valueList[monsterActor.bossMonster ? (int)eActorStatus.BossMonsterDamageIncreaseAddRate : (int)eActorStatus.NormalMonsterDamageIncreaseAddRate];
				if (damageIncreaseAddRate != 0.0f)
					damage *= (1.0f + damageIncreaseAddRate);
			}
		}

		if (_actor.IsPlayerActor())
		{
			if (hitParameter.statusStructForHitObject.monsterActor)
			{
				float damageDecreaseAddRate = _actor.actorStatus.GetValue(hitParameter.statusStructForHitObject.bossMonsterActor ? eActorStatus.BossMonsterDamageDecreaseAddRate : eActorStatus.NormalMonsterDamageDecreaseAddRate);
				if (damageDecreaseAddRate != 0.0f)
					damage *= (1.0f - damageDecreaseAddRate);
			}
		}

		// 리코셰 몹관통 등에 의한 데미지 감소 처리. 레벨팩 없이 시그널에 의해 동작할땐 적용하지 않는다.
		if (hitParameter.statusStructForHitObject.monsterThroughAddCountByLevelPack > 0 || hitParameter.statusStructForHitObject.monsterThroughIndex > 0)
		{
			float damageRate = MonsterThroughHitObjectAffector.GetDamageRate(hitParameter.statusStructForHitObject.monsterThroughAddCountByLevelPack, hitParameter.statusStructForHitObject.monsterThroughIndex, hitParameter.statusStructForHitObject.actorInstanceId);
			if (damageRate != 1.0f)
				damage *= damageRate;
		}

		if (hitParameter.statusStructForHitObject.ricochetAddCountByLevelPack > 0 || hitParameter.statusStructForHitObject.ricochetIndex > 0)
		{
			float damageRate = RicochetHitObjectAffector.GetDamageRate(hitParameter.statusStructForHitObject.ricochetAddCountByLevelPack, hitParameter.statusStructForHitObject.ricochetIndex, hitParameter.statusStructForHitObject.actorInstanceId);
			if (damageRate != 1.0f)
				damage *= damageRate;
		}

		if (hitParameter.statusStructForHitObject.bounceWallQuadAddCountByLevelPack > 0 || hitParameter.statusStructForHitObject.bounceWallQuadIndex > 0)
		{
			float damageRate = BounceWallQuadHitObjectAffector.GetDamageRate(hitParameter.statusStructForHitObject.bounceWallQuadAddCountByLevelPack, hitParameter.statusStructForHitObject.bounceWallQuadIndex, hitParameter.statusStructForHitObject.actorInstanceId);
			if (damageRate != 1.0f)
				damage *= damageRate;
		}

		if (hitParameter.statusStructForHitObject.parallelAddCountByLevelPack > 0)
		{
			float damageRate = ParallelHitObjectAffector.GetDamageRate(hitParameter.statusStructForHitObject.parallelAddCountByLevelPack, hitParameter.statusStructForHitObject.actorInstanceId);
			if (damageRate != 1.0f)
				damage *= damageRate;
		}

		if (hitParameter.statusStructForHitObject.repeatAddCountByLevelPack > 0 && hitParameter.statusStructForHitObject.repeatIndex > 0)
		{
			float damageRate = RepeatHitObjectAffector.GetDamageRate(hitParameter.statusStructForHitObject.repeatAddCountByLevelPack, hitParameter.statusStructForHitObject.repeatIndex, hitParameter.statusStructForHitObject.actorInstanceId);
			if (damageRate != 1.0f)
				damage *= damageRate;
		}

		float reduceDamageValue = 0.0f;
		if (affectorValueLevelTableData.fValue4 == 0.0f)
		{
			if (hitParameter.statusStructForHitObject.targetDetectType == HitObject.eTargetDetectType.Collider)
				reduceDamageValue = ReduceDamageAffector.GetValue(_affectorProcessor, ReduceDamageAffector.eReduceDamageType.Collider);
		}
		else
		{
			int reduceDamageType = Mathf.RoundToInt(affectorValueLevelTableData.fValue4);
			if (reduceDamageType == 1)
				reduceDamageValue = ReduceDamageAffector.GetValue(_affectorProcessor, ReduceDamageAffector.eReduceDamageType.Melee);
		}
		if (reduceDamageValue != 0.0f)
			damage *= (1.0f - (reduceDamageValue / (1.0f + reduceDamageValue)));

		if (_actor.IsPlayerActor())
		{
			// 연타저항 어펙터 처리.
			float reduceContinuousDamageValue = ReduceContinuousDamageAffector.GetValue(_affectorProcessor);
			if (reduceContinuousDamageValue != 0.0f)
			{
				damage *= (1.0f - (reduceContinuousDamageValue / (1.0f + reduceContinuousDamageValue)));
				FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.ReduceContinuousDamage, _actor);
			}

			// 강공격 방어 어펙터 처리.
			damage = DefenseStrongDamageAffector.OnDamage(_affectorProcessor, damage);
		}

		float enlargeDamageValue = EnlargeDamageAffector.GetValue(_affectorProcessor);
		if (enlargeDamageValue != 0.0f)
			damage *= (1.0f + enlargeDamageValue);

		if (_actor.actorStatus.GetHP() == _actor.actorStatus.GetValue(eActorStatus.MaxHp) && damage < _actor.actorStatus.GetHP())
		{
			if (attackerActor == null) attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
			if (attackerActor != null)
			{
				// 이렇게 statusStructForHitObject를 통해서 체크하지 않고 직접 어펙터로 체크하는 방식은
				// 발사하고 나서 즉사 어펙터를 얻었을때도 적용되기 때문에
				// 엄밀히 말하자면 안좋은 구조긴 하다.
				// 하지만 발사하고 나서 곧바로 즉사를 얻을 수 없다는 점 때문에, 샘플용으로 두기로 한다.
				// (이 방법을 쓰면 statusStructForHitObject에 멤버변수를 추가하지 않아도 된다는 장점이 있긴 하다.)
				if (InstantDeathAffector.CheckInstantDeath(attackerActor.affectorProcessor, _actor))
				{
					damage = _actor.actorStatus.GetHP() + 1.0f;
					FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Headshot, _actor);
				}
			}
		}

		if (_actor.IsPlayerActor())
			BattleManager.instance.AddDamageCountOnStage();

		// 버로우로 내려가있는 도중엔 본체에 HitRimBlink 할 필요 없다.
		// DieProcess 들어가기전에 물어보는게 가장 정확하다.
		// DieProcess 진행중엔 Burrow처럼 액터를 바로 끄는 경우가 있어서 ContinuousAffector가 클리어될 수 있기 때문.
		bool showHitBlink = true;
		if (BurrowAffector.CheckBurrow(_affectorProcessor)) showHitBlink = false;

		bool onDie = _actor.actorStatus.IsDie();
		_actor.actorStatus.AddHP(-damage);
		ChangeActorStatusAffector.OnDamage(_affectorProcessor);
		CallAffectorValueAffector.OnEvent(_affectorProcessor, CallAffectorValueAffector.eEventType.OnDamage, damage);
		ReduceContinuousDamageAffector.OnDamage(_affectorProcessor);
		if (attackerActor == null) attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
		if (attackerActor != null)
		{
			VampireAffector.OnHit(attackerActor.affectorProcessor, damage);
			HealSpOnHitAffector.OnHit(attackerActor.affectorProcessor);
			ReflectDamageAffector.OnDamage(_affectorProcessor, attackerActor, damage);
			CallAffectorValueAffector.OnEvent(attackerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnHit, damage);
			AttackWeightHitObjectAffector.OnEvent(attackerActor.affectorProcessor, _affectorProcessor, damageRatio);
			CertainHpHitObjectAffector.OnEvent(attackerActor.affectorProcessor, _affectorProcessor, _actor.actorStatus.GetHPRatio());
			TeleportingHitObjectAffector.OnEvent(attackerActor.affectorProcessor, _affectorProcessor);
		}

#if UNITY_EDITOR
		//Debug.LogFormat("Current = {0} / Max = {1} / Damage = {2} / frameCount = {3}", _actor.actorStatus.GetHP(), _actor.actorStatus.GetValue(eActorStatus.MaxHp), damage, Time.frameCount);
#endif

		onDie = (onDie == false && _actor.actorStatus.IsDie());
		if (onDie)
		{
			bool ignoreOnKill = false;
			if (_actor.IsMonsterActor())
			{
				MonsterActor monsterActor = _actor as MonsterActor;
				if (monsterActor != null && monsterActor.groupMonster && monsterActor.group.IsLastAliveMonster(monsterActor) == false)
					ignoreOnKill = true;
			}
			if (ignoreOnKill == false)
			{
				if (affectorValueLevelTableData.iValue2 == 1 && !string.IsNullOrEmpty(affectorValueLevelTableData.sValue2))
				{
					if (affectorValueLevelTableData.sValue2.Contains(","))
					{
						string[] affectorValueIdList = BattleInstanceManager.instance.GetCachedString2StringList(affectorValueLevelTableData.sValue2);
						for (int i = 0; i < affectorValueIdList.Length; ++i)
							_affectorProcessor.ApplyAffectorValue(affectorValueIdList[i], hitParameter, false);
					}
					else
						_affectorProcessor.ApplyAffectorValue(affectorValueLevelTableData.sValue2, hitParameter, false);
				}

				if (attackerActor != null)
				{
					VampireAffector.OnKill(attackerActor.affectorProcessor);
					CallAffectorValueAffector.OnEvent(attackerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnKill);
				}
			}
		}

		//Collider col = m_Actor.GetComponent<Collider>();
		//DamageFloaterManager.Instance.ShowDamage(intDamage, m_Actor.transform.position + new Vector3(0.0f, ColliderUtil.GetHeight(col), 0.0f));

		if (!showHitBlink)
			return;

		ShowHitBlink(hitParameter);
	}

	void ShowHitBlink(HitParameter hitParameter)
	{
		if (hitParameter.statusStructForHitObject.showHitBlink)
			HitBlink.ShowHitBlink(_affectorProcessor.cachedTransform);
		if (hitParameter.statusStructForHitObject.showHitRimBlink)
			HitRimBlink.ShowHitRimBlink(_affectorProcessor.cachedTransform, hitParameter.contactNormal);
	}
}
