using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class CreateHitObjectAffector : AffectorBase
{
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
			return;

		GameObject meHitObjectInfoPrefab = FindPreloadObject(affectorValueLevelTableData.sValue1);
		if (meHitObjectInfoPrefab == null)
			return;

		MeHitObjectInfo info = meHitObjectInfoPrefab.GetComponent<MeHitObjectInfo>();
		if (info == null)
			return;

		Transform spawnTransform = _actor.cachedTransform;
		Transform parentTransform = _actor.cachedTransform;
		
		if (info.meHit.createPositionType == HitObject.eCreatePositionType.Bone && !string.IsNullOrEmpty(info.meHit.boneName))
		{
			Transform attachTransform = _actor.actionController.dummyFinder.FindTransform(info.meHit.boneName);
			if (attachTransform != null)
				spawnTransform = attachTransform;
		}

		HitObject.InitializeHit(spawnTransform, info.meHit, _actor, parentTransform, null, 0.0f, 0, 0, 0);
	}
}