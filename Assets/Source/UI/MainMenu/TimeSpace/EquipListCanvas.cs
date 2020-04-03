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

	public SortButton sortButton;
	SortButton.eSortType _currentSortType;

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
		if (sortButton.onChangedCallback == null)
		{
			int sortType = PlayerPrefs.GetInt("_EquipListSort", 0);
			_currentSortType = (SortButton.eSortType)sortType;
			sortButton.SetSortType(_currentSortType);
			sortButton.onChangedCallback = OnChangedSortType;
		}

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		// CharacterListCanvas 와 비슷한 구조다.
		if (restore)
			return;

		SetInfoCameraMode(true);
		RefreshGrid(true);
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

	void OnChangedSortType(SortButton.eSortType sortType)
	{
		_currentSortType = sortType;
		int sortTypeValue = (int)sortType;
		PlayerPrefs.SetInt("_EquipListSort", sortTypeValue);
		RefreshGrid(false);
	}

	public void RefreshInfo(int equipType)
	{

	}

	List<SwapCanvasListItem> _listSwapCanvasListItem = new List<SwapCanvasListItem>();
	class CharacterInfo
	{
		public ActorTableData actorTableData;
		public CharacterData characterData;
	}
	List<CharacterInfo> _listAllCharacterInfo = new List<CharacterInfo>();
	public void RefreshGrid(bool onEnable)
	{
		
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