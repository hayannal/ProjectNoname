using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class EquipReconstructCanvas : EquipShowCanvasBase
{
	public static EquipReconstructCanvas instance;

	public const int ReconstructPointMax = 10000;
	public const int ReconstructMinimumLimit = 2000;

	public Text mainButtonText;
	public Image mainButtonImage;

	public GameObject selectTextObject;
	public Text selectCountText;
	public Text noNeedText;

	public SwitchAnim reconstructSwitch;
	public Text reconstructNameText;
	public Text reconstructOnOffText;
	public Button autoSelectButton;
	public Transform tooltipTargetTransform;

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

		if (EventManager.instance.reservedOpenReconstructEvent)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("AlchemyUI_CrateName"), UIString.instance.GetString("AlchemyUI_CrateDesc"), UIString.instance.GetString("AlchemyUI_CrateMore"), null, 0.785f);
			});
			EventManager.instance.reservedOpenReconstructEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.reconstruct);
		}
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

		// 상황에 따라 다르게 처리해야한다.
		if (TimeSpaceData.instance.reconstructPoint >= ReconstructPointMax)
		{
			// 맥스라면 재구축으로 보여야한다.
			if (reconstructSwitch.isOn == false)
				reconstructSwitch.AnimateSwitch();
			else
			{
				autoSelectButton.gameObject.SetActive(false);
				RefreshMainButton(true);
				RefreshGrid(true);
			}
		}
		else
		{
			// 맥스가 아닐때는 분해로 보여야한다.
			if (reconstructSwitch.isOn)
				reconstructSwitch.AnimateSwitch();
			else
			{
				autoSelectButton.gameObject.SetActive(true);
				RefreshMainButton(false);
				RefreshGrid(false);
			}
		}

		EquipReconstructGround.instance.SetBaseValue((float)TimeSpaceData.instance.reconstructPoint / ReconstructPointMax);
		EquipReconstructGround.instance.ClearTargetValue();
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
	void RefreshGrid(bool reconstruct)
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
		if (reconstruct == false)
		{
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
		}
		RefreshCountText(reconstruct);
		noNeedText.gameObject.SetActive(_listCurrentEquipData.Count == 0);
		selectTextObject.SetActive(false);
		EquipReconstructGround.instance.ClearTargetValue();

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

	void RefreshCountText(bool reconstruct)
	{
		selectCountText.gameObject.SetActive(reconstruct == false);
		selectTextObject.SetActive(_listMultiSelectEquipData.Count > 0);
		if (_listMultiSelectEquipData.Count == 0)
			selectCountText.text = string.Format(TimeSpaceData.instance.IsInventoryVisualMax() ? "<color=#FF2200>{0}</color> / {1}" : "{0} / {1}", TimeSpaceData.instance.inventoryItemCount, TimeSpaceData.InventoryVisualMax);
		else
			selectCountText.text = string.Format("{0} / {1}", _listMultiSelectEquipData.Count, _multiSelectMax);
	}

	void RefreshMainButton(bool reconstruct)
	{
		mainButtonText.SetLocalizedText(UIString.instance.GetString(reconstruct ? "AlchemyUI_ReconstructBig" : "AlchemyUI_DeconstructBig"));
		mainButtonImage.color = reconstruct ? Color.white : ColorUtil.halfGray;
		mainButtonText.color = reconstruct ? Color.white : ColorUtil.halfGray;
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
	public void OnMultiSelectListItem(EquipData equipData)
	{
		bool contains = _listMultiSelectUniqueId.Contains(equipData.uniqueId);
		if (contains == false && _listMultiSelectEquipData.Count >= MAX_SELECT_COUNT)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_CannotSelectMore"), 1.0f);
			return;
		}

		if (contains == false && IsReachableGaugeMax())
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

		RefreshCountText(false);
		OnMultiSelectMaterial();
	}

	bool IsReachableGaugeMax()
	{
		return ((TimeSpaceData.instance.reconstructPoint + _sumPoint) >= ReconstructPointMax);
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

			if (result && _listMultiSelectUniqueId.Count >= MAX_SELECT_COUNT)
				result = false;

			if (result && IsReachableGaugeMax())
				result = false;

			if (result)
			{
				_listMultiSelectUniqueId.Add(_listEquipCanvasListItem[i].equipData.uniqueId);
				_listMultiSelectEquipData.Add(_listEquipCanvasListItem[i].equipData);
			}
			_listEquipCanvasListItem[i].ShowSelectObject(result);

			RefreshSumPoint();
		}

		RefreshCountText(false);
		OnMultiSelectMaterial();

		if (_listMultiSelectEquipData.Count == 0)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_NoneForCondition"), 2.0f);
	}

	void RefreshSumPoint()
	{
		_sumPoint = 0;
		for (int i = 0; i < _listMultiSelectEquipData.Count; ++i)
		{
			InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_listMultiSelectEquipData[i].cachedEquipTableData.innerGrade);
			if (innerGradeTableData == null)
				continue;
			_sumPoint += innerGradeTableData.reconstructPoint;
		}
	}

	ObscuredInt _sumPoint;
	void OnMultiSelectMaterial()
	{
		RefreshSumPoint();
		EquipReconstructGround.instance.SetTargetValue((float)_sumPoint / ReconstructPointMax);

		if (_listMultiSelectEquipData.Count > 0)
		{
			mainButtonImage.color = Color.white;
			mainButtonText.color = Color.white;
		}
		else
		{
			mainButtonImage.color = ColorUtil.halfGray;
			mainButtonText.color = ColorUtil.halfGray;
		}
	}

	int BASE_GRADE = 4;
	public void OnClickAutoSelect()
	{
		UIInstanceManager.instance.ShowCanvasAsync("AutoSelectCanvas", () =>
		{
			AutoSelectCanvas.instance.InitializeGrade(BASE_GRADE);
		});
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		reconstructSwitch.AnimateSwitch();
	}

	public void OnSwitchOnReconstruct()
	{
		// On을 했는데 On할 수 없는 상황이라면 다시 되돌려야한다.
		if (TimeSpaceData.instance.reconstructPoint < ReconstructMinimumLimit)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_MinimumLimit"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
			return;
		}

		reconstructNameText.SetLocalizedText(UIString.instance.GetString("AlchemyUI_ReconstructOn"));
		reconstructOnOffText.text = "ON";
		reconstructOnOffText.color = Color.white;
		autoSelectButton.gameObject.SetActive(false);
		RefreshMainButton(true);
		RefreshGrid(true);
	}

	public void OnSwitchOffReconstruct()
	{
		// Off를 했는데 Off할 수 없는 상황이라면 다시 되돌려야한다.
		if (TimeSpaceData.instance.reconstructPoint >= ReconstructPointMax)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_MaxPoint"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
			return;
		}

		reconstructNameText.SetLocalizedText(UIString.instance.GetString("AlchemyUI_ReconstructOff"));
		reconstructOnOffText.text = "OFF";
		reconstructOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
		autoSelectButton.gameObject.SetActive(true);
		RefreshMainButton(false);
		RefreshGrid(false);
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AlchemyUI_ReconstructMore"), 250, tooltipTargetTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickMainButton()
	{
		if (reconstructSwitch.isOn)
		{
			// 재구축일때는 인벤토리 공간 체크
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			// 재구축은 드랍을 굴려야한다.
			//PlayFabApiManager.instance.RequestSellEquip(_listMultiSelectEquipData, _sumPoint, OnRecvSellEquip);
		}
		else
		{
			if (_listMultiSelectEquipData.Count == 0)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_SelectMaterial"), 2.0f);
				return;
			}

			string alertStirngId = CheckDeconstructAlert();
			System.Action action = () =>
			{
				DeconstructEquip();
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
	}

	bool _greatSuccess;
	bool _leftEquip;
	void DeconstructEquip()
	{
		// 대성공 확률은 고정
		bool greatSuccess = (Random.value < 0.1f);
		_greatSuccess = false;

		// 대성공하지 않았다면 그냥 들어있는 재료의 양만큼 적용하면 되고 대성공 했다면 최대치를 넘었는지 확인해서 넘치는 재료를 골라내야한다.
		if (greatSuccess)
		{
			_greatSuccess = true;
			int tempResultValue = TimeSpaceData.instance.reconstructPoint + _sumPoint * 2;
			if (tempResultValue > ReconstructPointMax)
			{
				tempResultValue = TimeSpaceData.instance.reconstructPoint;
				int removeIndex = -1;
				for (int i = 0; i < _listMultiSelectEquipData.Count; ++i)
				{
					InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_listMultiSelectEquipData[i].cachedEquipTableData.innerGrade);
					if (innerGradeTableData == null)
						continue;
					tempResultValue += innerGradeTableData.reconstructPoint * 2;
					if (tempResultValue >= ReconstructPointMax)
					{
						removeIndex = i + 1;
						break;
					}
				}

				if (removeIndex != -1 && removeIndex < _listMultiSelectEquipData.Count)
				{
					for (int i = _listMultiSelectEquipData.Count - 1; i >= 0; --i)
					{
						if (i >= removeIndex)
						{
							_listMultiSelectEquipData.RemoveAt(i);
							_leftEquip = true;
						}
					}
				}
			}
		}
		
		PlayFabApiManager.instance.RequestDeconstructEquip(_listMultiSelectEquipData, _sumPoint, OnRecvDeconstructEquip);
	}

	string CheckDeconstructAlert()
	{
		for (int i = 0; i < _listMultiSelectEquipData.Count; ++i)
		{
			if (_listMultiSelectEquipData[i].cachedEquipTableData.grade >= BASE_GRADE)
				return "EquipUI_WarningSell";
		}
		return "";
	}

	void OnRecvDeconstructEquip()
	{
		_sumPoint = 0;
		EquipReconstructGround.instance.SetDeconstructResult(_greatSuccess, (float)TimeSpaceData.instance.reconstructPoint / ReconstructPointMax);
		EquipReconstructGround.instance.SetTargetValue(0.0f);

		// refresh
		if (TimeSpaceData.instance.reconstructPoint >= ReconstructPointMax)
		{
			reconstructSwitch.AnimateSwitch();
		}
		else
		{
			RefreshMainButton(false);
			RefreshGrid(false);
		}

		if (_leftEquip)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ResultLeftEquip"), 2.0f);
		else if (TimeSpaceData.instance.reconstructPoint >= ReconstructPointMax)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ReconstructModeOff"), 2.0f);
		else if (_greatSuccess)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ResultGreatSuccess"), 2.0f);
		else
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ResultSuccess"), 2.0f);
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