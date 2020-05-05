using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoldListItem : MonoBehaviour
{
	public Image goldBoxImage;
	public RectTransform goldBoxImageRectTransform;
	public Text amountText;
	public Text priceText;
	public GameObject extraObject;
	public Text extraText;

	ShopGoldTableData _shopGoldTableData;
	public void SetInfo(ShopGoldTableData shopGoldTableData)
	{
		_shopGoldTableData = shopGoldTableData;
		amountText.text = shopGoldTableData.buyingGold.ToString("N0");
		if (priceText != null) priceText.text = shopGoldTableData.requiredDiamond.ToString("N0");

		bool useExtra = (string.IsNullOrEmpty(shopGoldTableData.addText) == false);
		extraObject.SetActive(useExtra);
		if (useExtra)
			extraText.text = UIString.instance.GetString(shopGoldTableData.addText, shopGoldTableData.addTextValue);
	}

	public void OnClickButton()
	{
		if (CurrencyData.instance.dia < _shopGoldTableData.requiredDiamond)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("GoldBoxConfirmCanvas", () =>
		{
			GoldBoxConfirmCanvas.instance.ShowCanvas(true, _shopGoldTableData, goldBoxImage.sprite, goldBoxImageRectTransform.anchoredPosition, goldBoxImageRectTransform.sizeDelta);
		});
	}
}