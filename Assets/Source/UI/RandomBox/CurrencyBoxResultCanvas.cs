using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyBoxResultCanvas : MonoBehaviour
{
	public static CurrencyBoxResultCanvas instance;

	public RectTransform goldGroupRectTransform;
	public Text goldValueText;
	public RectTransform diaGroupRectTransform;
	public Text diaValueText;

	void Awake()
	{
		instance = this;
	}

	public void RefreshInfo(int addGold, int addDia)
	{
		goldGroupRectTransform.gameObject.SetActive(addGold > 0);
		diaGroupRectTransform.gameObject.SetActive(addDia > 0);
		goldValueText.text = addGold.ToString("N0");
		diaValueText.text = addDia.ToString("N0");
	}

	public void OnClickExitButton()
	{
		gameObject.SetActive(false);
		RandomBoxScreenCanvas.instance.gameObject.SetActive(false);
	}
}