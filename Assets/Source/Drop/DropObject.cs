using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DropObject : MonoBehaviour
{
	public float getRange = 0.2f;
	public bool getAfterBattle = true;

	[Space(10)]
	public bool useJump = false;
	public Transform jumpTransform;
	public Transform rotateTransform;
	public float jumpPower = 1.0f;
	public float jumpStartY = 0.5f;
	public float jumpDuration = 1.0f;
	public float secondJumpPower = 0.5f;
	public float secondJumpDuration = 0.5f;

	[Space(10)]
	public float rotationY = 30.0f;

	[Space(10)]
	public float pullStartDelay = 0.5f;
	public float pullStartSpeed = 3.0f;
	public float pullAcceleration = 2.0f;

	[Space(10)]
	public bool useLootEffect = false;
	public bool useIncreaseSearchRange = false;
	public float searchRangeAddSpeed = 2.0f;

	public void Initialize(DropProcessor.eDropType dropType, float floatValue, int intValue)
	{
		_onAfterBattle = false;
		_pullStarted = false;
		_increaseSearchRangeStarted = false;
		_searchRange = 0.0f;

		rotateTransform.localRotation = Quaternion.identity;

		if (useJump)
		{
			jumpTransform.localPosition = new Vector3(0.0f, jumpStartY, 0.0f);
			jumpTransform.DOLocalJump(Vector3.zero, jumpPower, 1, jumpDuration).SetEase(Ease.InSine);
			_lastJump = false;
			_jumpRemainTime = jumpDuration;
			_rotateEuler.x = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);
			_rotateEuler.z = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);
		}
		else
			CheckPull();

		BattleInstanceManager.instance.OnInitializeDropObject(this);
	}

	void CheckPull()
	{
		bool nextStep = false;
		if (getAfterBattle == false) nextStep = true;
		if (getAfterBattle && BattleInstanceManager.instance.cachedDropItemOnAfterBattle)
			_onAfterBattle = true;
		if (getAfterBattle && _onAfterBattle) nextStep = true;
		if (!nextStep)
			return;

		if (useIncreaseSearchRange)
		{
			if (_increaseSearchRangeStarted == false)
			{
				_increaseSearchRangeStarted = true;
				return;
			}
		}

		_pullStarted = true;
		_pullDelay = pullStartDelay;
		_pullSpeed = pullStartSpeed;
	}

	void Update()
	{
		UpdateJump();
		UpdateRotationY();
		UpdatePull();
		UpdateSearchRange();
		UpdateDistance();
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
		//else
		//	rotateTransform.rotation = Quaternion.Slerp(rotateTransform.rotation, Quaternion.identity, Time.deltaTime * 10.0f);

		if (_jumpRemainTime <= 0.0f)
		{
			if (_lastJump)
			{
				_jumpRemainTime = 0.0f;
				BattleInstanceManager.instance.CheckFinishJumpAllDropObject();
				CheckPull();
			}
			else
			{
				rotateTransform.rotation = Quaternion.identity;
				jumpTransform.DOLocalJump(Vector3.zero, secondJumpPower, 1, secondJumpDuration).SetEase(Ease.Linear);
				_jumpRemainTime = secondJumpDuration;
				_lastJump = true;
			}
		}
	}

	public bool jumpFinished
	{
		get
		{
			if (useJump == false)
				return true;
			return (_jumpRemainTime <= 0.0f);
		}
	}

	void UpdateRotationY()
	{
		if (_jumpRemainTime > 0.0f)
			return;

		rotateTransform.Rotate(0.0f, rotationY * Time.deltaTime, 0.0f, Space.Self);
	}

	void UpdateDistance()
	{
		if (_jumpRemainTime > 0.0f)
			return;

		if (getAfterBattle && _onAfterBattle == false)
			return;

		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		float playerRadius = BattleInstanceManager.instance.playerActor.actorRadius;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = playerPosition.x - position.x;
		diff.y = playerPosition.z - position.z;
		if (diff.x * diff.x + diff.y * diff.y < (getRange + playerRadius) * (getRange + playerRadius))
			GetDropObject();
	}

	void GetDropObject()
	{
		BattleInstanceManager.instance.OnFinalizeDropObject(this);
		gameObject.SetActive(false);
	}

	bool _onAfterBattle = false;
	public void OnAfterBattle()
	{
		if (getAfterBattle)
		{
			_onAfterBattle = true;
			CheckPull();
		}
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
				_pullDelay = 0.0f;
			return;
		}

		_pullSpeed += pullAcceleration * Time.deltaTime;

		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = playerPosition.x - position.x;
		diff.y = playerPosition.z - position.z;
		if (diff.magnitude < _pullSpeed * Time.deltaTime)
			cachedTransform.Translate(new Vector3(diff.x, 0.0f, diff.y));
		else
			cachedTransform.Translate(new Vector3(diff.normalized.x, 0.0f, diff.normalized.y) * _pullSpeed * Time.deltaTime);
	}

	bool _increaseSearchRangeStarted = false;
	float _searchRange = 0.0f;
	void UpdateSearchRange()
	{
		if (useIncreaseSearchRange == false)
			return;
		if (_increaseSearchRangeStarted == false)
			return;
		if (_pullStarted)
			return;

		_searchRange += Time.deltaTime * searchRangeAddSpeed;

		Vector3 playerPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		Vector3 position = cachedTransform.position;
		Vector2 diff;
		diff.x = playerPosition.x - position.x;
		diff.y = playerPosition.z - position.z;
		if (diff.x * diff.x + diff.y * diff.y < _searchRange * _searchRange)
			CheckPull();
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

#if UNITY_EDITOR
	void OnDrawGizmosSelected()
	{
		UnityEditor.Handles.DrawWireDisc(cachedTransform.position, Vector3.up, getRange);

		if (useIncreaseSearchRange)
			UnityEditor.Handles.DrawWireDisc(cachedTransform.position, Vector3.up, _searchRange);
	}
#endif
}
