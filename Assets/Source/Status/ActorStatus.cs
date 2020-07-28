﻿#define CHEAT_RESURRECT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Networking;
using ActorStatusDefine;
using MecanimStateDefine;

public class ActorStatus : MonoBehaviour
{
	// 네트워크가 알아서 맞춰주는 SyncVar같은거로 동기화 하기엔 너무 위험하다. 타이밍을 재기 위해 더이상 이런식으로 하진 않는다.
	//[SyncVar(hook = "OnChangeHp")]
	//float _hp;

	// 이 statusBase가 캐싱 역할을 수행한다. UI에 표기되는 수치도 이 값을 로그화 시켜서 보여주는거다.
	StatusBase _statusBase;
	public StatusBase statusBase { get { return _statusBase; } }
	public Actor actor { get; private set; }
	public int powerLevel { get; private set; }

	static float s_criticalPowerConstantA = 5.0f;
	static float s_criticalPowerConstantB = 3.0f;

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	// 로비에서 파워레벨이 바뀌든 연구소 장비가 바뀌든 이 함수 호출해주면 알아서 모든 스탯을 재계산하게 된다.
	public void InitializeActorStatus(int overridePowerLevel = -1)
	{
		if (_statusBase == null)
			_statusBase = new ActorStatusList();
		else
			_statusBase.ClearValue();

		CharacterData characterData = PlayerData.instance.GetCharacterData(actor.actorId);
		if (overridePowerLevel == -1)
		{
			powerLevel = 1;
			if (characterData != null) powerLevel = characterData.powerLevel;
		}
		else
			powerLevel = overridePowerLevel;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actor.actorId);
		PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(powerLevel);
		_statusBase.valueList[(int)eActorStatus.MaxHp] = powerLevelTableData.hp;
		_statusBase.valueList[(int)eActorStatus.Attack] = powerLevelTableData.atk;
		_statusBase.valueList[(int)eActorStatus.AttackDelay] = actorTableData.attackDelay;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = actorTableData.moveSpeed;
		_statusBase.valueList[(int)eActorStatus.MaxSp] = actorTableData.sp;

		// 보유한 캐릭터들 처리
		if (PlayerData.instance.ContainsActor(actor.actorId))
		{
			// 내가 가지고 있는 캐릭터들에 한해서만 장비 계산을 적용한다.
			// _statusBase 에 로비에서의 스탯을 caching해둔다.
			// 이걸 완전히 합쳐버릴때의 단점이 장비로 올라가는 공% 합산값만 따로 확인하기 어렵다는건데
			// 어차피 TimeSpace에서는 별도로 캐싱된 값을 가지고 표시하기 때문에 상관없다.

			// equip
			for (int i = 0; i < _statusBase.valueList.Length; ++i)
				_statusBase.valueList[i] += TimeSpaceData.instance.cachedEquipStatusList.valueList[i];

			// research
			if (PlayerData.instance.researchLevel > 0)
			{
				ResearchTableData researchTableData = TableDataManager.instance.FindResearchTableData(PlayerData.instance.researchLevel);
				if (researchTableData != null)
				{
					_statusBase.valueList[(int)eActorStatus.MaxHp] += researchTableData.accumulatedHp;
					_statusBase.valueList[(int)eActorStatus.Attack] += researchTableData.accumulatedAtk;
				}
			}

			// equip rate + potential stat rate + training stat rate
			float maxHpAddRate = 1.0f;
			float attackAddRate = 1.0f;
			maxHpAddRate += TimeSpaceData.instance.cachedEquipStatusList.valueList[(int)eActorStatus.MaxHpAddRate];
			attackAddRate += TimeSpaceData.instance.cachedEquipStatusList.valueList[(int)eActorStatus.AttackAddRate];
			if (characterData.listStatPoint.Count == (int)CharacterInfoStatsCanvas.eStatsType.Amount)
			{
				maxHpAddRate += GetValueByCharacterStats(characterData, CharacterInfoStatsCanvas.eStatsType.Vit, false);
				attackAddRate += GetValueByCharacterStats(characterData, CharacterInfoStatsCanvas.eStatsType.Str, false);

				// potential MoveSpeedAddRate, SpGainAddRate
				_statusBase.valueList[(int)eActorStatus.MaxSp] *= (1.0f + GetValueByCharacterStats(characterData, CharacterInfoStatsCanvas.eStatsType.Int, false));
				_statusBase.valueList[(int)eActorStatus.MoveSpeed] += GetValueByCharacterStats(characterData, CharacterInfoStatsCanvas.eStatsType.Dex, false);
				_statusBase.valueList[(int)eActorStatus.AttackSpeedAddRate] += GetValueByCharacterStats(characterData, CharacterInfoStatsCanvas.eStatsType.Dex, true);
			}
			if (characterData.trainingValue > 0)
			{
				float trainingRatio = (float)characterData.trainingValue / CharacterInfoTrainingCanvas.TrainingMax;
				maxHpAddRate += actorTableData.trainingHp * trainingRatio;
				attackAddRate += actorTableData.trainingAtk * trainingRatio;
			}
			if (characterData.listWingGradeId.Count == (int)CharacterInfoWingCanvas.eStatsType.Amount)
			{
				_statusBase.valueList[(int)eActorStatus.AttackSpeedAddRate] += GetValueByCharacterWing(characterData, CharacterInfoWingCanvas.eStatsType.AttackSpeed);
				_statusBase.valueList[(int)eActorStatus.CriticalRate] += GetValueByCharacterWing(characterData, CharacterInfoWingCanvas.eStatsType.CriticalRate);
				_statusBase.valueList[(int)eActorStatus.CriticalDamageAddRate] += GetValueByCharacterWing(characterData, CharacterInfoWingCanvas.eStatsType.CriticalDamage);
			}

			// over max pp
			attackAddRate += GetAttackAddRateByOverPP();

			_statusBase.valueList[(int)eActorStatus.MaxHp] *= maxHpAddRate;
			_statusBase.valueList[(int)eActorStatus.Attack] *= attackAddRate;
		}

