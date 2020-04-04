using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TimeSpaceAltar : MonoBehaviour
{
	public int positionIndex;
	public Transform equipRootTransform;
	public DOTweenAnimation rotateTweenAnimation;

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
		if ((diff.x * diff.x + diff.z * diff.z) > 2.0f * 2.0f)
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

	bool _wait = false;
	EquipPrefabInfo _currentEquipObject = null;
	void RefreshEquipObject()
	{
		if (_wait)
			return;

		EquipData equipData = TimeSpaceData.instance.GetEquipDataByType((TimeSpaceData.eEquipSlotType)positionIndex);
		if (equipData == null)
		{
			if (_currentEquipObject != null)
			{
				_currentEquipObject.gameObject.SetActive(false);
				_currentEquipObject = null;
				rotateTweenAnimation.DOComplete();
			}
			return;
		}

		_wait = true;
		AddressableAssetLoadManager.GetAddressableGameObject(equipData.cachedEquipTableData.prefabAddress, "Equip", OnLoadedEquip);
	}

	void OnLoadedEquip(GameObject prefab)
	{
		_wait = false;
		if (this == null) return;
		if (gameObject == null) return;
		if (gameObject.activeSelf == false) return;

		EquipPrefabInfo newEquipPrefabInfo = BattleInstanceManager.instance.GetCachedEquipObject(prefab, equipRootTransform);
		newEquipPrefabInfo.cachedTransform.localPosition = Vector3.zero;
		newEquipPrefabInfo.cachedTransform.Translate(0.0f, newEquipPrefabInfo.pivotOffset, 0.0f, Space.World);
		rotateTweenAnimation.DORestart();
	}
}