using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoldListItem : MonoBehaviour
{
	public Text amountText;
	public Text priceText;
	public GameObject extraObject;
	public Text extraText;

	public void SetInfo(ShopGoldTableData shopGoldTableData)
	{
		amountText.text = shopGoldTableData.buyingGold.ToString("N0");
		priceText.text = shopGoldTableData.requiredDiamond.ToString("N0");

		bool useExtra = (string.IsNullOrEmpty(shopGoldTableData.addText) == false);
		extraObject.SetActive(useExtra);
		if (useExtra)
			extraText.text = UIString.instance.GetString(shopGoldTableData.addText, shopGoldTableData.addTextValue);
	}

	public void OnClickButton()
	{

	}
}