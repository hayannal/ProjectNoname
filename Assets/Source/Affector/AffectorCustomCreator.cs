using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AffectorCustomCreator
{
	public static AffectorBase CreateAffector(eAffectorType affectorType)
	{
		AffectorBase affectorBase = null;
		switch(affectorType)
		{
			#region USER_CODE
			//case eAffectorType.ChangeAction: affectorBase = new ChangeActionAffector(); break;
			//case eAffectorType.AddForce: affectorBase = new AddForceAffector(); break;
			case eAffectorType.BaseDamage: affectorBase = new BaseDamageAffector(); break;
			//case eAffectorType.DotDamage: affectorBase = new DotDamageAffector(); break;
			//case eAffectorType.MoveToTarget: affectorBase = new MoveToTargetAffector(); break;
			case eAffectorType.AddActorState: affectorBase = new AddActorStateAffector(); break;
			case eAffectorType.ChangeActorStatus: affectorBase = new ChangeActorStatusAffector(); break;
			case eAffectorType.Heal: affectorBase = new HealAffector(); break;
			case eAffectorType.CallAffectorValue: affectorBase = new CallAffectorValueAffector(); break;
			case eAffectorType.ReduceDamage: affectorBase = new ReduceDamageAffector(); break;
			case eAffectorType.HealOverTime: affectorBase = new HealOverTimeAffector(); break;
			case eAffectorType.ReflectDamage: affectorBase = new ReflectDamageAffector(); break;
			case eAffectorType.AddAttackByHp: affectorBase = new AddAttackByHpAffector(); break;
			case eAffectorType.AddCriticalDamageByTargetHp: affectorBase = new AddCriticalDamageByTargetHpAffector(); break;
			case eAffectorType.InstantDeath: affectorBase = new InstantDeathAffector(); break;
			case eAffectorType.ImmortalWill: affectorBase = new ImmortalWillAffector(); break;
			case eAffectorType.MonsterThroughHitObject: affectorBase = new MonsterThroughHitObjectAffector(); break;
			case eAffectorType.RicochetHitObject: affectorBase = new RicochetHitObjectAffector(); break;
			case eAffectorType.BounceWallQuadHitObject: affectorBase = new BounceWallQuadHitObjectAffector(); break;
			case eAffectorType.ParallelHitObject: affectorBase = new ParallelHitObjectAffector(); break;
			case eAffectorType.DiagonalNwayGenerator: affectorBase = new DiagonalNwayGeneratorAffector(); break;
			case eAffectorType.LeftRightNwayGenerator: affectorBase = new LeftRightNwayGeneratorAffector(); break;
			case eAffectorType.BackNwayGenerator: affectorBase = new BackNwayGeneratorAffector(); break;
			case eAffectorType.RepeatHitObject: affectorBase = new RepeatHitObjectAffector(); break;
			case eAffectorType.Invincible: affectorBase = new InvincibleAffector(); break;
			case eAffectorType.CountBarrier: affectorBase = new CountBarrierAffector(); break;
			case eAffectorType.InvincibleTortoise: affectorBase = new InvincibleTortoiseAffector(); break;
			case eAffectorType.Burrow: affectorBase = new BurrowAffector(); break;
			case eAffectorType.IgnoreEvadeVisual: affectorBase = new IgnoreEvadeVisualAffector(); break;
			case eAffectorType.DropAdjust: affectorBase = new DropAdjustAffector(); break;
			case eAffectorType.SlowHitObjectSpeed: affectorBase = new SlowHitObjectSpeedAffector(); break;
			#endregion
		}
		return affectorBase;
	}

	public static bool IsContinuousAffector(eAffectorType affectorType)
	{
		switch(affectorType)
		{
			#region USER_CODE
			case eAffectorType.DotDamage:
			case eAffectorType.MoveToTarget:
			case eAffectorType.ChangeActorStatus:
			case eAffectorType.CannotAction:
			case eAffectorType.CannotMove:
			case eAffectorType.DefaultContainer:
			case eAffectorType.AddAffectorHitObject:
			case eAffectorType.CallAffectorValue:
			case eAffectorType.ReduceDamage:
			case eAffectorType.HealOverTime:
			case eAffectorType.ReflectDamage:
			case eAffectorType.AddAttackByHp:
			case eAffectorType.AddCriticalDamageByTargetHp:
			case eAffectorType.InstantDeath:
			case eAffectorType.ImmortalWill:
			case eAffectorType.MonsterThroughHitObject:
			case eAffectorType.RicochetHitObject:
			case eAffectorType.BounceWallQuadHitObject:
			case eAffectorType.ParallelHitObject:
			case eAffectorType.DiagonalNwayGenerator:
			case eAffectorType.LeftRightNwayGenerator:
			case eAffectorType.BackNwayGenerator:
			case eAffectorType.RepeatHitObject:
			case eAffectorType.Invincible:
			case eAffectorType.CountBarrier:
			case eAffectorType.InvincibleTortoise:
			case eAffectorType.Burrow:
			case eAffectorType.IgnoreEvadeVisual:
			case eAffectorType.DropAdjust:
			case eAffectorType.SlowHitObjectSpeed:
				return true;
			#endregion
		}
		return false;
	}

	public static bool IsOnlyServer(eAffectorType affectorType)
	{
		switch(affectorType)
		{
			#region USER_CODE
			case eAffectorType.BaseDamage:
				return true;
			#endregion
		}
		return false;
	}
}
