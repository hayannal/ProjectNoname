using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using ActorStatusDefine;

public class RoadSign : MonoBehaviour
{
	public string uiStringKey;

	bool _spawnedGatePillar = false;
	void OnEnable()
	{
		_indicatorShowRemainTime = 0.4f;

		// ClientSaveData에 새로운 타입을 추가해서 저장할까 하다가 게이트필라가 보여진 상태라면 동작하지 않으면 되는거라 게이트필라 여부를 가져다쓰기로 한다.
		if (ClientSaveData.instance.IsLoadingInProgressGame())
		{
			if (ClientSaveData.instance.GetCachedGatePillar())
				_spawnedGatePillar = true;
		}
	}

	void OnDisable()
	{
		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}
	}

	float _indicatorShowRemainTime;
	void Update()
	{
		if (_indicatorShowRemainTime > 0.0f)
		{
			_indicatorShowRemainTime -= Time.deltaTime;
			if (_indicatorShowRemainTime <= 0.0f)
			{
				_indicatorShowRemainTime = 0.0f;
				ShowIndicator();
			}
		}
	}

	ObjectIndicatorCanvas _objectIndicatorCanvas;
	void ShowIndicator()
	{
		AddressableAssetLoadManager.GetAddressableGameObject("MultipleLineIndicator", "Canvas", (prefab) =>
		{
			// 로딩하는 중간에 맵이동시 다음맵으로 넘어가서 인디케이터가 뜨는걸 방지.
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeSelf == false) return;

			_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;

			// 어차피 하나만 보일거라서 이렇게 instance로도 접근 가능.
			MultipleLineIndicatorCanvas.instance.contextText.SetLocalizedText(UIString.instance.GetString(_spawnedGatePillar ? "Tutorial_GatePillarMapClear" : uiStringKey));
		});
	}
}