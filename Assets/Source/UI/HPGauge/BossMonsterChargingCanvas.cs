using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BossMonsterChargingCanvas : MonoBehaviour
{
	public static BossMonsterChargingCanvas instance;

	public Slider chargingSlider;
	public DOTweenAnimation shakeTween;

	void Awake()
	{
		instance = this;
	}

	public void RefreshGauge(float ratio, bool useTween)
	{
		chargingSlider.value = ratio;

		if (useTween)
			shakeTween.DORestart();
	}


	/*
	// Update is called once per frame
	void Update()
	{
		UpdateLateFill();
	}

	float _lastLateHpLineRatio;

	const float LateFillDelay = 0.9f;
	float _lateFillDelayRemainTime = 0.0f;
	bool _lateFillLerpStarted = false;
	void UpdateLateFill()
	{
		if (_lateFillDelayRemainTime > 0.0f)
		{
			_lateFillDelayRemainTime -= Time.deltaTime;
			if (_lateFillDelayRemainTime <= 0.0f)
			{
				_lateFillDelayRemainTime = 0.0f;
				_lateFillLerpStarted = true;
			}
		}

		if (_lateFillLerpStarted == false)
			return;

		_lastLateHpLineRatio = Mathf.Lerp(_lastLateHpLineRatio, _lastHpLineRatio, Time.deltaTime * 4.0f);

		if (Mathf.Abs(_lastLateHpLineRatio - _lastHpLineRatio) < 0.005f * 2.0f)
		{
			_lastLateHpLineRatio = _lastHpLineRatio;
			_lateFillLerpStarted = false;
		}

		float value1 = 0.0f;
		float value2 = 0.0f;
		if (_lastLateHpLineRatio > 1.0f)
		{
			value1 = 1.0f;
			value2 = _lastLateHpLineRatio - 1.0f;

			// 3줄 넘게 있다가 한번에 2줄 이하로 진입할때를 대비해서 맥스 처리
			if (value2 > 1.0f) value2 = 1.0f;
		}
		else
		{
			value1 = _lastLateHpLineRatio;
			value2 = 0.0f;
		}
		lateFill1RectTransform.anchorMax = new Vector2(value1, lateFill1RectTransform.anchorMax.y);
		lateFill2RectTransform.anchorMax = new Vector2(value2, lateFill1RectTransform.anchorMax.y);
	}
	*/
}