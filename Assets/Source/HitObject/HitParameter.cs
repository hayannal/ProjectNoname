using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public struct StatusStructForHitObject
{
	public int teamID;
	public float hp;
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
