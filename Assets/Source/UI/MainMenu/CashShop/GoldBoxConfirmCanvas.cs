using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoldBoxConfirmCanvas : MonoBehaviour
{
	public static GoldBoxConfirmCanvas instance = null;

	public GoldListItem goldListItem;
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

	ShopGoldTableData _shopGoldTableData;
	public void ShowCanvas(bool show, ShopGoldTableData shopGoldTableData, Sprite goldBoxSprite, Vector2 anchoredPosition, Vector2 sizeDelta)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		// 하단 다이아 영역이 36이었는데 잘려나가면서 강제로 offset처리를 해줘야한다.
		anchoredPosition.y -= 36.0f * 0.5f;

		_shopGoldTableData = shopGoldTableData;
		goldListItem.SetInfo(shopGoldTableData);
		goldListItem.goldBoxImage.sprite = goldBoxSprite;
		goldListItem.goldBoxImageRectTransform.anchoredPosition = anchoredPosition;
		goldListItem.goldBoxImageRectTransform.sizeDelta = sizeDelta;
		priceText.text = shopGoldTableData.requiredDiamond.ToString("N0");
	}

	public void OnClickOkButton()
	{
		// CharacterBox나 EquipBox와 달리 GoldBox는 사실은 구매고 연출만 뽑기처리 하는거다.
		PlayFabApiManager.instance.RequestBuyGoldBox(_shopGoldTableData.serverItemId, _shopGoldTableData.requiredDiamond, _shopGoldTableData.buyingGold, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
			{
				// 연출에 의해 캐시샵 가려질때 같이 하이드 시켜야한다.
				gameObject.SetActive(false);

				DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "ShopGold", "", true, true);
				dropProcessor.AdjustDropRange(3.7f);
				RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Gold, dropProcessor, 0, () =>
				{
					// 다음번 드랍에 영향을 주지 않게 하기위해 미리 클리어해둔다.
					DropManager.instance.ClearLobbyDropInfo();
					CashShopCanvas.instance.currencySmallInfo.RefreshInfo();

					// 결과로는 공용 재화 획득창을 띄워준다.
					UIInstanceManager.instance.ShowCanvasAsync("CurrencyBoxResultCanvas", () =>
					{
						CurrencyBoxResultCanvas.instance.RefreshInfo(_shopGoldTableData.buyingGold, 0);
					});
				});
			});
		});

		// 패킷 보내고 먼저 버튼부터 하이드
		buttonObject.SetActive(false);
	}
}