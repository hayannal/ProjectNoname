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
			case eAffectorType.CannotAction: affectorBase = new CannotActionAffector(); break;
			case eAffectorType.CannotMove: affectorBase = new CannotMoveAffector(); break;
			case eAffectorType.Heal: affectorBase = new HealAffector(); break;
			case eAffectorType.DefaultContainer: affectorBase = new DefaultContainerAffector(); break;
			case eAffectorType.CallAffectorValue: affectorBase = new CallAffectorValueAffector(); break;
			case eAffectorType.ReduceDamage: affectorBase = new ReduceDamageAffector(); break;
			case eAffectorType.HealOverTime: affectorBase = new HealOverTimeAffector(); break;
			case eAffectorType.ReflectDamage: affectorBase = new ReflectDamageAffector(); break;
			case eAffectorType.AddAttackByHp: affectorBase = new AddAttackByHpAffector(); break;
			case eAffectorType.AddCriticalDamageByTargetHp: affectorBase = new AddCriticalDamageByTargetHpAffector(); break;
			case eAffectorType.InstantDeath: affectorBase = new InstantDeathAffector(); break;
			case eAffectorType.ImmortalWill: affectorBase = new ImmortalWillAffector(); break;
			case eAffectorType.CreateHitObject: affectorBase = new CreateHitObjectAffector(); break;
			case eAffectorType.EnlargeDamage: affectorBase = new EnlargeDamageAffector(); break;
			case eAffectorType.RemoveColliderHitObject: affectorBase = new RemoveColliderHitObjectAffector(); break;
			case eAffectorType.MonsterThroughHitObject: affectorBase = new MonsterThroughHitObjectAffector(); break;
			case eAffectorType.RicochetHitObject: affectorBase = new RicochetHitObjectAffector(); break;
			case eAffectorType.BounceWallQuadHitObject: affectorBase = new BounceWallQuadHitObjectAffector(); break;
			case eAffectorType.ParallelHitObject: affectorBase = new ParallelHitObjectAffector(); break;
			case eAffectorType.DiagonalNwayGenerator: affectorBase = new DiagonalNwayGeneratorAffector(); break;
			case eAffectorType.LeftRightNwayGenerator: affectorBase = new LeftRightNwayGeneratorAffector(); break;
			case eAffectorType.BackNwayGenerator: affectorBase = new BackNwayGeneratorAffector(); break;
			case eAffectorType.RepeatHitObject: affectorBase = new RepeatHitObjectAffector(); break;
			case eAffectorType.AttackWeightHitObject: affectorBase = new AttackWeightHitObjectAffector(); break;
			case eAffectorType.CertainHpHitObject: affectorBase = new CertainHpHitObjectAffector(); break;
			case eAffectorType.TeleportingHitObject: affectorBase = new TeleportingHitObjectAffector(); break;
			case eAffectorType.AddGeneratorCreateCount: affectorBase = new AddGeneratorCreateCountAffector(); break;
			case eAffectorType.Invincible: affectorBase = new InvincibleAffector(); break;
			case eAffectorType.CountBarrier: affectorBase = new CountBarrierAffector(); break;
			case eAffectorType.InvincibleTortoise: affectorBase = new InvincibleTortoiseAffector(); break;
			case eAffectorType.Burrow: affectorBase = new BurrowAffector(); break;
			case eAffectorType.IgnoreEvadeVisual: affectorBase = new IgnoreEvadeVisualAffector(); break;
			case eAffectorType.DropAdjust: affectorBase = new DropAdjustAffector(); break;
			case eAffectorType.SlowHitObjectSpeed: affectorBase = new SlowHitObjectSpeedAffector(); break;
			case eAffectorType.CollisionDamage: affectorBase = new CollisionDamageAffector(); break;
			case eAffectorType.Teleported: affectorBase = new TeleportedAffector(); break;
			case eAffectorType.CreateHitObjectMoving: affectorBase = new CreateHitObjectMovingAffector(); break;
			case eAffectorType.CreateWall: affectorBase = new CreateWallAffector(); break;
			case eAffectorType.PositionBuff: affectorBase = new PositionBuffAffector(); break;
			case eAffectorType.ReduceContinuousDamage: affectorBase = new ReduceContinuousDamageAffector(); break;
			case eAffectorType.DefenseStrongDamage: affectorBase = new DefenseStrongDamageAffector(); break;
			case eAffectorType.HealSpOnHit: affectorBase = new HealSpOnHitAffector(); break;
			case eAffectorType.PaybackSp: affectorBase = new PaybackSpAffector(); break;
			case eAffectorType.Vampire: affectorBase = new VampireAffector(); break;
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
			case eAffectorType.EnlargeDamage:
			case eAffectorType.RemoveColliderHitObject:
			case eAffectorType.MonsterThroughHitObject:
			case eAffectorType.RicochetHitObject:
			case eAffectorType.BounceWallQuadHitObject:
			case eAffectorType.ParallelHitObject:
			case eAffectorType.DiagonalNwayGenerator:
			case eAffectorType.LeftRightNwayGenerator:
			case eAffectorType.BackNwayGenerator:
			case eAffectorType.RepeatHitObject:
			case eAffectorType.AttackWeightHitObject:
			case eAffectorType.CertainHpHitObject:
			case eAffectorType.TeleportingHitObject:
			case eAffectorType.AddGeneratorCreateCount:
			case eAffectorType.Invincible:
			case eAffectorType.CountBarrier:
			case eAffectorType.InvincibleTortoise:
			case eAffectorType.Burrow:
			case eAffectorType.IgnoreEvadeVisual:
			case eAffectorType.DropAdjust:
			case eAffectorType.SlowHitObjectSpeed:
			case eAffectorType.Teleported:
			case eAffectorType.CreateHitObjectMoving:
			case eAffectorType.CreateWall:
			case eAffectorType.PositionBuff:
			case eAffectorType.ReduceContinuousDamage:
			case eAffectorType.DefenseStrongDamage:
			case eAffectorType.HealSpOnHit:
			case eAffectorType.PaybackSp:
			case eAffectorType.Vampire:
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
