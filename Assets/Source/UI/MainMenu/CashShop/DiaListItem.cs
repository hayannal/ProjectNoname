using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiaListItem : MonoBehaviour
{
	public Text amountText;
	public Text priceText;
	public RectTransform priceTextTransform;
	public Text wonText;
	public GameObject addObject;
	public Text addText;

	public void SetInfo(ShopDiamondTableData shopDiamondTableData)
	{
		amountText.text = shopDiamondTableData.buyingGems.ToString("N0");
		if (OptionManager.instance.language == "KOR")
		{
			priceTextTransform.anchoredPosition = new Vector2(10.0f, 0.0f);
			priceText.text = shopDiamondTableData.kor;
			wonText.gameObject.SetActive(true);
			wonText.SetLocalizedText(BattleInstanceManager.instance.GetCachedGlobalConstantString("KoreaWon"));
		}
		else
		{
			priceTextTransform.anchoredPosition = Vector2.zero;
			priceText.text = shopDiamondTableData.eng;
			wonText.gameObject.SetActive(false);
		}

		bool useAdd = (string.IsNullOrEmpty(shopDiamondTableData.addText) == false);
		addObject.SetActive(useAdd);
		if (useAdd)
			addText.SetLocalizedText(UIString.instance.GetString(shopDiamondTableData.addText));
	}

	public void OnClickButton()
	{

	}
}