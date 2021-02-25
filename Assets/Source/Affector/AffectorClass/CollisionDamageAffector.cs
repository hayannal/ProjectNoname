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
		if (StageManager.instance == null)
			return;
		if (StageManager.instance.currentStageTableData == null)
			return;

		// 플레이어도 보호막은 안쓰지만 무적은 일시적으로 사용하기로 했다. 무적 검사루틴 추가.
		if (InvincibleAffector.CheckInvincible(_affectorProcessor))
		{
			FloatingDamageTextRootCanvas.instance.ShowText(FloatingDamageText.eFloatingDamageType.Invincible, _actor);
			return;
		}

		float damage = StageManager.instance.currentStageTableData.standardAtk;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			damage = BattleManager.instance.GetSelectedNodeWarTableData().standardAtk;
		if (affectorValueLevelTableData.iValue2 == 1)
		{
			if (hitParameter.statusBase != null)
				damage = hitParameter.statusBase.valueList[(int)eActorStatus.Attack];
		}
		damage *= affectorValueLevelTableData.fValue1;

		if (_actor.IsPlayerActor())
		{
			BattleManager.instance.AddDamageCountOnStage();
			if (hitParameter.statusStructForHitObject.monsterActor)
			{
				float damageDecreaseAddRate = _actor.actorStatus.GetValue(hitParameter.statusStructForHitObject.bossMonsterActor ? eActorStatus.BossMonsterDamageDecreaseAddRate : eActorStatus.NormalMonsterDamageDecreaseAddRate);
				if (damageDecreaseAddRate != 0.0f)
					damage *= (1.0f - damageDecreaseAddRate);
			}

			// 연타저항은 충돌뎀지 감소팩, 트랩뎀지 감소팩이 각각 있기때문에 처리하지 않는다.
			// 강공격 방어도 마찬가지
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

		// 마인으로 몬스터 공격할때는 이 어펙터를 사용하므로 몬스터꺼도 호출해주긴 해야한다.
		MonsterSleepingAffector.OnDamage(_affectorProcessor);
		CastAffector.OnDamage(_affectorProcessor);
		AddAttackByContinuousKillAffector.OnDamage(_affectorProcessor);
		BurrowAffector.OnDamage(_affectorProcessor);

#if UNITY_EDITOR
		//Debug.LogFormat("Current = {0} / Max = {1} / Damage = {2} / frameCount = {3} : CollisionDamage", _actor.actorStatus.GetHP(), _actor.actorStatus.GetValue(eActorStatus.MaxHp), damage, Time.frameCount);
#endif
	}
}