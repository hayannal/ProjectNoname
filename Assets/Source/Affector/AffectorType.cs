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
	DropItem,
	CannotAction,
	CannotMove,
	DefaultContainer,
	Heal,
	PiercingHitObject,
	AddAffectorHitObject,
	GetLevelPack,
	CallAffectorValue,
}
#endregion