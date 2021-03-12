using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;
using PlayFab.ClientModels;

public class EquipReconstructCanvas : EquipShowCanvasBase
{
	public static EquipReconstructCanvas instance;

	public const int ReconstructPointMax = 10000;
	public const int ReconstructMinimumLimit = 1500;

	public CurrencySmallInfo currencySmallInfo;
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
	public RectTransform scrollViewRectTransform;

	public EquipListStatusInfo materialSmallStatusInfo;

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	float _defaultScrollViewWidth;
	void Awake()
	{
		instance = this;
		_defaultScrollViewWidth = scrollViewRectTransform.sizeDelta.x;
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
		// EquipSellCanvas와 마찬가지로 EquipListCanvas 에서 가져와서 EquipInfoGrowthCanvas와 합쳐서 만들어본다.
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		// 다른 캔버스들과 달리 뽑기연출 뜰때 카메라 모드를 풀어야하므로 StackCanvas.Pop함수보다 위로 올려둔다.
		SetInfoCameraMode(true);

		// 입장시 Grid는 무조건 1회 만들어둔다.
		RefreshGrid();

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
				RefreshTextInfo(true);
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
				RefreshTextInfo(false);
			}
		}

		EquipReconstructGround.instance.SetBaseValue((float)TimeSpaceData.instance.reconstructPoint / ReconstructPointMax);
		EquipReconstructGround.instance.ClearTargetValue();

		if (restore)
		{
			// 이 창에서 restore가 일어나는 경우는 재구축 연출 끝나고 돌아올때 뿐이다.
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ReconstructModeOff"), 2.0f);
			return;
		}
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		materialSmallStatusInfo.gameObject.SetActive(false);

		bool popResult = StackCanvas.Pop(gameObject);

		// 다른 캔버스들과 달리 뽑기연출 뜰때 카메라 모드를 풀어야하므로 StackCanvas.Pop함수보다 위로 올려두려고 했는데
		// 위로 올리면 DotMainMenu연채로 이 창 열고 닫을때 조명에 문제가 생겨서 popResult에 기억시켜두고 호출하기로 한다.
		SetInfoCameraMode(false);

		if (popResult)
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
	void RefreshGrid()
	{
		_sumPoint = 0;
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

	void RefreshTextInfo(bool reconstruct)
	{
		RefreshCountText(reconstruct);
		noNeedText.gameObject.SetActive(_listCurrentEquipData.Count == 0 || reconstruct);
		if (noNeedText.gameObject.activeSelf)
			noNeedText.SetLocalizedText(UIString.instance.GetString(reconstruct ? "AlchemyUI_NoNeedMaterial" : "GameUI_EmptyEquip"));
		selectTextObject.SetActive(false);
		EquipReconstructGround.instance.ClearTargetValue();
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
		RefreshTextInfo(true);

		// 전체 Refresh가 느려서 처음 생성할때만 하고 Switch 하는 형태로 간다.
		//RefreshGrid(true);
		SwitchGrid(true);
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
		RefreshTextInfo(false);
		//RefreshGrid(false);
		SwitchGrid(false);
	}

	void SwitchGrid(bool reconstruct)
	{
		// content 루트를 꺼보니 여전히 안에 있는 항목을 전부 꺼야해서 느리다.
		// 그래서 scrollView의 가로 크기를 줄이기로 한다.
		scrollViewRectTransform.sizeDelta = new Vector2(reconstruct ? -1 : _defaultScrollViewWidth, scrollViewRectTransform.sizeDelta.y);

		if (reconstruct)
		{
			_listMultiSelectUniqueId.Clear();
			_listMultiSelectEquipData.Clear();

			for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
				_listEquipCanvasListItem[i].ShowSelectObject(false);
		}
		_sumPoint = 0;
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

			string alertStirngId = CheckReconstructAlert();
			string arg = string.Format("{0:0.##}", ((float)TimeSpaceData.instance.reconstructPoint / ReconstructPointMax * 100.0f));
			System.Action action = () =>
			{
				ReconstructEquip();
			};

			if (string.IsNullOrEmpty(alertStirngId))
				action.Invoke();
			else
			{
				YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString(alertStirngId, arg), () =>
				{
					action.Invoke();
				});
			}
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
			_sumPoint *= 2;
			int tempResultValue = TimeSpaceData.instance.reconstructPoint + _sumPoint;
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

		// 누적시키고 나서 총량이 최대치를 넘으면 안된다.
		if (TimeSpaceData.instance.reconstructPoint + _sumPoint > ReconstructPointMax)
			_sumPoint = ReconstructPointMax - TimeSpaceData.instance.reconstructPoint;
		
		PlayFabApiManager.instance.RequestDeconstructEquip(_listMultiSelectEquipData, _sumPoint, OnRecvDeconstructEquip);
	}

	string CheckDeconstructAlert()
	{
		for (int i = 0; i < _listMultiSelectEquipData.Count; ++i)
		{
			if (_listMultiSelectEquipData[i].cachedEquipTableData.grade >= BASE_GRADE)
				return "AlchemyUI_WarningDeconstruct";
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
			reconstructSwitch.AnimateSwitch();

		for (int i = _listEquipCanvasListItem.Count - 1; i >= 0; --i)
		{
			if (_listEquipCanvasListItem[i].gameObject.activeSelf == false)
				continue;

			if (_listMultiSelectUniqueId.Contains(_listEquipCanvasListItem[i].equipData.uniqueId))
			{
				_listEquipCanvasListItem[i].ShowAlarm(false);
				_listEquipCanvasListItem[i].gameObject.SetActive(false);

				int removeIndex = i;
				EquipData removeEquipData = _listEquipCanvasListItem[i].equipData;

				_listMultiSelectEquipData.Remove(_listEquipCanvasListItem[i].equipData);
				_listMultiSelectUniqueId.Remove(removeEquipData.uniqueId);
				_listCurrentEquipData.RemoveAt(removeIndex);
				_listEquipCanvasListItem.RemoveAt(removeIndex);
			}
		}

		// _listMultiSelectEquipData 그리드 정리 후에 호출해야해서 아래로 빼둔다.
		if (TimeSpaceData.instance.reconstructPoint >= ReconstructPointMax)
		{ }
		else
		{
			RefreshMainButton(false);
			// 아이템 개수 많을때는 느려서 삭제할 항목만 지우기로 한다.
			//RefreshGrid(false);
			RefreshTextInfo(false);
		}

		if (_leftEquip)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ResultLeftEquip"), 2.0f);
		else if (TimeSpaceData.instance.reconstructPoint >= ReconstructPointMax)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ReconstructModeOn"), 2.0f);
		else if (_greatSuccess)
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ResultGreatSuccess"), 2.0f);
		else
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AlchemyUI_ResultSuccess"), 2.0f);

		_leftEquip = false;
		_greatSuccess = false;
	}

	DropProcessor _cachedDropProcessor;
	ObscuredInt _addDia;
	public int GetDiaAmount() { return _addDia; }
	void ReconstructEquip()
	{
		// 재구축 확률을 먼저 굴려보고 성공하면
		bool success = (Random.value <= ((float)TimeSpaceData.instance.reconstructPoint / ReconstructPointMax));
		if (success)
		{
			// 오리진 박스와 마찬가지로 먼저 드랍프로세서부터 만들어야한다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Wjstjfwkdqlk", "", true, true);
			if (CheatingListener.detectedCheatTable)
				return;

			PlayFabApiManager.instance.RequestReconstructEquip(DropManager.instance.GetLobbyDropItemInfo(), 0, OnRecvReconstructEquip);
		}
		else
		{
			// 실패시에는 쌓여있는 퍼센트에다가 0.3 곱한만큼의 다이아를 받아야한다.
			float calc = TimeSpaceData.instance.reconstructPoint * 0.01f * 0.075f;
			calc = Random.Range(calc - 0.625f, calc + 0.625f);
			int diaAmount = Mathf.RoundToInt(calc);
			_addDia = diaAmount;

			// 먼저 패킷을 보내서 통신 후
			PlayFabApiManager.instance.RequestReconstructEquip(null, diaAmount, OnRecvReconstructDia);
		}
	}

	string CheckReconstructAlert()
	{
		if (TimeSpaceData.instance.reconstructPoint < ReconstructPointMax)
		{
			return "AlchemyUI_ReconstructConfirm";
		}
		return "";
	}

	void OnRecvReconstructEquip(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;
		if (itemGrantString == "")
			return;

		// 캐릭터와 달리 장비는 드랍프로세서에서 정보를 뽑아쓰는게 아니라서 미리 클리어해도 상관없다.
		DropManager.instance.ClearLobbyDropInfo();

		EquipData grantEquipData = TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString, 1);
		if (grantEquipData == null)
			return;

		// 연출은 연출대로 두고
		// 연출 끝나고 나올 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ItemInstance> listGrantItem = null;
		if (itemGrantString != "")
		{
			listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
			for (int i = 0; i < listGrantItem.Count; ++i)
			{
				EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listGrantItem[i].ItemId);
				if (equipTableData == null)
					continue;

				AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
			}
		}

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Equip1, _cachedDropProcessor, 0, 0, () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
				{
					EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem);
				});
			});
		});
	}

	void OnRecvReconstructDia(bool serverFailure, string itemGrantString)
	{
		if (itemGrantString != "")
			return;

		// 연출
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "ReconstructDiamond", "", true, true);
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Equip1, dropProcessor, 0, 0, () =>
			{
				DropManager.instance.ClearLobbyDropInfo();
				currencySmallInfo.RefreshInfo();

				UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
				{
					CurrencyBoxResultCanvas.instance.RefreshInfo(0, _addDia, 0, false, true);
				});
			});
		});
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