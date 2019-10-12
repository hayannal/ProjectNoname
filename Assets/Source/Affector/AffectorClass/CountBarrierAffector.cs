using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CountBarrierAffector : AffectorBase
{
	int _remainCount;
	float _endTime;
	Transform _loopEffectTransform;
	GameObject _onBarrierEffectPrefab;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_remainCount = affectorValueLevelTableData.iValue2;
		
		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		// loop effect
		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue3))
		{
			GameObject loopEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue3);
			if (loopEffectPrefab != null)
				_loopEffectTransform = BattleInstanceManager.instance.GetCachedObject(loopEffectPrefab, _actor.cachedTransform).transform;
		}

		// onBarrier effect
		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
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
	}

	public override void FinalizeAffector()
	{
		if (_loopEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_loopEffectTransform);
			_loopEffectTransform = null;
		}
	}

	void OnBarrier()
	{
		if (_onBarrierEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_onBarrierEffectPrefab, _actor.cachedTransform.position, Quaternion.identity);

		_remainCount -= 1;
		if (_remainCount == 0)
			finalized = true;
	}

	public static bool CheckBarrier(AffectorProcessor affectorProcessor)
	{
		CountBarrierAffector countBarrierAffector = (CountBarrierAffector)affectorProcessor.GetFirstContinuousAffector(eAffectorType.CountBarrier);
		if (countBarrierAffector == null)
			return false;

		countBarrierAffector.OnBarrier();
		return true;
	}
}