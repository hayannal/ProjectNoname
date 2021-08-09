using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class PointShopEventListItem : MonoBehaviour
{
	public CumulativeEventData.eEventType eventType;
	public int day;

	public Image blurImage;
	public Image backgroundImge;
	public Sprite[] backgroundSpriteList;

	public GameObject goldIconObject;
	public GameObject diaIconObject;
	public GameObject energyIconObject;
	public GameObject returnScrollIconObject;
	public GameObject equipBoxObject;
	public Image equipIconImage;

	public Text countText;
	public Text nameText;
	public Text priceText;

	public GameObject addObject;
	public Text addText;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	public Image overrideIconImage;
	public RectTransform overrideIconRectTransform;
	public Text overrideCountText;

	void OnEnable()
	{
		CumulativeEventData.EventRewardInfo eventRewardInfo = CumulativeEventData.instance.FindRewardInfo(eventType, day);
		if (eventRewardInfo == null)
			return;
		RefreshInfo(eventRewardInfo);
	}

	CumulativeEventData.EventRewardInfo _slotInfo;
	public void RefreshInfo(CumulativeEventData.EventRewardInfo eventRewardInfo)
	{
		_slotInfo = eventRewardInfo;

		RefreshPriceText();
		RefreshInfoButton(eventRewardInfo);

		// 아마 포인트샵 상품들은 거의 다 alreadyDesigned를 쓸 것이다.
		if (_slotInfo.alreadyDesigned)
		{
			RefreshBackground(true);
			return;
		}

		if (eventRewardInfo.type == "cu")
		{
			if (eventRewardInfo.value == CurrencyData.GoldCode())
			{
				//_addGold = eventRewardInfo.count;
				goldIconObject.SetActive(true);
				diaIconObject.SetActive(false);
				energyIconObject.SetActive(false);
				returnScrollIconObject.SetActive(false);
				countText.color = DailyFreeItem.GetGoldTextColor();
			}
			else if (eventRewardInfo.value == CurrencyData.DiamondCode())
			{
				//_addDia = eventRewardInfo.cn;
				goldIconObject.SetActive(false);
				diaIconObject.SetActive(true);
				energyIconObject.SetActive(false);
				returnScrollIconObject.SetActive(false);
				countText.color = DailyFreeItem.GetDiaTextColor();
			}
			else if (eventRewardInfo.value == CurrencyData.EnergyCode())
			{
				//_addEnergy = createInfo.cn;
				goldIconObject.SetActive(false);
				diaIconObject.SetActive(false);
				energyIconObject.SetActive(true);
				returnScrollIconObject.SetActive(false);
				countText.color = Color.white;
			}
			else
			{
				goldIconObject.SetActive(false);
				diaIconObject.SetActive(false);
				energyIconObject.SetActive(false);
				returnScrollIconObject.SetActive(true);
				countText.color = Color.white;
			}
			countText.text = eventRewardInfo.count.ToString("N0");
			countText.gameObject.SetActive(true);
			equipBoxObject.SetActive(false);
			equipIconImage.gameObject.SetActive(false);
			nameText.gameObject.SetActive(false);
			addObject.SetActive(false);
			RefreshBackground(false);
		}
		else if (eventRewardInfo.type == "be")
		{
			goldIconObject.SetActive(false);
			diaIconObject.SetActive(false);
			energyIconObject.SetActive(false);
			returnScrollIconObject.SetActive(false);
			equipBoxObject.SetActive(true);
			equipIconImage.gameObject.SetActive(false);

			countText.gameObject.SetActive(false);
			nameText.gameObject.SetActive(true);
			nameText.SetLocalizedText(UIString.instance.GetString("MailUI_Equipment"));
			addObject.SetActive(true);
			addText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", eventRewardInfo.value)));
			int.TryParse(eventRewardInfo.value, out int grade);
			addText.color = EquipListStatusInfo.GetGradeDropObjectNameColor(grade);
			RefreshBackground(false);
		}
		else if (eventRewardInfo.type == "fe")
		{
			goldIconObject.SetActive(false);
			diaIconObject.SetActive(false);
			energyIconObject.SetActive(false);
			returnScrollIconObject.SetActive(false);
			equipBoxObject.SetActive(false);
			equipIconImage.gameObject.SetActive(true);

			RefreshEquipIconImage(eventRewardInfo.value);
		}
	}

	void RefreshEquipIconImage(string value)
	{
		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(value);
		if (equipTableData == null)
			return;

		RefreshBackground(equipTableData.grade == 4);

		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		countText.gameObject.SetActive(false);
		nameText.SetLocalizedText(UIString.instance.GetString(equipTableData.nameId));
		nameText.gameObject.SetActive(true);
		addObject.SetActive(false);
	}

	// 우선은 이벤트에서 쓰던거 가져와서 그대로 써본다.
	void RefreshBackground(bool isLightenBackground)
	{
		blurImage.color = isLightenBackground ? new Color(0.945f, 0.945f, 0.094f, 0.42f) : new Color(1.0f, 1.0f, 1.0f, 0.35f);
		backgroundImge.color = isLightenBackground ? new Color(1.0f, 1.0f, 1.0f, 0.42f) : new Color(0.829f, 0.915f, 0.514f, 0.604f);
		backgroundImge.sprite = backgroundSpriteList[isLightenBackground ? 0 : 1];
	}

	void RefreshInfoButton(CumulativeEventData.EventRewardInfo eventRewardInfo)
	{
		if (eventRewardInfo.type == "cu")
		{
			if (eventRewardInfo.value == CurrencyData.GoldCode())
			{
			}
			else if (eventRewardInfo.value == CurrencyData.DiamondCode())
			{
			}
			else if (eventRewardInfo.value == CurrencyData.EnergyCode())
			{
			}
		}
		// 상점처럼 쓰는거라 안쓸거는 빼둔다. infoButton은 안쓸거 같다.
		//else if (eventRewardInfo.type == "be")
		//{
		//	infoButtonImage.gameObject.SetActive(false);
		//}
		//else if (eventRewardInfo.type == "fe")
		//{
		//	infoButtonImage.gameObject.SetActive(true);
		//}
	}

	void RefreshPriceText()
	{
		// 프리팹을 하나로 쓰기때문에 모든 타입이 공용으로 사용하게 된다. 타입별로 나눠서 처리.
		if (_slotInfo == null)
			return;

		// cn1 이 여기서는 가격으로 쓰인다.
		priceText.text = string.Format("{0:N0} P", _slotInfo.cn1);

		bool disablePrice = (CumulativeEventData.instance.pointShopPoint < _slotInfo.cn1);
		if (disablePrice)
			AlarmObject.Hide(alarmRootTransform);
		else
			AlarmObject.Show(alarmRootTransform);

		/*
		if (day <= count)
		{
			claimText.gameObject.SetActive(false);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(true);
			blackObject.gameObject.SetActive(true);
			infoButtonImage.color = Color.gray;
			AlarmObject.Hide(alarmRootTransform);
		}
		else if (recorded == false && day == (count + 1))
		{
			claimText.gameObject.SetActive(true);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			infoButtonImage.color = Color.white;

			if (CumulativeEventData.instance.IsReceivableEvent(eventType))
			{
				claimText.color = new Color(0.0f, 1.0f, 0.0f);
				AlarmObject.Show(alarmRootTransform);
			}
			else
			{
				claimText.color = new Color(0.0f, 0.333f, 0.0f);
				AlarmObject.Hide(alarmRootTransform);
			}
		}
		else
		{
			claimText.gameObject.SetActive(false);
			dayText.SetLocalizedText(UIString.instance.GetString("LoginUI_DayNumber", day));
			dayText.gameObject.SetActive(true);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			infoButtonImage.color = Color.white;
			AlarmObject.Hide(alarmRootTransform);
		}
		*/
	}

	public void OnClickButton()
	{
		//if (blackObject.activeSelf)
		//{
		//	ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyFreeItem"), 2.0f);
		//	return;
		//}

		//if (dayText.gameObject.activeSelf)
		//{
		//	ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimYet"), 2.0f);
		//	return;
		//}

		// DailyBox 계열에서는 오늘의 일퀘를 완료했는지 체크해야한다.
		//if (CumulativeEventData.IsDailyBoxEvent(eventType))
		//{
		//	if (PlayerData.instance.sharedDailyBoxOpened == false)
		//	{
		//		ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimOrigin"), 2.0f);
		//		return;
		//	}
		//}

		if (CumulativeEventData.IsRepeatEvent(eventType) && CumulativeEventData.instance.removeRepeatServerFailure)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_InsufficientInfo"), 2.0f);
			return;
		}

		// 포인트 상점에선 dr을 사용하며 value에 적혀있는거로 드랍을 굴리면 된다.
		if (_slotInfo.type == "dr")
		{
			// 상자일 경우 드랍프로세서
			//if (TimeSpaceData.instance.IsInventoryVisualMax())
			//{
			//	ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			//	return;
			//}

			UIInstanceManager.instance.ShowCanvasAsync("PointShopConfirmCanvas", () =>
			{
				PointShopConfirmCanvas.instance.ShowCanvas(true, day, overrideIconImage.sprite, overrideIconRectTransform.anchoredPosition, overrideIconRectTransform.sizeDelta, overrideCountText.text);
			});
		}
	}
}