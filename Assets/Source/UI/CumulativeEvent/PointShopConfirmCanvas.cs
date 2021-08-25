using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointShopConfirmCanvas : MonoBehaviour
{
	public static PointShopConfirmCanvas instance = null;

	public PointShopEventListItem pointShopEventListItem;

	public Text priceText;
	public GameObject buttonObject;
	public Image priceButtonImage;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		buttonObject.SetActive(true);
	}

	CumulativeEventData.EventRewardInfo _slotInfo;
	public void ShowCanvas(bool show, int day, Sprite iconImageSprite, Vector2 anchoredPosition, Vector2 sizeDelta, string contentsString)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		CumulativeEventData.EventRewardInfo eventRewardInfo = CumulativeEventData.instance.FindRewardInfo(CumulativeEventData.eEventType.PointShop, day);
		if (eventRewardInfo == null)
			return;

		anchoredPosition.y += 10.0f;

		pointShopEventListItem.RefreshInfo(eventRewardInfo);
		pointShopEventListItem.overrideIconImage.sprite = iconImageSprite;
		pointShopEventListItem.overrideIconRectTransform.anchoredPosition = anchoredPosition;
		pointShopEventListItem.overrideIconRectTransform.sizeDelta = sizeDelta;
		pointShopEventListItem.overrideCountText.text = contentsString;
		AlarmObject.Hide(pointShopEventListItem.alarmRootTransform);

		_slotInfo = eventRewardInfo;
		_price = _slotInfo.cn1;

		priceText.text = string.Format("{0:N0} p", _price);
		bool disablePrice = (CumulativeEventData.instance.pointShopPoint < _price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
	}

	int _price;
	DropProcessor _cachedDropProcessor;
	public void OnClickOkButton()
	{
		if (CumulativeEventData.instance.pointShopPoint < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughPointShopPoint"), 2.0f);
			return;
		}

		// 최초에는 골드랜덤박스부터 시작했으나 앞으로 뭐가 될지 몰라서 드랍프로세서를 굴리는 쪽으로 가본다.
		_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _slotInfo.value, "", true, true);
		_cachedDropProcessor.AdjustDropRange(3.7f);
		if (CheatingListener.detectedCheatTable)
			return;

		int dropGold = DropManager.instance.GetLobbyGoldAmount();
		int dropDia = DropManager.instance.GetLobbyDiaAmount();
		PlayFabApiManager.instance.RequestBuyPointShopItem(_slotInfo.day, _slotInfo.type, _price, dropGold, dropDia, () =>
		{
			// 뽑기 후 새로 창이 열릴테니 미리 갱신하지 않는다.
			//CumulativeEventCanvas.instance.currencySmallInfo.RefreshInfo();
			CumulativeEventCanvas.instance.RefreshAlarmObject(CumulativeEventData.eEventType.PointShop);

			// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
			DropManager.instance.ClearLobbyDropInfo();

			UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
			{
				// 연출에 의해 캐시샵 가려질때 같이 하이드 시켜야한다.
				gameObject.SetActive(false);

				RandomBoxScreenCanvas.eBoxType boxType = RandomBoxScreenCanvas.eBoxType.Gold;
				if (dropDia > 0) boxType = RandomBoxScreenCanvas.eBoxType.Dia1_3;
				RandomBoxScreenCanvas.instance.SetInfo(boxType, _cachedDropProcessor, 0, 0, () =>
				{
					// 결과로는 공용 재화 획득창을 띄워준다.
					UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
					{
						CurrencyBoxResultCanvas.instance.RefreshInfo(dropGold, dropDia, 0);
					});
				});
			});
		});
	}







	/*
	// 장비를 사게된다면 이 코드 뜯어서 처리하면 될듯.
	// MailCanvasListItem의 OnRecvEquipBox에서 가져와서 수정해서 쓴다.
	void OnRecvEquipBox(bool serverFailure, string itemGrantString)
	{
		// 실패했는데 굳이 처리해줄 필요가 없다.
		if (serverFailure)
			return;
		if (itemGrantString == "")
			return;

		CumulativeEventCanvas.instance.currencySmallInfo.RefreshInfo();
		CumulativeEventCanvas.instance.RefreshAlarmObject(eventType);

		// 캐릭터와 달리 장비는 드랍프로세서에서 정보를 뽑아쓰는게 아니라서 미리 클리어해도 상관없다.
		DropManager.instance.ClearLobbyDropInfo();

		TimeSpaceData.instance.OnRecvGrantEquip(itemGrantString, _slotInfo.count);

		// 결과창에서 아이콘이 느리게 보이는걸 방지하기 위해 아이콘의 프리로드를 진행한다.
		List<ItemInstance> listGrantItem = TimeSpaceData.instance.DeserializeItemGrantResult(itemGrantString);
		for (int i = 0; i < listGrantItem.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listGrantItem[i].ItemId);
			if (equipTableData == null)
				continue;

			AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", null);
		}

		// 연출 및 보상 처리.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Equip1, _cachedDropProcessor, 0, 0, () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("EquipBoxResultCanvas", () =>
				{
					EquipBoxResultCanvas.instance.RefreshInfo(listGrantItem);
				});
			});
		});
	}
	*/
}