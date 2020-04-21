using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class EquipEnhanceCanvas : MonoBehaviour
{
	public static EquipEnhanceCanvas instance;

	public EquipListStatusInfo equipStatusInfo;
	public SwitchAnim transferSwitch;
	public Text transferNameText;
	public Text transferOnOffText;
	public Button autoSelectButton;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceButtonText;
	public Coffee.UIExtensions.UIEffect goldGrayscaleEffect;

	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	ObscuredInt _price;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		// EquipInfoGrowthCanvas와 달리 Stack으로 관리할수도 없는게
		// EquipInfoGrowthCanvas는 보여지는 상태에서 그 위에 이 Canvas가 보여야한다.
		// 그래서 이렇게 OnEnable에서 처리해버리면 연출하고 왔을때도 실행이 되서 모드가 강제로 바뀌어버리게 된다.
		// 이걸 막기 위해 Restore플래그를 추가해 제어하기로 한다.
		if (_restore)
		{
			// 복구모드일때 한가지 예외상황이 있는데 강화이전 받아서 3강을 넘기는때다.
			// 이땐 일반 강화모드로 되돌려야한다.
			bool needReset = false;
			if (transferSwitch.isOn)
			{
				int limit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ReceiveTransferMaxEnhance");
				if (_equipData.enhanceLevel > limit)
				{
					needReset = true;
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_TransferOff"), 2.0f);
				}
			}
			if (needReset == false)
				return;
		}

		if (transferSwitch.isOn)
			transferSwitch.AnimateSwitch();
		autoSelectButton.gameObject.SetActive(true);
	}

	#region Restore Canvas
	bool _restore = false;
	public void RestoreInfo(EquipData equipData)
	{
		_restore = true;
		gameObject.SetActive(true);
		RefreshInfo(equipData);
		_restore = false;
	}
	#endregion

	EquipData _equipData;
	public void RefreshInfo(EquipData equipData)
	{
		_equipData = equipData;
		equipStatusInfo.RefreshInfo(equipData, false);

		if (transferSwitch.isOn)
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transfer);
		else
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Enhance);

		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;
		RefreshButton(equipData.enhanceLevel >= innerGradeTableData.max);
	}

	void RefreshButton(bool showMaxButton)
	{
		_price = 0;
		if (showMaxButton)
		{
			priceButtonObject.SetActive(false);

			maxButtonImage.color = ColorUtil.halfGray;
			maxButtonText.color = ColorUtil.halfGray;
			maxButtonObject.SetActive(true);
		}
		else
		{
			priceButtonText.text = "0";
			priceButtonImage.color = ColorUtil.halfGray;
			priceButtonText.color = Color.gray;
			goldGrayscaleEffect.enabled = true;
			priceButtonObject.SetActive(true);
			maxButtonObject.SetActive(false);
		}
	}

	void RefreshPriceButton()
	{
		if (priceButtonObject.activeSelf == false)
			return;

		bool disablePrice = (CurrencyData.instance.gold < _price || _price == 0);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceButtonText.color = !disablePrice ? Color.white : Color.gray;
		priceButtonText.text = _price.ToString("N0");
		goldGrayscaleEffect.enabled = disablePrice;
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		transferSwitch.AnimateSwitch();
	}

	public void OnSwitchOnTransfer()
	{
		// On을 했는데 On할 수 없는 아이템이라면 다시 되돌려야한다.
		int limit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ReceiveTransferMaxEnhance");
		if (_equipData.enhanceLevel > limit)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_LimitTransferReceive", limit), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
			return;
		}

		transferNameText.SetLocalizedText(UIString.instance.GetString("EquipUI_TransferEnhanceOn"));
		transferOnOffText.text = "ON";
		transferOnOffText.color = Color.white;
		autoSelectButton.gameObject.SetActive(false);
		EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transfer);
		RefreshButton(false);
	}

	public void OnSwitchOffTransfer()
	{
		transferNameText.SetLocalizedText(UIString.instance.GetString("EquipUI_TransferEnhanceOff"));
		transferOnOffText.text = "OFF";
		transferOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
		autoSelectButton.gameObject.SetActive(true);
		EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Enhance);

		if (_equipData == null)
			return;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;
		RefreshButton(_equipData.enhanceLevel >= innerGradeTableData.max);
	}

	public void OnClickAutoSelect()
	{
		UIInstanceManager.instance.ShowCanvasAsync("AutoSelectCanvas", () =>
		{
			AutoSelectCanvas.instance.InitializeGrade(_equipData.cachedEquipTableData.grade);
		});
	}

	public void OnClickPriceButton()
	{
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;

		if (_equipData.enhanceLevel >= innerGradeTableData.max)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_MaxReachEnhanceToast"), 2.0f);
			return;
		}

		if (_price == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_SelectMaterial"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}


		if (transferSwitch.isOn)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EquipTransferConfirmCanvas", () =>
			{
				EquipTransferConfirmCanvas.instance.ShowCanvas(true, _equipData, equipStatusInfo.mainStatusText.text, _price);
			});
		}
		else
		{
			string alertStirngId = CheckEnhanceAlert();
			System.Action action = () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("EquipEnhanceConfirmCanvas", () =>
				{
					EquipEnhanceConfirmCanvas.instance.ShowCanvas(true, _equipData, equipStatusInfo.mainStatusText.text, _price);
				});
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

	string CheckEnhanceAlert()
	{
		List<EquipData> listMaterialEquipData = EquipInfoGrowthCanvas.instance.listMultiSelectEquipData;

		// 옵션 메뉴가 열려있을때만 옵션 관련 질문들을 한다.
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.EquipOption))
		{
			// 코드 재활용. 옵션변경꺼는 재료가 list가 아니라서 안된다.
			string alertStirngId = EquipOptionCanvas.CheckAmplifyRandomAlert(_equipData);
			if (string.IsNullOrEmpty(alertStirngId) == false)
				return alertStirngId;
			
			for (int i = 0; i < listMaterialEquipData.Count; ++i)
			{
				if (_equipData.cachedEquipTableData.equipType == listMaterialEquipData[i].cachedEquipTableData.equipType &&
					_equipData.cachedEquipTableData.grade == listMaterialEquipData[i].cachedEquipTableData.grade)
					return "EquipUI_WarningSameType";
			}
		}
		if (_equipData.cachedEquipTableData.grade > 0)
		{
			for (int i = 0; i < listMaterialEquipData.Count; ++i)
			{
				if (_equipData.cachedEquipTableData.grade == listMaterialEquipData[i].cachedEquipTableData.grade)
					return "EquipUI_WarningSameGrade";
			}
		}
		for (int i = 0; i < listMaterialEquipData.Count; ++i)
		{
			if (_equipData.cachedEquipTableData.grade < listMaterialEquipData[i].cachedEquipTableData.grade)
				return "EquipUI_WarningHighGrade";
		}
		return "";
	}


	public void OnMultiSelectMaterial(List<EquipData> listSelectedEquipData)
	{
		if (transferSwitch.isOn)
			return;

		_price = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;

		for (int i = 0; i < listSelectedEquipData.Count; ++i)
		{
			int price = 0;
			switch (listSelectedEquipData[i].cachedEquipTableData.innerGrade)
			{
				case 0: price = innerGradeTableData.innerGradeZeroEnhanceGold; break;
				case 1: price = innerGradeTableData.innerGradeOneEnhanceGold; break;
				case 2: price = innerGradeTableData.innerGradeTwoEnhanceGold; break;
				case 3: price = innerGradeTableData.innerGradeThreeEnhanceGold; break;
				case 4: price = innerGradeTableData.innerGradeFourEnhanceGold; break;
				case 5: price = innerGradeTableData.innerGradeFiveEnhanceGold; break;
				case 6: price = innerGradeTableData.innerGradeSixEnhanceGold; break;
			}
			_price += price;
		}
		RefreshPriceButton();
	}

	public void OnSelectMaterial(EquipData equipData)
	{
		if (transferSwitch.isOn == false)
			return;

		_price = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;

		_price = innerGradeTableData.transferGold;
		RefreshPriceButton();
	}
}