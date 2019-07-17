using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class AddActorStateAffector : AffectorBase {

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		string actorStateId = affectorValueLevelTableData.sValue2;
		ActorStateTableData data = TableDataManager.instance.FindActorStateTableData(actorStateId);
		if (data == null)
			return;

		eAffectorType affectorType = (eAffectorType)data.continuousAffectorId;
		_affectorProcessor.AddActorState(actorStateId, affectorType, affectorValueLevelTableData, hitParameter);
	}
}
