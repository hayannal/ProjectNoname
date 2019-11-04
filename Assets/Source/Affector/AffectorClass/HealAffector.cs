using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class HealAffector : AffectorBase
{
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
			return;

		float heal = 0.0f;
		if (affectorValueLevelTableData.fValue1 > 0.0f)
			heal += (_actor.actorStatus.GetValue(eActorStatus.MaxHp) * affectorValueLevelTableData.fValue1);
		if (affectorValueLevelTableData.fValue2 > 0.0f)
			heal += affectorValueLevelTableData.fValue2;

		_actor.actorStatus.AddHP(heal);
	}
}