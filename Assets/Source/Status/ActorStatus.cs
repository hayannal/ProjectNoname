using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Networking;
using ActorStatusDefine;

public class ActorStatus : MonoBehaviour
{
	// 네트워크가 알아서 맞춰주는 SyncVar같은거로 동기화 하기엔 너무 위험하다. 타이밍을 재기 위해 더이상 이런식으로 하진 않는다.
	//[SyncVar(hook = "OnChangeHp")]
	//float _hp;

	StatusBase _statusBase;
	public StatusBase statusBase { get { return _statusBase; } }

	public void InitializeActorStatus(int actorId)
	{
		_statusBase = new ActorStatusList();

		//string key = string.Format("id{0}", actorID);
		//m_ActorStatus.InitializeByTable(key);
		_statusBase.valueList[(int)eActorStatus.Attack] = 10.0f;
		_statusBase.valueList[(int)eActorStatus.MaxHP] = 100.0f;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = 3.0f;

		//if (isServer)
		_statusBase._hp = GetValue(eActorStatus.MaxHP);
	}

	public void InitializeMonsterStatus(int monsterActorId)
	{
		_statusBase = new MonsterStatusList();

		//string key = string.Format("id{0}", actorID);
		//m_ActorStatus.InitializeByTable(key);
		_statusBase.valueList[(int)eActorStatus.Attack] = 10.0f;
		_statusBase.valueList[(int)eActorStatus.MaxHP] = 100.0f;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = 3.0f;

		//if (isServer)
		_statusBase._hp = GetValue(eActorStatus.MaxHP);
	}

	public float GetValue(eActorStatus eType)
	{
		return _statusBase.valueList[(int)eType];
	}

	public float GetHP()
	{
		return _statusBase._hp;
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

	public virtual void AddHP(float addHP)
	{
		m_ActorStatus.HP += addHP;
		m_ActorStatus.HP = Mathf.Clamp(m_ActorStatus.HP, 0, GetStatus(BaseStatus.eStatus.MaxHP));
		if (m_ActorStatus.HP == 0)
			OnDie();
	}

	public float GetHPRatio()
	{
		return GetHP() / GetStatus(BaseStatus.eStatus.MaxHP);
	}
	*/
}
