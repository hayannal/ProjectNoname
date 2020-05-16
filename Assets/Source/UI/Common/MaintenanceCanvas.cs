using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class MaintenanceCanvas : MonoBehaviour
{
	static MaintenanceCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.maintenanceCanvasPrefab).GetComponent<MaintenanceCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static MaintenanceCanvas _instance = null;

	public static void Show(bool show, string text, float remainTime, float maxAlpha = 0.8f)
	{
		if (show)
		{
			instance.ShowCanvas(text, remainTime, maxAlpha);
			instance.gameObject.SetActive(true);
		}
		else
		{
			if (_instance == null)
				return;
			_instance.canvasGroup.alpha = 0.0f;
			_instance.gameObject.SetActive(false);
		}
	}

	public CanvasGroup canvasGroup;
	public Text messageText;

	public void ShowCanvas(string text, float remainTime, float maxAlpha = 0.8f)
	{
		messageText.SetLocalizedText(text);
		_showRemainTime = remainTime;
		_maxAlpha = maxAlpha;

		gameObject.SetActive(false);
		gameObject.SetActive(true);
	}

	float _maxAlpha;
	float _showRemainTime;
	void Update()
	{
		if (_showRemainTime > 0.0f)
		{
			_showRemainTime -= Time.deltaTime;
			canvasGroup.alpha = Mathf.Min(_maxAlpha, _showRemainTime * 2.0f);
			if (_showRemainTime <= 0.0f)
			{
				_showRemainTime = 0.0f;
				gameObject.SetActive(false);
			}
		}
	}
}