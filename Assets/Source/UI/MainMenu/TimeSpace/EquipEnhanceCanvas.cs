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
	public Text priceButtonText;

	ObscuredInt _price;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		if (transferSwitch.isOn)
			transferSwitch.AnimateSwitch();
		autoSelectButton.gameObject.SetActive(true);
	}

	EquipData _equipData;
	public void RefreshInfo(EquipData equipData)
	{
		_equipData = equipData;
		equipStatusInfo.RefreshInfo(equipData, false);

		if (transferSwitch.isOn)
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transfer);
		else
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Enhance);
		priceButtonText.text = "0";
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
		priceButtonText.text = "0";
	}

	public void OnSwitchOffTransfer()
	{
		transferNameText.SetLocalizedText(UIString.instance.GetString("EquipUI_TransferEnhanceOff"));
		transferOnOffText.text = "OFF";
		transferOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
		autoSelectButton.gameObject.SetActive(true);
		EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Enhance);
		priceButtonText.text = "0";
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
		priceButtonText.text = _price.ToString("N0");
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
		priceButtonText.text = _price.ToString("N0");
	}
}