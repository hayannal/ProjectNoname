using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class EquipInfoGround : MonoBehaviour
{
	public static EquipInfoGround instance;

	public MeshRenderer brazier01Renderer;
	public MeshRenderer brazier02Renderer;
	public Material brazier01Material;
	public Material brazier02Material;
	public Material brazier01MaterialForDiff;
	public Material brazier02MaterialForDiff;

	public Transform equipPositionRootTransform;
	public Transform equipRootTransform;
	public DOTweenAnimation rotateTweenAnimation;
	public ParticleSystem gradeParticleSystem;
	public Transform gradeParticleTransform;

	EquipPrefabInfo _currentEquipObject = null;

	Vector3 _defaultGradeParticleScale;
	void Awake()
	{
		instance = this;
		_defaultGradeParticleScale = gradeParticleTransform.localScale;
	}

#if UNITY_EDITOR
	Transform _diffEquipRootTransform;
	void Start()
	{
		_diffEquipRootTransform = rotateTweenAnimation.transform.Find("DiffEquipRootForEditor");
	}
#endif

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
		if (EquipListCanvas.instance != null && EquipListCanvas.instance.gameObject.activeSelf)
			EquipListCanvas.instance.detailButtonObject.gameObject.SetActive(false);
	}

	EquipData _currentEquipData;
	bool _playEquipAnimation;
	public void CreateEquipObject(EquipData equipData, bool playEquipAnimation = false)
	{
		// 중복 호출 되더라도 두번 생성 안하려면 여기서 검사하는게 맞다.
		if (_currentEquipData == equipData)
			return;

		// 로딩걸기전에 항상 현재값을 리셋해놓고 로드하기로 한다.
		ResetEquipObject();

		_currentEquipData = equipData;
		_playEquipAnimation = playEquipAnimation;
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

		_currentEquipObject = RefreshInfo(prefab, _currentEquipData.cachedEquipTableData.grade);
		rotateTweenAnimation.DORestart();

		if (_playEquipAnimation)
			PlayEquipAnimation();

		if (EquipListCanvas.instance != null && EquipListCanvas.instance.gameObject.activeSelf)
			EquipListCanvas.instance.detailButtonObject.gameObject.SetActive(true);
	}

	EquipPrefabInfo RefreshInfo(GameObject prefab, int grade)
	{
		// DropGacha나 Altar와 달리 화면 중앙에서 보는거라 pivotOffset을 사용하진 않고
		// 대신 미세조정을 위해 infoPivotAddOffset을 사용한다.
		EquipPrefabInfo newEquipPrefabInfo = BattleInstanceManager.instance.GetCachedEquipObject(prefab, equipRootTransform);
		newEquipPrefabInfo.cachedTransform.localPosition = Vector3.zero;
		newEquipPrefabInfo.cachedTransform.localRotation = Quaternion.identity;
		newEquipPrefabInfo.cachedTransform.Translate(0.0f, newEquipPrefabInfo.infoPivotAddOffset, 0.0f, Space.World);
		equipPositionRootTransform.localPosition = Vector3.zero;

		// 화면에 하나의 오브젝트만 뜨는거라 Altar와 달리 오브젝트 로딩이 끝나야만 파티클 처리를 한다.
		ParticleSystem.MainModule main = gradeParticleSystem.main;
		main.startColor = TimeSpaceAltar.GetGradeParticleColor(grade);
		gradeParticleSystem.gameObject.SetActive(true);

		return newEquipPrefabInfo;
	}

	public bool IsShowEquippedObject() { return gradeParticleSystem.gameObject.activeSelf; }

	void PlayEquipAnimation()
	{
		equipPositionRootTransform.localPosition = new Vector3(0.0f, 0.7f, 0.0f);
		equipPositionRootTransform.DOLocalMoveY(0.0f, 0.8f).SetEase(Ease.OutBack);
	}

	bool _scaleDownGradeParticle = false;
	public void ScaleDownGradeParticle(bool down)
	{
		if (_scaleDownGradeParticle == down)
			return;

		if (down)
			gradeParticleTransform.DOScaleZ(0.05f, 1.5f).SetEase(Ease.OutQuad);
		else
			gradeParticleTransform.DOScaleZ(_defaultGradeParticleScale.z, 1.0f).SetEase(Ease.OutQuad);

		_scaleDownGradeParticle = down;
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
#if UNITY_EDITOR
		if (_diffEquipRootTransform != null && _diffEquipRootTransform.childCount > 0)
		{
			Transform childTransform = _diffEquipRootTransform.GetChild(0);
			if (childTransform != null)
			{
				float ratioForEditor = -pointerEventData.delta.x * 2.54f;
				ratioForEditor /= Screen.dpi;
				ratioForEditor *= 80.0f;
				childTransform.Rotate(0.0f, ratioForEditor, 0.0f, Space.Self);
			}
		}
#endif
		if (_currentEquipObject == null)
			return;

		float ratio = -pointerEventData.delta.x * 2.54f;
		ratio /= Screen.dpi;
		ratio *= 80.0f;
		_currentEquipObject.cachedTransform.Rotate(0.0f, ratio, 0.0f, Space.Self);
	}
	#endregion



	#region Diff Equip Item
	// 별도의 공간으로 뺄까 하다가 어차피 그거나 이거나 작업량은 비슷할거 같아서 차라리 같은 공간에서 바꿔치기 하는 형태로 가기로 한다.
	public bool diffMode { get; set; }
	public void ChangeDiffMode(EquipData diffEquipData)
	{
		if (diffMode)
			return;

		// 이미 로드는 되어있는 상태니 바로 콜백이 올거다.
		AddressableAssetLoadManager.GetAddressableGameObject(diffEquipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
		{
			// 기존 오브젝트는 복구시 다시 만들테니 날린다.
			if (_currentEquipObject != null)
			{
				_currentEquipObject.gameObject.SetActive(false);
				_currentEquipObject = null;
			}

			// 오브젝트 셋팅을 하고
			_currentEquipObject = RefreshInfo(prefab, diffEquipData.cachedEquipTableData.grade);

			// 회전할때는 괜찮았는데 초기화 각도로 멈춰있으니 안예쁘게 나와서 45도 돌려두기로 한다.
			rotateTweenAnimation.transform.localRotation = Quaternion.Euler(0.0f, 45.0f, 0.0f);
			// 본체의 회전도 바꿔야 더 예쁘게 나와서 이것도 45도 추가로 돌려놓는다. 이게 강화창에서도 통할진 모르겠다.
			_currentEquipObject.cachedTransform.localRotation = Quaternion.Euler(0.0f, 45.0f, 0.0f);

			// 제단의 모양을 비장착 아이템용으로 전환한다.
			brazier01Renderer.material = brazier01MaterialForDiff;
			brazier02Renderer.material = brazier02MaterialForDiff;
		});

		diffMode = true;
	}

	public void RestoreDiffMode()
	{
		if (diffMode == false)
			return;

		// _currentEquipData는 그대로 남겨놨으니 이 정보로 복구하면 된다.
		_currentEquipObject.gameObject.SetActive(false);
		brazier01Renderer.material = brazier01Material;
		brazier02Renderer.material = brazier02Material;

		if (_currentEquipData != null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(_currentEquipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
			{
				_currentEquipObject = RefreshInfo(prefab, _currentEquipData.cachedEquipTableData.grade);
			});
		}

		diffMode = false;
	}
	#endregion
}