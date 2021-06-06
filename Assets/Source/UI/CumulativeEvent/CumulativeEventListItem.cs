using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CumulativeEventListItem : MonoBehaviour
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
	public Text claimText;
	public Text dayText;
	public Text completeText;

	public GameObject addObject;
	public Text addText;
	public GameObject infoButtonObject;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	void OnEnable()
	{
		CumulativeEventData.EventRewardInfo eventRewardInfo = CumulativeEventData.instance.FindRewardInfo(eventType, day);
		RefreshInfo(eventRewardInfo);
	}

	CumulativeEventData.EventRewardInfo _slotInfo;
	void RefreshInfo(CumulativeEventData.EventRewardInfo eventRewardInfo)
	{
		_slotInfo = eventRewardInfo;

		RefreshClaimState();
		RefreshInfoButton(eventRewardInfo);

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

	void RefreshBackground(bool isLightenBackground)
	{
		blurImage.color = isLightenBackground ? new Color(0.945f, 0.945f, 0.094f, 0.42f) : new Color(0.258f, 0.905f, 0.650f, 0.42f);
		backgroundImge.color = isLightenBackground ? new Color(1.0f, 1.0f, 1.0f, 0.42f) : new Color(0.0f, 1.0f, 0.749f, 0.42f);
		backgroundImge.sprite = backgroundSpriteList[isLightenBackground ? 0 : 1];
	}

	void RefreshInfoButton(CumulativeEventData.EventRewardInfo eventRewardInfo)
	{
		if (eventRewardInfo.type == "cu")
		{
			infoButtonObject.SetActive(false);
			if (eventRewardInfo.value == CurrencyData.GoldCode())
			{
			}
			else if (eventRewardInfo.value == CurrencyData.DiamondCode())
			{
			}
			else if (eventRewardInfo.value == CurrencyData.EnergyCode())
			{
			}
			else
			{
				infoButtonObject.SetActive(true);
			}
		}
		else if (eventRewardInfo.type == "be")
		{
			infoButtonObject.SetActive(false);
		}
		else if (eventRewardInfo.type == "fe")
		{
			infoButtonObject.SetActive(true);
		}
	}

	void RefreshClaimState()
	{
		// 프리팹을 하나로 쓰기때문에 모든 타입이 공용으로 사용하게 된다. 타입별로 나눠서 처리.

		int count = 0;
		switch (eventType)
		{
			case CumulativeEventData.eEventType.NewAccount:
				count = CumulativeEventData.instance.newAccountLoginEventCount;
				break;
			case CumulativeEventData.eEventType.DailyBox:
				count = CumulativeEventData.instance.newAccountDailyBoxEventCount;
				break;
		}

		bool recorded = false;
		switch (eventType)
		{
			case CumulativeEventData.eEventType.NewAccount:
				recorded = CumulativeEventData.instance.newAccountLoginRecorded;
				break;
			case CumulativeEventData.eEventType.DailyBox:
				recorded = CumulativeEventData.instance.newAccountDailyBoxRecorded;
				break;
		}

		if (day <= count)
		{
			claimText.gameObject.SetActive(false);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(true);
			blackObject.gameObject.SetActive(true);
			AlarmObject.Hide(alarmRootTransform);
		}
		else if (recorded == false && day == (count + 1))
		{
			claimText.gameObject.SetActive(true);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			AlarmObject.Show(alarmRootTransform);
		}
		else
		{
			claimText.gameObject.SetActive(false);
			dayText.SetLocalizedText(UIString.instance.GetString("LoginUI_DayNumber", day));
			dayText.gameObject.SetActive(true);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			AlarmObject.Hide(alarmRootTransform);
		}
	}

	public void OnClickInfoButton()
	{
		if (_slotInfo == null)
			return;

		if (_slotInfo.type == "cu")
		{
			if (_slotInfo.value == CurrencyData.GoldCode())
			{
			}
			else if (_slotInfo.value == CurrencyData.DiamondCode())
			{
			}
			else if (_slotInfo.value == CurrencyData.EnergyCode())
			{
			}
			else
			{
				UIInstanceManager.instance.ShowCanvasAsync("ReturnScrollInfoCanvas", null);
			}
		}
		else if (_slotInfo.type == "fe")
		{
			UIInstanceManager.instance.ShowCanvasAsync("CumulativeEventEquipInfoCanvas", () =>
			{
				CumulativeEventEquipInfoCanvas.instance.ShowCanvas(true, _slotInfo);
			});
		}
	}

	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyFreeItem"), 2.0f);	
			return;
		}

		if (dayText.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimYet"), 2.0f);
			return;
		}

		/*
		if (_slotInfo.type == "cu")
		{
			PlayFabApiManager.instance.RequestReceiveMailPresent(id, receiveDay, _type, _addDia, _addGold, _addEnergy, "", (serverFailure, itemGrantString) =>
			{
				DotMainMenuCanvas.instance.RefreshMailAlarmObject();
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("MailUI_AfterClaim"), 2.0f);
				MailCanvas.instance.RefreshGrid();
			});
		}
		else if (_type == "be")
		{
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			// 장비박스 했을때처럼 드랍프로세서로부터 하나 뽑아와야한다.
			string equipId = PrepareDropProcessor();
			if (equipId == "")
				return;
			if (CheatingListener.detectedCheatTable)
				return;

			PlayFabApiManager.instance.RequestReceiveMailPresent(id, receiveDay, _type, 0, 0, 0, equipId, OnRecvEquipBox);
		}
		*/



		/*
		// 획득 요청
		PlayFabApiManager.instance.RequestGetFreeItem(_addDia, _addGold, _addEnergy, (serverFailure) =>
		{
			RefreshInfo();
			CashShopCanvas.instance.currencySmallInfo.RefreshInfo();
			DotMainMenuCanvas.instance.RefreshCashShopAlarmObject();
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);
		});

		if (_slotInfo.type == "fe")
		{
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			UIInstanceManager.instance.ShowCanvasAsync("DailyShopEquipConfirmCanvas", () =>
			{
				DailyShopEquipConfirmCanvas.instance.ShowCanvas(true, _slotInfo);
			});
		}
		else
		{
			UIInstanceManager.instance.ShowCanvasAsync("DailyShopCharacterConfirmCanvas", () =>
			{
				DailyShopCharacterConfirmCanvas.instance.ShowCanvas(true, _slotInfo, gameObject.name.Contains("_Big"));
			});
		}
		*/
	}
}