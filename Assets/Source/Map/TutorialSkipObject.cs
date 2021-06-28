using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSkipObject : MonoBehaviour
{
	void OnEnable()
	{
		_indicatorShowRemainTime = 0.4f;
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
		AddressableAssetLoadManager.GetAddressableGameObject("TutorialSkipIndicator", "Canvas", (prefab) =>
		{
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeSelf == false) return;

			_objectIndicatorCanvas = UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;
		});
	}
}