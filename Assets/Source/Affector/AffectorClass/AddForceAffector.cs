using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;
using MEC;

public class AddForceAffector : AffectorBase
{
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
			return;
		if (_actor.GetRigidbody() == null)
			return;

		if (affectorValueLevelTableData.iValue2 == 1)
			_actor.GetRigidbody().velocity = Vector3.zero;

		if (affectorValueLevelTableData.iValue3 == 1)
		{
			Timing.RunCoroutine(AddForceProcess(affectorValueLevelTableData.iValue1, CalculatorForce(affectorValueLevelTableData, hitParameter), affectorValueLevelTableData.fValue4));
			return;
		}

		ApplyForce(affectorValueLevelTableData.iValue1, CalculatorForce(affectorValueLevelTableData, hitParameter));
	}

	Vector3 CalculatorForce(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		Vector3 force = Vector3.zero;
		switch (affectorValueLevelTableData.iValue1)
		{
			case 0:
				force = - hitParameter.contactNormal * affectorValueLevelTableData.fValue1;
				break;
			case 1:
				Actor attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
				if (attackerActor != null)
				{
					Vector3 diff = _actor.cachedTransform.position - attackerActor.cachedTransform.position;
					force = diff.normalized * affectorValueLevelTableData.fValue1;
				}
				break;
			case 2:
				force.x = affectorValueLevelTableData.fValue2;
				force.z = affectorValueLevelTableData.fValue3;
				break;
		}
		return force;
	}

	void ApplyForce(int iValue1, Vector3 force)
	{
		switch (iValue1)
		{
			case 0:
				_actor.GetRigidbody().AddForce(force, ForceMode.Impulse);
				break;
			case 1:
				_actor.GetRigidbody().AddForce(force, ForceMode.Impulse);
				break;
			case 2:
				_actor.GetRigidbody().AddRelativeForce(force, ForceMode.Impulse);
				break;
		}
		//Debug.Log(Time.time);
	}



	const float Tick = 0.05f;
	IEnumerator<float> AddForceProcess(int iValue1, Vector3 force, float remainTime)
	{
		float tickRemainTime = 0.0f;
		while (true)
		{
			tickRemainTime -= Time.deltaTime;

			if (tickRemainTime <= 0.0f)
			{
				tickRemainTime += Tick;
				ApplyForce(iValue1, force);
				remainTime -= Tick;

				if (remainTime < Tick)
					break;
			}

			yield return Timing.WaitForOneFrame;
		}
	}
}