using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;
using ActorStatusDefine;

public class EquipOptionCanvas : MonoBehaviour
{
	public static EquipOptionCanvas instance;

	public EquipListStatusInfo equipStatusInfo;
	public Text mainMinText;
	public Text mainMaxText;
	public RectTransform mainRectTransform;
	public GameObject[] optionRectObjectList;
	public Text[] optionMinTextList;
	public Text[] optionMaxTextList;
	public RectTransform[] optionRectTransformList;
	public RectTransform selectObjectRectTransform;
	public RectTransform selectImageRectTransform;

	public SwitchAnim transmuteSwitch;
	public Text transmuteNameText;
	public Text transmuteOnOffText;
	public Text transmuteRemainCountText;
	public Text transmuteRemainCountValueText;

	public Image priceButtonImage;
	public Text priceButtonText;
	public Coffee.UIExtensions.UIEffect goldGrayscaleEffect;

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

	void Update()
	{
		UpdateSelectImage();
	}

	EquipData _equipData;
	public void RefreshInfo(EquipData equipData)
	{
		_equipData = equipData;
		equipStatusInfo.RefreshInfo(equipData, false);
		RefreshOption();

		// 처음 들어왔을때 
		// 옵션1 옵션2 옵션3 메인1 순서대로 있는지 확인 후 선택박스를 체크해놔야한다.
		int optionCount = _equipData.optionCount;
		if (optionCount > 0)
			OnClickRandomOptionRect0();
		else
			OnClickMainStatusRect();

		transmuteRemainCountValueText.text = UIString.instance.GetString(transmuteSwitch.isOn ? "EquipUI_LeftCountValueOn" : "EquipUI_LeftCountValueOff", equipData.transmuteRemainCount.ToString());
		if (transmuteSwitch.isOn)
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transmute);
		else
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Amplify, _selectMain);
		_price = 0;
		RefreshPriceButton();
	}

	void RefreshOption()
	{
		mainMinText.text = ActorStatus.GetDisplayAttack(_equipData.cachedEquipTableData.min).ToString("N0");
		mainMaxText.text = ActorStatus.GetDisplayAttack(_equipData.cachedEquipTableData.max).ToString("N0");

		int optionCount = _equipData.optionCount;
		for (int i = 0; i < optionRectObjectList.Length; ++i)
			optionRectObjectList[i].gameObject.SetActive(i < optionCount);

		for (int i = 0; i < optionCount; ++i)
		{
			EquipData.RandomOptionInfo info = _equipData.GetOption(i);
			
			switch (info.statusType)
			{
				case eActorStatus.MaxHp:
					optionMinTextList[i].text = ActorStatus.GetDisplayMaxHp(info.cachedOptionTableData.min).ToString("N0");
					optionMaxTextList[i].text = ActorStatus.GetDisplayMaxHp(info.cachedOptionTableData.max).ToString("N0");
					break;
				case eActorStatus.Attack:
					optionMinTextList[i].text = ActorStatus.GetDisplayAttack(info.cachedOptionTableData.min).ToString("N0");
					optionMaxTextList[i].text = ActorStatus.GetDisplayAttack(info.cachedOptionTableData.max).ToString("N0");
					break;
				default:
					optionMinTextList[i].text = string.Format("{0:0.##}%", info.cachedOptionTableData.min * 100.0f);
					optionMaxTextList[i].text = string.Format("{0:0.##}%", info.cachedOptionTableData.max * 100.0f);
					break;
			}
		}
	}

	void RefreshPriceButton()
	{
		bool disablePrice = (CurrencyData.instance.gold < _price || _price == 0);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceButtonText.color = !disablePrice ? Color.white : Color.gray;
		priceButtonText.text = _price.ToString("N0");
		goldGrayscaleEffect.enabled = disablePrice;
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		transmuteSwitch.AnimateSwitch();
	}

	public void OnClickMainStatusRect()
	{
		_targetRectTransform = mainRectTransform;
		_selectMain = true;
	}

	public void OnClickRandomOptionRect0()
	{
		_targetRectTransform = optionRectTransformList[0];
		_selectMain = false;
		_selectRendomIndex = 0;
	}

	public void OnClickRandomOptionRect1()
	{
		_targetRectTransform = optionRectTransformList[1];
		_selectRendomIndex = 1;
	}

	public void OnClickRandomOptionRect2()
	{
		_targetRectTransform = optionRectTransformList[2];
		_selectRendomIndex = 2;
	}

	bool _selectMain = false;
	int _selectRendomIndex = -1;
	RectTransform _targetRectTransform;
	void UpdateSelectImage()
	{
		if (_targetRectTransform == null)
			return;

		float diff = _targetRectTransform.position.y - selectObjectRectTransform.position.y;
		if (diff == 0.0f)
			return;

		if (Mathf.Abs(diff) < 0.01f)
		{
			selectObjectRectTransform.position = new Vector3(selectObjectRectTransform.position.x, _targetRectTransform.position.y, selectObjectRectTransform.position.z);
			return;
		}

		float resultY = Mathf.Lerp(selectObjectRectTransform.position.y, _targetRectTransform.position.y, Time.deltaTime * 10.0f);
		selectObjectRectTransform.position = new Vector3(selectObjectRectTransform.position.x, resultY, selectObjectRectTransform.position.z);
		selectImageRectTransform.position = Vector3.Lerp(selectImageRectTransform.position, _targetRectTransform.position, Time.deltaTime * 11.0f);
		selectImageRectTransform.sizeDelta = Vector2.Lerp(selectImageRectTransform.sizeDelta, _targetRectTransform.sizeDelta, Time.deltaTime * 11.0f);
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

		_price = 0;
		RefreshPriceButton();
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
		EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Amplify, _selectMain);

		_price = 0;
		RefreshPriceButton();
	}

	public void OnClickPriceButton()
	{
		if (_price == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}
	}


	public void OnMultiSelectMaterial(List<EquipData> listSelectedEquipData)
	{
		if (transmuteSwitch.isOn)
			return;

		_price = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;

		for (int i = 0; i < listSelectedEquipData.Count; ++i)
		{
			if (_selectMain)
				_price += innerGradeTableData.amplifyMainGold;
			else
			{
				EquipData.RandomOptionInfo info = _equipData.GetOption(_selectRendomIndex);
				if (info != null)
					_price += info.cachedOptionTableData.amplifyGold;
			}
		}
		RefreshPriceButton();
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
		RefreshPriceButton();
	}
}