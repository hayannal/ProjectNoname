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
			case eAffectorType.CountBarrier: affectorBase = new CountBarrierAffector(); break;
			case eAffectorType.Invincible: affectorBase = new InvincibleAffector(); break;
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
			case eAffectorType.PiercingHitObject:
			case eAffectorType.AddAffectorHitObject:
			case eAffectorType.CountBarrier:
			case eAffectorType.Invincible:
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
