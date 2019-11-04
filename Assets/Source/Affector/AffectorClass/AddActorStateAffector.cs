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

		_affectorProcessor.AddActorState(affectorValueLevelTableData.sValue1, hitParameter);
	}
}
