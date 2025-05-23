﻿using UnityEngine;
using System.Collections;

#region USER_CODE
public enum eMecanimEventType
{
	State,
	Effect,
	Sound,
	TimeScale,
	Destroy,
	ApplyAffector,
	SendInfoToAffector,
	AnimatorSpeed,
	AnimatorParameter,
	HitObject,
	RangeHitObject,
	GlobalLight,
	MovePositionCurve,
	MoveToTarget,
	DontMove,
	DisableActorCollider,
	LookAt,
	IdleAnimator,
	AttackIndicator,
	Summon,
	BattleToast,
	ChangeMecanimState,
	SubAnimator,
	ContinuousAttack,
	Flash,
}
#endregion