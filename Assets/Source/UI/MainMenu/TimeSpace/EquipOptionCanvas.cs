using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class EquipOptionCanvas : MonoBehaviour
{
	public static EquipOptionCanvas instance;

	public EquipListStatusInfo equipStatusInfo;
	public SwitchAnim transmuteSwitch;
	public Text transmuteNameText;
	public Text transmuteOnOffText;
	public Text transmuteRemainCountText;
	public Text transmuteRemainCountValueText;
	public Text priceButtonText;

	ObscuredInt _price;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		if (transmuteSwitch.isOn)
			transmuteSwitch.AnimateSwitch();
	}

	EquipData _equipData;
	public void RefreshInfo(EquipData equipData)
	{
		_equipData = equipData;
		equipStatusInfo.RefreshInfo(equipData, false);

		transmuteRemainCountValueText.text = UIString.instance.GetString(transmuteSwitch.isOn ? "EquipUI_LeftCountValueOn" : "EquipUI_LeftCountValueOff", equipData.transmuteRemainCount.ToString());
		if (transmuteSwitch.isOn)
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transmute);
		else
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Amplify);
		priceButtonText.text = "0";

		// 처음 들어왔을때 
		// 옵션1 옵션2 옵션3 메인1 순서대로 풀로 차있는지 확인 후 선택해주는 절차가 필요하다.
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		transmuteSwitch.AnimateSwitch();
	}

	public void OnSwitchOnTransmute()
	{
		// On을 했는데 On할 수 없는 아이템이라면 다시 되돌려야한다.
		if (_equipData.transmuteRemainCount <= 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_LeftCountZero"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
			return;
		}

		int transmuteRemainCount = 0;
		if (_equipData != null) transmuteRemainCount = _equipData.transmuteRemainCount;

		transmuteNameText.SetLocalizedText(UIString.instance.GetString("EquipUI_TransmuteOptionOn"));
		transmuteOnOffText.text = "ON";
		transmuteOnOffText.color = Color.white;
		transmuteRemainCountText.SetLocalizedText(UIString.instance.GetString("EquipUI_LeftCountOn"));
		transmuteRemainCountValueText.text = UIString.instance.GetString("EquipUI_LeftCountValueOn", transmuteRemainCount.ToString());
		EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transmute);
		priceButtonText.text = "0";
	}

	public void OnSwitchOffTransmute()
	{
		int transmuteRemainCount = 0;
		if (_equipData != null) transmuteRemainCount = _equipData.transmuteRemainCount;

		transmuteNameText.SetLocalizedText(UIString.instance.GetString("EquipUI_TransmuteOptionOff"));
		transmuteOnOffText.text = "OFF";
		transmuteOnOffText.color = new Color(0.176f, 0.176f, 0.176f);
		transmuteRemainCountText.SetLocalizedText(UIString.instance.GetString("EquipUI_LeftCountOff"));
		transmuteRemainCountValueText.text = UIString.instance.GetString("EquipUI_LeftCountValueOff", transmuteRemainCount.ToString());
		EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Amplify);
		priceButtonText.text = "0";
	}

	public void OnClickPriceButton()
	{

	}


	public void OnMultiSelectMaterial(List<EquipData> listSelectedEquipData)
	{
		if (transmuteSwitch.isOn)
			return;

		_price = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;
	}

	public void OnSelectMaterial(EquipData equipData)
	{
		if (transmuteSwitch.isOn == false)
			return;

		_price = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;

		_price = innerGradeTableData.transmuteRandomGold;
		priceButtonText.text = _price.ToString("N0");
	}
}