using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MonsterHPGauge : MonoBehaviour
{
	public RectTransform widthRectTransform;
	public Slider hpRatioSlider;
	public RectTransform hpFillRectTransform;
	public RectTransform lateFillRectTransform;

	float _defaultWidth;
	void Awake()
	{
		_defaultWidth = widthRectTransform.sizeDelta.x;
	}

	void OnEnable()
	{
		_lateFillDelayRemainTime = 0.0f;
		_lateFillLerpStarted = false;
		_prevTargetPosition = -Vector3.up;
	}

	public void InitializeGauge(MonsterActor monsterActor)
	{
		_offsetY = monsterActor.gaugeOffsetY;
		widthRectTransform.sizeDelta = new Vector2(monsterActor.monsterHpGaugeWidth * _defaultWidth, widthRectTransform.sizeDelta.y);
		hpRatioSlider.value = 1.0f;
		lateFillRectTransform.anchorMin = new Vector2(hpFillRectTransform.anchorMax.x, 0.0f);
		lateFillRectTransform.anchorMax = hpFillRectTransform.anchorMax;
		_targetTransform = monsterActor.cachedTransform;
		_targetHeight = ColliderUtil.GetHeight(monsterActor.GetCollider());
	}

	// 컬리더 히트오브젝트냐 Area 히트오브젝트냐에 따라 Monster Hp Gauge Show 위치가 달라지는데
	// 이랬더니 Area 히트오브젝트에선 한프레임 늦게 몬스터의 포지션을 따라가게 나와서 어색하게 보였다.
	// LateUpdate로 교체해서 해결한다.
	Vector3 _prevTargetPosition = -Vector3.up;
	void LateUpdate()
	{
		if (_targetTransform != null)
		{
			if (_targetTransform.position != _prevTargetPosition)
			{
				UpdateGaugePosition();
				UpdateGaugeRotation();
				_prevTargetPosition = _targetTransform.position;
			}
		}
		
		UpdateLateFill();
	}

	Transform _targetTransform;
	float _targetHeight;
	float _offsetY;
	void UpdateGaugePosition()
	{
		Vector3 desiredPosition = _targetTransform.position;
		if (desiredPosition.y < 0.0f)
			desiredPosition.y = 0.0f;
		desiredPosition.y += _targetHeight;
		desiredPosition.y += _offsetY;
		cachedTransform.position = desiredPosition;
	}

	void UpdateGaugeRotation()
	{
		float rotateY = cachedTransform.position.x * 2.0f;
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			rotateY = 0.0f;
		cachedTransform.rotation = Quaternion.Euler(0.0f, rotateY, 0.0f);
	}

	public void OnChangedHP(float hpRatio)
	{
		float prevValue = hpRatioSlider.value;
		if (prevValue < hpRatio)
		{
			hpRatioSlider.value = hpRatio;
			lateFillRectTransform.anchorMin = new Vector2(hpFillRectTransform.anchorMax.x, 0.0f);
			lateFillRectTransform.anchorMax = hpFillRectTransform.anchorMax;
		}
		else
		{
			hpRatioSlider.value = hpRatio;
			lateFillRectTransform.anchorMin = new Vector2(hpFillRectTransform.anchorMax.x, 0.0f);
			if (_lateFillLerpStarted == false && _lateFillDelayRemainTime == 0.0f)
				_lateFillDelayRemainTime = LateFillDelay;
		}
	}

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

		lateFillRectTransform.anchorMax = Vector2.Lerp(lateFillRectTransform.anchorMax, hpFillRectTransform.anchorMax, Time.deltaTime * 4.0f);

		if (Mathf.Abs(lateFillRectTransform.anchorMax.x - hpFillRectTransform.anchorMax.x) < 0.005f)
		{
			lateFillRectTransform.anchorMax = hpFillRectTransform.anchorMax;
			_lateFillLerpStarted = false;
		}
	}



	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