		// actor multi
		_statusBase.valueList[(int)eActorStatus.MaxHp] *= actorTableData.multiHp;
		_statusBase.valueList[(int)eActorStatus.Attack] *= actorTableData.multiAtk;

		//if (isServer)
		_statusBase._hp = _lastMaxHp = GetValue(eActorStatus.MaxHp);
		_statusBase._sp = 0.0f;

		OnChangedStatus();
	}

	public void InitializeMonsterStatus()
	{
		if (_statusBase == null)
			_statusBase = new MonsterStatusList();
		else
			_statusBase.ClearValue();

		bool nodeWarCachingMonster = false;
		MonsterTableData monsterTableData = TableDataManager.instance.FindMonsterTableData(actor.actorId);
		float standardHp = StageManager.instance.currentMonstrStandardHp;
		float standardAtk = StageManager.instance.currentMonstrStandardAtk;
		if (actor.team.teamId == (int)Team.eTeamID.DefaultAlly)
		{
			// 아군 몹이라면 플레이어 액터 스탯을 기반으로 계산해야한다.
			if (BattleInstanceManager.instance.playerActor != null)
			{
				standardHp = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.MaxHp);
				standardAtk = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.Attack);
			}
		}
		else if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
		{
			NodeWarTableData nodeWarTableData = BattleManager.instance.GetSelectedNodeWarTableData();
			if (nodeWarTableData != null)
			{
				standardHp = nodeWarTableData.standardHp;
				standardAtk = nodeWarTableData.standardAtk;
			}
			else
			{
				nodeWarCachingMonster = true;
			}
		}
		_statusBase.valueList[(int)eActorStatus.MaxHp] = standardHp * monsterTableData.multiHp;
		_statusBase.valueList[(int)eActorStatus.Attack] = standardAtk * monsterTableData.multiAtk;
		_statusBase.valueList[(int)eActorStatus.AttackDelay] = monsterTableData.attackDelay;
		_statusBase.valueList[(int)eActorStatus.EvadeRate] = monsterTableData.evadeRate;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = monsterTableData.moveSpeed;

		//if (isServer)
		_statusBase._hp = _lastMaxHp = GetValue(eActorStatus.MaxHp);
		if (nodeWarCachingMonster)
			_statusBase._hp = 0.0f;

		OnChangedStatus();
	}

	float _lastMaxHp = 0.0f;
	public void OnChangedStatus(eActorStatus eType = eActorStatus.ExAmount)
	{
		if (eType == eActorStatus.MoveSpeed || eType == eActorStatus.MoveSpeedAddRate || eType == eActorStatus.ExAmount)
			actor.baseCharacterController.speed = GetValue(eActorStatus.MoveSpeed);
		if (eType == eActorStatus.AttackSpeedAddRate || eType == eActorStatus.ExAmount)
			actor.actionController.OnChangedAttackSpeedAddRatio(GetValue(eActorStatus.AttackSpeedAddRate));

		// 로비에서는 장비 변경해도 만피로 유지되면서 이 함수 호출되는데 항상 ExAmount로 올거다.
		// 그래서 ExAmount로 올땐 처리하지 않고 인게임 내에서 MaxHp 관련 스탯으로 올때만 처리해주면 된다.
		if (eType == eActorStatus.MaxHp || eType == eActorStatus.MaxHpAddRate)
		{
			float maxHp = GetValue(eActorStatus.MaxHp);
			if (maxHp > _lastMaxHp)
			{
				AddHP(maxHp - _lastMaxHp);
			}
			else
			{
				if (GetHP() > maxHp)
					AddHP(maxHp - GetHP());
				else
					actor.OnChangedHP();
			}
			_lastMaxHp = maxHp;
		}
	}

	public float GetCachedValue(eActorStatus eType)
	{
		if ((int)eType >= _statusBase.valueList.Length)
			return 0.0f;

		return _statusBase.valueList[(int)eType];
	}

	public float GetValue(eActorStatus eType)
	{
		float value = 0.0f;
		if ((int)eType < _statusBase.valueList.Length)
			value += _statusBase.valueList[(int)eType];
		value += ChangeActorStatusAffector.GetValue(actor.affectorProcessor, eType);

		float addRate = 0.0f;
		switch (eType)
		{
			case eActorStatus.MaxHp:
				addRate = GetValue(eActorStatus.MaxHpAddRate);
				if (addRate != 0.0f) value *= (1.0f + addRate);
				break;
			case eActorStatus.Attack:
				addRate = GetValue(eActorStatus.AttackAddRate);
				if (addRate != 0.0f) value *= (1.0f + addRate);
				break;
			case eActorStatus.AttackDelay:
				float attackSpeedAddRate = GetValue(eActorStatus.AttackSpeedAddRate);
				if (attackSpeedAddRate != 0.0f) value /= (1.0f + attackSpeedAddRate);
				break;
			case eActorStatus.MoveSpeed:
				// 0으로 고정시키면 ai에서 아예 Move애니 대신 Idle이 나오게 된다. 그래서 0보다는 큰 값으로 설정해둔다.
				if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction)) value = 0.00001f;
				// 0에 수렴할수록 아예 회전조차 보이지 않게 되서 랜덤무브의 방향전환이 보이지 않게 된다. 그래서 값을 좀더 높여둔다.
				if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove)) value = 0.05f;
				addRate = GetValue(eActorStatus.MoveSpeedAddRate);
				if (addRate != 0.0f) value *= (1.0f + addRate);
				break;
			case eActorStatus.CriticalRate:
				float criticalPower1 = GetValue(eActorStatus.CriticalPower);
				if (criticalPower1 != 0.0f) value += (criticalPower1 / (criticalPower1 * s_criticalPowerConstantA / s_criticalPowerConstantB + BattleInstanceManager.instance.GetCachedGlobalConstantFloat("DefaultCriticalDamageRate")));
				break;
			case eActorStatus.CriticalDamageAddRate:
				float criticalPower2 = GetValue(eActorStatus.CriticalPower);
				if (criticalPower2 != 0.0f) value += (criticalPower2 * s_criticalPowerConstantA / s_criticalPowerConstantB);
				break;
			case eActorStatus.AttackAddRate:
				value += AddAttackByHpAffector.GetValue(actor.affectorProcessor, actor.actorStatus.GetHPRatio());
				value += PositionBuffAffector.GetAttackAddRate(actor.affectorProcessor);
				break;
		}
		return value;
	}

	#region Stats Point
	float GetValueByCharacterStats(CharacterData characterData, CharacterInfoStatsCanvas.eStatsType statsType, bool value2)
	{
		// 호출하는 부분에서 검사하고 호출한다.
		//if (characterData == null)
		//	return 0.0f;
		//if (characterData.listStatPoint.Count != (int)CharacterInfoStatsCanvas.eStatsType.Amount)
		//	return 0.0f;
		int point = characterData.listStatPoint[(int)statsType];
		if (point == 0)
			return 0.0f;
		ExtraStatTableData extraStatTableData = TableDataManager.instance.FindExtraStatsTableData(statsType, point);
		if (extraStatTableData == null)
			return 0.0f;
		return value2 ? extraStatTableData.value2 : extraStatTableData.value1;
	}
	#endregion

	#region Wing
	float GetValueByCharacterWing(CharacterData characterData, CharacterInfoWingCanvas.eStatsType statsType)
	{
		//if (characterData == null)
		//	return 0.0f;
		//if (characterData.listWingGradeId.Count != (int)CharacterInfoWingCanvas.eStatsType.Amount)
		//	return 0.0f;
		int grade = characterData.listWingGradeId[(int)statsType];
		if (grade == 0)
			return 0.0f;
		WingPowerTableData wingPowerTableData = TableDataManager.instance.FindWingPowerTableData((int)statsType, grade);
		if (wingPowerTableData == null)
			return 0.0f;
		return wingPowerTableData.value1;
	}
	#endregion

	static float LnAtkConstant1 = 123.315173118822f;
	static float LnAtkConstant2 = -282.943679363379f;
	public float GetDisplayAttack()
	{
		return GetDisplayAttack(GetValue(eActorStatus.Attack));
	}

	public static float GetDisplayAttack(float value)
	{
		float result = LnAtkConstant1 * Mathf.Log(value) + LnAtkConstant2;
		return result;
	}

	static float LnHpConstant1 = 197.304276990115f;
	static float LnHpConstant2 = -766.858870654696f;
	public float GetDisplayMaxHp()
	{
		return GetDisplayMaxHp(GetValue(eActorStatus.MaxHp));
	}

	public static float GetDisplayMaxHp(float value)
	{
		float result = LnHpConstant1 * Mathf.Log(value) + LnHpConstant2;
		return result;
	}

	public void GetNextPowerLevelDisplayValue(ref float nextAttack, ref float nextMaxHp)
	{
		InitializeActorStatus(powerLevel + 1);
		nextAttack = GetDisplayAttack();
		nextMaxHp = GetDisplayMaxHp();
		InitializeActorStatus();
	}

	static float LnOverConstant1 = 0.00822722242162062f;
	static float LnOverConstant2 = 0.0001f;
	public float GetAttackAddRateByOverPP()
	{
		int overMaxPp = 0;
		CharacterData characterData = PlayerData.instance.GetCharacterData(actor.actorId);
		if (characterData != null)
		{
			// 캐릭터의 파워레벨 대신 저장되어있는 powerLevel을 써도 된다.
			if (powerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
				overMaxPp = characterData.pp - characterData.maxPp;
		}
		if (overMaxPp == 0)
			return 0.0f;
		return LnOverConstant1 * Mathf.Log(overMaxPp) + LnOverConstant2;
	}

	public bool IsDie()
	{
		return GetHP() <= 0;
	}

	public float GetHP()
	{
		return _statusBase._hp;
	}

	public float GetSP()
	{
		return _statusBase._sp;
	}

	//void OnChangeHp(float hp)
	//{
	//	Debug.Log("OnChange HP : " + hp.ToString());
	//}


	#region For HitObject
	public void CopyStatusBase(ref StatusBase targetStatusBase)
	{
		//if (targetStatusBase == null) targetStatusBase = new StatusBase();

		int minLength = Mathf.Min(_statusBase.valueList.Length, targetStatusBase.valueList.Length);
		for (int i = 0; i < targetStatusBase.valueList.Length; ++i)
		{
			if (i < minLength)
				targetStatusBase.valueList[i] = GetValue((eActorStatus)i);
			else
				targetStatusBase.valueList[i] = 0.0f;
		}

		targetStatusBase._hp = _statusBase._hp;
		targetStatusBase._sp = _statusBase._sp;
	}
	#endregion

	/*
	// Current Status with Buff
	public virtual float GetStatus(BaseStatus.eStatus eType)
	{
		switch(eType)
		{
		case BaseStatus.eStatus.MaxHP:
			return GetCalcStatus(eType);
		case BaseStatus.eStatus.Attack:
			return GetCalcStatus(BaseStatus.eStatus.Attack) * (1.0f + GetCalcStatus(BaseStatus.eStatus.AttackRatio));
		case BaseStatus.eStatus.Defense:mf
			return GetCalcStatus(eType);

		case BaseStatus.eStatus.Critical:
		case BaseStatus.eStatus.Evade:
			return GetCalcStatus(eType);
		default:
			return GetCalcStatus(eType);
		}
		// with Buff
		//BuffAffector.CheckBuff(eType, GetCalcStatus(eType));
		//BuffAffector.CheckBuff(eType, GetCalcStatus(eType), GetCalcStatus(BaseStatus.eStatus.AttackRatio));
	}

	protected virtual float GetCalcStatus(BaseStatus.eStatus eType)
	{
		if ((int)eType < m_ActorStatus.Values.Length)
			return m_ActorStatus.Values[(int)eType];
		return 0.0f;
	}
	*/

#if CHEAT_RESURRECT
	public bool cheatDontDie { get; set; }
	public int cheatDontDieUseCount { get; private set; }
#endif
	public virtual void AddHP(float addHP)
	{
		if (addHP == 0.0f)
			return;

#if UNITY_EDITOR
		if (HUDDPS.isActive && actor.IsMonsterActor() && addHP < 0.0f)
		{
			float damage = -addHP;
			float overDamage = damage - _statusBase._hp;
			if (overDamage < 0.0f) overDamage = 0.0f;
			damage -= overDamage;
			HUDDPS.instance.AddDamage(damage, overDamage);
		}
#endif
		_statusBase._hp += addHP;
		_statusBase._hp = Mathf.Clamp(_statusBase._hp, 0, GetValue(eActorStatus.MaxHp));

		bool onDie = false;
		if (_statusBase._hp <= 0)
		{
			// 애니 중에 죽으면 정말 이상하게 보이는 상황이 있다. 공중같이.
			// 이럴때를 대비해서 Die를 무시하고 hp를 1로 복구시켜주는 DontDie 애니 시그널을 추가해둔다. 불굴의 의지로 자세(애니)를 유지.
			bool dontDie = false;
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.DontDie) || ImmortalWillAffector.CheckImmortal(actor.affectorProcessor))
				dontDie = true;
#if CHEAT_RESURRECT
			if (cheatDontDie)
			{
				dontDie = true;
				cheatDontDieUseCount += 1;
				Debug.LogFormat("Cheat Resurrect Count = {0}", cheatDontDieUseCount);
			}
#endif
			if (dontDie)
			{
				_statusBase._hp = 1.0f;
			}
			else
			{
				onDie = true;
			}
#if CHEAT_RESURRECT
			if (cheatDontDie)
				_statusBase._hp = GetValue(eActorStatus.MaxHp);
#endif
		}
		actor.OnChangedHP();
		if (onDie) actor.OnDie();
	}

	public float GetHPRatio()
	{
		return GetHP() / GetValue(eActorStatus.MaxHp);
	}


	public virtual void AddSP(float addSP)
	{
		_statusBase._sp += addSP;
		_statusBase._sp = Mathf.Clamp(_statusBase._sp, 0, GetValue(eActorStatus.MaxSp));
		actor.OnChangedSP();
		if (_statusBase._sp <= 0)
			_statusBase._sp = 0.0f;
	}

	public float GetSPRatio()
	{
		return GetSP() / GetValue(eActorStatus.MaxSp);
	}


	// for Swap
	public void SetHpRatio(float hpRatio)
	{
		_statusBase._hp = GetValue(eActorStatus.MaxHp) * hpRatio;
	}

	public void SetSpRatio(float ratio)
	{
		_statusBase._sp = GetValue(eActorStatus.MaxSp) * ratio;
	}


	// for Experience
	public void ChangeExperienceMode(PlayerActor attackerPlayerActor)
	{
		float value = attackerPlayerActor.actorStatus.GetValue(eActorStatus.Attack);
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(attackerPlayerActor.actorId);
		if (actorTableData == null)
			return;
		float result = value * 1.2f / actorTableData.multiAtk;
		_statusBase._hp = _statusBase.valueList[(int)eActorStatus.MaxHp] = result;
	}
}
