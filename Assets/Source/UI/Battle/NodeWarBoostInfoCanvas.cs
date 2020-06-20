using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NodeWarBoostInfoCanvas : MonoBehaviour
{
	public static NodeWarBoostInfoCanvas instance;

	public Transform subTitleTransform;
	public Text messageText;

	public Text priceText;
	public GameObject buttonObject;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		CurrencySmallInfoCanvas.Show(true);
		RefreshInfo();
		buttonObject.SetActive(true);
	}

	void OnDisable()
	{
		CurrencySmallInfoCanvas.Show(false);
	}

	void RefreshInfo()
	{
		bool enablePurchase = false;

		string text1 = UIString.instance.GetString("GameUI_NodeWarBoostDesc1");
		string text3 = UIString.instance.GetString("GameUI_NodeWarBoostDesc3", BattleInstanceManager.instance.GetCachedGlobalConstantInt("RefillBoostCount"));
		if (PlayerData.instance.nodeWarBoostRemainCount > 0)
		{
			string text2 = string.Format("{0} <color=#30BEE3>{1}</color>", UIString.instance.GetString("EquipUI_LeftCountOn"), PlayerData.instance.nodeWarBoostRemainCount);
			messageText.SetLocalizedText(string.Format("{0}\n\n{1}\n{2}", text1, text2, text3));
			enablePurchase = (PlayerData.instance.nodeWarBoostRemainCount <= 3);
		}
		else
		{
			messageText.SetLocalizedText(string.Format("{0}\n\n{1}", text1, text3));
			enablePurchase = true;
		}

		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RefillBoostDiamond");

		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < _price || enablePurchase == false);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;
		_price = price;
	}

	public void OnClickMoreButton()
	{
		int boost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("NodeWarRepeatBoost");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GameUI_NodeWarBoostMore", boost), 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}

	int _price;
	public void OnClickButton()
	{
		if (CurrencyData.instance.dia < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		if (PlayerData.instance.nodeWarBoostRemainCount <= 3)
		{
		}
		else
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarBoostNotAvailable"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestPurchaseNodeWarBoost(_price, OnRecvPurchase);

		buttonObject.SetActive(false);
	}

	void OnRecvPurchase()
	{
		CurrencySmallInfoCanvas.RefreshInfo();

		gameObject.SetActive(false);

		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NodeWarBoostPurchased"), 2.0f);
		NodeWarGround.instance.RefreshNodeWarBoostApplyState();
		NodeWarBoostIndicatorCanvas.instance.RefreshAlarmObject();
	}
}