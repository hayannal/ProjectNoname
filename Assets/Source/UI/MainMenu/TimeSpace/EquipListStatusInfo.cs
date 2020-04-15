using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ActorStatusDefine;

public class EquipListStatusInfo : MonoBehaviour
{
	public Image gradeBackImage;
	public Text gradeText;
	public Text nameText;
	public Button detailShowButton;

	public EquipCanvasListItem equipListItem;

	public Button lockButton;
	public Button unlockButton;

	public Text mainStatusText;
	public Image mainStatusFillImage;
	public GameObject[] optionStatusObjectList;
	public Text[] optionStatusTextList;
	public Text[] optionStatusValueTextList;
	public Image[] optionStatusFillImageList;
	public GameObject noOptionTextObject;

	public GameObject equipButtonObject;
	public GameObject unequipButtonObject;
	public Image optionButtonImage;
	public Text optionButtonText;

	bool _equipped = false;
	EquipData _equipData = null;
	public void RefreshInfo(EquipData equipData, bool equipped)
	{
		_equipped = equipped;
		_equipData = equipData;
		switch (equipData.cachedEquipTableData.grade)
		{
			case 0:
				gradeBackImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				gradeBackImage.color = new Color(0.0f, 1.0f, 0.51f);
				break;
			case 2:
				gradeBackImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 3:
				gradeBackImage.color = new Color(0.63f, 0.0f, 1.0f);
				break;
			case 4:
				gradeBackImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", equipData.cachedEquipTableData.grade)));
		nameText.SetLocalizedText(UIString.instance.GetString(equipData.cachedEquipTableData.nameId));
		if (detailShowButton != null) detailShowButton.gameObject.SetActive(!equipped);

		equipListItem.Initialize(equipData, null);
		RefreshLockInfo();
		RefreshStatus();

		if (equipButtonObject != null) equipButtonObject.gameObject.SetActive(!equipped);
		if (unequipButtonObject != null) unequipButtonObject.gameObject.SetActive(equipped);

		bool usableEquipOption = ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.EquipOption);
		optionButtonImage.color = usableEquipOption ? Color.white : ColorUtil.halfGray;
		optionButtonText.color = usableEquipOption ? Color.white : Color.gray;
	}

	void RefreshLockInfo()
	{
		if (lockButton != null) lockButton.gameObject.SetActive(_equipData.isLock);
		if (unlockButton != null) unlockButton.gameObject.SetActive(!_equipData.isLock);
	}

	static Color _gaugeColor = new Color(0.819f, 0.505f, 0.458f, 0.862f);
	static Color _fullGaugeColor = new Color(0.937f, 0.937f, 0.298f, 0.862f);
	void RefreshStatus()
	{
		mainStatusFillImage.fillAmount = _equipData.GetMainStatusRatio();
		bool fullGauge = (mainStatusFillImage.fillAmount == 1.0f);
		float displayValue = ActorStatus.GetDisplayAttack(_equipData.mainStatusValue);
		string displayString = displayValue.ToString("N0");
		bool adjustValue = false;
		if (!fullGauge)
		{
			string maxDisplayString = ActorStatus.GetDisplayAttack(_equipData.cachedEquipTableData.max).ToString("N0");
			if (displayString == maxDisplayString)
				adjustValue = true;
		}
		mainStatusText.text = adjustValue ? (displayValue - 1.0f).ToString("N0") : displayString;
		mainStatusFillImage.color = fullGauge ? _fullGaugeColor : _gaugeColor;

		int optionCount = _equipData.optionCount;
		for (int i = 0; i < optionStatusObjectList.Length; ++i)
			optionStatusObjectList[i].gameObject.SetActive(i < optionCount);

		// option
		for (int i = 0; i < optionCount; ++i)
		{
			EquipData.RandomOptionInfo info = _equipData.GetOption(i);
			optionStatusTextList[i].SetLocalizedText(UIString.instance.GetString(string.Format("Op_{0}", info.statusType.ToString())));
			optionStatusFillImageList[i].fillAmount = info.GetRandomStatusRatio();
			fullGauge = (optionStatusFillImageList[i].fillAmount == 1.0f);
			adjustValue = false;
			switch (info.statusType)
			{
				case eActorStatus.MaxHp:
					displayValue = ActorStatus.GetDisplayMaxHp(info.value);
					displayString = displayValue.ToString("N0");
					break;
				case eActorStatus.Attack:
					displayValue = ActorStatus.GetDisplayAttack(info.value);
					displayString = displayValue.ToString("N0");
					break;
				default:
					// 나머진 일괄 %
					displayValue = info.value;
					displayString = string.Format("{0:0.##}%", displayValue * 100.0f);
					break;
			}
			if (!fullGauge)
			{
				string maxDisplayString = "";
				switch (info.statusType)
				{
					case eActorStatus.MaxHp: maxDisplayString = ActorStatus.GetDisplayMaxHp(info.cachedOptionTableData.max).ToString("N0"); break;
					case eActorStatus.Attack: maxDisplayString = ActorStatus.GetDisplayAttack(info.cachedOptionTableData.max).ToString("N0"); break;
					default: maxDisplayString = string.Format("{0:0.##}%", info.cachedOptionTableData.max * 100.0f); break;
				}
				if (displayString == maxDisplayString)
					adjustValue = true;
			}
			if (adjustValue)
			{
				switch (info.statusType)
				{
					case eActorStatus.MaxHp:
					case eActorStatus.Attack:
						optionStatusValueTextList[i].text = (displayValue - 1.0f).ToString("N0");
						break;
					default:
						optionStatusValueTextList[i].text = string.Format("{0:0.##}%", (info.cachedOptionTableData.max * 100.0f) - 0.01f);
						break;
				}
			}
			else
				optionStatusValueTextList[i].text = displayString;
			optionStatusFillImageList[i].color = fullGauge ? _fullGaugeColor : _gaugeColor;
		}
		noOptionTextObject.SetActive(optionCount == 0);
	}

