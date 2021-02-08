using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReturnScrollInfoCanvas : MonoBehaviour
{
	public Text countText;

	void OnEnable()
	{
		countText.SetLocalizedText(UIString.instance.GetString("GameUI_ReturnScrollCount", CurrencyData.instance.returnScroll));
	}
}