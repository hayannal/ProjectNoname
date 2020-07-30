using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;
using ActorStatusDefine;
using DG.Tweening;

public class AttackOnMovingAffector : AffectorBase
{
	float _endTime;
	Transform _loopEffectTransform;
	List<Transform> _listLoopEffectTransform;
	GameObject _muzzlePrefab;
	PlayerAI _playerAI;
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

		if (_actor.IsPlayerActor())
		{
			PlayerActor playerActor = _actor as PlayerActor;
			if (playerActor != null)
				_playerAI = playerActor.playerAI;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// muzzle effect
		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue2) == false)
			_muzzlePrefab = FindPreloadObject(affectorValueLevelTableData.sValue2);

		// loop effect
		GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
		if (loopEffectPrefab != null)
		{
			if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
			{
				_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation).transform;
				_loopEffectTransform.localScale = Vector3.zero;
			}
			else
			{
				if (_listLoopEffectTransform == null)
					_listLoopEffectTransform = new List<Transform>();
				string[] boneNameList = BattleInstanceManager.instance.GetCachedString2StringList(affectorValueLevelTableData.sValue4);
				for (int i = 0; i < boneNameList.Length; ++i)
				{
					Transform attachTransform = _actor.actionController.dummyFinder.FindTransform(boneNameList[i]);
					Transform loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, attachTransform).transform;
					loopEffectTransform.localScale = Vector3.zero;
					_listLoopEffectTransform.Add(loopEffectTransform);
				}
			}
		}
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		UpdateAttackOnMoving();
		UpdateSignalDelay();
		UpdateLoopEffect();
	}

	Cooltime _normalAttackCooltime;
	void UpdateAttackOnMoving()
	{
		if (_actor.actorStatus.IsDie())
			return;

		if (_normalAttackCooltime == null)
			_normalAttackCooltime = _actor.cooltimeProcessor.GetCooltime(PlayerAI.NormalAttackName);

		// Attack Delay
		// 평타에 어택 딜레이가 쿨타임으로 적용되어있기 때문에 이걸 얻어와서 쓴다.
		// 참고로 스턴중에도 어택 딜레이는 줄어들게 되어있다.
		if (_normalAttackCooltime != null && _normalAttackCooltime.CheckCooltime())
			return;

		if (_actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction) || _actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove))
			return;

		if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			return;

		// 사거리와 네비는 AI를 수행하는 주체에게 물어보는게 제일 정확하다.
		// PlayerAI 혹은 MonsterAI 에게 물어봐야하는데 MonsterAI쪽에는 사거리 같은 개념이 없으니 우선은 PlayerAI만 처리해두기로 한다.
		if (_playerAI != null)
		{
			Vector3 diff = Vector3.zero;
			if (_playerAI.IsTargetColliderInAttackRange(ref diff) == false)
				return;
		}

		if (_affectorValueLevelTableData.fValue2 == 0.0f)
			CreateHitObject();
		else
			_signalDelayRemainTime = GetSignalDelayTime();

		_actor.cooltimeProcessor.ApplyCooltime(PlayerAI.NormalAttackName, _actor.actorStatus.GetValue(eActorStatus.AttackDelay));
		_normalAttackCooltime = _actor.cooltimeProcessor.GetCooltime(PlayerAI.NormalAttackName);
	}

	float _signalDelayRemainTime;
	float GetSignalDelayTime()
	{
		// 공속에 따라 딜레이는 애니메이션 속도 변하듯 변해야한다. ActionController의 OnChangedAttackSpeedAddRatio에서 가져왔다.
		float remainTime = _affectorValueLevelTableData.fValue2;
		float attackSpeedAddRate = _actor.actorStatus.GetValue(eActorStatus.AttackSpeedAddRate);
		return (remainTime / (1.0f + attackSpeedAddRate * 0.3333f));
	}
	void UpdateSignalDelay()
	{
		// Attack 액션으로 공격할때는 애니메이션의 시작 부분에서 쿨타임 딜레이를 걸어두고 시그널 발동때 히트 오브젝트를 만드는데
		// 이렇게 무빙중에 공격할때 비슷한 템포에 맞춰서 공격하려면 쿨타임이 끝나자마자가 히트 오브젝트를 만들면 안된다.
		// Attack 액션이 시작되고나서 Attack Signal이 발동될때 정도의 시간(대략 평균적으로 0.3초)을 기다렸다가 만들어야
		// 이상하게 빨리 쏘는 느낌을 안받을 수 있게 된다.
		// 이걸 위해 저 대기시간을 fValue2에다 넣어두고 이 시간이 지나면 발사되게 해준다.
		if (_signalDelayRemainTime > 0.0f)
		{
			// 평타 캔슬과 마찬가지로 이게 걸려있는 도중에 이동을 하지 않으면
			// 대기타던거를 취소하고 쿨타임도 초기화 시켜야한다.
			if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			{
				if (_normalAttackCooltime != null && _normalAttackCooltime.CheckCooltime())
					_normalAttackCooltime.cooltime = 0.0f;
				_signalDelayRemainTime = 0.0f;
				return;
			}

			_signalDelayRemainTime -= Time.deltaTime;
			if (_signalDelayRemainTime <= 0.0f)
			{
				_signalDelayRemainTime = 0.0f;
				CreateHitObject();
			}
		}
	}

	void UpdateLoopEffect()
	{
		float targetScale = _actor.actionController.mecanimState.IsState((int)eMecanimState.Move) ? 1.0f : 0.0f;
		if (targetScale == 1.0f && _playerAI.targetCollider == null) targetScale = 0.0f;
		if (_loopEffectTransform != null)
		{
			if (_loopEffectTransform.localScale.x != targetScale)
			{
				_loopEffectTransform.localScale = Vector3.Lerp(_loopEffectTransform.localScale, new Vector3(targetScale, targetScale, targetScale), Time.deltaTime * 5.0f);
				if (Mathf.Abs(_loopEffectTransform.localScale.x - targetScale) < 0.01f)
					_loopEffectTransform.localScale = new Vector3(targetScale, targetScale, targetScale);
			}
		}
		if (_listLoopEffectTransform != null)
		{
			for (int i = 0; i < _listLoopEffectTransform.Count; ++i)
			{
				Transform loopEffectTransform = _listLoopEffectTransform[i];
				if (loopEffectTransform.localScale.x != targetScale)
				{
					loopEffectTransform.localScale = Vector3.Lerp(loopEffectTransform.localScale, new Vector3(targetScale, targetScale, targetScale), Time.deltaTime * 5.0f);
					if (Mathf.Abs(loopEffectTransform.localScale.x - targetScale) < 0.01f)
						loopEffectTransform.localScale = new Vector3(targetScale, targetScale, targetScale);
				}
			}
		}
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			_loopEffectTransform.gameObject.SetActive(false);
			_loopEffectTransform = null;
		}
		if (_listLoopEffectTransform != null)
		{
			for (int i = 0; i < _listLoopEffectTransform.Count; ++i)
				_listLoopEffectTransform[i].gameObject.SetActive(false);
			_listLoopEffectTransform.Clear();
		}
	}

	// 교체시엔 어차피 이동중이 아닐 가능성이 높으니 굳이 끌 필요도 없을거 같다.
	//public override void DisableAffector()
	//{
	//	// 플레이어가 죽지도 않았는데 Disable시킬 일은 없을거 같지만
	//	// 플레이어한테 걸릴걸 대비해서 미리 해둔다.
	//	FinalizeAffector();
	//}




	void CreateHitObject()
	{
		if (_meHit == null)
			_meHit = GetMeHitObject();
		if (_meHit == null)
			return;

		Transform spawnTransform = _actor.cachedTransform;
		Transform parentTransform = _actor.cachedTransform;

		if (_muzzlePrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_muzzlePrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation);

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