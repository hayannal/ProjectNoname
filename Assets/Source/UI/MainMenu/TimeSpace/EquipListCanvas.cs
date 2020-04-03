using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using MEC;

public class EquipListCanvas : EquipShowCanvasBase
{
	public static EquipListCanvas instance;

	public EquipSortButton equipSortButton;
	EquipSortButton.eSortType _currentSortType;
	public GameObject emptyEquipObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SwapCanvasListItem>
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

	void OnChangedSortType(EquipSortButton.eSortType sortType)
	{
		_currentSortType = sortType;
		int sortTypeValue = (int)sortType;
		PlayerPrefs.SetInt("_EquipListSort", sortTypeValue);
		RefreshGrid(true);
	}

	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	List<EquipData> _listCurrentEquipData = new List<EquipData>();
	TimeSpaceData.eEquipSlotType _currentEquipType;
	public void RefreshInfo(int equipType, bool onlySort = false)
	{
		_currentEquipType = (TimeSpaceData.eEquipSlotType)equipType;
		RefreshGrid(onlySort);
	}

	void RefreshGrid(bool onlySort)
	{
		for (int i = 0; i < _listSwapCanvasListItem.Count; ++i)
			_listSwapCanvasListItem[i].gameObject.SetActive(false);
		_listSwapCanvasListItem.Clear();

		if (onlySort == false)
		{
			_listCurrentEquipData.Clear();
			for (int i = 0; i < TimeSpaceData.instance.listEquipData.Count; ++i)
			{
				if (_currentEquipType != TimeSpaceData.eEquipSlotType.Amount)
				{
					if (_currentEquipType != (TimeSpaceData.eEquipSlotType)TimeSpaceData.instance.listEquipData[i].cachedEquipTableData.equipType)
						continue;
				}
				_listCurrentEquipData.Add(TimeSpaceData.instance.listEquipData[i]);
			}
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
	}

	public void OnClickListItem(EquipData equipData)
	{
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