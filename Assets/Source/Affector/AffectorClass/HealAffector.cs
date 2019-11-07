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
		if (affectorValueLevelTableData.fValue3 > 0.0f)
			heal += (_actor.actorStatus.GetValue(eActorStatus.MaxHp) * affectorValueLevelTableData.fValue3);
		if (affectorValueLevelTableData.fValue4 > 0.0f)
			heal += (hitParameter.statusStructForHitObject.damage * affectorValueLevelTableData.fValue4);

		_actor.actorStatus.AddHP(heal);
	}
}