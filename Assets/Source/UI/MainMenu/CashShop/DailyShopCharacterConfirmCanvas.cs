using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyShopCharacterConfirmCanvas : MonoBehaviour
{
	public static DailyShopCharacterConfirmCanvas instance;

	public GameObject dailyListItemGroupObject;
	public GameObject bigDailyListItemGroupObject;
	public DailyShopListItem dailyListItem;
	public DailyShopListItem bigDailyListItem;
	public RectTransform detailButtonRectTransform;
	public Text priceText;
	public GameObject buttonObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		buttonObject.SetActive(true);
	}

	public void ShowCanvas(bool show, DailyShopData.DailyShopSlotInfo dailyShopSlotInfo, bool big)
	{
		// 골드나 다른 박스들과 달리 지우기엔 컴포넌트가 많아서 차라리 마스킹으로 가리도록 해본다.
		if (big)
		{
			bigDailyListItem.RefreshInfo(dailyShopSlotInfo);
			detailButtonRectTransform.anchoredPosition = new Vector3(128.0f, detailButtonRectTransform.anchoredPosition.y);
		}
		else
		{
			dailyListItem.RefreshInfo(dailyShopSlotInfo);
			detailButtonRectTransform.anchoredPosition = new Vector3(90.0f, detailButtonRectTransform.anchoredPosition.y);
		}
		dailyListItemGroupObject.SetActive(!big);
		bigDailyListItemGroupObject.SetActive(big);

		priceText.text = dailyShopSlotInfo.price.ToString("N0");
	}

	public void OnClickDetailButton()
	{
		// 타입에 따라 달라져야 한다.
	}

	public void OnClickOkButton()
	{
		// 타입에 따라 드랍 연출이 있는거. 캐릭터 영입창이 뜨는거 등등 나뉘어진다.
	}
}