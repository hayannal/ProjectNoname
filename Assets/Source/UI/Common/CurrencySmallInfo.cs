using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrencySmallInfo : MonoBehaviour
{
	public Text diamondText;
	public Text goldText;
	public Transform diamondIconTransform;
	public Transform goldIconTransform;
	public Canvas[] backImageCanvasList;

	void Awake()
	{
		_parentCanvas = transform.parent.GetComponentInParent<Canvas>();
	}

	Canvas _parentCanvas;
	void OnEnable()
	{
		if (_parentCanvas != null)
		{
			for (int i = 0; i < backImageCanvasList.Length; ++i)
				backImageCanvasList[i].sortingOrder = _parentCanvas.sortingOrder - 1;
		}

		RefreshInfo();
	}

	public void RefreshInfo()
	{
		diamondText.text = CurrencyData.instance.dia.ToString("N0");
		goldText.text = CurrencyData.instance.gold.ToString("N0");
	}

	public void OnClickDiamondButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.LeftBottom, UIString.instance.GetString("GameUI_DiamondDesc"), 200, diamondIconTransform, new Vector2(-40.0f, 0.0f));
	}

	public void OnClickGoldButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.LeftBottom, UIString.instance.GetString("GameUI_GoldDesc"), 200, goldIconTransform, new Vector2(-40.0f, 7.0f));
	}
}