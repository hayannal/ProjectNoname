using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipSellCanvas : EquipShowCanvasBase
{
	public static EquipSellCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject selectTextObject;
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
	}

	void OnEnable()
	{
		// EquipListCanvas 에서 가져와서 EquipInfoGrowthCanvas와 합쳐서 사용한다.
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		SetInfoCameraMode(true);

		RefreshGrid();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		materialSmallStatusInfo.gameObject.SetActive(false);

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

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].ShowAlarm(false);
		TimeSpaceData.instance.ResetNewEquip();
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


	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	List<EquipData> _listCurrentEquipData = new List<EquipData>();
	int _multiSelectMax = 0;
	int MAX_SELECT_COUNT = 20;
	public void RefreshGrid()
	{
		_listMultiSelectUniqueId.Clear();
		_listMultiSelectEquipData.Clear();

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			_listEquipCanvasListItem[i].ShowAlarm(false);
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		}
		_listEquipCanvasListItem.Clear();

		_listCurrentEquipData.Clear();
		List<EquipData> listEquipData = null;
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
				_listCurrentEquipData.Add(listEquipData[j]);
			}
		}
		RefreshCountText();
		emptyText.gameObject.SetActive(_listCurrentEquipData.Count == 0);
		selectTextObject.SetActive(false);

		_listCurrentEquipData.Sort(delegate (EquipData x, EquipData y)
		{
			if (x.newEquip && y.newEquip == false) return -1;
			else if (x.newEquip == false && y.newEquip) return 1;
			if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
			{
				if (x.cachedEquipTableData.grade < y.cachedEquipTableData.grade) return -1;
				else if (x.cachedEquipTableData.grade > y.cachedEquipTableData.grade) return 1;
				if (x.enhanceLevel < y.enhanceLevel) return -1;
				else if (x.enhanceLevel > y.enhanceLevel) return 1;
				if (x.mainStatusValue < y.mainStatusValue) return -1;
				else if (x.mainStatusValue > y.mainStatusValue) return 1;
			}
			return 0;
		});

		for (int i = 0; i < _listCurrentEquipData.Count; ++i)
		{
			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(_listCurrentEquipData[i], OnClickListItem);
			if (_listCurrentEquipData[i].newEquip) equipCanvasListItem.ShowAlarm(true);
			_listEquipCanvasListItem.Add(equipCanvasListItem);
		}
	}

	void RefreshCountText()
	{
		selectTextObject.SetActive(_listMultiSelectEquipData.Count > 0);
		if (_listMultiSelectEquipData.Count == 0)
			selectCountText.text = string.Format(TimeSpaceData.instance.IsInventoryVisualMax() ? "<color=#FF2200>{0}</color> / {1}" : "{0} / {1}", TimeSpaceData.instance.inventoryItemCount, TimeSpaceData.InventoryVisualMax);
		else
			selectCountText.text = string.Format("{0} / {1}", _listMultiSelectEquipData.Count, _multiSelectMax);
	}

	public void OnClickListItem(EquipData equipData)
	{
		materialSmallStatusInfo.RefreshInfo(equipData, false);
		materialSmallStatusInfo.gameObject.SetActive(false);
		materialSmallStatusInfo.gameObject.SetActive(true);
		_materialSmallStatusInfoShowRemainTime = 2.0f;

		OnMultiSelectListItem(equipData);
	}

	List<string> _listMultiSelectUniqueId = new List<string>();
	List<EquipData> _listMultiSelectEquipData = new List<EquipData>();
	public List<EquipData> listMultiSelectEquipData { get { return _listMultiSelectEquipData; } }
	public void OnMultiSelectListItem(EquipData equipData)
	{
		bool contains = _listMultiSelectUniqueId.Contains(equipData.uniqueId);
		if (contains == false && _listMultiSelectEquipData.Count >= MAX_SELECT_COUNT)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_CannotSelectMore"), 1.0f);
			return;
		}

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
		OnMultiSelectMaterial();
	}

	public void OnAutoSelect(List<int> listGrade, bool includeEnhanced)
	{
		_listMultiSelectUniqueId.Clear();
		_listMultiSelectEquipData.Clear();

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			bool result = true;
			if (includeEnhanced == false && _listEquipCanvasListItem[i].equipData.enhanceLevel > 0)
				result = false;

			if (result && listGrade.Contains(_listEquipCanvasListItem[i].equipData.cachedEquipTableData.grade) == false)
				result = false;

			if (result)
			{
				_listMultiSelectUniqueId.Add(_listEquipCanvasListItem[i].equipData.uniqueId);
				_listMultiSelectEquipData.Add(_listEquipCanvasListItem[i].equipData);
			}
			_listEquipCanvasListItem[i].ShowSelectObject(result);

			if (_listMultiSelectUniqueId.Count >= MAX_SELECT_COUNT)
				break;
		}

		RefreshCountText();
		OnMultiSelectMaterial();

		if (_listMultiSelectEquipData.Count == 0)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_NoneForCondition"), 2.0f);
	}

	int _price;
	void OnMultiSelectMaterial()
	{
		_price = 0;
		for (int i = 0; i < _listMultiSelectEquipData.Count; ++i)
		{
			InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_listMultiSelectEquipData[i].cachedEquipTableData.innerGrade);
			if (innerGradeTableData == null)
				continue;
			_price += innerGradeTableData.sellGold;
		}
		EquipSellGround.instance.SetTargetPrice(_price);
	}

	int BASE_GRADE = 3;
	public void OnClickAutoSelect()
	{
		UIInstanceManager.instance.ShowCanvasAsync("AutoSelectCanvas", () =>
		{
			AutoSelectCanvas.instance.InitializeGrade(BASE_GRADE);
		});
	}

	public void OnClickSellButton()
	{
		if (_listMultiSelectEquipData.Count == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_SelectMaterial"), 2.0f);
			return;
		}

		string alertStirngId = CheckSellAlert();
		System.Action action = () =>
		{
			PlayFabApiManager.instance.RequestSellEquip(_listMultiSelectEquipData, _price, OnRecvSellEquip);
		};

		if (string.IsNullOrEmpty(alertStirngId))
			action.Invoke();
		else
		{
			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString(alertStirngId), () =>
			{
				action.Invoke();
			});
		}
	}

	string CheckSellAlert()
	{
		for (int i = 0; i < _listMultiSelectEquipData.Count; ++i)
		{
			if (_listMultiSelectEquipData[i].cachedEquipTableData.grade >= BASE_GRADE)
				return "EquipUI_WarningSell";
		}
		return "";
	}

	void OnRecvSellEquip()
	{
		currencySmallInfo.RefreshInfo();
		RefreshGrid();
		EquipSellGround.instance.SetTargetPrice(0);
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_SellComplete"), 2.0f);
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