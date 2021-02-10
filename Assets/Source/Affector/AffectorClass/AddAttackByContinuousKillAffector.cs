using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class AddAttackByContinuousKillAffector : AffectorBase
{
	float _endTime;
	float _value;
	float value { get { return _value; } }
	bool _allyTeam;
	bool allyTeam { get { return _allyTeam; } }

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			// something else? for breakable object
			return;
		}

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_value = affectorValueLevelTableData.fValue2;

		_allyTeam = (_actor.team.teamId == (int)Team.eTeamID.DefaultAlly);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	float GetAddAttack()
	{
		//Debug.LogFormat("allyContinuousKillCount : {0}", BattleInstanceManager.instance.allyContinuousKillCount);

		int count = 0;
		if (_allyTeam)
			count = BattleInstanceManager.instance.allyContinuousKillCount;
		else
			count = _continuousKillCount;
		return _value * count;
	}

	ObscuredInt _continuousKillCount = 0;
	void OnDamage()
	{
		if (_allyTeam)
			BattleInstanceManager.instance.allyContinuousKillCount = 0;
		else
			_continuousKillCount = 0;
	}

	void OnKill()
	{
		if (_allyTeam)
			BattleInstanceManager.instance.allyContinuousKillCount += 1;
		else
			_continuousKillCount += 1;
	}

	public static void OnKill(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddAttackByContinuousKillAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByContinuousKill);
		if (listAddAttackByContinuousKillAffector == null)
			return;

		for (int i = 0; i < listAddAttackByContinuousKillAffector.Count; ++i)
		{
			if (listAddAttackByContinuousKillAffector[i].finalized)
				continue;
			AddAttackByContinuousKillAffector addAttackByContinuousKillAffector = listAddAttackByContinuousKillAffector[i] as AddAttackByContinuousKillAffector;
			if (addAttackByContinuousKillAffector == null)
				continue;
			addAttackByContinuousKillAffector.OnKill();

			// 아군 팀의 어펙터는 내부변수를 공유해서 쓰니까 1회만 적용해야한다.
			if (addAttackByContinuousKillAffector.allyTeam)
				break;
		}
	}

	public static void OnDamage(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddAttackByContinuousKillAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByContinuousKill);
		if (listAddAttackByContinuousKillAffector == null)
			return;

		for (int i = 0; i < listAddAttackByContinuousKillAffector.Count; ++i)
		{
			if (listAddAttackByContinuousKillAffector[i].finalized)
				continue;
			AddAttackByContinuousKillAffector addAttackByContinuousKillAffector = listAddAttackByContinuousKillAffector[i] as AddAttackByContinuousKillAffector;
			if (addAttackByContinuousKillAffector == null)
				continue;
			addAttackByContinuousKillAffector.OnDamage();

			// 아군 팀의 어펙터는 내부변수를 공유해서 쓰니까 1회만 적용해야한다.
			if (addAttackByContinuousKillAffector.allyTeam)
				break;
		}
	}

	public static float GetValue(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listAddAttackByContinuousKillAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.AddAttackByContinuousKill);
		if (listAddAttackByContinuousKillAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listAddAttackByContinuousKillAffector.Count; ++i)
		{
			if (listAddAttackByContinuousKillAffector[i].finalized)
				continue;
			AddAttackByContinuousKillAffector addAttackByContinuousKillAffector = listAddAttackByContinuousKillAffector[i] as AddAttackByContinuousKillAffector;
			if (addAttackByContinuousKillAffector == null)
				continue;
			result += addAttackByContinuousKillAffector.GetAddAttack();
		}
		return result;
	}
}