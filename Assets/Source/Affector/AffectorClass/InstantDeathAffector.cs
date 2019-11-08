using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class InstantDeathAffector : AffectorBase
{
	float _endTime;
	float _value;
	float value { get { return _value; } }

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
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public static bool CheckInstantDeath(AffectorProcessor affectorProcessor, Actor defenderActor)
	{
		List<AffectorBase> listInstantDeathAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.InstantDeath);
		if (listInstantDeathAffector == null)
			return false;

		float result = 0.0f;
		for (int i = 0; i < listInstantDeathAffector.Count; ++i)
		{
			if (listInstantDeathAffector[i].finalized)
				continue;
			InstantDeathAffector instantDeathAffector = listInstantDeathAffector[i] as InstantDeathAffector;
			if (instantDeathAffector == null)
				continue;
			result += instantDeathAffector.value;
		}
		if (result == 0.0f)
			return false;

		if (defenderActor is MonsterActor)
		{
			MonsterActor monsterActor = defenderActor as MonsterActor;
			if (monsterActor != null && monsterActor.bossMonster)
				return false;
		}

		if (defenderActor.actionController.mecanimState.IsState((int)eMecanimState.DontDie))
			return false;

		// 로직 순서상 여기서 즉사 결정이 되더라도
		// ImmortalWillAffector에서 안 죽을 수도 있는건데
		// 몬스터는 ImmortalWillAffector를 가지지 않는다는 점을 이용해서 체크하지 않도록 한다.
		//if (ImmortalWillAffector.CheckImmortal(defenderActor.affectorProcessor))

		return (Random.value <= result);
	}
}