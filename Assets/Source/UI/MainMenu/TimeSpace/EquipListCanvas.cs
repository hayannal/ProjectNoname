using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipListCanvas : EquipShowCanvasBase
{
	public static EquipListCanvas instance;

	public EquipTypeButton[] equipTypeButtonList;
	public EquipListStatusInfo diffStatusInfo;
	public EquipListStatusInfo equippedStatusInfo;
	public GameObject reopenEquippedStatusInfoTextObject;
	bool _closeEquippedStatusInfoByUser;
	public GameObject detailButtonObject;

	public EquipSortButton equipSortButton;
	EquipSortButton.eSortType _currentSortType;
	public Text countText;
	public GameObject emptyEquipObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		if (equipSortButton.onChangedCallback == null)
		{
			int sortType = PlayerPrefs.GetInt("_EquipListSort", 0);
			_currentSortType = (EquipSortButton.eSortType)sortType;
			equipSortButton.SetSortType(_currentSortType);
			equipSortButton.onChangedCallback = OnChangedSortType;
		}

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		reopenEquippedStatusInfoTextObject.gameObject.SetActive(false);

		// CharacterListCanvas 와 비슷한 구조다.
		if (restore)
			return;

		SetInfoCameraMode(true);

		// 캐릭터리스트와 달리 장비종류별로 Grid가 달라질 수 있어서 외부에서 RefreshInfo 함수을 통해서 처리하기로 한다.
		//RefreshGrid(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		// CharacterListCanvas 와 비슷한 구조다.
		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		SetInfoCameraMode(false);
	}

	public void RefreshInfo(int positionIndex)
	{
		_forceRefresh = true;
		OnSelectEquipType(positionIndex);
		_forceRefresh = false;

		// 외부에서 제단 인디케이터를 클릭해서 들어온거니 장착이 되어있다면 자동으로 정보창을 보여준다.
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)positionIndex);
		if (equipData != null)
			RefreshEquippedStatusInfo(equipData);
		_closeEquippedStatusInfoByUser = false;
	}

	#region EquipTypeButton
	bool _forceRefresh = false;
	public void OnSelectEquipType(int positionIndex)
	{
		if (_forceRefresh == false &&_currentEquipType == (TimeSpaceData.eEquipSlotType)positionIndex)
			return;

		for (int i = 0; i < equipTypeButtonList.Length; ++i)
			equipTypeButtonList[i].selected = (equipTypeButtonList[i].positionIndex == positionIndex);

		_currentEquipType = (TimeSpaceData.eEquipSlotType)positionIndex;
		RefreshGrid(true, true);
		RefreshEquippedObject();

		// 탭바뀔땐 비교창 하이드
		diffStatusInfo.gameObject.SetActive(false);
		// 탭이 바뀔때 장착된 아이템이 있고 유저가 닫기버튼을 직접 누르지 않은 상태라면
		if (_closeEquippedStatusInfoByUser == false)
		{
			EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)positionIndex);
			if (equipData != null)
				RefreshEquippedStatusInfo(equipData);
			else
				equippedStatusInfo.gameObject.SetActive(false);
		}
	}
	#endregion

	void RefreshEquippedObject(bool playEquipAnimation = false)
	{
		// 빠르게 탭을 바꾸다보면 로딩중에 취소되고 다음 템을 로드할수도 있을거다.
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType(_currentEquipType);
		if (equipData == null)
		{
			EquipInfoGround.instance.ResetEquipObject();
			return;
		}

		EquipInfoGround.instance.CreateEquipObject(equipData, playEquipAnimation);
	}

	void OnChangedSortType(EquipSortButton.eSortType sortType)
	{
		_currentSortType = sortType;
		int sortTypeValue = (int)sortType;
		PlayerPrefs.SetInt("_EquipListSort", sortTypeValue);
		RefreshGrid(false, false);
	}

	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	List<EquipData> _listCurrentEquipData = new List<EquipData>();
	TimeSpaceData.eEquipSlotType _currentEquipType;
	public void RefreshGrid(bool refreshInventory, bool resetSelected)
	{
		// 강화나 옵션탭에서 장비를 업하면서 재료로 소모했다면 인벤토리는 리프레쉬 하되 현재 선택된건 유지해야한다.
		// 이럴때 대비해서 몇가지 예외처리 해둔다.
		if (refreshInventory && resetSelected == false)
		{
			if (equippedStatusInfo.gameObject.activeSelf)
				equippedStatusInfo.RefreshStatus();
		}

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		_listEquipCanvasListItem.Clear();

		if (refreshInventory)
		{
			_listCurrentEquipData.Clear();
			List<EquipData> listEquipData = TimeSpaceData.instance.GetEquipListByType(_currentEquipType);
			for (int i = 0; i < listEquipData.Count; ++i)
			{
				if (TimeSpaceData.instance.IsEquipped(listEquipData[i]))
					continue;
				_listCurrentEquipData.Add(listEquipData[i]);
			}

			// 장착된 템을 인벤에서 못찾는다면 아마도 강화이전을 통해 장착된걸 재료로 써버려서 삭제가 되었을 경우일거다.
			// 이럴땐 새로 장착된 템이 있는지 보고 있으면 갱신하고 없으면 닫기로 한다.
			if (equippedStatusInfo.gameObject.activeSelf)
			{
				EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType(_currentEquipType);
				if (equipData != null)
					RefreshEquippedStatusInfo(equipData);
				else
					equippedStatusInfo.gameObject.SetActive(false);
			}

			// 인벤토리를 리프레쉬 하는데 열려있는 정보창의 equipData가 삭제되었다면 템을 삭제한 후 리프레쉬 한걸거다. 이땐 정보창을 강제로 닫아준다.
			if (diffStatusInfo.gameObject.activeSelf && _listCurrentEquipData.Contains(diffStatusInfo.equipData) == false)
			{
				diffStatusInfo.gameObject.SetActive(false);
				RefreshEquippedObject();
			}
			if (_selectedEquipData != null && _listCurrentEquipData.Contains(_selectedEquipData) == false)
				_selectedEquipData = null;

			countText.text = string.Format(TimeSpaceData.instance.IsInventoryVisualMax() ? "<color=#FF2200>{0}</color> / {1}" : "{0} / {1}", TimeSpaceData.instance.inventoryItemCount, TimeSpaceData.InventoryVisualMax);
		}
		if (_listCurrentEquipData.Count == 0)
		{
			emptyEquipObject.SetActive(true);
			return;
		}
		emptyEquipObject.SetActive(false);

		switch (_currentSortType)
		{
			case EquipSortButton.eSortType.Grade:
				_listCurrentEquipData.Sort(equipSortButton.comparisonGrade);
				break;
			case EquipSortButton.eSortType.Attack:
				_listCurrentEquipData.Sort(equipSortButton.comparisonAttack);
				break;
			case EquipSortButton.eSortType.Enhance:
				_listCurrentEquipData.Sort(equipSortButton.comparisonEnhance);
				break;
		}

		for (int i = 0; i < _listCurrentEquipData.Count; ++i)
		{
			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(_listCurrentEquipData[i], OnClickListItem);
			_listEquipCanvasListItem.Add(equipCanvasListItem);
		}

		// 인벤도 리프레쉬하지 않고 selected아이템도 해제하지 않는거면 정렬같이 Grid 순서만 바꾸는 상황일거다.
		// 이땐 아예 갱신하지 않는다.
		if (refreshInventory == false && resetSelected == false)
			return;

		if (resetSelected)
			OnClickListItem(null);
		else
			OnClickListItem(_selectedEquipData);
	}

	EquipData _selectedEquipData;
	public void OnClickListItem(EquipData equipData)
	{
		_selectedEquipData = equipData;

		string selectedUniqueId = "";
		if (_selectedEquipData != null)
			selectedUniqueId = _selectedEquipData.uniqueId;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].ShowSelectObject(_listEquipCanvasListItem[i].equipData.uniqueId == selectedUniqueId);

		if (_selectedEquipData == null)
			return;

		RefreshDiffStatusInfo(equipData);
	}

	void RefreshDiffStatusInfo(EquipData equipData)
	{
		if (diffStatusInfo.gameObject.activeSelf)
			diffStatusInfo.gameObject.SetActive(false);
		diffStatusInfo.RefreshInfo(equipData, false);
		diffStatusInfo.gameObject.SetActive(true);
		detailButtonObject.gameObject.SetActive(false);
	}

	void RefreshEquippedStatusInfo(EquipData equipData)
	{
		if (equippedStatusInfo.gameObject.activeSelf)
			equippedStatusInfo.gameObject.SetActive(false);
		equippedStatusInfo.RefreshInfo(equipData, true);
		equippedStatusInfo.gameObject.SetActive(true);
	}

	public void OnCloseDiffStatusInfo()
	{
		OnClickListItem(null);

		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType(_currentEquipType);
		if (equipData == null)
			return;
		if (EquipInfoGround.instance.IsShowEquippedObject() == false)
			return;
		detailButtonObject.gameObject.SetActive(true);
	}

	public void OnCloseEquippedStatusInfo()
	{
		reopenEquippedStatusInfoTextObject.SetActive(true);
		_closeEquippedStatusInfoByUser = true;
	}

	public void RefreshSelectedItem()
	{
		if (_selectedEquipData == null)
			return;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			if (_listEquipCanvasListItem[i].equipData.uniqueId == _selectedEquipData.uniqueId)
				_listEquipCanvasListItem[i].RefreshStatus();
		}
	}

	public void OnEquip(EquipData equipData)
	{
		if (_selectedEquipData != equipData)
		{
			// 선택하지 않은걸 장착할 수 있나?
			return;
		}

		// 장착시 내려오는 애니 적용
		RefreshEquippedObject(true);
		RefreshGrid(true, true);

		// 모든 비교창을 닫는다.
		diffStatusInfo.gameObject.SetActive(false);
		equippedStatusInfo.gameObject.SetActive(false);

		// 밖에 있는 시공간 제단을 업데이트 해줘야한다.
		int positionIndex = equipData.cachedEquipTableData.equipType;
		TimeSpaceGround.instance.timeSpaceAltarList[positionIndex].RefreshEquipObject();
	}

	public void OnUnequip(EquipData equipData)
	{
		RefreshGrid(true, true);
		RefreshEquippedObject();

		// 모든 비교창을 닫는다.
		diffStatusInfo.gameObject.SetActive(false);
		equippedStatusInfo.gameObject.SetActive(false);

		// 밖에 있는 시공간 제단을 업데이트 해줘야한다.
		int positionIndex = equipData.cachedEquipTableData.equipType;
		TimeSpaceGround.instance.timeSpaceAltarList[positionIndex].RefreshEquipObject();
	}

	

	public void OnClickEquippedInfoButton()
	{
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType(_currentEquipType);
		if (equipData == null)
			return;

		if (equippedStatusInfo.gameObject.activeSelf)
			return;

		reopenEquippedStatusInfoTextObject.gameObject.SetActive(false);
		RefreshEquippedStatusInfo(equipData);

		// 장비 오브젝트를 탭해서 켜면 플래그를 초기화시킨다.
		_closeEquippedStatusInfoByUser = false;
	}

	public void OnClickDetailButton()
	{
		// 현재 보여지고 있는 장착된 템이라서 카메라만 옮겨주면 될거다.
		UIInstanceManager.instance.ShowCanvasAsync("EquipInfoDetailCanvas", null);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		LobbyCanvas.Home();
	}
}