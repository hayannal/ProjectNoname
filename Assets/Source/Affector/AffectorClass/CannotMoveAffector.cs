using UnityEngine;
using System.Collections;

public class CannotMoveAffector : AffectorBase
{
	// 해당 어펙터를 쓰려면 액터에다가 본 추가로 넣고 액터 preload 오브젝트 리스트에다가 루프 이펙트나 피격 이벤트 이펙트를 넣어놔야한다.
	static string BONE_NAME = "Bone_CannotMove_";

	float _endTime;
	Transform _boneTransform;
	Transform _loopEffectTransform;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// attach bone
		if (_actor != null)
			_boneTransform = _actor.actionController.dummyFinder.FindTransform(BONE_NAME);
		else
			_boneTransform = _affectorProcessor.cachedTransform;

		// loop effect
		GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
		if (loopEffectPrefab != null)
		{
			_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _boneTransform).transform;
			_loopEffectTransform.localPosition = Vector3.zero;
			_loopEffectTransform.localRotation = Quaternion.identity;
			_loopEffectTransform.localScale = Vector3.one;
		}

		_actor.actorStatus.OnChangedStatus(ActorStatusDefine.eActorStatus.MoveSpeed);
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
		{
			_actor.actorStatus.OnChangedStatus(ActorStatusDefine.eActorStatus.MoveSpeed);
			return;
		}
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_loopEffectTransform);
			//_loopEffectTransform.gameObject.SetActive(false);
			_loopEffectTransform = null;
		}
	}
}