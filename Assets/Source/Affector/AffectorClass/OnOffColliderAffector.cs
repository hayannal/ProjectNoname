﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OnOffColliderAffector : AffectorBase
{
	float _endTime;
	Transform _loopEffectTransform;
	bool _changedBlackColor;
	GameObject _onLightEffectPrefab;

	// 장치로 인한 해제 상태인지 나타내는 변수
	float _lightAppliedRemainTime;

	AffectorValueLevelTableData _affectorValueLevelTableData;
	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}
		_affectorValueLevelTableData = affectorValueLevelTableData;

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// 소환용 몹으로 쓰게되면서 장치를 활성화한 상태에서 몹이 생성되는 경우가 생기게 되었다.
		// 그래서 혹시 활성화 중이라면 유령화가 적용되지 않은채 쿨타임만 적용해서 시작하면 된다.
		bool ignoreApply = false;
		if (OnOffColliderArea.instance != null && OnOffColliderArea.instance.gameObject.activeSelf)
		{
			if (OnOffColliderArea.instance.activeRemainTime > 0.0f)
			{
				_lightAppliedRemainTime = OnOffColliderArea.instance.activeRemainTime;
				ignoreApply = true;
			}
		}

		if (ignoreApply == false)
			ApplyAffector(affectorValueLevelTableData);

#if UNITY_EDITOR
		_affectorProcessor.dontClearOnDisable = true;
#endif
	}

	// 재사용때문에 함수로 빼둔다.
	void ApplyAffector(AffectorValueLevelTableData affectorValueLevelTableData)
	{
		// effect
		bool useLoopEffect = !string.IsNullOrEmpty(affectorValueLevelTableData.sValue3);
		bool useOnLightEffect = !string.IsNullOrEmpty(affectorValueLevelTableData.sValue4);
		bool changeBlackColor = affectorValueLevelTableData.iValue1 == 1;

		// loop effect
		if (useLoopEffect)
		{
			GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (loopEffectPrefab != null)
			{
				_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, null).transform;
				_loopEffectTransform.localPosition = Vector3.zero;
				_loopEffectTransform.localRotation = Quaternion.identity;
				_loopEffectTransform.localScale = Vector3.one;
				FollowTransform.Follow(_loopEffectTransform, _affectorProcessor.cachedTransform, Vector3.zero);
			}
		}

		// onLight effect
		if (useOnLightEffect && _onLightEffectPrefab == null)
			_onLightEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);

		if (changeBlackColor)
		{
			// 검은 컬러로 바꿔주는건 몬스터에 한해서 처리한다.
			// 몬스터는 항상 DiffuseNormalRim으로 되어있기 때문에 담당 클래스에게 요청하는 형태다.
			ChangeMainColor.ChangeColor(_affectorProcessor.cachedTransform, Color.black);
			_changedBlackColor = true;
		}

		Collider collider = _actor.GetCollider();
		if (collider != null)
			collider.enabled = false;
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// 쓴 상태에서 또 쓸일은 없겠지만 혹시 시간 버프로 만들어놓고 쓸까봐 처리는 해둔다.
		// 대신 상황에 따라 다른게
		if (_lightAppliedRemainTime > 0.0f)
		{
			// 장치로 인해 해제된 상태라면 다시 덮어쓰게 해야하지 않나
			ApplyAffector(affectorValueLevelTableData);
		}
		else
		{
			// 이미 유령화 중에 또 쓴거다.
			// 별다른 처리가 필요하지 않다.
		}
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		UpdateOnLight();
	}

	void UpdateOnLight()
	{
		// 유령화 해제 상태 처리
		if (_lightAppliedRemainTime > 0.0f)
		{
			_lightAppliedRemainTime -= Time.deltaTime;
			if (_lightAppliedRemainTime <= 0.0f)
			{
				_lightAppliedRemainTime = 0.0f;

				// 다시 유령화를 쓰는거처럼 해야한다.
				ApplyAffector(_affectorValueLevelTableData);
			}
		}
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_loopEffectTransform);
			_loopEffectTransform = null;
		}

		_lightAppliedRemainTime = 0.0f;

		// 이미 죽은 상태라면 collider를 복구할 필요는 없다.
		if (_actor.actorStatus.IsDie())
			return;

		Collider collider = _actor.GetCollider();
		if (collider != null)
			collider.enabled = true;

		// 죽어서 풀릴때는 리셋하지 않는다.
		if (_changedBlackColor)
		{
			ChangeMainColor.ResetColor(_affectorProcessor.cachedTransform);
			_changedBlackColor = false;
		}
	}

#if UNITY_EDITOR
	public override void DisableAffector()
	{
		// 배틀씬에서 스폰 플래그를 꺼내서 테스트할때 문제가 발생하는건 배리어때랑 똑같아서 비슷한 구조로 처리해둔다.
		FinalizeAffector();
	}
#endif

	void OnLight(float duration)
	{
		// 장치를 작동시키면 유령화 해둔 몬스터들의 버프가 풀려야한다.
		if (_lightAppliedRemainTime > 0.0f)
		{
			// 그런데 이미 풀려있는 상태라면 시간만 갱신하면 될거같다.
			_lightAppliedRemainTime = duration;
		}
		else
		{
			// 어펙터를 삭제하는건 아니지만 없어진거처럼 처리.
			FinalizeAffector();

			if (_onLightEffectPrefab != null)
			{
				Transform onLightEffectTransform = BattleInstanceManager.instance.GetCachedObject(_onLightEffectPrefab, _affectorProcessor.cachedTransform.position + Vector3.up, Quaternion.identity).transform;
				FollowTransform.Follow(onLightEffectTransform, _affectorProcessor.cachedTransform, Vector3.up);
			}

			_lightAppliedRemainTime = duration;
		}
	}

	void CheckDie()
	{
		// 죽는 순간엔 강제로 해제
		finalized = true;
	}

	bool IsActive()
	{
		// 컬리더가 꺼있다면 유령상태일거다.
		Collider collider = _actor.GetCollider();
		if (collider == null)
			return false;
		return (collider.enabled == false);
	}

	public static bool OnLight(AffectorProcessor affectorProcessor, float duration)
	{
		OnOffColliderAffector onOffColliderAffector = (OnOffColliderAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.OnOffCollider);
		if (onOffColliderAffector == null)
			return false;

		onOffColliderAffector.OnLight(duration);
		return true;
	}

	public static void CheckDie(AffectorProcessor affectorProcessor)
	{
		OnOffColliderAffector onOffColliderAffector = (OnOffColliderAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.OnOffCollider);
		if (onOffColliderAffector == null)
			return;

		onOffColliderAffector.CheckDie();
	}

	public static bool IsActive(AffectorProcessor affectorProcessor)
	{
		OnOffColliderAffector onOffColliderAffector = (OnOffColliderAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.OnOffCollider);
		if (onOffColliderAffector == null)
			return false;

		return onOffColliderAffector.IsActive();
	}
}