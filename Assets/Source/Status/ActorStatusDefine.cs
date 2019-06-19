using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ActorStatusDefine {
	
	public enum eActorStatus
	{
		MaxHP,
		Attack,
		Defense,
		MoveSpeed,

		BaseAmount,

		AttackRatio = BaseAmount,
		DefenseRatio,

		ExAmount,
	}

	public class StatusBase
	{
		public float[] valueList;

		public StatusBase()
		{
			Initialize();	
		}

		public virtual void Initialize()
		{
			valueList = new float[(int)eActorStatus.BaseAmount];
		}
	}

	public class ActorStatusList : StatusBase
	{
		public override void Initialize()
		{
			valueList = new float[(int)eActorStatus.ExAmount];
		}
	}
}