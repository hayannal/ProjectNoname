using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaintenanceCanvas : MonoBehaviour
{
	public static MaintenanceCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonCanvasGroup.instance.maintenanceCanvasPrefab).GetComponent<MaintenanceCanvas>();
			}
			return _instance;
		}
	}
	static MaintenanceCanvas _instance = null;

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