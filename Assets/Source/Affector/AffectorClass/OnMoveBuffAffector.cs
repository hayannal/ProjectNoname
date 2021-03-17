using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MecanimStateDefine;

public class OnMoveBuffAffector : AffectorBase
{
	float _endTime;
	bool _applied;
	GameObject _onStartEffectPrefab;
	Transform _onOffLoopEffectTransform;
	List<ParticleSystem> _listOnOffLoopParticleSystem;
	Transform _loopEffectTransform;

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
		
		// sValue4 이펙트는 필수
		GameObject onOffLoopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
		_onOffLoopEffectTransform = BattleInstanceManager.instance.GetCachedObject(onOffLoopEffectPrefab, _actor.cachedTransform.position, Quaternion.identity).transform;
		ParticleSystem[] particleSystems = _onOffLoopEffectTransform.GetComponentsInChildren<ParticleSystem>();
		_listOnOffLoopParticleSystem = new List<ParticleSystem>();
		for (int i = 0; i < particleSystems.Length; ++i)
		{
			// 애초에 꺼있는건 리스트에서 포함시키지 않는다.
			if (particleSystems[i].emission.enabled == false)
				continue;

			_listOnOffLoopParticleSystem.Add(particleSystems[i]);
		}
		LoopEffectOnOff(false);
		FollowTransform.Follow(_onOffLoopEffectTransform, _actor.cachedTransform, Vector3.zero);

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue3) == false)
		{
			GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (loopEffectPrefab != null)
			{
				_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform.position, Quaternion.identity).transform;
				FollowTransform.Follow(_loopEffectTransform, _actor.cachedTransform, Vector3.zero);
			}
		}

		if (string.IsNullOrEmpty(affectorValueLevelTableData.sValue2) == false)
			_onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue2);
	}

	void LoopEffectOnOff(bool enabled)
	{
		if (_listOnOffLoopParticleSystem == null)
			return;

		for (int i = 0; i < _listOnOffLoopParticleSystem.Count; ++i)
		{
			ParticleSystem.EmissionModule emissionModule = _listOnOffLoopParticleSystem[i].emission;
			emissionModule.enabled = enabled;
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		if (_applied)
		{
			if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			{
				LoopEffectOnOff(false);
				_applied = false;
			}
		}
		else
		{
			if (_actor.actionController.mecanimState.IsState((int)eMecanimState.Move))
			{
				if (_onStartEffectPrefab != null)
					BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);
				LoopEffectOnOff(true);
				_applied = true;
			}
		}
	}

	public override void FinalizeAffector()
	{
		// 끌때는 재활용을 위해 복구해놓기로 한다.
		if (_applied)
		{
			LoopEffectOnOff(true);
			_applied = false;
		}

		if (_onOffLoopEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_onOffLoopEffectTransform);
			_onOffLoopEffectTransform = null;
		}

		if (_loopEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_loopEffectTransform);
			_loopEffectTransform = null;
		}
	}
	

	
	public static float GetEvadeAddRate(AffectorProcessor affectorProcessor)
	{
		// PositionBuff때처럼 동시에 하나만 존재하지 않을까.
		OnMoveBuffAffector onMoveBuffAffector = (OnMoveBuffAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.OnMoveBuff);
		if (onMoveBuffAffector == null)
			return 0.0f;
		if (affectorProcessor.actor.actionController.mecanimState.IsState((int)eMecanimState.Move) == false)
			return 0.0f;
		return onMoveBuffAffector._affectorValueLevelTableData.fValue4;
	}
}