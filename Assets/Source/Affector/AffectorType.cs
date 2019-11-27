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
	AddAttackByHp,
	AddCriticalDamageByTargetHp,
	InstantDeath,
	ImmortalWill,

	CreateHitObject,
	EnlargeDamage,

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
}
#endregion