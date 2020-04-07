using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
		detailShowButton.gameObject.SetActive(!equipped);

		equipListItem.Initialize(equipData, null);
		RefreshLockInfo();
		RefreshStatus();

		equipButtonObject.gameObject.SetActive(!equipped);
		unequipButtonObject.gameObject.SetActive(equipped);

		bool usableEquipOption = ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.EquipOption);
		optionButtonImage.color = usableEquipOption ? Color.white : ColorUtil.halfGray;
		optionButtonText.color = usableEquipOption ? Color.white : Color.gray;
	}

	void RefreshLockInfo()
	{
		lockButton.gameObject.SetActive(_equipData.isLock);
		unlockButton.gameObject.SetActive(!_equipData.isLock);
	}

	void RefreshStatus()
	{
		mainStatusText.text = ActorStatus.GetDisplayAttack(_equipData.mainStatusValue).ToString("N0");
		mainStatusFillImage.fillAmount = _equipData.GetMainStatusRatio();
		int optionCount = _equipData.optionCount;
		for (int i = 0; i < optionStatusObjectList.Length; ++i)
			optionStatusObjectList[i].gameObject.SetActive(i < optionCount);

		// option
		for (int i = 0; i < optionCount; ++i)
		{
			//optionStatusTextList[i].SetLocalizedText()
		}
	}

	public void OnClickDetailShowButton()
	{
		// 장착되지 않은거라 별도의 공간으로 보내야한다. 로딩도 그쪽에서 담당한다.
		//UIInstanceManager.instance.ShowCanvasAsync("DiffEquipDetailCanvas", null);
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

	}

	public void OnClickOptionButton()
	{
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.EquipOption) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_RequiredResearch"), 2.0f);
			return;
		}
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