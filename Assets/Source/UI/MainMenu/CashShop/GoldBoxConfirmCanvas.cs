using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoldBoxConfirmCanvas : MonoBehaviour
{
	public static GoldBoxConfirmCanvas instance = null;

	public GoldListItem goldListItem;
	public Image goldBoxImage;
	public RectTransform goldBoxImageRectTransform;
	public Text priceText;

	void Awake()
	{
		instance = this;
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
		goldBoxImage.sprite = goldBoxSprite;
		goldBoxImageRectTransform.anchoredPosition = anchoredPosition;
		goldBoxImageRectTransform.sizeDelta = sizeDelta;
		priceText.text = shopGoldTableData.requiredDiamond.ToString("N0");
	}

	public void OnClickOkButton()
	{
		// CharacterBox나 EquipBox와 달리 GoldBox는 사실은 구매고 연출만 뽑기처리 하는거다.
		PlayFabApiManager.instance.RequestBuyGoldBox(_shopGoldTableData.serverItemId, _shopGoldTableData.requiredDiamond, _shopGoldTableData.buyingGold, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
			{
				DropProcessor dropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Zoflrflr", "", true, true);
				dropProcessor.AdjustDropRange(3.7f);
				RandomBoxScreenCanvas.instance.SetInfo(RandomBoxScreenCanvas.eBoxType.Gold, dropProcessor, 0, () =>
				{
					// 결과로는 공용 재화 획득창을 띄워준다.
					UIInstanceManager.instance.ShowCanvasAsync("CommonCurrencyResultCanvas", () =>
					{
						CharacterBoxResultCanvas.instance.RefreshInfo();
					});
				});
			});
		});

		// 패킷 보내고 바로 닫는다.
		gameObject.SetActive(false);
	}
}