using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public struct StatusStructForHitObject
{
	public int teamId;
	public int weaponIDAtCreation;
	public int skillLevel;  // current action skill level
	public int hitSignalIndexInAction;
	public int repeatIndex;
	//public float hp;	// hp is in StatusBase
	//public int level;
	//public int liveComradeCount;
}

public struct HitParameter
{
	public Vector3 hitNormal;
	public Vector3 contactPoint;
	public Vector3 contactNormal;
	public StatusBase statusBase;	// class
	public StatusStructForHitObject statusStructForHitObject;
}
