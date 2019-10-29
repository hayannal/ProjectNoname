using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

namespace ActorStatusDefine {
	
	public enum eActorStatus
	{
		MaxHp,
		Attack,
		AttackDelay,
		AttackSpeedAddRatio,
		EvadeRate,
		MoveSpeed,

		MonsterStatusAmount,

		MaxSp = MonsterStatusAmount,

		BaseAmount,

		MaxHpRate = BaseAmount,
		AttackRate,

		ExAmount,
	}

	public class StatusBase
	{
		public ObscuredFloat _hp;
		public ObscuredFloat _sp;

		public ObscuredFloat[] valueList;

		public StatusBase()
		{
			Initialize();	
		}

		public virtual void Initialize()
		{
			valueList = new ObscuredFloat[(int)eActorStatus.BaseAmount];
		}

		public void ClearValue()
		{
			if (valueList == null)
				return;

			for (int i = 0; i < valueList.Length; ++i)
				valueList[i] = 0.0f;
		}

		public bool isPlayerBaseStatus { get { return valueList.Length == (int)eActorStatus.BaseAmount; } }
		public bool IsPlayerExStatus { get { return valueList.Length == (int)eActorStatus.ExAmount; } }
		public bool IsMonsterStatus { get { return valueList.Length == (int)eActorStatus.MonsterStatusAmount; } }
	}

	public class MonsterStatusList : StatusBase
	{
		public override void Initialize()
		{
			valueList = new ObscuredFloat[(int)eActorStatus.MonsterStatusAmount];
		}
	}

	public class ActorStatusList : StatusBase
	{
		public override void Initialize()
		{
			valueList = new ObscuredFloat[(int)eActorStatus.ExAmount];
		}
	}

	public class EquipStatusList
	{
		public ObscuredFloat[] valueList;

		public EquipStatusList()
		{
			Initialize();
		}

		public virtual void Initialize()
		{
			valueList = new ObscuredFloat[(int)eActorStatus.ExAmount];
		}

		public void ClearValue()
		{
			if (valueList == null)
				return;

			for (int i = 0; i < valueList.Length; ++i)
				valueList[i] = 0.0f;
		}
	}
}