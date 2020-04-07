using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class EquipInfoGround : MonoBehaviour
{
	public static EquipInfoGround instance;

	public GameObject altarObject;
	public GameObject emptyAltarObject;
	public Transform equipPositionRootTransform;
	public Transform equipRootTransform;
	public DOTweenAnimation rotateTweenAnimation;
	public ParticleSystem gradeParticleSystem;

	EquipPrefabInfo _currentEquipObject = null;

	void Awake()
	{
		instance = this;
	}

	public void ResetEquipObject()
	{
		_currentEquipData = null;
		if (_currentEquipObject != null)
		{
			_currentEquipObject.gameObject.SetActive(false);
			_currentEquipObject = null;
			rotateTweenAnimation.DOComplete();
		}
		gradeParticleSystem.gameObject.SetActive(false);

		// 오브젝트의 Show 상태와 동일해야하기 때문에 여기서 대신 관리한다.
		EquipListCanvas.instance.detailButtonObject.gameObject.SetActive(false);
	}

	EquipData _currentEquipData;
	public void CreateEquipObject(EquipData equipData)
	{
		// 로딩걸기전에 항상 현재값을 리셋해놓고 로드하기로 한다.
		ResetEquipObject();

		_currentEquipData = equipData;
		AddressableAssetLoadManager.GetAddressableGameObject(equipData.cachedEquipTableData.prefabAddress, "Equip", OnLoadedEquip);
	}

	void OnLoadedEquip(GameObject prefab)
	{
		if (this == null) return;
		if (gameObject == null) return;
		if (gameObject.activeSelf == false) return;

		// 로딩 중에 다른 장비로 Refresh되었다면 이전 로드를 반영하지 않고 그냥 리턴
		if (_currentEquipData == null)
			return;
		if (_currentEquipData.cachedEquipTableData.prefabAddress != prefab.name)
			return;

		EquipPrefabInfo newEquipPrefabInfo = BattleInstanceManager.instance.GetCachedEquipObject(prefab, equipRootTransform);
		newEquipPrefabInfo.cachedTransform.localPosition = Vector3.zero;
		newEquipPrefabInfo.cachedTransform.localRotation = Quaternion.identity;
		newEquipPrefabInfo.cachedTransform.Translate(0.0f, newEquipPrefabInfo.pivotOffset, 0.0f, Space.World);
		_currentEquipObject = newEquipPrefabInfo;
		rotateTweenAnimation.DORestart();

		// 화면에 하나의 오브젝트만 뜨는거라 Altar와 달리 오브젝트 로딩이 끝나야만 파티클 처리를 한다.
		ParticleSystem.MainModule main = gradeParticleSystem.main;
		main.startColor = TimeSpaceAltar.GetGradeParticleColor(_currentEquipData.cachedEquipTableData.grade);
		gradeParticleSystem.gameObject.SetActive(true);
		equipPositionRootTransform.localPosition = Vector3.zero;

		EquipListCanvas.instance.detailButtonObject.gameObject.SetActive(true);
	}

	public bool IsShowEquippedObject() { return gradeParticleSystem.gameObject.activeSelf; }

	public void PlayEquipAnimation()
	{
		equipPositionRootTransform.localPosition = new Vector3(0.0f, 0.7f, 0.0f);
		equipPositionRootTransform.DOLocalMoveY(0.0f, 0.8f).SetEase(Ease.OutBack);
	}



	#region Equip
	public void EnableRotationTweenAnimation(bool enable)
	{
		if (enable)
			rotateTweenAnimation.DOTogglePause();
		else
			rotateTweenAnimation.DOPause();
	}

	public void OnDragRect(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = baseEventData as PointerEventData;
		if (pointerEventData == null)
			return;
		if (_currentEquipObject == null)
			return;

		float ratio = -pointerEventData.delta.x * 2.54f;
		ratio /= Screen.dpi;
		ratio *= 80.0f;
		_currentEquipObject.cachedTransform.Rotate(0.0f, ratio, 0.0f, Space.Self);
	}
	#endregion
}