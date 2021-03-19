using UnityEngine;
using System.Collections;
using ActorStatusDefine;

public class CreateHitObjectAffector : AffectorBase
{
	HitObject _cachedMainHitObject;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;

		if (affectorValueLevelTableData.iValue1 == 0 && _actor.actorStatus.IsDie())
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

		Actor parentActor = _actor;
		// 특정 조건에서는 parentActor로 공격한 액터를 넘겨줘야한다.
		bool useAttackerParentActor = false;
		if (affectorValueLevelTableData.iValue2 == 1)
			useAttackerParentActor = true;
		if (useAttackerParentActor)
		{
			Actor findActor = BattleInstanceManager.instance.FindActorByInstanceId(hitParameter.statusStructForHitObject.actorInstanceId);
			if (findActor != null)
				parentActor = findActor;
		}

		// MeHitObject에서와는 달리 문제가 있는게 MeHitObject는 캐릭터의 시그널에 있다보니 각각의 시그널이 다 각자의 인스턴스를 가지지만
		// 여기 이 CreateHitObjectAffector는 AffectorProcessor당 한개만 만들어서 공용으로 쓰기때문에 _cachedMainHitObject를 공유하게 되버린다.
		// 그런데 이렇게 해도 큰 문제가 없는게
		// 캐릭하나에 여러개의 CreateHitObjectAffector를 연결해서 쓰지 않고 거의 대부분은 한개 쓰거나 안쓰기때문이라
		// 우선은 이런 형태로 가보기로 한다.
		if (info.meHit.aliveOnlyOne && _cachedMainHitObject != null && _cachedMainHitObject.gameObject.activeSelf)
		{
			// Area든 Collider든 아래 함수로 다 지워질거다.
			_cachedMainHitObject.FinalizeHitObject(true);
			_cachedMainHitObject = null;
		}

		HitObject hitObject = HitObject.InitializeHit(spawnTransform, info.meHit, parentActor, parentTransform, null, 0.0f, 0, 0, 0);
		if (hitObject != null)
		{
			// 이 OverrideSkillLevel 구조가 Collider 타입으로 히트오브젝트가 생성될땐 통하는데
			// lifeTime없는 Area. 즉 킵시리즈의 단타 필살기 같은데에선 레벨을 전달할 방법이 없어서 1렙짜리로 나가게 된다.
			// 이걸 수정하려면 위 HitObject.InitializeHit 함수에다가 전달을 해야하는데 그려려면 디폴트 인자가 또 하나 늘어나서 고민..
			// 우선은 이런 형태의 공격을 레벨팩 같은데서 사용할게 아니기 때문에
			// 인지만 하고 넘어가기로 한다.
			hitObject.OverrideSkillLevel(affectorValueLevelTableData.level);

			// 리코세를 시작부터 적용하는 옵션이다. 지금은 RpgKnight가 쓴다.
			// 이게 체크되어있지 않다면 날아가는 거부터 시작하는 일반적인 HitObject겠지만 이게 체크되어있다면 리코세 할게 있는지 판단 후 생성해야한다.
			// 하나 특이한 점은 맞는 몬스터가 생성한다는 점에서 TeamCheckFilter가 Ally로 되어있다는거다.
			if (affectorValueLevelTableData.iValue2 == 1 && info.meHit.ricochetCount > 0 && hitObject.hitObjectMovement != null)
			{
				Collider col = _actor.GetCollider();
				hitObject.hitObjectMovement.AddRicochet(col, true);
				hitObject.OnPostCollided(true, false, false, false, true, Vector3.forward, false);
			}

			if (info.meHit.aliveOnlyOne)
				_cachedMainHitObject = hitObject;
		}
	}
}