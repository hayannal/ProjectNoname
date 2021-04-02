using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class TransportAttackAffector : AffectorBase
{
	AffectorValueLevelTableData _baseDamageAffectorValue;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		Actor attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);

		MonsterActor monsterActor = null;
		if (_actor.IsMonsterActor())
			monsterActor = _actor as MonsterActor;

		if (_actor.IsMonsterActor() && monsterActor != null)
		{
			if (monsterActor.bossMonster)
			{
				// 보스에게는 데미지 어펙터를 전달해야하고
				if (_baseDamageAffectorValue == null)
				{
					_baseDamageAffectorValue = new AffectorValueLevelTableData();
					_baseDamageAffectorValue.fValue1 = affectorValueLevelTableData.fValue1;
					_baseDamageAffectorValue.iValue3 = affectorValueLevelTableData.iValue3;
					_baseDamageAffectorValue.sValue4 = affectorValueLevelTableData.sValue4;
				}
				
				_affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.BaseDamage, _baseDamageAffectorValue, attackerActor, false);
			}
			else
			{
				// 잔몹이라면 즉사로 처리한다.
				float damage = _actor.actorStatus.GetHP() + 1.0f;
				FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Headshot, _actor);
				_actor.actorStatus.AddHP(-damage);

				// 강제 즉사로 죽은거라서 우선 이벤트 처리를 하지 않기로 한다.
				//if (attackerActor == null) attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
				//if (attackerActor != null)
				//{
				//	VampireAffector.OnHit(attackerActor.affectorProcessor, damage);
				//	HealSpOnHitAffector.OnHit(attackerActor.affectorProcessor);
				//	HitFlagAffector.OnHit(attackerActor.affectorProcessor, hitParameter.statusStructForHitObject.targetDetectType);
				//	ReflectDamageAffector.OnDamage(_affectorProcessor, attackerActor, damage);
				//	CallAffectorValueAffector.OnEvent(attackerActor.affectorProcessor, CallAffectorValueAffector.eEventType.OnHit, damage);
				//	AttackWeightHitObjectAffector.OnEvent(attackerActor.affectorProcessor, _affectorProcessor, damageRatio);
				//	CertainHpHitObjectAffector.OnEvent(attackerActor.affectorProcessor, _affectorProcessor, _actor.actorStatus.GetHPRatio());
				//	TeleportingHitObjectAffector.OnEvent(attackerActor.affectorProcessor, _affectorProcessor);
				//}
			}
			
			ShowHitBlink(hitParameter);
		}
	}

	void ShowHitBlink(HitParameter hitParameter)
	{
		if (hitParameter.statusStructForHitObject.showHitBlink)
			HitBlink.ShowHitBlink(_affectorProcessor.cachedTransform);
		if (hitParameter.statusStructForHitObject.showHitRimBlink)
			HitRimBlink.ShowHitRimBlink(_affectorProcessor.cachedTransform, hitParameter.contactNormal);
	}
}