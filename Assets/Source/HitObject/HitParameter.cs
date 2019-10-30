using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public struct StatusStructForHitObject
{
	public int actorInstanceId;
	public int teamId;
	public int skillLevel;  // current action skill level

	// for hitObject
	public int weaponIDAtCreation;
	public int hitSignalIndexInAction;
	public int repeatIndex;
	public bool showHitBlink;
	public bool showHitRimBlink;
	public bool monsterActor;
	public bool bossMonsterActor;

	// example
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
