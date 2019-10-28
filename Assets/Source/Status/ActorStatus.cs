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

	StatusBase _statusBase;
	public StatusBase statusBase { get { return _statusBase; } }
	public Actor actor { get; private set; }

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	public void InitializeActorStatus(string actorId)
	{
		if (_statusBase == null)
			_statusBase = new ActorStatusList();
		else
			_statusBase.ClearValue();

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(1);
		_statusBase.valueList[(int)eActorStatus.MaxHP] = powerLevelTableData.hp;
		_statusBase.valueList[(int)eActorStatus.Attack] = powerLevelTableData.atk;
		_statusBase.valueList[(int)eActorStatus.AttackDelay] = actorTableData.attackDelay;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = actorTableData.moveSpeed;
		_statusBase.valueList[(int)eActorStatus.MaxSP] = actorTableData.sp;

		//if (isServer)
		_statusBase._hp = GetValue(eActorStatus.MaxHP);
		_statusBase._sp = 0.0f;

		OnChangedStatus();
	}

	public void InitializeMonsterStatus(string monsterActorId)
	{
		if (_statusBase == null)
			_statusBase = new MonsterStatusList();
		else
			_statusBase.ClearValue();

		MonsterTableData monsterTableData = TableDataManager.instance.FindMonsterTableData(monsterActorId);
		_statusBase.valueList[(int)eActorStatus.MaxHP] = StageManager.instance.currentMonstrStandardHp * monsterTableData.multiHp;
		_statusBase.valueList[(int)eActorStatus.Attack] = StageManager.instance.currentMonstrStandardAtk * monsterTableData.multiAtk;
		_statusBase.valueList[(int)eActorStatus.AttackDelay] = monsterTableData.attackDelay;
		_statusBase.valueList[(int)eActorStatus.EvadeRate] = monsterTableData.evadeRate;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = monsterTableData.moveSpeed;

		//if (isServer)
		_statusBase._hp = GetValue(eActorStatus.MaxHP);

		OnChangedStatus();
	}

	void OnChangedStatus(eActorStatus eType = eActorStatus.ExAmount)
	{
		if (eType == eActorStatus.MoveSpeed || eType == eActorStatus.ExAmount)
			actor.baseCharacterController.speed = GetValue(eActorStatus.MoveSpeed);
		if (eType == eActorStatus.AttackSpeedAddRatio || eType == eActorStatus.ExAmount)
			actor.actionController.OnChangedAttackSpeedAddRatio(GetValue(eActorStatus.AttackSpeedAddRatio));
	}

	public float GetValue(eActorStatus eType)
	{
		if ((int)eType >= _statusBase.valueList.Length)
			return 0.0f;

		float value = _statusBase.valueList[(int)eType];
		switch (eType)
		{
			case eActorStatus.Attack:
				break;
			case eActorStatus.AttackDelay:
				value = value / (1.0f + GetValue(eActorStatus.AttackSpeedAddRatio));
				break;
		}
		return value;
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
		for (int i = 0; i < minLength; ++i)
			targetStatusBase.valueList[i] = GetValue((eActorStatus)i);

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

	public virtual void AddHP(float addHP)
	{
		_statusBase._hp += addHP;
		_statusBase._hp = Mathf.Clamp(_statusBase._hp, 0, GetValue(eActorStatus.MaxHP));

		bool onDie = false;
		if (_statusBase._hp <= 0)
		{
			// 애니 중에 죽으면 정말 이상하게 보이는 상황이 있다. 공중같이.
			// 이럴때를 대비해서 Die를 무시하고 hp를 1로 복구시켜주는 DontDie 애니 시그널을 추가해둔다. 불굴의 의지로 자세(애니)를 유지.
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.DontDie) == false)
			{
				_statusBase._hp = 0.0f;
				onDie = true;
			}
			else
			{
				_statusBase._hp = 1.0f;
			}
		}
		actor.OnChangedHP();
		if (onDie) actor.OnDie();
	}

	public float GetHPRatio()
	{
		return GetHP() / GetValue(eActorStatus.MaxHP);
	}


	public virtual void AddSP(float addSP)
	{
		_statusBase._sp += addSP;
		_statusBase._sp = Mathf.Clamp(_statusBase._sp, 0, GetValue(eActorStatus.MaxSP));
		actor.OnChangedSP();
		if (_statusBase._sp <= 0)
			_statusBase._sp = 0.0f;
	}

	public float GetSPRatio()
	{
		return GetSP() / GetValue(eActorStatus.MaxSP);
	}
}