	public void OnClickDetailShowButton()
	{
		// 장착되지 않은거라 새로 로딩부터 해야한다.
		// 전환할 캔버스를 열어두고 로딩한답시고 대기할 순 없으니 여기서 로딩이 끝나는걸 확인한 후 처리하기로 한다.
		DelayedLoadingCanvas.Show(true);
		AddressableAssetLoadManager.GetAddressableGameObject(_equipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("EquipInfoDetailCanvas", () =>
			{
				// 비교템을 보여주는 모드로 전환
				EquipInfoGround.instance.ChangeDiffMode(_equipData);
				DelayedLoadingCanvas.Show(false);
			});
		});
	}

	public void OnClickEquipButton()
	{
		if (_equipped)
			return;
		if (_equipData == null)
			return;

		PlayFabApiManager.instance.RequestEquip(_equipData, () =>
		{
			// 대부분 다 EquipList가 해야하는 것들이라 ListCanvas에게 알린다.
			EquipListCanvas.instance.OnEquip(_equipData);
		});
	}

	public void OnClickUnequipButton()
	{
		if (!_equipped)
			return;
		if (_equipData == null)
			return;

		PlayFabApiManager.instance.RequestUnequip(_equipData, () =>
		{
			EquipListCanvas.instance.OnUnequip(_equipData);
		});
	}

	public void OnClickEnhanceButton()
	{
		DelayedLoadingCanvas.Show(true);
		AddressableAssetLoadManager.GetAddressableGameObject(_equipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("EquipInfoGrowthCanvas", () =>
			{
				if (_equipped == false)
					EquipInfoGround.instance.ChangeDiffMode(_equipData);
				EquipInfoGrowthCanvas.instance.RefreshInfo(0, _equipData);
				DelayedLoadingCanvas.Show(false);
			});
		});
	}

	public void OnClickOptionButton()
	{
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.EquipOption) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_RequiredResearch"), 2.0f);
			return;
		}

		DelayedLoadingCanvas.Show(true);
		AddressableAssetLoadManager.GetAddressableGameObject(_equipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("EquipInfoGrowthCanvas", () =>
			{
				if (_equipped == false)
					EquipInfoGround.instance.ChangeDiffMode(_equipData);
				EquipInfoGrowthCanvas.instance.RefreshInfo(1, _equipData);
				DelayedLoadingCanvas.Show(false);
			});
		});
	}

	public void OnClickUnlockButton()
	{
		if (_equipData == null)
			return;

		// 장비가 생성되면 기본이 언락상태고 언락 버튼이 보이게 된다.
		// 이 회색 언락버튼을 눌러야 lock 상태로 바뀌게 된다.
		PlayFabApiManager.instance.RequestLockEquip(_equipData, true, () =>
		{
			// 장착된 아이템이라면 정보창만 갱신하면 되지만
			// 장착되지 않은 아이템이라면 하단 그리드도 갱신해야하니 ListCanvas에 알려야한다.
			equipListItem.Initialize(_equipData, null);
			RefreshLockInfo();
			if (!_equipped)
				EquipListCanvas.instance.RefreshSelectedItem();

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_Locked"), 2.0f);
		});
	}

	public void OnClickLockButton()
	{
		if (_equipData == null)
			return;
		
		PlayFabApiManager.instance.RequestLockEquip(_equipData, false, () =>
		{
			equipListItem.Initialize(_equipData, null);
			RefreshLockInfo();
			if (!_equipped)
				EquipListCanvas.instance.RefreshSelectedItem();

			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_Unlocked"), 2.0f);
		});
	}

	public void OnClickCloseButton()
	{
		if (EquipListCanvas.instance != null)
		{
			if (_equipped)
				EquipListCanvas.instance.OnCloseEquippedStatusInfo();
			else
				EquipListCanvas.instance.OnCloseDiffStatusInfo();
		}
		gameObject.SetActive(false);
	}
}