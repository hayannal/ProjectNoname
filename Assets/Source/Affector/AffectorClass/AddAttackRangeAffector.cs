using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class AddAttackRangeAffector : AffectorBase
{
	float _endTime;
	GameObject _startEffectPrefab;
	GameObject _startEffectObject;

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

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue4) == false)
		{
			_startEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);

			if (_startEffectPrefab != null)
				_startEffectObject = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
		}

		// 매프레임 Get할때마다 어펙터 검색하는게 느릴거 같아서 차라리 캐싱하는 쪽에다가 덧셈할 값을 알려주기로 한다.
		if (_actor.IsPlayerActor())
		{
			PlayerActor playerActor = _actor as PlayerActor;
			if (playerActor != null)
				playerActor.playerAI.addAttackRange = affectorValueLevelTableData.fValue2;

			LocalPlayerController localPlayerController = BattleInstanceManager.instance.playerActor.baseCharacterController as LocalPlayerController;
			if (localPlayerController != null)
				localPlayerController.dontMove = true;
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		if (_startEffectObject != null)
			_startEffectObject.SetActive(false);

		if (_startEffectPrefab != null)
			_startEffectObject = BattleInstanceManager.instance.GetCachedObject(_startEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;
	}

	public override void FinalizeAffector()
	{
		if (_actor.IsPlayerActor())
		{
			PlayerActor playerActor = _actor as PlayerActor;
			if (playerActor != null)
				playerActor.playerAI.addAttackRange = 0.0f;
		}
	}
}