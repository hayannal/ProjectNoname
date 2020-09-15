﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DropSealGainObject : MonoBehaviour
{
	public float getRange = 0.1f;

	// 점프는 해야하니 NodeWarItem처럼 DropObject에서 점프 코드만 가져와서 쓴다.
	[Space(10)]
	public Transform jumpTransform;
	public Transform rotateTransform;
	public float jumpPower = 1.0f;
	public float jumpStartY = 0.5f;
	public float jumpEndY = 0.0f;
	public float jumpDuration = 1.0f;
	public float secondJumpPower = 0.5f;
	public float secondJumpDuration = 0.5f;

	// 이것도 있어야 같은 아이템으로 보인다.
	[Space(10)]
	public float rotationY = 30.0f;

	// 아무래도 수집인데 pull 없이 자동획득되니 이상하다. pull 넣어본다.
	[Space(10)]
	public float pullStartDelay = 0.5f;
	public float pullStartSpeed = 3.0f;
	public float pullAcceleration = 2.0f;
	public Transform trailTransform;

	// 이름까지 해야 같은 아이템으로 본다.
	[Space(10)]
	public RectTransform nameCanvasRectTransform;

	void OnEnable()
	{
		if (trailTransform != null)
			trailTransform.gameObject.SetActive(false);
		if (nameCanvasRectTransform != null)
			nameCanvasRectTransform.gameObject.SetActive(false);

		InitializeJump();
	}

	void Update()
	{
		UpdateJump();
		UpdateRotationY();
		UpdatePull();
		UpdateDistance();
	}

	void UpdateDistance()
	{
		if (_jumpRemainTime > 0)
			return;

		Vector3 targetPosition = DailyBoxGaugeCanvas.instance.sealImageTransform.position;
		float radius = 0.2f;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = targetPosition.x - position.x;
		diff.y = targetPosition.z - position.z;
		float sqrMagnitude = diff.x * diff.x + diff.y * diff.y;
		if (sqrMagnitude < (getRange + radius) * (getRange + radius))
			GetDropObject();
	}

	void InitializeJump()
	{
		jumpTransform.localPosition = new Vector3(0.0f, jumpStartY, 0.0f);
		jumpTransform.DOLocalJump(new Vector3(0.0f, jumpEndY, 0.0f), jumpPower, 1, jumpDuration).SetEase(Ease.Linear);
		_lastJump = (secondJumpPower == 0.0f || secondJumpDuration == 0.0f) ? true : false;
		_jumpRemainTime = jumpDuration;
		_rotateEuler.x = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);
		_rotateEuler.z = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);
	}

	float _jumpRemainTime = 0.0f;
	Vector3 _rotateEuler;
	bool _lastJump = false;
	void UpdateJump()
	{
		if (_jumpRemainTime <= 0.0f)
			return;

		_jumpRemainTime -= Time.deltaTime;

		if (_lastJump == false)
			rotateTransform.Rotate(_rotateEuler * Time.deltaTime, Space.Self);

		if (_jumpRemainTime <= 0.0f)
		{
			rotateTransform.rotation = Quaternion.identity;
			if (_lastJump)
			{
				_jumpRemainTime = 0.0f;
				if (nameCanvasRectTransform != null) nameCanvasRectTransform.gameObject.SetActive(true);

				_pullStarted = true;
				_pullDelay = pullStartDelay;
				_pullSpeed = pullStartSpeed;
				if (trailTransform != null) trailTransform.gameObject.SetActive(true);
				if (nameCanvasRectTransform != null && _pullDelay == 0.0f) nameCanvasRectTransform.gameObject.SetActive(false);
			}
			else
			{
				jumpTransform.DOLocalJump(new Vector3(0.0f, jumpEndY, 0.0f), secondJumpPower, 1, secondJumpDuration).SetEase(Ease.Linear);
				_jumpRemainTime = secondJumpDuration;
				_lastJump = true;
			}
		}
	}

	void UpdateRotationY()
	{
		if (_jumpRemainTime > 0.0f)
			return;

		rotateTransform.Rotate(0.0f, rotationY * Time.deltaTime, 0.0f, Space.Self);
	}

	bool _pullStarted = false;
	float _pullSpeed = 0.0f;
	float _pullDelay = 0.0f;
	void UpdatePull()
	{
		if (_jumpRemainTime > 0.0f)
			return;
		if (_pullStarted == false)
			return;
		if (_pullDelay > 0.0f)
		{
			_pullDelay -= Time.deltaTime;
			if (_pullDelay <= 0.0f)
			{
				_pullDelay = 0.0f;
				if (nameCanvasRectTransform != null) nameCanvasRectTransform.gameObject.SetActive(false);
			}
			return;
		}

		_pullSpeed += pullAcceleration * Time.deltaTime;

		Vector3 targetPosition = DailyBoxGaugeCanvas.instance.sealImageTransform.position;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = targetPosition.x - position.x;
		diff.y = targetPosition.z - position.z;
		if (diff.magnitude < _pullSpeed * Time.deltaTime)
			cachedTransform.Translate(new Vector3(diff.x, 0.0f, diff.y));
		else
			cachedTransform.Translate(new Vector3(diff.normalized.x, 0.0f, diff.normalized.y) * _pullSpeed * Time.deltaTime);
	}

	void GetDropObject()
	{
		DailyBoxGaugeCanvas.instance.FillGauge();
		gameObject.SetActive(false);
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