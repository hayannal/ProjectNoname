using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ActorStatusDefine {
	
	public enum eActorStatus
	{
		MaxHP,
		Attack,
		AttackDelay,
		AttackSpeedRatio,
		Defense,
		MoveSpeed,

		MonsterStatusAmount,

		AttackSpeedStep = MonsterStatusAmount,

		BaseAmount,

		AttackRatio = BaseAmount,
		DefenseRatio,

		ExAmount,
	}

	public class StatusBase
	{
		public float _hp;	// EncriptedFloat
		// public float _mp;

		public float[] valueList;

		public StatusBase()
		{
			Initialize();	
		}

		public virtual void Initialize()
		{
			valueList = new float[(int)eActorStatus.BaseAmount];
			ClearValue();
		}

		public void ClearValue()
		{
			if (valueList == null)
				return;

			for (int i = 0; i < valueList.Length; ++i)
				valueList[i] = 0.0f;
			valueList[(int)eActorStatus.AttackSpeedRatio] = 1.0f;
		}

		public bool isPlayerBaseStatus { get { return valueList.Length == (int)eActorStatus.BaseAmount; } }
		public bool IsPlayerExStatus { get { return valueList.Length == (int)eActorStatus.ExAmount; } }
		public bool IsMonsterStatus { get { return valueList.Length == (int)eActorStatus.MonsterStatusAmount; } }
	}

	public class MonsterStatusList : StatusBase
	{
		public override void Initialize()
		{
			valueList = new float[(int)eActorStatus.MonsterStatusAmount];
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