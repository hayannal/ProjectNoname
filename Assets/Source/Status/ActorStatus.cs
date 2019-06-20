using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Networking;
using ActorStatusDefine;

public class ActorStatus : MonoBehaviour
{
	//[SyncVar(hook = "OnChangeHp")]
	float _hp;

	ActorStatusList _actorStatus = new ActorStatusList();
	public StatusBase statusBase { get { return _actorStatus; } }

	public void InitializeActorStatus(int actorID)
	{
		//string key = string.Format("id{0}", actorID);
		//m_ActorStatus.InitializeByTable(key);
		_actorStatus.valueList[(int)eActorStatus.Attack] = 10.0f;
		_actorStatus.valueList[(int)eActorStatus.MaxHP] = 100.0f;
		_actorStatus.valueList[(int)eActorStatus.MoveSpeed] = 3.0f;

		//if (isServer)
			_hp = GetValue(eActorStatus.MaxHP);
	}
	
	public float GetValue(eActorStatus eType)
	{
		return _actorStatus.valueList[(int)eType];
	}

	public float GetHP()
	{
		return _hp;
	}

	void OnChangeHp(float hp)
	{
		Debug.Log("OnChange HP : " + hp.ToString());
	}


	#region For HitObject
	public void CopyStatusBase(ref StatusBase targetStatusBase)
	{
		//if (targetStatusBase == null) targetStatusBase = new StatusBase();
		for (int i = 0; i < (int)eActorStatus.BaseAmount; ++i)
			targetStatusBase.valueList[i] = GetValue((eActorStatus)i);
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
