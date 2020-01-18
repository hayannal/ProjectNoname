using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

// 처음엔 충돌데미지 어펙터로 만들었다가 조금씩 확장되면서 BaseDamageAffector보다 작은 BasicDamageAffector 같은 클래스가 되었지만
// 이제와서 이름을 바꾸긴 뭐하니 그냥 둔다. 크리가 터지지 않고 발사체 증가같은게 들어있지 않는게 특징이다.
// 충돌 말고 트랩 데미지에도 쓴다.
public class CollisionDamageAffector : AffectorBase
{
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;

		float damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack];
		damage *= affectorValueLevelTableData.fValue1;

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

		float reduceDamageValue = 0.0f;
		if (affectorValueLevelTableData.iValue1 == 0)
			reduceDamageValue = ReduceDamageAffector.GetValue(_affectorProcessor, ReduceDamageAffector.eReduceDamageType.Crash);
		else if (affectorValueLevelTableData.iValue1 == 1)
			reduceDamageValue = ReduceDamageAffector.GetValue(_affectorProcessor, ReduceDamageAffector.eReduceDamageType.Trap);
		if (reduceDamageValue != 0.0f)
			damage *= (1.0f - (reduceDamageValue / (1.0f + reduceDamageValue)));

		float enlargeDamageValue = EnlargeDamageAffector.GetValue(_affectorProcessor);
		if (enlargeDamageValue != 0.0f)
			damage *= (1.0f + enlargeDamageValue);

		_actor.actorStatus.AddHP(-damage);
		ChangeActorStatusAffector.OnDamage(_affectorProcessor);
		CallAffectorValueAffector.OnEvent(_affectorProcessor, CallAffectorValueAffector.eEventType.OnDamage, damage);

#if UNITY_EDITOR
		//Debug.LogFormat("Current = {0} / Max = {1} / Damage = {2} / frameCount = {3}", _actor.actorStatus.GetHP(), _actor.actorStatus.GetValue(eActorStatus.MaxHp), damage, Time.frameCount);
#endif
	}
}