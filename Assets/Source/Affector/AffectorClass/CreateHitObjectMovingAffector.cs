using UnityEngine;
using System.Collections;
using MecanimStateDefine;

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
			return;

		float deltaDistance = Vector3.Distance(_actor.cachedTransform.position, _lastPosition);
		_movedDistance += deltaDistance;
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

		HitObject hitObject = HitObject.InitializeHit(spawnTransform, _meHit, _actor, parentTransform, 0, 0, 0);
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