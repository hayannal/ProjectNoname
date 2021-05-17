﻿using System.Collections;
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
	public bool useIncreaseSearchRange = false;
	public float searchRangeAddSpeed = 2.0f;

	[Space(10)]
	public RectTransform nameCanvasRectTransform;
	public Text nameText;

	float _defaultRotateTransformPositionY;
	private void Awake()
	{
		_defaultRotateTransformPositionY = rotateTransform.localPosition.y;
	}

	EquipPrefabInfo _currentEquipObject;
	void OnDisable()
	{
		if (_currentEquipObject != null)
		{
			_currentEquipObject.gameObject.SetActive(false);
			_currentEquipObject = null;
		}
	}

	DropProcessor.eDropType _dropType;
	float _floatValue;
	int _intValue;
	string _stringValue;
	public void Initialize(DropProcessor.eDropType dropType, float floatValue, int intValue, string stringValue, bool forceAfterBattle)
	{
		_dropType = dropType;
		_floatValue = floatValue;
		_intValue = intValue;
		_stringValue = stringValue;

		_onAfterBattle = forceAfterBattle ? forceAfterBattle : false;
		_onAfterDropAnimation = false;
		_lastDropObject = false;
		_pullStarted = false;
		_increaseSearchRangeStarted = false;
		_searchRange = 0.0f;
		if (trailTransform != null) trailTransform.gameObject.SetActive(false);

		rotateTransform.localRotation = Quaternion.identity;
		_getDelay = getDelay;
		_lootEffectIndex = -1;

		if (dropType == DropProcessor.eDropType.Gacha)
		{
			// 로딩이 늦어질걸 대비해서 기본값을 미리 정해둔다.
			// 이 방법 대신 DropObject의 생성 자체를 늦추는 방법도 있었는데
			// 로딩이 느려질 경우 게임의 진행에 방해가 된다는 점에서 그냥 이렇게 진행은 진행대로 가고 비쥬얼이 늦게 뜨는 형태로 가기로 한다.
			float tempPivotOffset = 0.5f;
			if (nameCanvasRectTransform != null) nameCanvasRectTransform.localPosition = new Vector3(0.0f, tempPivotOffset * 2.0f + rotateTransform.localPosition.y + 0.5f, 0.0f);
			rotateTransform.localPosition = new Vector3(0.0f, _defaultRotateTransformPositionY + tempPivotOffset, 0.0f);

			// create object
			Transform itemRootTransform = rotateTransform.GetChild(0);
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(stringValue);
			if (equipTableData != null)
			{
				AddressableAssetLoadManager.GetAddressableGameObject(equipTableData.prefabAddress, "Equip", (prefab) =>
				{
					if (this == null) return;
					if (gameObject == null) return;
					if (gameObject.activeSelf == false) return;

					EquipPrefabInfo newEquipPrefabInfo = BattleInstanceManager.instance.GetCachedEquipObject(prefab, itemRootTransform);
					newEquipPrefabInfo.cachedTransform.localPosition = Vector3.zero;
					newEquipPrefabInfo.cachedTransform.localRotation = Quaternion.identity;
					_currentEquipObject = newEquipPrefabInfo;

					float pivotOffset = newEquipPrefabInfo.pivotOffset;
					if (trailTransform != null) trailTransform.localPosition = new Vector3(0.0f, pivotOffset + rotateTransform.localPosition.y, 0.0f);
					if (nameCanvasRectTransform != null) nameCanvasRectTransform.localPosition = new Vector3(0.0f, pivotOffset * 2.0f + rotateTransform.localPosition.y + 0.5f, 0.0f);
					rotateTransform.localPosition = new Vector3(0.0f, _defaultRotateTransformPositionY + pivotOffset, 0.0f);
				});
				if (nameText != null)
				{
					nameText.SetLocalizedText(UIString.instance.GetString(equipTableData.nameId));
					nameText.color = EquipListStatusInfo.GetGradeDropObjectNameColor(equipTableData.grade);
				}
				_lootEffectIndex = equipTableData.grade;
			}

			// 해당 층의 마지막 몹을 잡고나면 이후 드랍될 DropObject들은 onAfterBattle이 true로 된채 드랍되게 된다.
			// 이 시점에 저장하면 드랍되고나서 회수되지 않더라도 재진입시 템을 획득한거로 처리할 수 있다.
			if (_onAfterBattle)
			{
				// 예외처리. TimeSpace 이벤트 템의 경우 AdjustDrop호출과 동시에 클라이언트 세이브로 저장해놨으니 여기서는 추가하면 안된다.
				// 혹시 빠르게 넘어갈걸 대비해서 다음 스테이지까지도 검사하기로 한다.
				bool ignoreSave = false;
				if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace) == false && stringValue == "Equip0001")
				{
					if (ContentsManager.IsDropChapterStage(StageManager.instance.playChapter, StageManager.instance.playStage) ||
						ContentsManager.IsDropChapterStage(StageManager.instance.playChapter, StageManager.instance.playStage - 1))
						ignoreSave = true;
				}
				if (ignoreSave == false)
					ClientSaveData.instance.OnAddedDropItemId(stringValue);
			}
		}
		else if (dropType == DropProcessor.eDropType.Origin)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(stringValue);
			if (actorTableData != null)
			{
				// 장비에 쓰던거를 같이 쓰는 구조라 이렇게 인덱스 변화시켜서 쓰면 된다.
				switch (actorTableData.grade)
				{
					case 0: _lootEffectIndex = 0; break;
					case 1: _lootEffectIndex = 2; break;
					case 2: _lootEffectIndex = 4; break;
				}
			}
		}
		else if (dropType == DropProcessor.eDropType.PowerPoint)
		{
			if (nameText != null) nameText.SetLocalizedText(CharacterData.GetNameByActorId(stringValue));
		}

		if (useLootEffect && _lootEffectIndex != -1)
			_lootEffectTransform = BattleInstanceManager.instance.GetCachedObject(DropObjectGroup.instance.lootEffectPrefabList[_lootEffectIndex], cachedTransform.position, Quaternion.identity).transform;

		if (nameCanvasRectTransform != null)
			nameCanvasRectTransform.gameObject.SetActive(false);

		jumpTransform.localPosition = new Vector3(0.0f, jumpStartY, 0.0f);
		jumpTransform.DOLocalJump(new Vector3(0.0f, jumpEndY, 0.0f), jumpPower, 1, jumpDuration).SetEase(Ease.Linear);
		_lastJump = (secondJumpPower == 0.0f || secondJumpDuration == 0.0f) ? true : false;
		_jumpRemainTime = jumpDuration;
		_rotateEuler.x = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);
		_rotateEuler.z = Random.Range(360.0f, 720.0f) * (Random.value > 0.5f ? 1.0f : -1.0f);

		_initialized = true;
		DropManager.instance.OnInitializeDropObject(this);
	}

	#region state flag
	bool _initialized = false;
	bool _onAfterBattle = false;
	public bool onAfterBattle { get { return _onAfterBattle; } }
	public void OnAfterBattle()
	{
		if (getAfterAllDropAnimationInStage)
		{
			// 해당 층의 마지막 몹을 잡는 순간 드랍되어있던 DropObject에다가 onAfterBattle을 알리게 되는데
			// 이 시점에 저장하면 회수되지 않은채 종료되더라도 재진입시 템을 획득한거로 처리할 수 있다.
			if (_onAfterBattle == false)
			{
				_onAfterBattle = true;
				if (_dropType == DropProcessor.eDropType.Gacha)
					ClientSaveData.instance.OnAddedDropItemId(_stringValue);
			}
		}
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
				DropManager.instance.OnFinishLastDropAnimation();
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
		if (nameCanvasRectTransform != null && _pullDelay == 0.0f) nameCanvasRectTransform.gameObject.SetActive(false);
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
					DropManager.instance.OnFinishLastDropAnimation();
				CheckShowNameCanvas();
				CheckPull();
			}
			else
			{
				jumpTransform.DOLocalJump(new Vector3(0.0f, jumpEndY, 0.0f), secondJumpPower, 1, secondJumpDuration).SetEase(Ease.Linear);
				_jumpRemainTime = secondJumpDuration;
				_lastJump = true;

				// Gain과 달리 Drop은 종류별로 다르게 가져가기 위해서 프리팹 안에다가 사운드를 넣기로 한다.
				//switch (_dropType)
				//{
				//	case DropProcessor.eDropType.LevelPack:
				//	case DropProcessor.eDropType.Heart:
				//	case DropProcessor.eDropType.Seal:
				//	case DropProcessor.eDropType.Origin:
				//	//case DropProcessor.eDropType.PowerPoint:
				//	case DropProcessor.eDropType.Balance:
				//	case DropProcessor.eDropType.ReturnScroll:
				//		SoundManager.instance.PlaySFX("DropObject");
				//		break;
				//	case DropProcessor.eDropType.Gacha:
				//		SoundManager.instance.PlaySFX("DropEquip");
				//		break;
				//}
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

		// 로비에서 굴릴땐 굴림과 동시에 이미 다 계산해서 패킷으로 보내기때문에 이 아래항목들을 적용할 필요가 없다.
		// 적용해버리면 스테이지 들어가서 정산할때 합산이 되니 하면 안된다.
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
		{
			switch (_dropType)
			{
				case DropProcessor.eDropType.Gold:
					DropManager.instance.AddDropGold(_floatValue);
					break;
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
				case DropProcessor.eDropType.Gacha:
					DropManager.instance.AddDropItem(_stringValue);
					break;
				case DropProcessor.eDropType.Seal:
					DropManager.instance.AddDropSeal(1);
					break;
			}
		}

		string soundName = "DropGainObject";
		switch (_dropType)
		{
			case DropProcessor.eDropType.Gold:
				if (lobby == false)
					soundName = "";
				break;
			case DropProcessor.eDropType.LevelPack:
				if (_floatValue > 0.0f)
					soundName = "DropGainNoHitLevelPack";
				break;
			case DropProcessor.eDropType.Heart:
				soundName = "DropGainHeart";
				break;
			case DropProcessor.eDropType.Gacha:
				soundName = "DropGainEquip";
				break;
			case DropProcessor.eDropType.Diamond:
				soundName = "DropGainDiamond";
				break;
			case DropProcessor.eDropType.Origin:
				soundName = "DropGainOrigin";
				break;
		}
		if (string.IsNullOrEmpty(soundName) == false)
		{
			if (SoundManager.instance.CheckSameFrameSound(soundName) == false)
			{
				SoundManager.instance.PlaySFX(soundName);
				SoundManager.instance.RegisterCharacterSound(soundName);
			}
		}

		_initialized = false;
		DropManager.instance.OnFinalizeDropObject(this);
		gameObject.SetActive(false);
	}

	int _lootEffectIndex = -1;
	Transform _lootEffectTransform;
	void DiableEffectObject()
	{
		if (useLootEffect && _lootEffectIndex != -1)
			BattleInstanceManager.instance.GetCachedObject(DropObjectGroup.instance.lootEndEffectPrefabList[_lootEffectIndex], (_lootEffectTransform != null) ? _lootEffectTransform.position : cachedTransform.position, Quaternion.identity);

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
			{
				_pullDelay = 0.0f;
				if (nameCanvasRectTransform != null) nameCanvasRectTransform.gameObject.SetActive(false);
			}
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
	}

	// 정산을 위해 추가한 함수. 획득가능한지 본다.
	public bool IsAcquirableForEnd()
	{
		// 이미 pull이 시작되었으면 언제나 가능
		if (_pullStarted)
			return true;

		// 범위 늘려가고 있는 중이어도 가능
		if (_increaseSearchRangeStarted)
			return true;

		// 점프만 끝나면 pullStart가 시작될거기 때문에 가능
		if (getAfterAllDropAnimationInStage == false)
			return true;

		// 전투 종료가 되야 획득가능한 오브젝트들에게 _onAfterBattle 켜져있단건
		// 해당 스테이지에서의 마지막 몹이 죽어서 전투가 끝났음이 체크되어있는거다.
		// 단지 드랍 애니나 나머지 항목을 기다리는 중인거라 시간이 지나면 획득가능할거다.
		// _onAfterDropAnimation 는 드랍 애니를 기다리는거라 체크하지 않아야한다.
		if (getAfterAllDropAnimationInStage && _onAfterBattle)
			return true;

		return false;
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
