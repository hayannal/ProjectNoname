using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DropObject : MonoBehaviour
{
	public float getRange = 0.2f;
	//public bool getAfterBattle = true;
	// 스테이지가 끝나고 나서 해당 스테이지에서 드랍된 템들의 애니가 전부다 끝나면 습득된다. 가장 자연스러움.
	// 이걸 위해 onAfterBattle와 onAfterDropAnimation 두개를 써서 관리한다.
	public bool getAfterAllDropAnimationInStage = true;
	public float getDelay = 0.0f;

	[Space(10)]
	public Transform jumpTransform;
	public Transform rotateTransform;
	public float jumpPower = 1.0f;
	public float jumpStartY = 0.5f;
	public float jumpEndY = 0.0f;
	public float jumpDuration = 1.0f;
	public float secondJumpPower = 0.5f;
	public float secondJumpDuration = 0.5f;

	[Space(10)]
	public float rotationY = 30.0f;

	[Space(10)]
	public float pullStartDelay = 0.5f;
	public float pullStartSpeed = 3.0f;
	public float pullAcceleration = 2.0f;
	public Transform trailTransform;

	[Space(10)]
	public bool useLootEffect = false;
	public GameObject lootEffectPrefab;
	public GameObject lootEndEffectPrefab;
	public bool useIncreaseSearchRange = false;
	public float searchRangeAddSpeed = 2.0f;

	[Space(10)]
	public RectTransform nameCanvasRectTransform;
	public Text nameText;

	DropProcessor.eDropType _dropType;
	float _floatValue;
	int _intValue;
	public void Initialize(DropProcessor.eDropType dropType, float floatValue, int intValue, bool forceAfterBattle)
	{
		_dropType = dropType;
		_floatValue = floatValue;
		_intValue = intValue;

		_onAfterBattle = forceAfterBattle ? forceAfterBattle : false;
		_onAfterDropAnimation = false;
		_lastDropObject = false;
		_pullStarted = false;
		_increaseSearchRangeStarted = false;
		_searchRange = 0.0f;
		if (trailTransform != null) trailTransform.gameObject.SetActive(false);

		rotateTransform.localRotation = Quaternion.identity;
		_getDelay = getDelay;

		if (dropType == DropProcessor.eDropType.Gacha)
		{
			// create item prefab
			// temp code
			//GameObject itemObject = rotateTransform.GetChild(0).gameObject;

			// object height
			float itemHeight = ColliderUtil.GetHeight(GetComponentInChildren<Collider>());
			if (trailTransform != null) trailTransform.localPosition = new Vector3(0.0f, itemHeight * 0.5f, 0.0f);
			if (nameCanvasRectTransform != null) nameCanvasRectTransform.localPosition = new Vector3(0.0f, itemHeight, 0.0f);
		}

		if (useLootEffect && lootEffectPrefab != null)
			_lootEffectTransform = BattleInstanceManager.instance.GetCachedObject(lootEffectPrefab, cachedTransform.position, Quaternion.identity).transform;

		if (nameCanvasRectTransform != null)
			nameCanvasRectTransform.gameObject.SetActive(false);

		jumpTransform.localPosition = new Vector3(0.0f, jumpStartY, 0.0f);
		jumpTransform.DOLocalJump(new Vector3(0.0f, jumpEndY, 0.0f), jumpPower, 1, jumpDuration).SetEase(Ease.Linear);
		_lastJump = (secondJumpPower == 0.0f || secondJumpDuration == 0.0f) ? true : false;
		_jumpRemainTime = jumpDuration;
		_rotateEuler.x = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);
		_rotateEuler.z = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);

		_initialized = true;
		BattleInstanceManager.instance.OnInitializeDropObject(this);
	}

	#region state flag
	bool _initialized = false;
	bool _onAfterBattle = false;
	public bool onAfterBattle { get { return _onAfterBattle; } }
	public void OnAfterBattle()
	{
		if (getAfterAllDropAnimationInStage)
			_onAfterBattle = true;
	}

	bool _onAfterDropAnimation;
	public void OnAfterAllDropAnimation()
	{
		if (getAfterAllDropAnimationInStage)
		{
			_onAfterDropAnimation = true;
			CheckPull();
		}
	}

	bool _lastDropObject;
	public void ApplyLastDropObject()
	{
		_lastDropObject = true;

		// 드랍시에 설정되는게 아니고 이미 생성되어있는 상태에서 셋팅되는 경우 점프 여부를 판단해서
		// 이미 점프가 끝났다면 드랍 회수를 알려야한다.
		// 이렇게 해야 점프애니가 끝난 lastDropObject도 회수할 수 있게 된다.
		if (_initialized && _jumpRemainTime <= 0.0f)
		{
			if (getAfterAllDropAnimationInStage && _onAfterBattle)
				BattleInstanceManager.instance.OnFinishLastDropAnimation();
		}
	}
	#endregion

	void CheckPull()
	{
		if (_pullStarted)
			return;

		bool nextStep = false;
		if (getAfterAllDropAnimationInStage == false) nextStep = true;
		if (getAfterAllDropAnimationInStage && _onAfterBattle && _onAfterDropAnimation) nextStep = true;
		if (!nextStep)
			return;

		if (useIncreaseSearchRange)
		{
			if (_increaseSearchRangeStarted == false)
			{
				_increaseSearchRangeStarted = true;
				_onSearchRange = false;
				return;
			}
			if (_onSearchRange == false)
				return;
		}

		_pullStarted = true;
		_pullDelay = pullStartDelay;
		_pullSpeed = pullStartSpeed;
		if (trailTransform != null) trailTransform.gameObject.SetActive(true);
		if (nameCanvasRectTransform != null) nameCanvasRectTransform.gameObject.SetActive(false);
		DiableEffectObject();
	}

	float _getDelay = 0.0f;
	void Update()
	{
		UpdateJump();
		UpdateRotationY();

		if (_getDelay > 0.0f)
		{
			_getDelay -= Time.deltaTime;
			if (_getDelay <= 0.0f)
				_getDelay = 0.0f;
			return;
		}

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

		if (_jumpRemainTime <= 0.0f)
		{
			rotateTransform.rotation = Quaternion.identity;
			if (_lastJump)
			{
				_jumpRemainTime = 0.0f;
				if (getAfterAllDropAnimationInStage && _onAfterBattle && _lastDropObject)
					BattleInstanceManager.instance.OnFinishLastDropAnimation();
				CheckShowNameCanvas();
				CheckPull();
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

	void UpdateDistance()
	{
		if (_jumpRemainTime > 0.0f)
			return;

		if (getAfterAllDropAnimationInStage)
		{
			if (_onAfterBattle == false || _onAfterDropAnimation == false)
				return;
		}

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
		// 드랍되자마자 먹으면 EndEffect가 호출 안될 수 있어서 pullStart시키는 곳 말고 획득하는 곳에서도 호출하게 해놨다.
		// 두번 나오지 않게 pullStart 체크를 추가로 한다.
		if (_pullStarted == false)
			DiableEffectObject();

		switch (_dropType)
		{
			case DropProcessor.eDropType.LevelPack:
				int levelPackCount = (_floatValue == 0.0f) ? 1 : 0;
				int noHitLevelPackCount = (_floatValue > 0.0f) ? 1 : 0;
				LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, 0, levelPackCount, noHitLevelPackCount);
				break;
			case DropProcessor.eDropType.Heart:
				AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
				healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("DropHeal");
				BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, BattleInstanceManager.instance.playerActor, false);
				break;
		}

		_initialized = false;
		BattleInstanceManager.instance.OnFinalizeDropObject(this);
		gameObject.SetActive(false);
	}

	Transform _lootEffectTransform;
	void DiableEffectObject()
	{
		if (useLootEffect && lootEndEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(lootEndEffectPrefab, (_lootEffectTransform != null) ? _lootEffectTransform.position : cachedTransform.position, Quaternion.identity);

		if (_lootEffectTransform != null)
		{
			DisableParticleEmission.DisableEmission(_lootEffectTransform);
			_lootEffectTransform = null;
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
	bool _onSearchRange = false;
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
		{
			_onSearchRange = true;
			CheckPull();
		}
	}

	void CheckShowNameCanvas()
	{
		if (nameCanvasRectTransform != null)
			nameCanvasRectTransform.gameObject.SetActive(true);

		// 우선 임시로 LocalizedText 함수만 호출해둔다.
		if (nameText != null)
			nameText.SetLocalizedText(nameText.text);
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
