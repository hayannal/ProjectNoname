using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleToastCanvas : MonoBehaviour
{
	public static BattleToastCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(BattleManager.instance.battleToastCanvasPrefab).GetComponent<BattleToastCanvas>();
			}
			return _instance;
		}
	}
	static BattleToastCanvas _instance = null;

	public CanvasGroup canvasGroup;
	public Text toastText;

	public void ShowToast(string text, float remainTime, float maxAlpha = 1.0f)
	{
		toastText.SetLocalizedText(text);
		_showRemainTime = remainTime;
		_maxAlpha = maxAlpha;
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