using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class AddActorStateAffector : AffectorBase {

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			// something else? for breakable object
			return;
		}

		string actorStateId = affectorValueLevelTableData.sValue2;
		ActorStateTableData data = TableDataManager.instance.FindActorStateTableData(actorStateId);
		if (data == null)
			return;

		_affectorProcessor.actorStateProcessor.AddActorState(actorStateId, data.continuousAffectorValueId, hitParameter);
	}
}
