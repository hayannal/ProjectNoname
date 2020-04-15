using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipInfoGrowthCanvas : MonoBehaviour
{
	public static EquipInfoGrowthCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject[] innerMenuPrefabList;
	public Transform innerMenuRootTransform;
	public MenuButton[] menuButtonList;
	public GameObject menuRootObject;

	public Text selectMaterialText;
	public Text selectCountText;
	public Text emptyText;
	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public EquipListStatusInfo materialSmallStatusInfo;

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

		// 현재 두번째 메뉴가 연구레벨 일정렙 이후에 개방되기 때문에 시작할때 한번씩 체크해야한다. 캐릭터 메뉴와 달리 연구레벨 한번만 검사하면 된다.
		RefreshOpenMenuSlot();
	}

	public void RefreshOpenMenuSlot()
	{
		menuRootObject.SetActive(ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.EquipOption));
	}

	void OnEnable()
	{
		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(false);
		}
		_lastIndex = -1;

		if (EquipInfoGround.instance.diffMode)
			EquipInfoGround.instance.RestoreDiffMode();

		materialSmallStatusInfo.gameObject.SetActive(false);

		StackCanvas.Pop(gameObject);
	}

	float _materialSmallStatusInfoShowRemainTime;
	void Update()
	{
		if (_materialSmallStatusInfoShowRemainTime > 0.0f)
		{
			_materialSmallStatusInfoShowRemainTime -= Time.deltaTime;
			if (_materialSmallStatusInfoShowRemainTime <= 0.0f)
			{
				_materialSmallStatusInfoShowRemainTime = 0.0f;
				materialSmallStatusInfo.gameObject.SetActive(false);
			}
		}
	}

	EquipData _equipData;
	public void RefreshInfo(int menuIndex, EquipData equipData)
	{
		_equipData = equipData;
		OnValueChangedToggle(menuIndex);
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



	#region Menu Button
	public void OnClickMenuButton1() { OnValueChangedToggle(0); }
	public void OnClickMenuButton2() { OnValueChangedToggle(1); }

	List<Transform> _listMenuTransform = new List<Transform>();
	int _lastIndex = -1;
	void OnValueChangedToggle(int index)
	{
		if (index == _lastIndex)
			return;

		if (_listMenuTransform.Count == 0)
		{
			for (int i = 0; i < menuButtonList.Length; ++i)
				_listMenuTransform.Add(null);
		}

		if (_listMenuTransform[index] == null && innerMenuPrefabList[index] != null)
		{
			GameObject newObject = Instantiate<GameObject>(innerMenuPrefabList[index], innerMenuRootTransform);
			_listMenuTransform[index] = newObject.transform;
		}

		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			menuButtonList[i].isOn = (index == i);
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(index == i);
		}
		TooltipCanvas.Hide();

		_lastIndex = index;

		switch (index)
		{
			case 0:
				EquipEnhanceCanvas.instance.RefreshInfo(_equipData);
				break;
			case 1:
				EquipOptionCanvas.instance.RefreshInfo(_equipData);
				break;
		}
	}
	#endregion


	public enum eGrowthGridType
	{
		Enhance,	// 강화
		Transfer,	// 강화 전수
		Transmute,	// 옵션 변경
		Amplify,	// 옵션 증폭
	}

	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	List<EquipData> _listCurrentEquipData = new List<EquipData>();
	bool _multiSelectMode = false;
	int _multiSelectMax = 0;
	int MAX_SELECT_COUNT = 20;
	public void RefreshGrid(eGrowthGridType gridType, bool amplifyMain = false)
	{
		_listMultiSelectUniqueId.Clear();
		_listMultiSelectEquipData.Clear();
		_selectedEquipData = null;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		_listEquipCanvasListItem.Clear();

		_listCurrentEquipData.Clear();
		switch (gridType)
		{
			case eGrowthGridType.Enhance: selectMaterialText.SetLocalizedText(UIString.instance.GetString("EquipUI_MaterialForEnhance")); break;
			case eGrowthGridType.Transfer: selectMaterialText.SetLocalizedText(UIString.instance.GetString("EquipUI_MaterialForTransfer")); break;
			case eGrowthGridType.Transmute: selectMaterialText.SetLocalizedText(UIString.instance.GetString("EquipUI_MaterialForTransmute")); break;
			case eGrowthGridType.Amplify: selectMaterialText.SetLocalizedText(UIString.instance.GetString("EquipUI_MaterialForAmplify")); break;
		}
		List<EquipData> listEquipData = null;
		switch (gridType)
		{
			case eGrowthGridType.Enhance:
				_multiSelectMode = true;
				_multiSelectMax = MAX_SELECT_COUNT;
				for (int i = 0; i < (int)TimeSpaceData.eEquipSlotType.Amount; ++i)
				{
					listEquipData = TimeSpaceData.instance.GetEquipListByType((TimeSpaceData.eEquipSlotType)i);
					for (int j = 0; j < listEquipData.Count; ++j)
					{
						if (TimeSpaceData.instance.IsEquipped(listEquipData[j]))
							continue;
						if (listEquipData[j].isLock)
							continue;
						if (_equipData.uniqueId == listEquipData[j].uniqueId)
							continue;
						_listCurrentEquipData.Add(listEquipData[j]);
					}
				}
				break;
			case eGrowthGridType.Transfer:
				_multiSelectMode = false;
				listEquipData = TimeSpaceData.instance.GetEquipListByType((TimeSpaceData.eEquipSlotType)_equipData.cachedEquipTableData.equipType);
				for (int i = 0; i < listEquipData.Count; ++i)
				{
					// 편의상 장착이 된 것도 전수받을 수 있어야하지 않나. 그러면서 자동으로 장착도 새로 적용. 장착 중 표시도 해준다.
					//if (TimeSpaceData.instance.IsEquipped(listEquipData[i]))
					//	continue;
					if (listEquipData[i].isLock)
						continue;
					if (_equipData.enhanceLevel == 0)
						continue;
					if (_equipData.uniqueId == listEquipData[i].uniqueId)
						continue;
					if (_equipData.cachedEquipTableData.grade < listEquipData[i].cachedEquipTableData.grade)
						continue;
					if (_equipData.cachedEquipTableData.grade == listEquipData[i].cachedEquipTableData.grade && _equipData.enhanceLevel >= listEquipData[i].enhanceLevel)
						continue;
					_listCurrentEquipData.Add(listEquipData[i]);
				}
				break;
			case eGrowthGridType.Transmute:
				_multiSelectMode = false;
				for (int i = 0; i < (int)TimeSpaceData.eEquipSlotType.Amount; ++i)
				{
					listEquipData = TimeSpaceData.instance.GetEquipListByType((TimeSpaceData.eEquipSlotType)i);
					for (int j = 0; j < listEquipData.Count; ++j)
					{
						if (TimeSpaceData.instance.IsEquipped(listEquipData[j]))
							continue;
						if (listEquipData[j].isLock)
							continue;
						if (_equipData.uniqueId == listEquipData[j].uniqueId)
							continue;
						if (_equipData.cachedEquipTableData.grade != listEquipData[j].cachedEquipTableData.grade)
							continue;
						_listCurrentEquipData.Add(listEquipData[j]);
					}
				}
				break;
			case eGrowthGridType.Amplify:
				_multiSelectMode = true;
				_multiSelectMax = MAX_SELECT_COUNT;
				listEquipData = TimeSpaceData.instance.GetEquipListByType((TimeSpaceData.eEquipSlotType)_equipData.cachedEquipTableData.equipType);
				for (int i = 0; i < listEquipData.Count; ++i)
				{
					if (TimeSpaceData.instance.IsEquipped(listEquipData[i]))
						continue;
					if (listEquipData[i].isLock)
						continue;
					if (_equipData.uniqueId == listEquipData[i].uniqueId)
						continue;
					if (_equipData.cachedEquipTableData.equipId != listEquipData[i].cachedEquipTableData.equipId)
						continue;
					_listCurrentEquipData.Add(listEquipData[i]);
				}
				break;
		}
		RefreshCountText();

		if (_listCurrentEquipData.Count == 0)
		{
			switch (gridType)
			{
				case eGrowthGridType.Enhance: emptyText.SetLocalizedText(UIString.instance.GetString("EquipUI_NoMaterialForEnhance")); break;
				case eGrowthGridType.Transfer: emptyText.SetLocalizedText(UIString.instance.GetString("EquipUI_NoMaterialForTransfer")); break;
				case eGrowthGridType.Transmute: emptyText.SetLocalizedText(UIString.instance.GetString("EquipUI_NoMaterialForTransmute")); break;
				case eGrowthGridType.Amplify: emptyText.SetLocalizedText(UIString.instance.GetString(amplifyMain ? "EquipUI_NoMaterialForAmplifyMain" : "EquipUI_NoMaterialForAmplifyRandom")); break;
			}
			emptyText.gameObject.SetActive(true);
			return;
		}
		emptyText.gameObject.SetActive(false);

		switch (gridType)
		{
			case eGrowthGridType.Enhance:
			case eGrowthGridType.Transmute:
			case eGrowthGridType.Amplify:
				_listCurrentEquipData.Sort(delegate (EquipData x, EquipData y)
				{
					if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
					{
						if (x.cachedEquipTableData.grade > y.cachedEquipTableData.grade) return -1;
						else if (x.cachedEquipTableData.grade < y.cachedEquipTableData.grade) return 1;
						if (x.mainStatusValue > y.mainStatusValue) return -1;
						else if (x.mainStatusValue < y.mainStatusValue) return 1;
						if (x.enhanceLevel > y.enhanceLevel) return -1;
						else if (x.enhanceLevel < y.enhanceLevel) return 1;
					}
					return 0;
				});
				break;
			case eGrowthGridType.Transfer:
				_listCurrentEquipData.Sort(delegate (EquipData x, EquipData y)
				{
					if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
					{
						if (x.mainStatusValue > y.mainStatusValue) return -1;
						else if (x.mainStatusValue < y.mainStatusValue) return 1;
						if (x.cachedEquipTableData.grade > y.cachedEquipTableData.grade) return -1;
						else if (x.cachedEquipTableData.grade < y.cachedEquipTableData.grade) return 1;
						if (x.enhanceLevel > y.enhanceLevel) return -1;
						else if (x.enhanceLevel < y.enhanceLevel) return 1;
					}
					return 0;
				});
				break;
		}

		for (int i = 0; i < _listCurrentEquipData.Count; ++i)
		{
			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(_listCurrentEquipData[i], OnClickListItem);
			_listEquipCanvasListItem.Add(equipCanvasListItem);
		}
	}

	void RefreshCountText()
	{
		if (_multiSelectMode)
			selectCountText.text = string.Format("{0} / {1}", _listMultiSelectEquipData.Count, _multiSelectMax);
		else
			selectCountText.text = string.Format("{0} / 1", (_selectedEquipData == null) ? 0 : 1);
	}
	
	public void OnClickListItem(EquipData equipData)
	{
		materialSmallStatusInfo.RefreshInfo(equipData, false);
		materialSmallStatusInfo.gameObject.SetActive(false);
		materialSmallStatusInfo.gameObject.SetActive(true);
		_materialSmallStatusInfoShowRemainTime = 2.0f;

		if (_multiSelectMode)
			OnMultiSelectListItem(equipData);
		else
			OnSelectListItem(equipData);
	}

	List<string> _listMultiSelectUniqueId = new List<string>();
	List<EquipData> _listMultiSelectEquipData = new List<EquipData>();
	public void OnMultiSelectListItem(EquipData equipData)
	{
		bool contains = _listMultiSelectUniqueId.Contains(equipData.uniqueId);
		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			if (_listEquipCanvasListItem[i].equipData.uniqueId == equipData.uniqueId)
				_listEquipCanvasListItem[i].ShowSelectObject(!contains);
		}
		if (contains)
		{
			_listMultiSelectUniqueId.Remove(equipData.uniqueId);
			_listMultiSelectEquipData.Remove(equipData);
		}
		else
		{
			_listMultiSelectUniqueId.Add(equipData.uniqueId);
			_listMultiSelectEquipData.Add(equipData);
		}

		RefreshCountText();

		switch (_lastIndex)
		{
			case 0: EquipEnhanceCanvas.instance.OnMultiSelectMaterial(_listMultiSelectEquipData); break;
			case 1: EquipOptionCanvas.instance.OnMultiSelectMaterial(_listMultiSelectEquipData); break;
		}
	}

	EquipData _selectedEquipData;
	public void OnSelectListItem(EquipData equipData)
	{
		_selectedEquipData = equipData;

		string selectedUniqueId = "";
		if (_selectedEquipData != null)
			selectedUniqueId = _selectedEquipData.uniqueId;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].ShowSelectObject(_listEquipCanvasListItem[i].equipData.uniqueId == selectedUniqueId);

		RefreshCountText();

		if (_selectedEquipData == null)
			return;

		switch (_lastIndex)
		{
			case 0: EquipEnhanceCanvas.instance.OnSelectMaterial(_selectedEquipData); break;
			case 1: EquipOptionCanvas.instance.OnSelectMaterial(_selectedEquipData); break;
		}
	}
}