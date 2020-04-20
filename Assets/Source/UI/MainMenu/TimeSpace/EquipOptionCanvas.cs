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

	public RectTransform switchRootRectTransform;
	public RectTransform switchRootOutRectTransform;
	public SwitchAnim transmuteSwitch;
	public Text transmuteNameText;
	public Text transmuteOnOffText;
	public Text transmuteRemainCountText;
	public Text transmuteRemainCountValueText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceButtonText;
	public Coffee.UIExtensions.UIEffect goldGrayscaleEffect;

	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	ObscuredInt _price;

	Vector2 _defaultSwitchRootPosition;
	void Awake()
	{
		instance = this;
		_defaultSwitchRootPosition = switchRootRectTransform.anchoredPosition;
	}

	void Start()
	{
		selectImageRectTransform.SetAsFirstSibling();
	}

	void OnEnable()
	{
		if (_restore)
			return;

		if (transmuteSwitch.isOn)
			transmuteSwitch.AnimateSwitch();
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

		if (_restore)
		{
			// 복구할땐 인덱스 건드리는거 없이 그리드와 가격버튼만 갱신해야한다.
			if (_selectMain || transmuteSwitch.isOn == false)
			{
				EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Amplify, _selectMain);
				float ratio = 0.0f;
				if (_selectMain)
					ratio = _equipData.GetMainStatusRatio();
				else
				{
					EquipData.RandomOptionInfo info = _equipData.GetOption(_selectRendomIndex);
					ratio = info.GetRandomStatusRatio();
				}
				RefreshButton(ratio == 1.0f);
			}
			else
			{
				EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transmute);
				RefreshButton(false);
			}
			return;
		}

		// 처음 들어왔을때 시작은 항상 연마모드
		transmuteRemainCountValueText.text = UIString.instance.GetString(transmuteSwitch.isOn ? "EquipUI_LeftCountValueOn" : "EquipUI_LeftCountValueOff", equipData.transmuteRemainCount.ToString());

		// 옵션1 옵션2 옵션3 메인1 순서대로 있는지 확인 후 선택박스를 체크해놔야한다.
		int selectedIndex = -1;
		int optionCount = _equipData.optionCount;
		for (int i = 0; i < optionCount; ++i)
		{
			EquipData.RandomOptionInfo info = _equipData.GetOption(i);
			if (info.GetRandomStatusRatio() != 1.0f)
			{
				switch (i)
				{
					case 0: OnClickRandomOptionRect(0, true); break;
					case 1: OnClickRandomOptionRect(1, true); break;
					case 2: OnClickRandomOptionRect(2, true); break;
				}
				selectedIndex = i;
				break;
			}
		}
		if (selectedIndex == -1)
		{
			if (_equipData.GetMainStatusRatio() != 1.0f)
				OnClickMainStatusRectInternal(true);
			else
				OnClickRandomOptionRect(0, true);
		}
	}

	public void RefreshOption()
	{
		mainMinText.text = ActorStatus.GetDisplayAttack(_equipData.GetMainStatusValueMin()).ToString("N0");
		mainMaxText.text = ActorStatus.GetDisplayAttack(_equipData.GetMainStatusValueMax()).ToString("N0");

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
		transmuteSwitch.AnimateSwitch();
	}

	public void OnClickMainStatusRectInternal(bool forceRefresh = false)
	{
		_targetRectTransform = mainRectTransform;
		if (_selectMain == false || forceRefresh)
		{
			_selectMain = true;

			// 메인옵은 무조건 Amplify만 되기 때문에 스위치 자체는 건드리지 않은채 그리드만 연마 모드로 바꾼다.
			// 이래야 다시 랜덤옵션 선택했을때 변경모드로 돌아갈 수 있다.
			EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Amplify, _selectMain);
			RefreshButton(_equipData.GetMainStatusRatio() == 1.0f);
		}
		// 스위치를 끄니 OnEnable때 AnimateSwitch걸어둔 애니가 적용되지 않아서 이상하게 보인다. 그래서 화면밖으로 옮기도록 한다.
		//switchObject.SetActive(false);
		switchRootRectTransform.anchoredPosition = switchRootOutRectTransform.anchoredPosition;
	}

	void OnClickRandomOptionRect(int randomIndex, bool forceRefresh = false)
	{
		_targetRectTransform = optionRectTransformList[randomIndex];
		if (_selectMain || forceRefresh)
		{
			_selectMain = false;
			if (transmuteSwitch.isOn)
			{
				EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Transmute);
				RefreshButton(false);
			}
			else
			{
				EquipInfoGrowthCanvas.instance.RefreshGrid(EquipInfoGrowthCanvas.eGrowthGridType.Amplify, _selectMain);
				EquipData.RandomOptionInfo info = _equipData.GetOption(randomIndex);
				RefreshButton(info.GetRandomStatusRatio() == 1.0f);
			}
		}
		_selectRendomIndex = randomIndex;
		//switchObject.SetActive(true);
		switchRootRectTransform.anchoredPosition = _defaultSwitchRootPosition;
	}

	public void OnClickMainStatusRect() { OnClickMainStatusRectInternal(); }
	public void OnClickRandomOptionRect0() { OnClickRandomOptionRect(0); }
	public void OnClickRandomOptionRect1() { OnClickRandomOptionRect(1); }
	public void OnClickRandomOptionRect2() { OnClickRandomOptionRect(2); }

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
		RefreshButton(false);
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

		if (_equipData != null)
		{
			float ratio = 0.0f;
			if (_selectMain)
				ratio = _equipData.GetMainStatusRatio();
			else
			{
				EquipData.RandomOptionInfo info = _equipData.GetOption(_selectRendomIndex);
				ratio = info.GetRandomStatusRatio();
			}
			RefreshButton(ratio == 1.0f);
		}
		else
		{
			_price = 0;
			RefreshPriceButton();
		}
	}

	public void OnClickPriceButton()
	{
		if (_equipData != null && (_selectMain || transmuteSwitch.isOn == false))
		{
			float ratio = 0.0f;
			if (_selectMain)
				ratio = _equipData.GetMainStatusRatio();
			else
			{
				EquipData.RandomOptionInfo info = _equipData.GetOption(_selectRendomIndex);
				ratio = info.GetRandomStatusRatio();
			}
			if (ratio == 1.0f)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_MaxReachAmplifyToast"), 2.0f);
				return;
			}
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

		if (_selectMain == false && transmuteSwitch.isOn)
		{
			string alertStirngId = CheckTransmuteAlert();
			System.Action action = () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("EquipTransmuteConfirmCanvas", () =>
				{
					EquipTransmuteConfirmCanvas.instance.ShowCanvas(true, _equipData, _selectRendomIndex,
						equipStatusInfo.optionStatusTextList[_selectRendomIndex].text, equipStatusInfo.optionStatusValueTextList[_selectRendomIndex].text,
						optionMinTextList[_selectRendomIndex].text, optionMaxTextList[_selectRendomIndex].text, _price);
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
		else
		{
			string alertStirngId = "";
			if (_selectMain)
				CheckAmplifyMainAlert();
			else
				CheckAmplifyRandomAlert();

			System.Action action = () =>
			{
				if (_selectMain)
				{
					UIInstanceManager.instance.ShowCanvasAsync("EquipAmplifyMainConfirmCanvas", () =>
					{
						EquipAmplifyMainConfirmCanvas.instance.ShowCanvas(true, _equipData, equipStatusInfo.mainStatusText.text, mainMinText.text, mainMaxText.text, _price);
					});
				}
				else
				{
					UIInstanceManager.instance.ShowCanvasAsync("EquipAmplifyRandomConfirmCanvas", () =>
					{
						EquipAmplifyRandomConfirmCanvas.instance.ShowCanvas(true, _equipData, _selectRendomIndex,
							equipStatusInfo.optionStatusTextList[_selectRendomIndex].text, equipStatusInfo.optionStatusValueTextList[_selectRendomIndex].text,
							optionMinTextList[_selectRendomIndex].text, optionMaxTextList[_selectRendomIndex].text, _price);
					});
				}
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

	string CheckTransmuteAlert()
	{
		return "";
	}

	string CheckAmplifyMainAlert()
	{
		return "";
	}

	string CheckAmplifyRandomAlert()
	{
		return "";
	}


	public void OnMultiSelectMaterial(List<EquipData> listSelectedEquipData)
	{
		if (transmuteSwitch.isOn && _selectMain == false)
			return;

		_price = 0;
		InnerGradeTableData innerGradeTableData = TableDataManager.instance.FindInnerGradeTableData(_equipData.cachedEquipTableData.innerGrade);
		if (innerGradeTableData == null)
			return;

		if (_selectMain)
			_price += (innerGradeTableData.amplifyMainGold * listSelectedEquipData.Count);
		else
			_price += (innerGradeTableData.amplifyRandomGold * listSelectedEquipData.Count);
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