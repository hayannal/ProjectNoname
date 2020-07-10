using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region USER_CODE
public enum eAffectorType
{
	ChangeAction = 1,
	BaseDamage,
	DotDamage,
	AddForce,
	Velocity,
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
	AddAttackByHp,
	AddCriticalDamageByTargetHp,
	InstantDeath,
	ImmortalWill,

	CreateHitObject,
	EnlargeDamage,
	RemoveColliderHitObject,
	ChangeAttackState,

	MonsterThroughHitObject = 31,
	RicochetHitObject,
	BounceWallQuadHitObject,
	ParallelHitObject,
	DiagonalNwayGenerator,
	LeftRightNwayGenerator,
	BackNwayGenerator,
	RepeatHitObject,
	AttackWeightHitObject,
	CertainHpHitObject,

	TeleportingHitObject,
	AddGeneratorCreateCount,
	ArcFormHitObject,

	DropItem = 51,
	Invincible,
	CountBarrier,
	InvincibleTortoise,
	Burrow,
	IgnoreEvadeVisual,
	DropAdjust,
	SlowHitObjectSpeed,
	CollisionDamage,
	Teleported,

	CreateHitObjectMoving,
	CreateWall,
	PositionBuff,
	ReduceContinuousDamage,
	DefenseStrongDamage,
	HealSpOnHit,
	PaybackSp,
	Vampire,
	Rush,
	TeleportTargetPosition,
	Rotate,
	Suicide,
	DelayedBaseDamage,
}
#endregion