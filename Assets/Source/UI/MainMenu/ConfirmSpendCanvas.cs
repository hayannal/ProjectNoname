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

	void OnEnable()
	{
		CurrencySmallInfoCanvas.Show(true);
	}

	void OnDisable()
	{
		CurrencySmallInfoCanvas.Show(false);
	}

	public void ShowCanvas(bool show, string title, string message, CurrencyData.eCurrencyType currencyType, int spendCount, System.Action okAction = null)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		titleText.SetLocalizedText(title);
		messageText.SetLocalizedText(message);
		spendItemImage.sprite = CurrencyData.CurrencyType2Sprite(currencyType);
		spendCountText.text = spendCount.ToString();
		_okAction = okAction;
	}

	public void OnClickOkButton()
	{
		//gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}