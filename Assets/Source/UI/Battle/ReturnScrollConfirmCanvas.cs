using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReturnScrollConfirmCanvas : MonoBehaviour
{
	public static ReturnScrollConfirmCanvas instance;

	public Transform subTitleTransform;
	public Text messageText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		messageText.SetLocalizedText(UIString.instance.GetString("GameUI_SaveDesc", CurrencyData.instance.returnScroll));
	}

	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GameUI_SaveMore"), 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}

	int _price;
	public void OnClickButton()
	{
		if (CurrencyData.instance.returnScroll <= 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotBuyReturnScroll"), 2.0f);
			return;
		}

		if (StageManager.instance.returnScrollUsed)
		{
			// 어떻게 이 창을 연겨지? 그냥 리턴시킨다.
			return;
		}

		// 세이브 포인트 적용
		StageManager.instance.SaveReturnScrollPoint();
		ReturnScrollPointIndicatorCanvas.instance.gameObject.SetActive(false);
		ReturnScrollPoint.instance.SetTextIndicatorShowRemainTime(2.0f);
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_SavePointSet"), 2.0f);
		gameObject.SetActive(false);
	}
}