using UnityEngine;
using System.Collections;
using ActorStatusDefine;

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

		switch (affectorValueLevelTableData.iValue1)
		{
			case 0:
				_actor.GetRigidbody().AddForce(hitParameter.contactNormal * affectorValueLevelTableData.fValue1, ForceMode.Impulse);
				break;
			case 1:
				Actor attackerActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
				if (attackerActor != null)
				{
					Vector3 diff = _actor.cachedTransform.position - attackerActor.cachedTransform.position;
					_actor.GetRigidbody().AddForce(diff.normalized * affectorValueLevelTableData.fValue1, ForceMode.Impulse);
				}
				break;
			case 2:
				Vector3 force = Vector3.zero;
				force.x = affectorValueLevelTableData.fValue2;
				force.z = affectorValueLevelTableData.fValue3;
				_actor.GetRigidbody().AddRelativeForce(force, ForceMode.Impulse);
				break;
		}
	}
}