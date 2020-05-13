using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopEquipConfirmCanvas : MonoBehaviour
{
	public static DailyShopEquipConfirmCanvas instance;

	public Text gradeText;
	public DailyShopListItem dailyListItem;
	// 장비는 big이 없다.
	//public DailyShopListItem bigDailyListItem;
	// 대신 민맥스 공격력이 필요하다.
	public Text attackText;
	public RectTransform detailButtonRectTransform;
	public Text priceText;
	public GameObject[] priceTypeObjectList;
	public GameObject buttonObject;

	public EquipListStatusInfo materialSmallStatusInfo;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		buttonObject.SetActive(true);
	}

	void OnDisable()
	{
		materialSmallStatusInfo.gameObject.SetActive(false);
	}

	public void ShowCanvas(bool show, DailyShopData.DailyShopSlotInfo dailyShopSlotInfo)
	{
		// 골드나 다른 박스들과 달리 지우기엔 컴포넌트가 많아서 차라리 마스킹으로 가리도록 해본다.
		dailyListItem.RefreshInfo(dailyShopSlotInfo);

		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(dailyShopSlotInfo.value);
		if (equipTableData == null)
			return;

		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", equipTableData.grade)));
		gradeText.color = EquipListStatusInfo.GetGradeDropObjectNameColor(equipTableData.grade);
		attackText.text = UIString.instance.GetString("GameUI_NumberRange", ActorStatus.GetDisplayAttack(equipTableData.min).ToString("N0"), ActorStatus.GetDisplayAttack(equipTableData.max).ToString("N0"));

		// 장착중인 정보
		EquipData equipData = TimeSpaceData.instance.GetEquippedDataByType((TimeSpaceData.eEquipSlotType)equipTableData.equipType);
		if (equipData != null)
		{
			materialSmallStatusInfo.RefreshInfo(equipData, false);
			materialSmallStatusInfo.gameObject.SetActive(false);
			materialSmallStatusInfo.gameObject.SetActive(true);
		}
		else
			materialSmallStatusInfo.gameObject.SetActive(false);

		priceText.text = dailyShopSlotInfo.price.ToString("N0");
		CurrencyData.eCurrencyType currencyType = CurrencyData.eCurrencyType.Diamond;
		if (dailyShopSlotInfo.priceType == CurrencyData.GoldCode())
			currencyType = CurrencyData.eCurrencyType.Gold;
		for (int i = 0; i < priceTypeObjectList.Length; ++i)
			priceTypeObjectList[i].SetActive((int)currencyType == i);
	}

	public void OnClickDetailButton()
	{
		// 장비일때만 오는거라 장비가 보이면 된다.
	}

	public void OnClickOkButton()
	{
		// 장비는 직접 구매만 있으니 드랍은 연결할필요 없을거다.
	}
}