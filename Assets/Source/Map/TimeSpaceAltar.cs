using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TimeSpaceAltar : MonoBehaviour
{
	public int positionIndex;
	public Transform equipRootTransform;
	public DOTweenAnimation rotateTweenAnimation;
	public GameObject emptyIconObject;
	public ParticleSystem gradeParticleSystem;
	public Text enhanceText;

	void Start()
	{
		_position = transform.position;
	}

	void OnEnable()
	{
		RefreshEquipObject();
	}

	void OnDisable()
	{
		DisableEquipObject();

		if (_objectIndicatorCanvas != null)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_objectIndicatorCanvas = null;
		}
		_spawnedIndicator = false;
	}

	Vector3 _position;
	void Update()
	{
		if (_spawnedIndicator == false)
			return;
		if (_objectIndicatorCanvas == null)
			return;
		if (_objectIndicatorCanvas.gameObject.activeSelf == false)
			return;

		Vector3 diff = BattleInstanceManager.instance.playerActor.cachedTransform.position - _position;
		diff.y = 0.0f;
		if ((diff.x * diff.x + diff.z * diff.z) > 2.2f * 2.2f)
		{
			_objectIndicatorCanvas.gameObject.SetActive(false);
			_spawnedIndicator = false;
		}
	}

	void ShowIndicator()
	{
		AddressableAssetLoadManager.GetAddressableGameObject("TimeSpaceAltarIndicator", "Canvas", (prefab) =>
		{
			if (this == null) return;
			if (gameObject == null) return;
			if (gameObject.activeInHierarchy == false) return;

			_objectIndicatorCanvas = (TimeSpaceAltarIndicatorCanvas)UIInstanceManager.instance.GetCachedObjectIndicatorCanvas(prefab);
			_objectIndicatorCanvas.targetTransform = transform;
			_objectIndicatorCanvas.positionIndex = positionIndex;
		});

		_spawnedIndicator = true;
	}

	TimeSpaceAltarIndicatorCanvas _objectIndicatorCanvas;
	bool _spawnedIndicator;
	void OnTriggerEnter(Collider other)
	{
		if (_spawnedIndicator)
			return;

		AffectorProcessor affectorProcessor = BattleInstanceManager.instance.GetAffectorProcessorFromCollider(other);
		if (affectorProcessor == null)
			return;
		if (affectorProcessor.actor == null)
			return;
		if (affectorProcessor.actor.team.teamId == (int)Team.eTeamID.DefaultMonster)
			return;

		ShowIndicator();
	}

	EquipPrefabInfo _currentEquipObject = null;
	public void RefreshEquipObject()
	{
		// 비쥬얼용 오브젝트들은 우선 끄고 처리
		DisableEquipObject();

		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)positionIndex);
		if (equipData == null)
		{
			gradeParticleSystem.gameObject.SetActive(false);
			emptyIconObject.SetActive(true);
			// 두가지 조건이다. 제단이 비어있는데 장착할 수 있는 장비가 있거나 새로운 템을 얻었거나
			//alarm3dObject.SetActive(TimeSpaceData.instance.IsExistEquipByType((TimeSpaceData.eEquipSlotType)positionIndex);
			enhanceText.text = "";
			enhanceText.gameObject.SetActive(false);
			return;
		}

		// 제단은 9개가 동시에 있다보니 오브젝트 로딩을 기다리다보면 강화수치도 등급 이펙트도 아무것도 안떠서 휑해질 수 있다.
		// 그러니 등급 이펙트까지 다 미리 보여지게 한채 오브젝트를 로드한다.
		// EquipInfoGround 로 가서는 하나의 오브젝트만 줌인해서 보는거라 로딩이 다 되서 오브젝트가 바뀔때 등급 이펙트도 같이 바꾼다.
		ParticleSystem.MainModule main = gradeParticleSystem.main;
		main.startColor = GetGradeParticleColor(equipData.cachedEquipTableData.grade);
		gradeParticleSystem.gameObject.SetActive(true);

		emptyIconObject.SetActive(false);
		enhanceText.text = string.Format("+{0}", equipData.enhanceLevel);
		enhanceText.gameObject.SetActive(equipData.enhanceLevel > 0);
		AddressableAssetLoadManager.GetAddressableGameObject(equipData.cachedEquipTableData.prefabAddress, "Equip", OnLoadedEquip);
	}

	void DisableEquipObject()
	{
		if (_currentEquipObject != null)
		{
			ShowOutline(false, _currentEquipObject.gameObject, -1);
			_currentEquipObject.gameObject.SetActive(false);
			_currentEquipObject = null;
			rotateTweenAnimation.DOComplete();
		}
	}

	public static Color GetGradeParticleColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.5f, 0.5f, 0.5f);
			case 1: return new Color(0.0f, 1.0f, 0.51f);
			case 2: return new Color(0.0f, 0.51f, 1.0f);
			case 3: return new Color(0.63f, 0.0f, 1.0f);
			case 4: return new Color(1.0f, 0.5f, 0.0f);
		}
		return Color.white;
	}

	public static Color GetGradeOutlineColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.8f, 0.8f, 0.8f);
			case 1: return new Color(0.0f, 1.0f, 0.51f);
			case 2: return new Color(0.0f, 0.51f, 1.0f);
			case 3: return new Color(0.63f, 0.0f, 1.0f);
			case 4: return new Color(1.0f, 0.5f, 0.0f);
		}
		return Color.white;
	}

	void OnLoadedEquip(GameObject prefab)
	{
		if (this == null) return;
		if (gameObject == null) return;
		if (gameObject.activeSelf == false) return;

		// 로딩 중에 다른 장비로 Refresh되었다면 이전 로드를 반영하지 않고 그냥 리턴
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)positionIndex);
		if (equipData == null)
			return;
		if (equipData.cachedEquipTableData.prefabAddress != prefab.name)
			return;

		EquipPrefabInfo newEquipPrefabInfo = BattleInstanceManager.instance.GetCachedEquipObject(prefab, equipRootTransform);
		newEquipPrefabInfo.cachedTransform.localPosition = Vector3.zero;
		newEquipPrefabInfo.cachedTransform.Translate(0.0f, newEquipPrefabInfo.pivotOffset, 0.0f, Space.World);
		ShowOutline(true, newEquipPrefabInfo.gameObject, equipData.cachedEquipTableData.grade);
		_currentEquipObject = newEquipPrefabInfo;
		rotateTweenAnimation.DORestart();
	}

	void ShowOutline(bool show, GameObject newObject, int grade)
	{
		if (show)
		{
			QuickOutline quickOutline = newObject.GetComponent<QuickOutline>();
			if (quickOutline == null)
			{
				quickOutline = newObject.AddComponent<QuickOutline>();
				quickOutline.OutlineColor = GetGradeOutlineColor(grade);
				quickOutline.OutlineWidth = 0.9f;
				quickOutline.SetBlink(1.0f);
			}
			quickOutline.enabled = true;
		}
		else
		{
			QuickOutline quickOutline = newObject.GetComponent<QuickOutline>();
			if (quickOutline != null)
				quickOutline.enabled = false;
		}
	}
}