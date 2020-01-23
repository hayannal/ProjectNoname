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

		HitObject hitObject = HitObject.InitializeHit(spawnTransform, info.meHit, _actor, parentTransform, null, 0.0f, 0, 0, 0);
		if (hitObject != null)
		{
			// 이 OverrideSkillLevel 구조가 Collider 타입으로 히트오브젝트가 생성될땐 통하는데
			// lifeTime없는 Area. 즉 킵시리즈의 단타 필살기 같은데에선 레벨을 전달할 방법이 없어서 1렙짜리로 나가게 된다.
			// 이걸 수정하려면 위 HitObject.InitializeHit 함수에다가 전달을 해야하는데 그려려면 디폴트 인자가 또 하나 늘어나서 고민..
			// 우선은 이런 형태의 공격을 레벨팩 같은데서 사용할게 아니기 때문에
			// 인지만 하고 넘어가기로 한다.
			hitObject.OverrideSkillLevel(affectorValueLevelTableData.level);
		}
	}
}