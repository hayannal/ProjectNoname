using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class ReflectDamageAffector : AffectorBase
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
	
	public static void OnHit(AffectorProcessor affectorProcessor, float damage)
	{
		if (affectorProcessor.actor == null)
			return;
		List<AffectorBase> listReflectDamageAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ReflectDamage);
		if (listReflectDamageAffector == null)
			return;

		float result = 0.0f;
		for (int i = 0; i < listReflectDamageAffector.Count; ++i)
		{
			if (listReflectDamageAffector[i].finalized)
				continue;
			ReflectDamageAffector reflectDamageAffector = listReflectDamageAffector[i] as ReflectDamageAffector;
			if (reflectDamageAffector == null)
				continue;
			result += reflectDamageAffector.value;
		}
		if (result == 0.0f)
			return;

		Actor actor = affectorProcessor.actor;
		if (actor.actorStatus.GetHP() <= 1.0f)
			return;

		float reflectDamage = damage * result;
		if (actor.actorStatus.GetHP() - reflectDamage < 1.0f)
			reflectDamage = actor.actorStatus.GetHP() - 1.0f;
		actor.actorStatus.AddHP(-reflectDamage);

#if UNITY_EDITOR
		//Debug.LogFormat("Current = {0} / Max = {1} / Damage = {2} / frameCount = {3}", actor.actorStatus.GetHP(), actor.actorStatus.GetValue(eActorStatus.MaxHp), reflectDamage, Time.frameCount);
#endif
	}
}