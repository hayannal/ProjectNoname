using UnityEngine;
using System.Collections;
using MecanimStateDefine;
using ActorStatusDefine;

public class CreateHitObjectMovingAffector : AffectorBase
{
	float _endTime;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_lastPosition = _actor.cachedTransform.position;
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		UpdateMovedDistance();
	}

	float _movedDistance = 0.0f;
	Vector3 _lastPosition;
	void UpdateMovedDistance()
	{
		if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
		{
			_lastPosition = _actor.cachedTransform.position;
			return;
		}

		float deltaDistance = Vector3.Distance(_actor.cachedTransform.position, _lastPosition);
		// 게이트 필라 이동전후에 플래그를 넣으려고 했는데 포탈부터 내 위치가 이동될 경우가 많다.
		// 예외코드가 여러곳에 들어갈바엔 차라리 프레임 당 최대 이동량을 정해두고 이 값을 넘지 못하게 해본다.
		_movedDistance += Mathf.Min(deltaDistance, _actor.baseCharacterController.speed * Time.deltaTime);
		_lastPosition = _actor.cachedTransform.position;

		if (_movedDistance > _affectorValueLevelTableData.fValue2)
		{
			_movedDistance -= _affectorValueLevelTableData.fValue2;
			CreateHitObject();
		}
	}

	void CreateHitObject()
	{
		if (_meHit == null)
			_meHit = GetMeHitObject();
		if (_meHit == null)
			return;

		Transform spawnTransform = _actor.cachedTransform;
		Transform parentTransform = _actor.cachedTransform;

		if (_meHit.createPositionType == HitObject.eCreatePositionType.Bone && !string.IsNullOrEmpty(_meHit.boneName))
		{
			Transform attachTransform = _actor.actionController.dummyFinder.FindTransform(_meHit.boneName);
			if (attachTransform != null)
				spawnTransform = attachTransform;
		}

		HitObject hitObject = HitObject.InitializeHit(spawnTransform, _meHit, _actor, parentTransform, null, 0.0f, 0, 0, 0);
		hitObject.OverrideSkillLevel(_affectorValueLevelTableData.level);
		BattleInstanceManager.instance.AddHitObjectMoving(hitObject);
	}

	MeHitObject _meHit;
	MeHitObject GetMeHitObject()
	{
		if (_meHit != null)
			return _meHit;

		GameObject meHitObjectInfoPrefab = FindPreloadObject(_affectorValueLevelTableData.sValue1);
		if (meHitObjectInfoPrefab == null)
			return null;

		MeHitObjectInfo info = meHitObjectInfoPrefab.GetComponent<MeHitObjectInfo>();
		if (info == null)
			return null;

		_meHit = info.meHit;
		return _meHit;
	}
}