using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class CollisionDamageAffector : AffectorBase
{
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;

		float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack];
		if (_actor is PlayerActor)
		{
			BattleManager.instance.AddDamageCountOnStage();
			if (hitParameter.statusStructForHitObject.monsterActor)
			{
				float damageDecreaseAddRate = _actor.actorStatus.GetValue(hitParameter.statusStructForHitObject.bossMonsterActor ? eActorStatus.BossMonsterDamageDecreaseAddRate : eActorStatus.NormalMonsterDamageDecreaseAddRate);
				if (damageDecreaseAddRate != 0.0f)
					damage *= (1.0f - damageDecreaseAddRate);
			}
		}

		float reduceDamageRate = ReduceDamageAffector.GetValue(_affectorProcessor, ReduceDamageAffector.eReduceDamageType.Crash);
		if (reduceDamageRate != 0.0f)
			damage *= (1.0f - reduceDamageRate);

		_actor.actorStatus.AddHP(-damage);
		ChangeActorStatusAffector.OnDamage(_affectorProcessor);
		CallAffectorValueAffector.OnEvent(_affectorProcessor, CallAffectorValueAffector.eEventType.OnDamage, damage);

#if UNITY_EDITOR
		//Debug.LogFormat("Current = {0} / Max = {1} / Damage = {2} / frameCount = {3}", _actor.actorStatus.GetHP(), _actor.actorStatus.GetValue(eActorStatus.MaxHp), damage, Time.frameCount);
#endif
	}
}