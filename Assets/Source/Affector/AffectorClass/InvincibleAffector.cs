using UnityEngine;
using System.Collections;

public class InvincibleAffector : AffectorBase
{
	float _endTime;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	void OnDamage()
	{

	}

	public static bool CheckInvincible(AffectorProcessor affectorProcessor)
	{
		InvincibleAffector invincibleAffector = (InvincibleAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.Invincible);
		if (invincibleAffector == null)
			return false;

		invincibleAffector.OnDamage();
		return true;
	}
}
