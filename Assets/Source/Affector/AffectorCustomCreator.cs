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
			case eAffectorType.CallAffectorValue: affectorBase = new CallAffectorValueAffector(); break;
			case eAffectorType.Heal: affectorBase = new HealAffector(); break;
			case eAffectorType.Invincible: affectorBase = new InvincibleAffector(); break;
			case eAffectorType.CountBarrier: affectorBase = new CountBarrierAffector(); break;
			case eAffectorType.InvincibleTortoise: affectorBase = new InvincibleTortoiseAffector(); break;
			case eAffectorType.Burrow: affectorBase = new BurrowAffector(); break;
			case eAffectorType.IgnoreEvadeVisual: affectorBase = new IgnoreEvadeVisualAffector(); break;
			case eAffectorType.DropAdjust: affectorBase = new DropAdjustAffector(); break;
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
			case eAffectorType.Invincible:
			case eAffectorType.CountBarrier:
			case eAffectorType.InvincibleTortoise:
			case eAffectorType.Burrow:
			case eAffectorType.IgnoreEvadeVisual:
			case eAffectorType.DropAdjust:
			case eAffectorType.PiercingHitObject:
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
