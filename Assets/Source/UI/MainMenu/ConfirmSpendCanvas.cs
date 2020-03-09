using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmSpendCanvas : MonoBehaviour
{
	public static ConfirmSpendCanvas instance = null;

	public Text titleText;
	public Text messageText;
	public Text spendCountText;
	public Image spendItemImage;

	System.Action _okAction;

	void Awake()
	{
		instance = this;
	}

	public void ShowCanvas(bool show, string title, string message, CurrencyData.eCurrencyType currencyType, int spendCount, System.Action okAction = null)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		titleText.SetLocalizedText(title);
		messageText.SetLocalizedText(message);
		spendCountText.text = spendCount.ToString();
		_okAction = okAction;

		spendItemImage.color = Color.clear;
		AddressableAssetLoadManager.GetAddressableSprite(CurrencyData.CurrencyType2Address(currencyType), "Icon", (sprite) =>
		{
			spendItemImage.sprite = null;
			spendItemImage.sprite = sprite;
			spendItemImage.color = Color.white;
		});
	}

	public void OnClickOkButton()
	{
		//gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}