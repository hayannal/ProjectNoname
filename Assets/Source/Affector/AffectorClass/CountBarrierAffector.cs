using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CountBarrierAffector : AffectorBase
{
	// 해당 어펙터를 쓰려면 액터에다가 본 추가로 넣고 액터 preload 오브젝트 리스트에다가 루프 이펙트나 피격 이벤트 이펙트를 넣어놔야한다.
	static string BONE_NAME = "Bone_CountBarrier_";

	Color _defaultBarrierColor = new Color(50.0f / 255.0f, 50.0f / 255.0f, 50.0f / 255.0f);
	Color _hitBarrierColor = new Color(320.0f / 255.0f, 100.0f / 255.0f, 100.0f / 255.0f);

	int _remainCount;
	float _endTime;
	Transform _boneTransform;
	Transform _loopEffectTransform;
	Material _loopEffectMaterial;
	GameObject _onBarrierEffectPrefab;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_remainCount = affectorValueLevelTableData.iValue2;
		
		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// attach bone
		bool useLoopEffect = !string.IsNullOrEmpty(affectorValueLevelTableData.sValue3);
		bool useOnBarrierEffect = !string.IsNullOrEmpty(affectorValueLevelTableData.sValue4);
		if (useLoopEffect || useOnBarrierEffect)
		{
			if (_actor != null)
				_boneTransform = _actor.actionController.dummyFinder.FindTransform(BONE_NAME);
			else
				_boneTransform = _affectorProcessor.cachedTransform;
		}

		// loop effect
		if (useLoopEffect)
		{
			GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (loopEffectPrefab != null)
			{
				_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _boneTransform).transform;
				_loopEffectTransform.localPosition = Vector3.zero;
				_loopEffectTransform.localRotation = Quaternion.identity;
				_loopEffectTransform.localScale = Vector3.one;
				Renderer loopEffectRenderer = _loopEffectTransform.GetComponentInChildren<Renderer>();
				if (loopEffectRenderer != null)
				{
					_loopEffectMaterial = loopEffectRenderer.material;
					_loopEffectMaterial.SetColor(BattleInstanceManager.instance.GetShaderPropertyId("_TintColor"), _defaultBarrierColor);
					_currentLoopEffectColor = _targetLoopEffectColor = _defaultBarrierColor;
				}
			}
		}

		// onBarrier effect
		if (useOnBarrierEffect)
		{
			_onBarrierEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);

			// 원래는 Preload에서 못찾으면 어드레서블 로더에서 받아오게 하려고 했다.
			// 그런데 생각보다 로딩 후 들고있어야하는 문제부터 어드레서블 해제하는거까지 은근 할게 많다.
			// 굳이 안전 처리를 위해 이런일을 하는건 낭비인거 같다.
			// 어차피 제대로 로딩하려면 (액티브 스킬에서 쓰는 이펙트들 컨트롤러에 연결시켜두는 것처럼) 패시브에 쓸 이펙트도 선별해서 넣어두는게 맞는거니
			// 안전처리는 빼기로 한다.
			//_handleOnBarrierEffectObjectPrefab = AddressableAssetLoadManager.GetAddressableAsset(affectorValueLevelTableData.sValue3);
		}
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
			return;

		UpdateLoopEffectColor();
	}

	Color _currentLoopEffectColor;
	Color _targetLoopEffectColor;
	float _barrierBlinkRemainTime = 0.0f;
	const float BarrierBlinkTime = 0.1f;
	bool _lastBarrierPingpongState = false;
	float _lastBarrierPingpongRemainTime = 0.0f;
	const float LastBarrierPingpongTime = 0.2f;
	void UpdateLoopEffectColor()
	{
		if (_loopEffectMaterial == null)
			return;

		float lerpPower = 2.0f;
		if (_barrierBlinkRemainTime > 0.0f)
		{
			lerpPower = 30.0f;
			_barrierBlinkRemainTime -= Time.deltaTime;
			if (_barrierBlinkRemainTime <= 0.0f)
			{
				_barrierBlinkRemainTime = 0.0f;
				_targetLoopEffectColor = _defaultBarrierColor;
				lerpPower = 2.0f;

				if (_remainCount == 1)
				{
					_lastBarrierPingpongState = false;
					_lastBarrierPingpongRemainTime = LastBarrierPingpongTime;
					_targetLoopEffectColor = Color.black;
				}
			}
		}

		if (_lastBarrierPingpongRemainTime > 0.0f)
		{
			lerpPower = 10.0f;
			_lastBarrierPingpongRemainTime -= Time.deltaTime;
			if (_lastBarrierPingpongRemainTime <= 0.0f)
			{
				_lastBarrierPingpongRemainTime += LastBarrierPingpongTime;
				_lastBarrierPingpongState ^= true;

				if (_lastBarrierPingpongState)
					_targetLoopEffectColor = _defaultBarrierColor;
				else
					_targetLoopEffectColor = Color.black;
			}
		}
		_currentLoopEffectColor = Color.Lerp(_currentLoopEffectColor, _targetLoopEffectColor, Time.deltaTime * lerpPower);
		_loopEffectMaterial.SetColor(BattleInstanceManager.instance.GetShaderPropertyId("_TintColor"), _currentLoopEffectColor);
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			_loopEffectTransform.gameObject.SetActive(false);
			_loopEffectTransform = null;
		}
	}

	void OnBarrier(HitParameter hitParameter)
	{
		_remainCount -= 1;

		if (_loopEffectMaterial != null)
		{
			_targetLoopEffectColor = _hitBarrierColor;
			_barrierBlinkRemainTime = BarrierBlinkTime;
		}

		if (_onBarrierEffectPrefab != null)
		{
			Transform effectTransform = BattleInstanceManager.instance.GetCachedObject(_onBarrierEffectPrefab, hitParameter.contactPoint, Quaternion.LookRotation(hitParameter.contactNormal)).transform;
			effectTransform.parent = _boneTransform;
		}
		
		if (_remainCount == 0)
			finalized = true;
	}

	public static bool CheckBarrier(AffectorProcessor affectorProcessor, HitParameter hitParameter)
	{
		CountBarrierAffector countBarrierAffector = (CountBarrierAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.CountBarrier);
		if (countBarrierAffector == null)
			return false;

		countBarrierAffector.OnBarrier(hitParameter);
		return true;
	}
}