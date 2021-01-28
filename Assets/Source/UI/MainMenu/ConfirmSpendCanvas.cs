using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmSpendCanvas : MonoBehaviour
{
	public static ConfirmSpendCanvas instance = null;

	public Text titleText;
	public Text messageText;

	public Text priceText;
	public GameObject[] priceTypeObjectList;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect[] priceGrayscaleEffect;

	System.Action _okAction;

	void Awake()
	{
		instance = this;
	}

	bool _showCurrencySmallInfoCanvas;
	void OnDisable()
	{
		if (_showCurrencySmallInfoCanvas)
			CurrencySmallInfoCanvas.Show(false);
	}

	public void ShowCanvas(bool show, string title, string message, CurrencyData.eCurrencyType currencyType, int spendCount, bool showCurrencySmallInfoCanvas, System.Action okAction = null)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		titleText.SetLocalizedText(title);
		messageText.SetLocalizedText(message);

		priceText.text = spendCount.ToString("N0");
		bool disablePrice = false;
		if (currencyType == CurrencyData.eCurrencyType.Gold)
			disablePrice = (CurrencyData.instance.gold < spendCount);
		else
			disablePrice = (CurrencyData.instance.dia < spendCount);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		for (int i = 0; i < priceTypeObjectList.Length; ++i)
		{
			priceTypeObjectList[i].SetActive((int)currencyType == i);
			if ((int)currencyType == i)
				priceGrayscaleEffect[i].enabled = disablePrice;
		}

		_showCurrencySmallInfoCanvas = showCurrencySmallInfoCanvas;
		if (showCurrencySmallInfoCanvas)
			CurrencySmallInfoCanvas.Show(true);

		_okAction = okAction;
	}

	public void OnClickOkButton()
	{
		//gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}