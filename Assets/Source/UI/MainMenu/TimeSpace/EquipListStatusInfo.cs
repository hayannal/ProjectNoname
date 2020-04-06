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
	public void RefreshInfo(EquipData equipData, bool equipped)
	{
		_equipped = equipped;
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
		mainStatusText.text = ActorStatus.GetDisplayAttack(equipData.mainStatusValue).ToString("N0");
		mainStatusFillImage.fillAmount = equipData.GetMainStatusRatio();
		int optionCount = equipData.optionCount;
		for (int i = 0; i < optionStatusObjectList.Length; ++i)
			optionStatusObjectList[i].gameObject.SetActive(i < optionCount);

		// option
		for (int i = 0; i < optionCount; ++i)
		{
			//optionStatusTextList[i].SetLocalizedText()
		}

		equipButtonObject.gameObject.SetActive(!equipped);
		unequipButtonObject.gameObject.SetActive(equipped);

		bool usableEquipOption = ContentsManager.IsOpen(ContentsManager.eOpenContentsByResearchLevel.EquipOption);
		optionButtonImage.color = usableEquipOption ? Color.white : ColorUtil.halfGray;
		optionButtonText.color = usableEquipOption ? Color.white : Color.gray;
	}

	public void OnClickDetailShowButton()
	{
		// 장착되지 않은거라 별도의 공간으로 보내야한다.
		//UIInstanceManager.instance.ShowCanvasAsync("DiffEquipDetailCanvas", null);
	}

	public void OnClickEquipButton()
	{

	}

	public void OnClickUnequipButton()
	{
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

	public void OnClickCloseButton()
	{

	}
}