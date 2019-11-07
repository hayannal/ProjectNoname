using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region USER_CODE
public enum eAffectorType
{
	ChangeAction = 1,
	AddForce,
	BaseDamage,
	DotDamage,
	MoveToTarget,
	AddActorState,
	ChangeActorStatus,
	CannotAction,
	CannotMove,
	DefaultContainer,

	Heal,
	AddAffectorHitObject,
	CallAffectorValue,
	ReduceDamage,
	HealOverTime,
	ReflectDamage,

	MonsterThroughHitObject = 31,
	RicochetHitObject,
	BounceWallQuadHitObject,
	ParallelHitObject,
	DiagonalNwayGenerator,
	LeftRightNwayGenerator,
	BackNwayGenerator,
	RepeatHitObject,

	DropItem = 51,
	Invincible,
	CountBarrier,
	Headshot,
	InvincibleTortoise,
	Burrow,
	IgnoreEvadeVisual,
	DropAdjust,
}
#endregion