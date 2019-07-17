using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class AddActorStateAffector : AffectorBase {

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		ActorStateTableData data = TableDataManager.instance.FindActorStateTableData(affectorValueLevelTableData.sValue2);
		if (data == null)
			return;

		eAffectorType affectorType = (eAffectorType)data.continuousAffectorId;
		_affectorProcessor.ExcuteAffector(affectorType, affectorValueLevelTableData, hitParameter);
	}
}
