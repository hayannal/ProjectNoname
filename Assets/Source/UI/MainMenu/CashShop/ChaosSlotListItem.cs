using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChaosSlotListItem : MonoBehaviour
{
	public GameObject priceObject;
	public Text priceText;
	public GameObject purchasedObject;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	int _index;
	public void RefreshInfo(int index)
	{
		if (index > DailyShopData.instance.chaosSlotUnlockLevel)
		{
			gameObject.SetActive(false);
			return;
		}
		_index = index;
		RefreshAlarm();
		RefreshPrice();
		gameObject.SetActive(true);
	}

	void RefreshAlarm()
	{
		bool showAlarm = (PlayerData.instance.chaosFragmentCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosPowerPointsCost"));
		if (showAlarm)
		{
			for (int i = 0; i <= DailyShopData.ChaosSlotMax; ++i)
			{
				if (DailyShopData.instance.IsPurchasedTodayChaosData(i))
				{
					if (i == _index)
					{
						showAlarm = false;
						break;
					}
					else
						continue;
				}

				if (i > DailyShopData.instance.chaosSlotUnlockLevel)
				{
					if (i == _index)
					{
						showAlarm = false;
						break;
					}
					else
						continue;
				}

				if (showAlarm)
				{
					if (i == _index)
						break;
					else
					{
						showAlarm = false;
						break;
					}
				}
			}
		}
		if (showAlarm)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);
	}

	void RefreshPrice()
	{
		bool purchased = DailyShopData.instance.IsPurchasedTodayChaosData(_index);
		if (purchased)
		{
			blackObject.SetActive(purchased);
			purchasedObject.SetActive(purchased);
			priceObject.SetActive(false);
			return;
		}

		blackObject.SetActive(false);
		purchasedObject.SetActive(false);

		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosPowerPointsCost");
		priceText.text = price.ToString("N0");
		priceObject.SetActive(true);
	}

	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyThatItem"), 2.0f);
			return;
		}

		if (PlayerData.instance.IsWaitingRefreshDailyInfo())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_CannotBuyCalculating"), 2.0f);
			return;
		}

		// 현재 몇개 가지고 있는지 확인해야하기 때문에 이미 구매한게 아니라면 무조건 구매확인 팝업을 띄워야한다.
		UIInstanceManager.instance.ShowCanvasAsync("ChaosFragmentConfirmCanvas", () =>
		{
			ChaosFragmentConfirmCanvas.instance.slotIndex = _index;
		});
	}
}